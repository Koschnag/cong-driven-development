// Browser contract for the public, read-only Research Studio.
// [spot: spec-research-studio-test-1] SPOT data becomes visible projections.
// [spot: spec-research-studio-test-2] Feedback opens only a reviewable issue draft.
import { readFile } from "node:fs/promises";
import { createServer } from "node:http";
import { extname, resolve, sep } from "node:path";
import puppeteer from "puppeteer";

const PORT = 5601;
const repo = new URL("../..", import.meta.url).pathname;
const docsRoot = resolve(repo, "docs");
const mime = { ".html": "text/html; charset=utf-8", ".css": "text/css; charset=utf-8", ".js": "text/javascript; charset=utf-8", ".json": "application/json", ".png": "image/png", ".mp4": "video/mp4" };

const server = createServer(async (req, res) => {
  try {
    const requested = decodeURIComponent((req.url || "/").split("?")[0]);
    const path = resolve(docsRoot, `.${requested.endsWith("/") ? `${requested}index.html` : requested}`);
    if (path !== docsRoot && !path.startsWith(docsRoot + sep)) throw new Error("outside docs root");
    const body = await readFile(path);
    res.writeHead(200, { "content-type": mime[extname(path)] || "application/octet-stream" }).end(body);
  } catch {
    res.writeHead(404).end();
  }
});
await new Promise(resolveReady => server.listen(PORT, "127.0.0.1", resolveReady));

const fails = [];
const ok = (condition, name) => { console.log(`${condition ? "OK  " : "FAIL"} ${name}`); if (!condition) fails.push(name); };
let browser;
try {
  browser = await puppeteer.launch({ args: ["--no-sandbox", "--disable-setuid-sandbox"] });
  const page = await browser.newPage();
  const jsErrors = [];
  page.on("pageerror", error => jsErrors.push(error.message));
  await page.setViewport({ width: 1440, height: 1000 });
  await page.goto(`http://127.0.0.1:${PORT}/research/`, { waitUntil: "networkidle2", timeout: 60000 });
  await page.waitForFunction(() => document.querySelectorAll(".claim").length > 0);

  // [spot: spec-research-studio-test-1]
  ok(await page.evaluate(() => Number(document.querySelector("#metric-nodes")?.textContent) > 100), "SPOT-Kennzahlen gerendert");
  ok(await page.evaluate(() => document.querySelectorAll(".claim").length >= 3), "Claims projiziert");
  ok(await page.evaluate(() => document.querySelectorAll(".source").length >= 5), "Quellen projiziert");
  ok(await page.evaluate(() => document.querySelectorAll(".risk").length >= 4), "Risiken projiziert");
  ok(await page.evaluate(() => document.querySelectorAll(".project").length === 8), "Teilprojekte vollständig");

  // [spot: spec-riftward-t053-public-registry-test-2] Public source binding only; no runtime claim.
  const registry = await page.evaluate(() => document.querySelector("[data-riftward-registry]")?.textContent || "");
  ok(registry.includes("Raw-Export publikationsgesperrt") && registry.includes("unknown"), "T-053 Registry zeigt das fail-closed Export-Gate");
  ok(await page.evaluate(() => document.body.textContent.includes("d7d5f949") && document.body.textContent.includes("riftward-research-observability") && document.body.textContent.includes("2.0.1")), "T-053 Registry zeigt die öffentliche Quellenbindung");

  // [spot: spec-research-studio-test-2]
  await page.evaluate(() => { window.open = url => { window.__researchIssueDraft = url; }; });
  await page.click('[data-feedback-ref="Gesamtprogramm"]');
  await page.type("#feedback-summary", "Claim gegen Baseline prüfen");
  await page.type("#feedback-details", "Bitte ein unabhängiges Vergleichsdesign ergänzen.");
  await page.click("#feedback-submit");
  const issueDraft = await page.evaluate(() => window.__researchIssueDraft || "");
  ok(issueDraft.includes("github.com/Koschnag/cong-driven-development/issues/new"), "Feedback bleibt GitHub-Issue-Entwurf");
  ok(issueDraft.includes("Research%20Feedback"), "Issue-Entwurf ist strukturiert");
  ok(jsErrors.length === 0, jsErrors.length ? `JS-Fehler: ${jsErrors.join("; ")}` : "keine JS-Fehler");

  await page.goto(`http://127.0.0.1:${PORT}/research/briefing.html`, { waitUntil: "networkidle2", timeout: 60000 });
  ok(await page.evaluate(() => document.querySelectorAll(".slide").length === 6), "Research Briefing hat sechs Folien");
  await page.keyboard.press("ArrowRight");
  ok(await page.evaluate(() => document.querySelector(".slide.active")?.dataset.slide === "02"), "Briefing ist per Tastatur steuerbar");
  ok(await page.evaluate(() => Number(document.querySelector("[data-nodes]")?.textContent) > 100), "Briefing-Kennzahlen kommen aus dem SPOT");

  await page.setViewport({ width: 390, height: 844, isMobile: true });
  await page.reload({ waitUntil: "networkidle2" });
  ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1), "mobil ohne horizontales Überlaufen");
} finally {
  if (browser) await browser.close();
  server.close();
}
console.log(fails.length ? `Research Studio E2E: ${fails.length} FEHLER` : "Research Studio E2E: ALLES GRÜN");
process.exit(fails.length ? 1 : 0);
