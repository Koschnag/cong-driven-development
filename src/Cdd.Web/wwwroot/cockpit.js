// CDD Cockpit — neu gedacht: Phasen-Leiste oben, Knotenart-Baum links,
// Hauptfenster geteilt (Nachbarschafts-Diagramm | Detail). Klick → im Hauptfenster.
import cytoscape from "https://cdn.jsdelivr.net/npm/cytoscape@3/+esm";
import { DEMO, demoApi, demoBanner } from "./demo.js";
import { buildPrompt, callClaude, parseChanges, getApiKey, setApiKey } from "./agent.js";
import { buildForm } from "./form.js";

const $ = (s) => document.querySelector(s);
const item = (e) => e.Payload.Fields.Item;
const kindOf = (e) => e.Payload.Case.replace("Node", "");
const KIND_LABEL = {
  Spec: "Specs", Term: "Ontologie", Risk: "Risiken", Decision: "Entscheidungen",
  Test: "Tests", Component: "Komponenten", Invariant: "Invarianten",
  Premise: "Prämissen", Knowledge: "Wissen", Tool: "Werkzeuge", Infra: "Infrastruktur",
};
const PHASES = [
  { key: "Pending", label: "Offen", desc: "Modell ohne gemessene Code-Entsprechung" },
  { key: "Diverged", label: "Abweichend", desc: "Modell und Code widersprechen sich" },
  { key: "Orphaned", label: "Verwaist", desc: "Code ohne Modellknoten" },
  { key: "Aligned", label: "Konvergiert", desc: "Modell und Code stimmen überein" },
];

let entries = [];
let selectedId = null;
let phaseFilter = null;
let editing = false;
const history = [];
let histIdx = -1;

async function api(path, opts) {
  if (DEMO) return demoApi(path, opts);
  const res = await fetch("/api/" + path, opts);
  if (res.status === 204) return null;
  const text = await res.text();
  const body = text ? JSON.parse(text) : null;
  if (!res.ok) throw new Error(body?.error ?? res.statusText);
  return body;
}

const byId = (id) => entries.find((e) => e.Id === id);

// Ausgehende Referenzen eines Knotens (für Diagramm + klickbare Beziehungen)
function outRefs(e) {
  const refs = [];
  const it = item(e);
  switch (kindOf(e)) {
    case "Test": if (it.SpecRef) refs.push({ id: it.SpecRef, label: "testet" }); break;
    case "Component": (it.DependsOn ?? []).forEach((d) => refs.push({ id: d, label: "hängt ab von" })); break;
    case "Decision": if (it.Supersedes) refs.push({ id: it.Supersedes, label: "ersetzt" }); break;
    case "Term": (it.Relations ?? []).forEach((r) => refs.push({ id: r.Fields.Item, label: r.Case })); break;
  }
  return refs.filter((r) => byId(r.id));
}
const inRefs = (id) => entries.filter((e) => outRefs(e).some((r) => r.id === id));

// ---------- Phasen-Leiste ----------
function renderPhasebar() {
  const bar = $("#phasebar");
  bar.innerHTML = "";
  for (let i = 0; i < PHASES.length; i++) {
    const p = PHASES[i];
    const n = entries.filter((e) => e.Convergence === p.key).length;
    const el = document.createElement("button");
    el.className = "phase" + (phaseFilter === p.key ? " active" : "") + (n ? "" : " empty");
    el.title = p.desc;
    el.innerHTML = `<span class="dot ${p.key}"></span><span class="phase-label">${p.label}</span><span class="phase-n">${n}</span>`;
    el.onclick = () => { phaseFilter = phaseFilter === p.key ? null : p.key; renderPhasebar(); renderNav(); };
    bar.appendChild(el);
    if (i < PHASES.length - 1) { const a = document.createElement("span"); a.className = "phase-arrow"; a.textContent = "→"; bar.appendChild(a); }
  }
}

