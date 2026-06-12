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
  { Id: "term-nutzer", Convergence: "Aligned",
    Payload: { Case: "TermNode", Fields: { Item: {
      Name: "Nutzer", Definition: "Person mit registriertem Konto, die sich authentifizieren kann",
      Synonyms: ["User", "Account-Inhaber"], Relations: [] } } } },
  { Id: "term-session", Convergence: "Aligned",
    Payload: { Case: "TermNode", Fields: { Item: {
      Name: "Session", Definition: "Zeitlich begrenzter, authentifizierter Zugriffskontext eines Nutzers",
      Synonyms: ["Sitzung"], Relations: [{ Case: "RelatesTo", Fields: { Item: "term-nutzer" } }] } } } },
  { Id: "term-credential", Convergence: "Aligned",
    Payload: { Case: "TermNode", Fields: { Item: {
      Name: "Credential", Definition: "Nachweis zur Authentifizierung, z. B. E-Mail + Passwort",
      Synonyms: [], Relations: [{ Case: "PartOf", Fields: { Item: "term-nutzer" } }] } } } },
];

const store = (es) => localStorage.setItem(KEY, JSON.stringify(es));

// Seed-Priorität: localStorage → spot-seed.json (das echte CDD-Selbstmodell,
// von Pages aus .spot/ gebündelt) → eingebauter Fallback.
async function load() {
  const stored = JSON.parse(localStorage.getItem(KEY) ?? "null");
  if (stored) return stored;
  try {
    const res = await fetch("./spot-seed.json");
    if (res.ok) {
      const es = await res.json();
      store(es);
      return es;
    }
  } catch { /* offline oder lokal ohne Bundle */ }
  return structuredClone(seed);
}
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
      case "TermNode": {
        if (!d.Definition?.trim()) f("Warning", e.Id, "Begriff ohne Definition — ubiquitäre Sprache braucht Bedeutung");
        const termIds = new Set(es.filter((x) => x.Payload.Case === "TermNode").map((x) => x.Id));
        for (const r of d.Relations ?? []) {
          const target = r.Fields?.Item;
          if (target === e.Id) f("Error", e.Id, "Begriff bezieht sich auf sich selbst");
          else if (!termIds.has(target)) f("Error", e.Id, `Term-Beziehung zeigt auf '${target}' — kein existierender Begriff`);
        }
        break;
      }
    }
    if (e.Convergence === "Orphaned") f("Warning", e.Id, "Orphaned: Code ohne Modell");
    if (e.Payload.Case === "InvariantNode") {
      const rule = typeof d.Rule === "string" ? d.Rule : d.Rule?.Case;
      const viol = (id, msg) => f("Error", id, `Invariante verletzt (${d.Description}): ${msg}`);
      if (rule === "SpecsNeedTests") {
        const tested = new Set(es.filter((x) => x.Payload.Case === "TestNode").map((x) => item(x).SpecRef));
        for (const x of es) if (x.Payload.Case === "SpecNode" && !tested.has(x.Id)) viol(x.Id, "Spec ohne Test");
      } else if (rule === "CriticalRisksNeedMitigation") {
        for (const x of es) { const r = item(x);
          if (x.Payload.Case === "RiskNode" && r.Impact === "Critical" && !r.Mitigation) viol(x.Id, "kritisches Risiko ohne Mitigation"); }
      } else if (rule === "TermsNeedDefinition") {
        for (const x of es) if (x.Payload.Case === "TermNode" && !item(x).Definition?.trim()) viol(x.Id, "Begriff ohne Definition");
      } else if (rule === "IdPrefix") {
        const { kind, prefix } = d.Rule.Fields ?? {};
        for (const x of es) {
          const k = x.Payload.Case.replace("Node", "").toLowerCase();
          if (k === kind && !x.Id.startsWith(prefix)) viol(x.Id, `Id beginnt nicht mit '${prefix}'`);
        }
      }
    }
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

