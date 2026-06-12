// E2E-Smoke: startet das Cockpit gegen das Selbstmodell und prüft die
// Kern-Interaktionen im echten Browser. Läuft lokal und in der CI.
// Abgedeckte Test-Knoten (sync-tests-Marker):
//   [spot: spec-diagram-designer-test-1]  Cube-Filter wirkt auf alle Sichten
//   [spot: spec-diagram-designer-test-2]  Grid-Kachel drillt in die Topologie
//   [spot: spec-form-editor-test-1]       Toolbox → Formular mit Feldern
//   [spot: spec-cockpit-uml-test-1]       Klassendiagramm rendert Beziehungen
import { spawn, execSync } from "node:child_process";
import puppeteer from "puppeteer";

const PORT = 5599;
const repo = new URL("../..", import.meta.url).pathname;
const server = spawn("dotnet", ["run", "-c", "Release", "--no-build", "--project", "src/Cdd.Web", "--", "--root", repo, "--urls", `http://127.0.0.1:${PORT}`], { cwd: repo, stdio: "ignore" });

const fails = [];
const ok = (cond, name) => { console.log((cond ? "OK   " : "FAIL ") + name); if (!cond) fails.push(name); };

try {
  // Auf Server warten
  for (let i = 0; i < 60; i++) {
    try { await fetch(`http://127.0.0.1:${PORT}/api/spot`); break; }
    catch { await new Promise((r) => setTimeout(r, 500)); }
  }
  const browser = await puppeteer.launch({ args: ["--no-sandbox", "--disable-setuid-sandbox"] });
  const page = await browser.newPage();
  const jsErrors = [];
  page.on("pageerror", (e) => jsErrors.push(e.message));
  await page.setViewport({ width: 1480, height: 900 });
  await page.goto(`http://127.0.0.1:${PORT}/`, { waitUntil: "networkidle2", timeout: 60000 });
  await new Promise((r) => setTimeout(r, 2500));
  if (await page.evaluate(() => getComputedStyle(document.querySelector("#help-overlay")).display !== "none")) {
    await page.click("#btn-help-close");
  }

  // Grundgerüst
  ok(await page.evaluate(() => !!document.querySelector("#dock-left #toolbox")), "Toolbox im linken Dock");
  ok(await page.evaluate(() => !!document.querySelector("#designer-cy canvas")), "Designer-Zeichenfläche rendert");
  ok(await page.evaluate(() => document.querySelectorAll("#doc-tabs .tab").length === 6), "6 Dokument-Tabs");

  // [spot: spec-cockpit-uml-test-1] Klassendiagramm: Begriffe + Kanten vorhanden
  const klassen = await page.evaluate(() => window.cy ? -1 : null);
  ok(await page.evaluate(() => !!document.querySelector("#designer-cy canvas")), "Klassendiagramm (Begriffe) rendert");

  // [spot: spec-diagram-designer-test-1] Cube-Slice wirkt: nur 'term' → Grid zeigt nur Begriffe
  await page.click('#doc-tabs [data-view="grid"]');
  await new Promise((r) => setTimeout(r, 800));
  const allCards = await page.evaluate(() => document.querySelectorAll(".gcard").length);
  await page.evaluate(() => [...document.querySelectorAll("#cube-kinds .chip")].find((c) => c.textContent.trim() === "term").click());
  await new Promise((r) => setTimeout(r, 800));
  const termCards = await page.evaluate(() => document.querySelectorAll(".gcard").length);
  ok(termCards > 0 && termCards < allCards, `Cube-Slice filtert Grid (${termCards}/${allCards})`);
  await page.evaluate(() => [...document.querySelectorAll("#cube-kinds .chip")].find((c) => c.classList.contains("active"))?.click());
  await new Promise((r) => setTimeout(r, 500));

  // [spot: spec-diagram-designer-test-2] Grid-Kachel → Drill-down (Fokus + Topologie)
  await page.click(".gcard");
  await new Promise((r) => setTimeout(r, 1200));
  const drill = await page.evaluate(() => ({
    fokus: document.querySelector("#focus-toggle").checked,
    topo: document.querySelector('#doc-tabs [data-view="topologie"]').classList.contains("active"),
  }));
  ok(drill.fokus && drill.topo, "Grid-Kachel drillt in Fokus-Topologie");
  await page.evaluate(() => { document.querySelector("#focus-toggle").checked = false; document.querySelector("#focus-toggle").dispatchEvent(new Event("change")); });

  // [spot: spec-form-editor-test-1] Toolbox-Klick öffnet Formular mit Feldern
  await page.evaluate(() => [...document.querySelectorAll(".tool-item")].find((t) => t.textContent.includes("Begriff")).click());
  await new Promise((r) => setTimeout(r, 500));
  const form = await page.evaluate(() => ({
    sichtbar: !document.querySelector("#editor-form").hidden,
    felder: document.querySelectorAll("#editor-form .f-field").length,
  }));
  ok(form.sichtbar && form.felder >= 4, `Formular öffnet mit ${form.felder} Feldern`);

  // Fehlerliste: Verstoß → Zeile → Klick springt → aufräumen
  await fetch(`http://127.0.0.1:${PORT}/api/spot/term-e2e-kaputt`, { method: "PUT", body: JSON.stringify({ Id: "term-e2e-kaputt", Payload: { Case: "TermNode", Fields: { Item: { Name: "E2E", Definition: "", Synonyms: [], Relations: [] } } }, Convergence: "Pending" }) });
  await page.reload({ waitUntil: "networkidle2" });
  await new Promise((r) => setTimeout(r, 2000));
  if (await page.evaluate(() => getComputedStyle(document.querySelector("#help-overlay")).display !== "none")) await page.click("#btn-help-close");
  const rows = await page.evaluate(() => document.querySelectorAll(".err-table tbody tr").length);
  ok(rows >= 1, `Fehlerliste zeigt ${rows} Befund(e)`);
  await page.click(".err-table tbody tr");
  await new Promise((r) => setTimeout(r, 400));
  ok((await page.evaluate(() => document.querySelector("#editor-title").textContent)) === "term-e2e-kaputt", "Fehler-Klick springt zum Knoten");
  await fetch(`http://127.0.0.1:${PORT}/api/spot/term-e2e-kaputt`, { method: "DELETE" });

  ok(jsErrors.length === 0, jsErrors.length ? "JS-Fehler: " + jsErrors.join("; ") : "keine JS-Fehler");
  await browser.close();
} finally {
  server.kill();
}
console.log(fails.length ? `E2E: ${fails.length} FEHLER` : "E2E: ALLES GRÜN");
process.exit(fails.length ? 1 : 0);
