// E2E-Smoke: das chat-primäre Cong-OS-Cockpit im echten Browser. Prüft, dass das Cockpit
// gegen das Selbstmodell bootet und die Kern-Flächen rendern. Läuft lokal und in der CI.
//   [spot: spec-cockpit-shell-test-1]   Omnibar + Menüleiste + Rail-Flächen + Faden
//   [spot: spec-diagram-surface-test-1] Split-Diagramm rendert (Cytoscape-Canvas) + Toolbox
//   [spot: spec-formal-view-test-1]     Formal-Sicht (code behind) rendert mit KaTeX
import { spawn } from "node:child_process";
import { cp, mkdtemp, rm } from "node:fs/promises";
import { createServer, request as httpRequest } from "node:http";
import { tmpdir } from "node:os";
import { join } from "node:path";
import puppeteer from "puppeteer";

const PORT = 5599;
const PROXY_PORT = 5600;
const repo = new URL("../..", import.meta.url).pathname;
const dataRoot = await mkdtemp(join(tmpdir(), "cdd-e2e-"));
await cp(join(repo, ".spot"), join(dataRoot, ".spot"), { recursive: true });
const server = spawn("dotnet", ["run", "-c", "Release", "--no-build", "--project", "src/Cdd.Web", "--", "--root", dataRoot, "--urls", `http://127.0.0.1:${PORT}`], {
  cwd: repo,
  stdio: "ignore",
  env: { ...process.env, CDD_ALLOW_MUTATIONS: "true" },
});
const proxy = createServer((req, res) => {
  if (!req.url?.startsWith("/cdd/")) {
    res.writeHead(404).end();
    return;
  }
  const upstream = httpRequest({
    hostname: "127.0.0.1",
    port: PORT,
    path: req.url.slice("/cdd".length) || "/",
    method: req.method,
    headers: req.headers,
  }, (response) => {
    res.writeHead(response.statusCode || 502, response.headers);
    response.pipe(res);
  });
  upstream.on("error", () => res.writeHead(502).end());
  req.pipe(upstream);
});
await new Promise((resolve) => proxy.listen(PROXY_PORT, "127.0.0.1", resolve));

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

  // EIDOS Studio: responsive PWA surface + a real ZT2 run and replay.
  await page.goto(`http://127.0.0.1:${PORT}/eidos.html`, { waitUntil: "networkidle2", timeout: 60000 });
  ok(await page.evaluate(() => document.querySelectorAll(".pipeline li").length === 6), "EIDOS-Pipeline vollständig");
  ok(await page.evaluate(() => /keine Credentials/.test(document.body.textContent || "")), "ZT2-Sicherheitsgrenze sichtbar");
  await page.click("#run-clean");
  await page.waitForFunction(() => document.querySelector("#run-badge")?.textContent === "promoted", { timeout: 30000 });
  ok(await page.evaluate(() => /In ZT2 befördert/.test(document.querySelector("#run-status")?.textContent || "")), "OpsLab-Lauf promoviert nach ZT2");
  ok(await page.evaluate(() => !document.querySelector("#open-sandbox")?.hidden), "Sandbox-Artefakt erreichbar");
  ok(await page.evaluate(() => document.querySelectorAll(".pipeline li.done").length === 6), "alle EIDOS-Schritte evidenzbelegt");
  ok(await page.evaluate(() => document.querySelector("#eidos-score")?.textContent === "10/10"), "Benchmark wird aus dem Kernel geladen");
  await page.click("#replay");
  await page.waitForFunction(() => /erneut verifiziert/.test(document.querySelector("#replay-state")?.textContent || ""), { timeout: 15000 });
  ok(true, "EIDOS-Replay erfolgreich");

  // Bug-/Feature-Kanal: lokaler, bewusst exportierter Issue-Entwurf ohne Credential.
  await page.click("#open-feedback");
  ok(await page.evaluate(() => document.querySelector("#feedback-dialog")?.open === true), "Feedback-Dialog öffnet");
  await page.select("#feedback-kind", "feature");
  await page.type("#feedback-summary", "Replay-Vergleich ergänzen");
  await page.type("#feedback-details", "Zwei Runs sollen nachvollziehbar verglichen werden.");
  await page.click("#feedback-form button[type=submit]");
  ok(await page.evaluate(() => {
    const drafts = JSON.parse(localStorage.getItem("eidos-feedback-drafts-v1") || "[]");
    return drafts.length === 1 && /Feature Request/.test(drafts[0].markdown);
  }), "Feature Request wird als aufbereiteter lokaler Issue-Entwurf gespeichert");

  await page.setViewport({ width: 390, height: 844, isMobile: true });
  await page.reload({ waitUntil: "networkidle2" });
  ok(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth + 1), "EIDOS mobil ohne horizontales Überlaufen");

  // Reverse-Proxy-Unterpfad: relative API-/PWA-Pfade müssen unter /cdd/ bleiben.
  await page.setViewport({ width: 1280, height: 800 });
  await page.goto(`http://127.0.0.1:${PROXY_PORT}/cdd/eidos.html`, { waitUntil: "networkidle2", timeout: 60000 });
  await page.click("#run-clean");
  await page.waitForFunction(() => document.querySelector("#run-badge")?.textContent === "promoted", { timeout: 30000 });
  ok(await page.evaluate(() => document.querySelector("#open-sandbox")?.getAttribute("href")?.startsWith("/cdd/api/eidos/")), "EIDOS API und Sandbox bleiben im Reverse-Proxy-Unterpfad");
  ok(await page.evaluate(() => document.querySelector('link[rel="manifest"]')?.getAttribute("href") === "eidos.webmanifest"), "PWA-Manifest bleibt deployment-relativ");

  ok(jsErrors.length === 0, jsErrors.length ? "JS-Fehler: " + jsErrors.join("; ") : "keine JS-Fehler");
  await browser.close();
} finally {
  server.kill();
  proxy.close();
  await rm(dataRoot, { recursive: true, force: true });
}
console.log(fails.length ? `E2E: ${fails.length} FEHLER` : "E2E: ALLES GRÜN");
process.exit(fails.length ? 1 : 0);
