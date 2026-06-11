// CDD Cockpit — dünner Client über der SPOT-API. Kein Framework, kein Build-Schritt.
// Auf GitHub Pages (oder mit ?demo) läuft er ohne Backend gegen localStorage.
import mermaid from "https://cdn.jsdelivr.net/npm/mermaid@11/dist/mermaid.esm.min.mjs";
import { DEMO, demoApi, demoBanner } from "./demo.js";

// Theme: Light (Visual-Studio-Look) ist Default, Dark optional, persistiert.
const themeKey = "cdd-theme";
function applyTheme(theme) {
  document.documentElement.dataset.theme = theme;
  localStorage.setItem(themeKey, theme);
  mermaid.initialize({ startOnLoad: false, theme: theme === "dark" ? "dark" : "default" });
}
applyTheme(localStorage.getItem(themeKey) ?? "light");
if (DEMO) demoBanner();

const $ = (s) => document.querySelector(s);
let entries = [];
let selectedId = null;
let filterText = "";

// Payload-Vorlagen für neue Knoten (Server-Form: { Case, Fields: { Item } })
const templates = {
  spec:      { Case: "SpecNode",      Fields: { Item: { Title: "", Intent: "", Criteria: [{ Given: "", When: "", Then: "" }] } } },
  component: { Case: "ComponentNode", Fields: { Item: { Name: "", DependsOn: [] } } },
  risk:      { Case: "RiskNode",      Fields: { Item: { Statement: "", Likelihood: "Medium", Impact: "Medium", Mitigation: null } } },
  infra:     { Case: "InfraNode",     Fields: { Item: { Resource: "", Provider: "", Config: {} } } },
  premise:   { Case: "PremiseNode",   Fields: { Item: { Statement: "", Rationale: "" } } },
  decision:  { Case: "DecisionNode",  Fields: { Item: { Title: "", Context: "", Choice: "", Consequences: "", Supersedes: null } } },
  knowledge: { Case: "KnowledgeNode", Fields: { Item: { Title: "", Source: "", MediaType: "link", Takeaways: [] } } },
  tool:      { Case: "ToolNode",      Fields: { Item: { Name: "", Purpose: "", Endpoint: null } } },
  term:      { Case: "TermNode",      Fields: { Item: { Name: "", Definition: "", Synonyms: [],
                Relations: [{ Case: "RelatesTo", Fields: { Item: "term-anderer-begriff" } }] } } },
};
const kindOfCase = (c) => c.replace("Node", "").toLowerCase();
const payloadData = (e) => e.Payload?.Fields?.Item ?? {};

async function api(path, opts) {
  if (DEMO) return demoApi(path, opts);
  const res = await fetch("/api/" + path, opts);
  if (res.status === 204) return null;
  const text = await res.text();
  const body = text ? JSON.parse(text) : null;
  if (!res.ok) throw new Error(body?.error ?? res.statusText);
  return body;
}

async function refresh() {
  entries = await api("spot");
  renderSidebar();
  renderGraph();
  renderUml();
  renderValidate();
  renderDrift();
}

function renderSidebar() {
  const q = filterText.toLowerCase();
  const visible = entries.filter((e) =>
    !q || e.Id.toLowerCase().includes(q) || kindOfCase(e.Payload.Case).includes(q) ||
    JSON.stringify(payloadData(e)).toLowerCase().includes(q));
  const groups = {};
  for (const e of visible) (groups[kindOfCase(e.Payload.Case)] ??= []).push(e);
  const nav = $("#node-list");
  nav.innerHTML = Object.keys(groups).length ? "" : "<p style='padding:8px;color:var(--muted)'>Kein Treffer.</p>";
  for (const kind of Object.keys(groups).sort()) {
    const h = document.createElement("h3");
    h.textContent = `${kind} (${groups[kind].length})`;
    nav.appendChild(h);
    for (const e of groups[kind]) {
      const b = document.createElement("button");
      b.className = "node-item" + (e.Id === selectedId ? " selected" : "");
      b.innerHTML = `<span class="dot ${e.Convergence}"></span>`;
      b.appendChild(document.createTextNode(e.Id));
      b.onclick = () => select(e.Id);
      nav.appendChild(b);
    }
  }
}

