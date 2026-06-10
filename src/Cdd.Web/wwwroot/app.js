// CDD Cockpit — dünner Client über der SPOT-API. Kein Framework, kein Build-Schritt.
import mermaid from "https://cdn.jsdelivr.net/npm/mermaid@11/dist/mermaid.esm.min.mjs";
mermaid.initialize({ startOnLoad: false, theme: "dark" });

const $ = (s) => document.querySelector(s);
let entries = [];
let selectedId = null;

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
};
const kindOfCase = (c) => c.replace("Node", "").toLowerCase();
const payloadData = (e) => e.Payload?.Fields?.Item ?? {};

async function api(path, opts) {
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
  renderValidate();
  renderDrift();
}

function renderSidebar() {
  const groups = {};
  for (const e of entries) (groups[kindOfCase(e.Payload.Case)] ??= []).push(e);
  const nav = $("#sidebar");
  nav.innerHTML = "";
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
  } catch {
    $("#graph").textContent = "Graph konnte nicht gerendert werden.";
  }
}

async function renderValidate() {
  const findings = await api("validate");
  const el = $("#tab-validate");
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
for (const t of document.querySelectorAll(".tab")) {
  t.onclick = () => {
    document.querySelectorAll(".tab, .tab-body").forEach((x) => x.classList.remove("active"));
    t.classList.add("active");
    $("#tab-" + t.dataset.tab).classList.add("active");
  };
}

refresh().catch((e) => setMsg("API nicht erreichbar: " + e.message, "error"));