// Vereinfachter Markdown-Export (Spiegel von Cdd.Core.Export.toMarkdown).
function exportMarkdown(es) {
  const L = [];
  const byCase = (c) => es.filter((e) => e.Payload.Case === c);
  L.push("# SPOT-Kontext", "");
  L.push(`Generiert aus ${es.length} Knoten (Demo-Modus). Der SPOT-Graph ist die Quelle.`, "");
  const conv = ["Aligned", "Pending", "Diverged", "Orphaned"]
    .map((c) => `${c} ${es.filter((e) => e.Convergence === c).length}`).join(" · ");
  L.push(`**Konvergenz:** ${conv}`, "");
  const section = (title, items, fmt) => {
    if (!items.length) return;
    L.push(`## ${title}`, "");
    for (const e of items) fmt(e, item(e));
    L.push("");
  };
  section("Ubiquitäre Sprache (Ontologie)", byCase("TermNode"), (e, d) => {
    const syn = d.Synonyms?.length ? ` *(auch: ${d.Synonyms.join(", ")})*` : "";
    L.push(`- **${d.Name}**${syn} — ${d.Definition}`);
    for (const r of d.Relations ?? []) L.push(`  - ${r.Case} \`${r.Fields?.Item}\``);
  });
  section("Invarianten (Governance)", byCase("InvariantNode"), (e, d) =>
    L.push(`- **${d.Description}** (${typeof d.Rule === "string" ? d.Rule : "IdPrefix " + JSON.stringify(d.Rule.Fields)})`));
  section("Prämissen (nicht verhandelbar)", byCase("PremiseNode"), (e, d) =>
    L.push(`- **${d.Statement}** — ${d.Rationale}`));
  section("Entscheidungen (ADRs)", byCase("DecisionNode"), (e, d) =>
    L.push(`- **${d.Title}** (\`${e.Id}\`): ${d.Choice}`));
  section("Spezifikationen", byCase("SpecNode"), (e, d) => {
    L.push(`### ${d.Title} (\`${e.Id}\`, ${e.Convergence})`, `**Intent:** ${d.Intent}`);
    for (const c of d.Criteria ?? []) L.push(`- GIVEN ${c.Given} WHEN ${c.When} THEN ${c.Then}`);
  });
  section("Risiken", byCase("RiskNode"), (e, d) =>
    L.push(`- **${d.Statement}** (${d.Likelihood}/${d.Impact})${d.Mitigation ? ` — Mitigation: ${d.Mitigation}` : ""}`));
  section("Komponenten", byCase("ComponentNode"), (e, d) =>
    L.push(`- **${d.Name}** (\`${e.Id}\`)${d.DependsOn?.length ? ` → ${d.DependsOn.join(", ")}` : ""}`));
  section("Wissensquellen", byCase("KnowledgeNode"), (e, d) =>
    L.push(`- **${d.Title}** (${d.MediaType}, ${d.Source})`));
  section("Tools (Agent-Capabilities)", byCase("ToolNode"), (e, d) =>
    L.push(`- **${d.Name}** — ${d.Purpose}`));
  const open = es.filter((e) => e.Convergence !== "Aligned");
  section("Offene Arbeit (nicht Aligned)", open, (e) => L.push(`- \`${e.Id}\` (${e.Convergence})`));
  return L.join("\n");
}

export async function demoApi(path, opts) {
  let es = await load();
  if (path === "spot") return es;
  if (path === "validate") return validate(es);
  if (path === "export") return exportMarkdown(es);
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
  b.textContent = "🧪 Demo-Modus — Daten liegen nur in deinem Browser. Gezeigt wird das CDD-Selbstmodell. ";
  const reset = document.createElement("a");
  reset.href = "#";
  reset.textContent = "Demo zurücksetzen";
  reset.style.color = "#4ea1ff";
  reset.onclick = (ev) => { ev.preventDefault(); localStorage.removeItem(KEY); location.reload(); };
  b.appendChild(reset);
  document.body.prepend(b);
}