// ---------- Navigation (Baum nach Art) ----------
function renderNav() {
  const q = $("#nav-search").value.trim().toLowerCase();
  const tree = $("#nav-tree");
  tree.innerHTML = "";
  const shown = entries.filter((e) =>
    (!phaseFilter || e.Convergence === phaseFilter) &&
    (!q || e.Id.toLowerCase().includes(q) || JSON.stringify(item(e)).toLowerCase().includes(q)));
  const groups = {};
  for (const e of shown) (groups[kindOf(e)] ??= []).push(e);
  for (const kind of Object.keys(groups).sort()) {
    const g = groups[kind];
    const grp = document.createElement("div");
    grp.className = "nav-group";
    grp.innerHTML = `<div class="nav-group-head">${KIND_LABEL[kind] ?? kind} <span class="nav-count">${g.length}</span></div>`;
    for (const e of g.sort((a, b) => a.Id.localeCompare(b.Id))) {
      const li = document.createElement("div");
      li.className = "nav-item" + (e.Id === selectedId ? " sel" : "");
      li.innerHTML = `<span class="dot ${e.Convergence}"></span><span class="nav-id">${e.Id}</span>`;
      li.onclick = () => select(e.Id);
      grp.appendChild(li);
    }
    tree.appendChild(grp);
  }
  if (!shown.length) tree.innerHTML = `<p class="hint">Keine Knoten${phaseFilter ? " in dieser Phase" : ""}.</p>`;
}

// ---------- Auswahl + History ----------
function select(id, pushHist = true) {
  if (!byId(id)) return;
  selectedId = id;
  editing = false;
  if (pushHist) { history.splice(histIdx + 1); history.push(id); histIdx = history.length - 1; }
  location.hash = id;
  renderNav();
  renderMain();
}

// ---------- Hauptfenster (geteilt) ----------
function renderMain() {
  const e = byId(selectedId);
  $("#main-title").textContent = e ? `${kindOf(e)} · ${e.Id}` : "Kein Knoten gewählt";
  $("#back").disabled = histIdx <= 0;
  $("#fwd").disabled = histIdx >= history.length - 1;
  renderActions(e);
  if (!e) { $("#main-detail").innerHTML = `<p class="hint">Wähle links einen Knoten.</p>`; $("#main-diagram").innerHTML = ""; return; }
  renderDiagram(e);
  if (editing) renderEdit(e); else renderDetail(e);
}

function renderActions(e) {
  const a = $("#main-actions");
  a.innerHTML = "";
  if (!e) return;
  const edit = document.createElement("button");
  edit.textContent = editing ? "✕ Abbrechen" : "✏️ Bearbeiten";
  edit.onclick = () => { editing = !editing; renderMain(); };
  a.appendChild(edit);
  const del = document.createElement("button");
  del.textContent = "🗑"; del.className = "danger"; del.title = "Löschen";
  del.onclick = () => delNode(e.Id);
  a.appendChild(del);
}

function renderDiagram(e) {
  const cont = $("#main-diagram");
  cont.innerHTML = "";
  const neighbors = [...outRefs(e).map((r) => ({ id: r.id, label: r.label, dir: "out" })),
                     ...inRefs(e.Id).map((s) => ({ id: s.Id, label: outRefs(s).find((r) => r.id === e.Id)?.label ?? "", dir: "in" }))];
  const seen = new Set([e.Id]);
  const els = [{ data: { id: e.Id, label: e.Id, conv: e.Convergence, center: 1 } }];
  for (const n of neighbors) {
    if (!seen.has(n.id)) { seen.add(n.id); const nb = byId(n.id); els.push({ data: { id: n.id, label: n.id, conv: nb?.Convergence ?? "Pending" } }); }
    els.push({ data: { id: n.dir === "out" ? `${e.Id}->${n.id}` : `${n.id}->${e.Id}`, source: n.dir === "out" ? e.Id : n.id, target: n.dir === "out" ? n.id : e.Id, label: n.label } });
  }
  const COL = { Aligned: "#2e7d32", Pending: "#c98a00", Diverged: "#c62828", Orphaned: "#6a1b9a" };
  const cy = cytoscape({
    container: cont, elements: els,
    style: [
      { selector: "node", style: { label: "data(label)", "font-size": 9, "text-valign": "center", "text-halign": "center",
        "background-color": (n) => COL[n.data("conv")] ?? "#888", color: "#fff", "text-wrap": "wrap", "text-max-width": 90,
        width: 96, height: 38, shape: "round-rectangle", "text-outline-width": 0 } },
      { selector: "node[center]", style: { "border-width": 3, "border-color": "#007acc", "font-weight": "bold" } },
      { selector: "edge", style: { label: "data(label)", "font-size": 7, "curve-style": "bezier",
        "target-arrow-shape": "triangle", "line-color": "#9aa", "target-arrow-color": "#9aa", color: "#778", width: 1.5 } },
    ],
    layout: { name: "concentric", concentric: (n) => (n.data("center") ? 10 : 1), minNodeSpacing: 30 },
  });
  cy.on("tap", "node", (ev) => select(ev.target.id()));
}

