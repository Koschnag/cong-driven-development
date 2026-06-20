// E2E-Smoke: das chat-primäre Cong-OS-Cockpit im echten Browser. Prüft, dass das Cockpit
// gegen das Selbstmodell bootet und die Kern-Flächen rendern. Läuft lokal und in der CI.
//   [spot: spec-cockpit-shell-test-1]   Omnibar + Menüleiste + Rail-Flächen + Faden
//   [spot: spec-diagram-surface-test-1] Split-Diagramm rendert (Cytoscape-Canvas) + Toolbox
//   [spot: spec-formal-view-test-1]     Formal-Sicht (code behind) rendert mit KaTeX
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
  await sleep(2000);

  // [spot: spec-cockpit-shell-test-1] Drei feste Regionen + die eine Tür
  ok(await page.evaluate(() => !!document.querySelector("#omni-in")), "Omnibar-Eingabe da");
  ok(await page.evaluate(() => document.querySelectorAll("#menubar .vs-menu").length > 0), "Menüleiste da");
  ok(await page.evaluate(() => document.querySelectorAll("#rail .surf").length >= 6), "Rail-Flächen da");
  ok(await page.evaluate(() => !!document.querySelector("#thread")), "Faden (Chat) da");
  ok(await page.evaluate(() => /Knoten/.test(document.querySelector("#status")?.textContent || "")), "Statuszeile zeigt Knotenzahl");

  // [spot: spec-diagram-surface-test-1] Split-Diagramm rendert + Toolbox
  ok(await page.evaluate(() => !!document.querySelector("#maindia .dia-bar")), "Diagramm-Leiste da");
  ok(await page.evaluate(() => document.querySelectorAll("#maindia .dia-v").length >= 5), "Diagramm-Sichten da");
  ok(await page.evaluate(() => !!document.querySelector("#dia-cy canvas")), "Diagramm rendert (Cytoscape-Canvas)");
  ok(await page.evaluate(() => document.querySelectorAll("#dia-palette .pkind").length >= 10), "Toolbox mit Knotenarten");

  // [spot: spec-formal-view-test-1] Formal-Sicht (code behind) mit KaTeX
  await page.evaluate(() => { const b = [...document.querySelectorAll(".dia-v")].find((x) => x.dataset.v === "formal-logik"); if (b) b.click(); });
  await sleep(900);
  ok(await page.evaluate(() => !!document.querySelector(".formal-wrap .katex")), "Formal-Logik-Sicht rendert (KaTeX)");
  await page.evaluate(() => { const b = [...document.querySelectorAll(".dia-v")].find((x) => x.dataset.v === "architecture"); if (b) b.click(); });
  await sleep(600);

  // Bühne: eine Fläche rufen → öffnet sich (data-open=true)
  await page.evaluate(() => { const b = document.querySelector("#rail .surf"); if (b) b.click(); });
  await sleep(600);
  ok(await page.evaluate(() => document.querySelector("#stage")?.dataset.open === "true"), "Bühne öffnet sich");

  // Fehlerliste: ein Verstoß wird als Befund sichtbar, dann aufräumen
  await fetch(`http://127.0.0.1:${PORT}/api/spot/term-e2e-kaputt`, { method: "PUT", body: JSON.stringify({ Id: "term-e2e-kaputt", Payload: { Case: "TermNode", Fields: { Item: { Name: "E2E", Definition: "", Synonyms: [], Relations: [] } } }, Convergence: "Pending" }) });
  await page.reload({ waitUntil: "networkidle2" });
  await sleep(1500);
  await fetch(`http://127.0.0.1:${PORT}/api/spot/term-e2e-kaputt`, { method: "DELETE" });

  ok(jsErrors.length === 0, jsErrors.length ? "JS-Fehler: " + jsErrors.join("; ") : "keine JS-Fehler");
  await browser.close();
} finally {
  server.kill();
}
console.log(fails.length ? `E2E: ${fails.length} FEHLER` : "E2E: ALLES GRÜN");
process.exit(fails.length ? 1 : 0);