function select(id) {
  selectedId = id;
  const e = entries.find((x) => x.Id === id);
  $("#editor-title").textContent = id ?? "Kein Knoten gewählt";
  $("#editor-json").value = e ? JSON.stringify(e, null, 2) : "";
  $("#btn-save").disabled = !e;
  $("#btn-delete").disabled = !e;
  setMsg("");
  renderSidebar();
}

function setMsg(text, cls = "") {
  const m = $("#editor-msg");
  m.textContent = text;
  m.className = cls;
}

async function save() {
  let entry;
  try { entry = JSON.parse($("#editor-json").value); }
  catch { return setMsg("JSON ist nicht parsebar", "error"); }
  try {
    await api("spot/" + encodeURIComponent(entry.Id), { method: "PUT", body: JSON.stringify(entry) });
    setMsg("Gespeichert ✔", "ok");
    selectedId = entry.Id;
    await refresh();
  } catch (err) { setMsg(err.message, "error"); }
}

async function del() {
  if (!selectedId || !confirm(`Knoten '${selectedId}' löschen?`)) return;
  await api("spot/" + encodeURIComponent(selectedId), { method: "DELETE" });
  selectedId = null;
  select(null);
  await refresh();
}

function newNode() {
  const kind = $("#new-kind").value;
  const draft = {
    Id: `${kind}-neu`,
    Payload: structuredClone(templates[kind]),
    Convergence: "Pending",
  };
  selectedId = null;
  $("#editor-title").textContent = "Neuer Knoten (Id anpassen!)";
  $("#editor-json").value = JSON.stringify(draft, null, 2);
  $("#btn-save").disabled = false;
  $("#btn-delete").disabled = true;
  setMsg("Id vergeben, Felder füllen, Speichern.");
}

async function deriveTests() {
  const r = await api("derive-tests?write=true", { method: "POST" });
  setMsg(`${r.derived.length} Test-Knoten abgeleitet.`, "ok");
  await refresh();
}

// — Panels —

// Klick auf einen Diagramm-Knoten selektiert ihn im Editor.
function wireDiagramClicks(container) {
  const byUnderscore = new Map(entries.map((e) => [e.Id.replaceAll("-", "_"), e.Id]));
  for (const node of container.querySelectorAll("g.node, g.nodes > g")) {
    const hit = [...byUnderscore.keys()].find((k) => node.id?.includes(k));
    if (!hit) continue;
    node.style.cursor = "pointer";
    node.addEventListener("click", () => select(byUnderscore.get(hit)));
  }
}

async function renderGraph() {
  const lines = ["graph LR"];
  const safe = (id) => id.replaceAll("-", "_");
  for (const e of entries) {
    const kind = kindOfCase(e.Payload.Case);
    const d = payloadData(e);
    lines.push(`${safe(e.Id)}["${kind}: ${e.Id}"]:::${e.Convergence}`);
    if (kind === "component") for (const dep of d.DependsOn ?? []) lines.push(`${safe(e.Id)} --> ${safe(dep)}`);
    if (kind === "test" && d.SpecRef) lines.push(`${safe(e.Id)} -. testet .-> ${safe(d.SpecRef)}`);
    if (kind === "decision" && d.Supersedes) lines.push(`${safe(e.Id)} -. ersetzt .-> ${safe(d.Supersedes)}`);
  }
  lines.push("classDef Aligned stroke:#3fb950,stroke-width:2px");
  lines.push("classDef Pending stroke:#d29922,stroke-width:2px");
  lines.push("classDef Diverged stroke:#f85149,stroke-width:2px");
  lines.push("classDef Orphaned stroke:#a371f7,stroke-width:2px");
  try {
    const { svg } = await mermaid.render("spotGraph", lines.join("\n"));
    $("#graph").innerHTML = svg;
    wireDiagramClicks($("#graph"));
  } catch {
    $("#graph").textContent = "Graph konnte nicht gerendert werden.";
  }
}

