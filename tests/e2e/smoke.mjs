// E2E-Smoke: startet das neu gestaltete Cockpit gegen das Selbstmodell und prüft die
// Kern-Interaktionen im echten Browser. Läuft lokal und in der CI.
// Abgedeckte Test-Knoten (sync-tests-Marker):
//   [spot: spec-cockpit-uml-test-1]       Hauptfenster zeigt Nachbarschaft als Diagramm
//   [spot: spec-diagram-designer-test-1]  Phasen-Leiste filtert die Navigation
//   [spot: spec-diagram-designer-test-2]  Klickbare Beziehung navigiert im Hauptfenster
//   [spot: spec-form-editor-test-1]       Bearbeiten öffnet Formular mit Feldern
import { spawn } from "node:child_process";
import puppeteer from "puppeteer";

const PORT = 5599;
const repo = new URL("../..", import.meta.url).pathname;
const server = spawn("dotnet", ["run", "-c", "Release", "--no-build", "--project", "src/Cdd.Web", "--", "--root", repo, "--urls", `http://127.0.0.1:${PORT}`], { cwd: repo, stdio: "ignore" });

const fails = [];
const ok = (cond, name) => { console.log((cond ? "OK   " : "FAIL ") + name); if (!cond) fails.push(name); };
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

try {
  for (let i = 0; i < 60; i++) {
    try { await fetch(`http://127.0.0.1:${PORT}/api/spot`); break; }
    catch { await sleep(500); }
  }
  const browser = await puppeteer.launch({ args: ["--no-sandbox", "--disable-setuid-sandbox"] });
  const page = await browser.newPage();
  const jsErrors = [];
  page.on("pageerror", (e) => jsErrors.push(e.message));
  await page.setViewport({ width: 1480, height: 900 });
  await page.goto(`http://127.0.0.1:${PORT}/`, { waitUntil: "networkidle2", timeout: 60000 });
  await sleep(1500);
  await page.evaluate(() => document.querySelector("#help-overlay")?.setAttribute("hidden", ""));

  // Grundgerüst: Phasen-Leiste + Navigationsbaum
  ok(await page.evaluate(() => document.querySelectorAll("#phasebar .phase").length === 4), "Phasen-Leiste mit 4 Phasen");
  ok(await page.evaluate(() => document.querySelectorAll(".nav-item").length > 0), "Navigationsbaum gefüllt");

  // Klick auf einen Test-Knoten (hat garantiert eine Beziehung zur Spec)
  const sel = await page.evaluate(() => {
    const it = [...document.querySelectorAll(".nav-item")].find((x) => x.textContent.includes("-test-"));
    if (it) { it.click(); return it.querySelector(".nav-id").textContent; }
    return null;
  });
  await sleep(1200);

  // [spot: spec-cockpit-uml-test-1] Hauptfenster: Nachbarschafts-Diagramm rendert
  ok(await page.evaluate(() => !!document.querySelector("#main-diagram canvas")), "Nachbarschafts-Diagramm rendert (Canvas)");
  ok(await page.evaluate((s) => document.querySelector("#main-title").textContent.includes(s), sel), "Hauptfenster-Titel zeigt gewählten Knoten");

  // [spot: spec-diagram-designer-test-2] Klickbare Beziehung navigiert im Hauptfenster
  const relTarget = await page.evaluate(() => {
    const rel = document.querySelector("#main-detail a.rel");
    if (rel) { const id = rel.dataset.id; rel.click(); return id; }
    return null;
  });
  await sleep(900);
  ok(relTarget && await page.evaluate((t) => document.querySelector("#main-title").textContent.includes(t), relTarget),
     "Klick auf Beziehung navigiert zum Zielknoten");

  // [spot: spec-diagram-designer-test-1] Phasen-Leiste filtert: 'Offen' → nur Pending im Baum
  await page.evaluate(() => [...document.querySelectorAll("#phasebar .phase")].find((p) => p.textContent.includes("Offen")).click());
  await sleep(700);
  const nurPending = await page.evaluate(() => {
    const dots = [...document.querySelectorAll(".nav-item .dot")];
    return dots.length > 0 && dots.every((d) => d.classList.contains("Pending"));
  });
  ok(nurPending, "Phasen-Filter 'Offen' zeigt nur Pending-Knoten");
  await page.evaluate(() => [...document.querySelectorAll("#phasebar .phase")].find((p) => p.classList.contains("active"))?.click());
  await sleep(400);

  // [spot: spec-form-editor-test-1] Bearbeiten öffnet Formular mit Feldern
  await page.evaluate(() => {
    [...document.querySelectorAll(".nav-item")].find((x) => x.textContent.includes("term-")).click();
  });
  await sleep(700);
  await page.evaluate(() => [...document.querySelectorAll("#main-actions button")].find((b) => b.textContent.includes("Bearbeiten")).click());
  await sleep(500);
  ok(await page.evaluate(() => document.querySelectorAll("#main-detail .f-field").length > 0), "Bearbeiten zeigt Formular mit Feldern");

  // Fehlerliste: Verstoß → Zeile → Klick springt → aufräumen
  await fetch(`http://127.0.0.1:${PORT}/api/spot/term-e2e-kaputt`, { method: "PUT", body: JSON.stringify({ Id: "term-e2e-kaputt", Payload: { Case: "TermNode", Fields: { Item: { Name: "E2E", Definition: "", Synonyms: [], Relations: [] } } }, Convergence: "Pending" }) });
  await page.reload({ waitUntil: "networkidle2" });
  await sleep(1500);
  await page.evaluate(() => document.querySelector("#help-overlay")?.setAttribute("hidden", ""));
  const rows = await page.evaluate(() => document.querySelectorAll(".err-table tr[data-id]").length);
  ok(rows >= 1, `Fehlerliste zeigt ${rows} Befund(e)`);
  await page.evaluate(() => [...document.querySelectorAll(".err-table tr[data-id]")].find((r) => r.dataset.id === "term-e2e-kaputt").click());
  await sleep(500);
  ok((await page.evaluate(() => document.querySelector("#main-title").textContent)).includes("term-e2e-kaputt"), "Fehler-Klick springt zum Knoten im Hauptfenster");
  await fetch(`http://127.0.0.1:${PORT}/api/spot/term-e2e-kaputt`, { method: "DELETE" });

  ok(jsErrors.length === 0, jsErrors.length ? "JS-Fehler: " + jsErrors.join("; ") : "keine JS-Fehler");
  await browser.close();
} finally {
  server.kill();
}
console.log(fails.length ? `E2E: ${fails.length} FEHLER` : "E2E: ALLES GRÜN");
process.exit(fails.length ? 1 : 0);
