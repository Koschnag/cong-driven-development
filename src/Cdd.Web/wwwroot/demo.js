// Demo-Modus für GitHub Pages: kein Backend — SPOT lebt im localStorage.
// Vereinfachte Spiegelung der Cdd.Core-Logik (validate ohne Zyklenerkennung).

export const DEMO =
  location.hostname.endsWith("github.io") ||
  new URLSearchParams(location.search).has("demo");

const KEY = "cdd-demo-spot";

const seed = [
  { Id: "spec-login", Convergence: "Pending",
    Payload: { Case: "SpecNode", Fields: { Item: {
      Title: "Login", Intent: "Nutzer authentifiziert sich mit E-Mail und Passwort",
      Criteria: [
        { Given: "ein registrierter Nutzer", When: "korrekte Credentials eingegeben werden", Then: "wird eine Session erstellt" },
        { Given: "ein registrierter Nutzer", When: "ein falsches Passwort eingegeben wird", Then: "wird die Anmeldung abgelehnt" },
      ] } } } },
  { Id: "risk-bruteforce", Convergence: "Pending",
    Payload: { Case: "RiskNode", Fields: { Item: {
      Statement: "Brute-Force gegen den Login-Endpunkt",
      Likelihood: "Medium", Impact: "High", Mitigation: "Rate-Limiting + Account-Lockout" } } } },
  { Id: "comp-auth", Convergence: "Pending",
    Payload: { Case: "ComponentNode", Fields: { Item: { Name: "AuthService", DependsOn: ["spec-login"] } } } },
];

const load = () => JSON.parse(localStorage.getItem(KEY) ?? "null") ?? structuredClone(seed);
const store = (es) => localStorage.setItem(KEY, JSON.stringify(es));
const validId = (id) => /^[a-zA-Z0-9][a-zA-Z0-9_-]*$/.test(id);
const item = (e) => e.Payload?.Fields?.Item ?? {};

function validate(es) {
  const ids = new Set(es.map((e) => e.Id));
  const specIds = new Set(es.filter((e) => e.Payload.Case === "SpecNode").map((e) => e.Id));
  const out = [];
  const f = (sev, id, msg) => out.push({ Severity: sev, EntityId: id, Message: msg });
  for (const e of es) {
    const d = item(e);
    switch (e.Payload.Case) {
      case "SpecNode":
        if (!d.Criteria?.length) f("Error", e.Id, "Spec hat keine Akzeptanzkriterien");
        break;
      case "TestNode":
        if (!specIds.has(d.SpecRef)) f("Error", e.Id, `Test referenziert unbekannte Spec '${d.SpecRef}'`);
        break;
      case "ComponentNode":
        for (const dep of d.DependsOn ?? []) {
          if (dep === e.Id) f("Error", e.Id, "Component hängt von sich selbst ab");
          else if (!ids.has(dep)) f("Error", e.Id, `Abhängigkeit '${dep}' existiert nicht`);
        }
        break;
      case "RiskNode":
        if (d.Impact === "Critical" && !d.Mitigation) f("Warning", e.Id, "Kritisches Risiko ohne Mitigation");
        break;
      case "DecisionNode":
        if (d.Supersedes && !ids.has(d.Supersedes)) f("Error", e.Id, `Supersedes verweist auf unbekannten Knoten '${d.Supersedes}'`);
        break;
      case "KnowledgeNode":
        if (!d.Source?.trim()) f("Warning", e.Id, "Knowledge-Quelle ohne Source (URL/Pfad/ISBN)");
        break;
    }
    if (e.Convergence === "Orphaned") f("Warning", e.Id, "Orphaned: Code ohne Modell");
    if (e.Convergence === "Diverged") f("Warning", e.Id, "Diverged: Implementierung weicht vom Modell ab");
  }
  return out;
}

function deriveTests(es) {
  const known = new Set(es.map((e) => e.Id));
  const derived = [];
  for (const e of es.filter((x) => x.Payload.Case === "SpecNode")) {
    const s = item(e);
    (s.Criteria ?? []).forEach((c, i) => {
      const id = `${e.Id}-test-${i + 1}`;
      if (known.has(id)) return;
      derived.push({ Id: id, Convergence: "Pending",
        Payload: { Case: "TestNode", Fields: { Item: {
          SpecRef: e.Id, Name: `${s.Title} — when ${c.When} then ${c.Then}`, Derived: true } } } });
    });
  }
  return derived;
}

export async function demoApi(path, opts) {
  let es = load();
  if (path === "spot") return es;
  if (path === "validate") return validate(es);
  if (path === "diff") {
    const by = (c) => es.filter((e) => e.Convergence === c);
    return { Aligned: by("Aligned"), Pending: by("Pending"), Diverged: by("Diverged"), Orphaned: by("Orphaned") };
  }
  if (path.startsWith("derive-tests")) {
    const derived = deriveTests(es);
    if (path.includes("write=true")) store([...es, ...derived]);
    return { derived, written: true };
  }
  if (path.startsWith("spot/")) {
    const id = decodeURIComponent(path.slice(5));
    if (opts?.method === "DELETE") { store(es.filter((e) => e.Id !== id)); return null; }
    if (opts?.method === "PUT") {
      const entry = JSON.parse(opts.body);
      if (entry.Id !== id) throw new Error("Id in URL und Body stimmen nicht überein");
      if (!validId(entry.Id)) throw new Error("Ungültige Id (erlaubt: a-zA-Z0-9_-)");
      store([...es.filter((e) => e.Id !== entry.Id), entry]);
      return entry;
    }
  }
  throw new Error("Demo-Modus: unbekannter Aufruf " + path);
}

export function demoBanner() {
  const b = document.createElement("div");
  b.style.cssText = "background:#2d333b;color:#d29922;padding:4px 16px;font-size:12px;text-align:center";
  b.textContent = "🧪 Demo-Modus — Daten liegen nur in deinem Browser (localStorage). Volle Version: GitHub Releases.";
  document.body.prepend(b);
}