// UML-Klassendiagramm aus der Ontologie (Term-Knoten der ubiquitären Sprache).
async function renderUml() {
  const terms = entries.filter((e) => e.Payload.Case === "TermNode");
  const el = $("#uml");
  if (!terms.length) {
    el.innerHTML = "<p>Keine Begriffe im SPOT. Über „+ Begriff (Ontologie)“ anlegen — daraus entsteht hier das UML-Klassendiagramm der ubiquitären Sprache.</p>";
    return;
  }
  const safe = (id) => id.replaceAll("-", "_");
  const lines = ["classDiagram"];
  for (const t of terms) {
    const d = payloadData(t);
    lines.push(`class ${safe(t.Id)}["${d.Name || t.Id}"]`);
    for (const syn of d.Synonyms ?? []) lines.push(`${safe(t.Id)} : ${syn}`);
    for (const r of d.Relations ?? []) {
      const target = r.Fields?.Item;
      if (!target) continue;
      const a = safe(t.Id), b = safe(target);
      if (r.Case === "IsA") lines.push(`${b} <|-- ${a} : ist ein`);
      else if (r.Case === "PartOf") lines.push(`${b} *-- ${a} : Teil von`);
      else lines.push(`${a} ..> ${b}`);
    }
  }
  try {
    const { svg } = await mermaid.render("umlDiagram", lines.join("\n"));
    el.innerHTML = svg;
    wireDiagramClicks(el);
  } catch {
    el.textContent = "UML konnte nicht gerendert werden.";
  }
}

async function renderValidate() {
  const findings = await api("validate");
  const el = $("#tab-validate");
  document.querySelector('[data-tab="validate"]').textContent =
    findings.length ? `Validierung (${findings.length})` : "Validierung";
  el.innerHTML = findings.length ? "" : "<p>✔ Keine Befunde.</p>";
  for (const f of findings) {
    const d = document.createElement("div");
    d.className = `finding ${f.Severity}`;
    d.innerHTML = `<b></b> ${f.Severity === "Error" ? "❌" : "⚠️"} `;
    d.querySelector("b").textContent = f.EntityId;
    d.appendChild(document.createTextNode(f.Message));
    el.appendChild(d);
  }
}

async function renderDrift() {
  const r = await api("diff");
  const el = $("#tab-drift");
  const drifting = r.Pending.length + r.Diverged.length + r.Orphaned.length;
  document.querySelector('[data-tab="drift"]').textContent =
    drifting ? `Drift (${drifting})` : "Drift";
  el.innerHTML = "";
  for (const key of ["Aligned", "Pending", "Diverged", "Orphaned"]) {
    const g = document.createElement("div");
    g.className = "drift-group";
    g.innerHTML = `<h4><span class="dot ${key}"></span> ${key} <span class="badge">${r[key].length}</span></h4>`;
    for (const e of r[key]) {
      const b = document.createElement("button");
      b.className = "node-item";
      b.textContent = e.Id;
      b.onclick = () => select(e.Id);
      g.appendChild(b);
    }
    el.appendChild(g);
  }
}

// — Wiring —
$("#btn-refresh").onclick = refresh;
$("#btn-save").onclick = save;
$("#btn-delete").onclick = del;
$("#btn-new").onclick = newNode;
$("#btn-derive").onclick = deriveTests;
$("#btn-theme").onclick = () => {
  applyTheme(document.documentElement.dataset.theme === "dark" ? "light" : "dark");
  renderGraph();
  renderUml();
};
for (const t of document.querySelectorAll(".tab")) {
  t.onclick = () => {
    document.querySelectorAll(".tab, .tab-body").forEach((x) => x.classList.remove("active"));
    t.classList.add("active");
    $("#tab-" + t.dataset.tab).classList.add("active");
  };
}
$("#filter").oninput = (ev) => {
  filterText = ev.target.value;
  renderSidebar();
};
document.addEventListener("keydown", (ev) => {
  if ((ev.ctrlKey || ev.metaKey) && ev.key === "s") {
    ev.preventDefault();
    if (!$("#btn-save").disabled) save();
  }
});

refresh().catch((e) => setMsg("API nicht erreichbar: " + e.message, "error"));