function field(label, value) {
  if (value == null || value === "" || (Array.isArray(value) && !value.length)) return "";
  return `<div class="f"><div class="fl">${label}</div><div class="fv">${value}</div></div>`;
}
const esc = (s) => String(s).replace(/[&<>]/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;" }[c]));

function renderDetail(e) {
  const it = item(e);
  const d = $("#main-detail");
  let html = `<div class="conv-badge ${e.Convergence}">${e.Convergence}</div>`;
  const k = kindOf(e);
  if (k === "Spec") {
    html += field("Titel", esc(it.Title)) + field("Absicht", esc(it.Intent));
    if (it.Criteria?.length) html += `<div class="fl">Akzeptanzkriterien</div>` +
      it.Criteria.map((c) => `<div class="crit"><b>Gegeben</b> ${esc(c.Given)} <b>Wenn</b> ${esc(c.When)} <b>Dann</b> ${esc(c.Then)}</div>`).join("");
  } else if (k === "Term") {
    html += field("Name", esc(it.Name)) + field("Definition", esc(it.Definition)) + field("Synonyme", (it.Synonyms ?? []).map(esc).join(", "));
  } else if (k === "Risk") {
    html += field("Aussage", esc(it.Statement)) + field("Wahrscheinlichkeit", it.Likelihood) + field("Impact", it.Impact) + field("Mitigation", esc(it.Mitigation ?? "—"));
  } else if (k === "Decision") {
    html += field("Titel", esc(it.Title)) + field("Kontext", esc(it.Context)) + field("Entscheidung", esc(it.Choice)) + field("Konsequenzen", esc(it.Consequences));
  } else if (k === "Premise") {
    html += field("Aussage", esc(it.Statement)) + field("Begründung", esc(it.Rationale));
  } else if (k === "Component") {
    html += field("Name", esc(it.Name));
  } else if (k === "Test") {
    html += field("Name", esc(it.Name)) + field("Abgeleitet", it.Derived ? "ja" : "nein");
  } else if (k === "Invariant") {
    html += field("Beschreibung", esc(it.Description)) + field("Regel", esc(JSON.stringify(it.Rule)));
  } else if (k === "Knowledge") {
    html += field("Titel", esc(it.Title)) + field("Quelle", esc(it.Source)) + field("Typ", esc(it.MediaType)) + field("Erkenntnisse", (it.Takeaways ?? []).map(esc).join("; "));
  } else {
    html += field("JSON", `<pre>${esc(JSON.stringify(it, null, 2))}</pre>`);
  }
  // Beziehungen (klickbar)
  const outs = outRefs(e), ins = inRefs(e.Id);
  if (outs.length) html += `<div class="fl">Verweist auf</div>` + outs.map((r) => `<a class="rel" data-id="${r.id}">${r.label}: ${r.id}</a>`).join("");
  if (ins.length) html += `<div class="fl">Verwiesen von</div>` + ins.map((s) => `<a class="rel" data-id="${s.Id}">${s.Id}</a>`).join("");
  d.innerHTML = html;
  d.querySelectorAll(".rel").forEach((a) => (a.onclick = () => select(a.dataset.id)));
}

function renderEdit(e) {
  const d = $("#main-detail");
  d.innerHTML = "";
  const { el, getValue } = buildForm(e, entries, { idEditable: false });
  d.appendChild(el);
  const save = document.createElement("button");
  save.textContent = "💾 Speichern"; save.className = "primary";
  save.onclick = async () => {
    try {
      const v = getValue();
      await api("spot/" + encodeURIComponent(v.Id), { method: "PUT", headers: { "content-type": "application/json" }, body: JSON.stringify(v) });
      editing = false; await load(); select(v.Id, false);
    } catch (err) { alert("Fehler: " + err.message); }
  };
  d.appendChild(save);
}

async function delNode(id) {
  if (!confirm(`Knoten '${id}' löschen?`)) return;
  await api("spot/" + encodeURIComponent(id), { method: "DELETE" });
  selectedId = null; await load();
}

async function addNode() {
  const kind = $("#nav-add-kind").value;
  const id = prompt("Id des neuen Knotens (a-z, 0-9, -, _):");
  if (!id) return;
  selectedId = id; editing = true;
  // temporären Leer-Knoten ins Formular geben
  const leer = { Id: id, Convergence: "Pending", Payload: { Case: kind, Fields: { Item: {} } } };
  entries.push(leer); renderMain();
}

// ---------- Unten: Fehlerliste / Dashboard / Agent ----------
async function renderErrors() {
  const pane = $("#dock-errors");
  let findings = [];
  try { findings = await api("validate"); } catch { findings = []; }
  const errs = findings.filter((f) => f.Severity === "Error").length;
  const warns = findings.filter((f) => f.Severity === "Warning").length;
  $("#sb-counts").textContent = `${entries.length} Knoten · ${entries.filter((e) => e.Convergence === "Aligned").length} ✓ · ${errs} Fehler`;
  if (!findings.length) { pane.innerHTML = `<p class="ok">✓ Keine Fehler, Warnungen oder Widersprüche — das Modell ist konsistent.</p>`; return; }
  pane.innerHTML = `<div class="err-sum">${errs} Fehler · ${warns} Warnungen</div>` +
    `<table class="err-table">` + findings.map((f) =>
      `<tr data-id="${f.EntityId}"><td>${f.Severity === "Error" ? "⛔" : "⚠️"}</td><td>${esc(f.Message)}</td><td class="err-id">${f.EntityId}</td></tr>`).join("") + `</table>`;
  pane.querySelectorAll("tr[data-id]").forEach((tr) => (tr.onclick = () => select(tr.dataset.id)));
}

function renderDashboard() {
  const pane = $("#dock-dashboard");
  const counts = {};
  for (const e of entries) (counts[kindOf(e)] ??= { t: 0, a: 0 }), counts[kindOf(e)].t++, (e.Convergence === "Aligned" && counts[kindOf(e)].a++);
  const aligned = entries.filter((e) => e.Convergence === "Aligned").length;
  pane.innerHTML = `<div class="dash-big">${aligned}/${entries.length} Knoten konvergiert (${entries.length ? Math.round(100 * aligned / entries.length) : 0} %)</div>` +
    `<table class="err-table">` + Object.keys(counts).sort().map((k) =>
      `<tr><td>${KIND_LABEL[k] ?? k}</td><td>${counts[k].a}/${counts[k].t} ✓</td></tr>`).join("") + `</table>`;
}

// ---------- Agent ----------
let pendingChanges = null;
function wireAgent() {
  $("#agent-key").value = getApiKey() ?? "";
  $("#agent-key").onchange = () => setApiKey($("#agent-key").value.trim());
  $("#btn-agent-prompt").onclick = async () => {
    const md = await api("export?format=markdown").catch(() => null);
    const ctx = typeof md === "string" ? md : JSON.stringify(entries);
    await navigator.clipboard.writeText(buildPrompt(ctx, $("#agent-prose").value));
    $("#agent-status").textContent = "Prompt kopiert — in eine KI einfügen, Antwort unten anwenden.";
  };
  $("#btn-agent-run").onclick = async () => {
    const prose = $("#agent-prose").value.trim();
    if (!prose) return;
    $("#agent-status").textContent = "Claude denkt nach …";
    try {
      const md = await api("export?format=markdown").catch(() => JSON.stringify(entries));
      const text = await callClaude(typeof md === "string" ? md : JSON.stringify(entries), prose, $("#agent-model").value);
      showChanges(parseChanges(text));
    } catch (err) { $("#agent-status").textContent = "Fehler: " + err.message; }
  };
  $("#btn-agent-apply").onclick = async () => {
    if (!pendingChanges) return;
    for (const u of pendingChanges.upsert) await api("spot/" + encodeURIComponent(u.Id), { method: "PUT", headers: { "content-type": "application/json" }, body: JSON.stringify(u) });
    for (const id of pendingChanges.delete) await api("spot/" + encodeURIComponent(id), { method: "DELETE" });
    pendingChanges = null; $("#agent-result").hidden = true; $("#agent-status").textContent = "Angewendet."; await load();
  };
  $("#btn-agent-discard").onclick = () => { pendingChanges = null; $("#agent-result").hidden = true; };
}
function showChanges(ch) {
  pendingChanges = ch;
  $("#agent-summary").textContent = ch.summary ?? "";
  $("#agent-changes").innerHTML = ch.upsert.map((u) => `<div>+ ${u.Id}</div>`).join("") + ch.delete.map((d) => `<div>− ${d}</div>`).join("");
  $("#agent-result").hidden = false;
  $("#agent-status").textContent = "Vorschlag — prüfen und anwenden.";
}

// ---------- Laden + Verdrahtung ----------
async function load() {
  entries = await api("spot");
  renderPhasebar();
  renderNav();
  renderMain();
  await renderErrors();
  renderDashboard();
}

function wire() {
  $("#nav-search").oninput = renderNav;
  $("#nav-add-btn").onclick = addNode;
  $("#back").onclick = () => { if (histIdx > 0) { histIdx--; select(history[histIdx], false); } };
  $("#fwd").onclick = () => { if (histIdx < history.length - 1) { histIdx++; select(history[histIdx], false); } };
  $("#mi-refresh").onclick = load;
  $("#mi-derive").onclick = async () => { await api("derive-tests", { method: "POST" }).catch(() => {}); await load(); };
  $("#btn-help").onclick = () => ($("#help-overlay").hidden = false);
  $("#btn-help-close").onclick = () => ($("#help-overlay").hidden = true);
  $("#btn-theme").onclick = toggleTheme;
  $("#dock-toggle").onclick = () => document.body.classList.toggle("dock-collapsed");
  document.querySelectorAll(".dtab").forEach((t) => (t.onclick = () => {
    document.querySelectorAll(".dtab").forEach((x) => x.classList.remove("active"));
    t.classList.add("active");
    document.querySelectorAll(".dock-pane").forEach((p) => (p.hidden = true));
    $("#dock-" + t.dataset.dock).hidden = false;
  }));
  wireAgent();
}

function toggleTheme() {
  const isDark = document.documentElement.getAttribute("data-theme") === "dark";
  const next = isDark ? "light" : "dark";
  document.documentElement.setAttribute("data-theme", next);
  localStorage.setItem("cdd-theme", next);
}
function applyTheme() {
  document.documentElement.setAttribute("data-theme", localStorage.getItem("cdd-theme") ?? "dark");
}

(async function main() {
  applyTheme();
  wire();
  if (DEMO) document.body.appendChild(demoBanner());
  await load();
  const hash = location.hash.slice(1);
  if (hash && byId(hash)) select(hash);
  if (!localStorage.getItem("cdd-visited")) { $("#help-overlay").hidden = false; localStorage.setItem("cdd-visited", "1"); }
})();
