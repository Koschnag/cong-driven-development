// CDD Cockpit — dünner Client über der SPOT-API. Kein Framework, kein Build-Schritt.
// Auf GitHub Pages (oder mit ?demo) läuft er ohne Backend gegen localStorage.
import mermaid from "https://cdn.jsdelivr.net/npm/mermaid@11/dist/mermaid.esm.min.mjs";
import cytoscape from "https://cdn.jsdelivr.net/npm/cytoscape@3/+esm";
import { DEMO, demoApi, demoBanner } from "./demo.js";
import { buildPrompt, callClaude, parseChanges, getApiKey, setApiKey } from "./agent.js";
import { buildForm } from "./form.js";

// Theme: Light (Visual-Studio-Look) ist Default, Dark optional, persistiert.
const themeKey = "cdd-theme";
function applyTheme(theme) {
  document.documentElement.dataset.theme = theme;
  localStorage.setItem(themeKey, theme);
  mermaid.initialize(
    theme === "dark"
      ? { startOnLoad: false, theme: "dark" }
      : { startOnLoad: false, theme: "base", themeVariables: {
          // Class-Designer-Look: hellblaue Boxen, dezente Linien
          primaryColor: "#e3eefb", primaryBorderColor: "#7a96b8",
          primaryTextColor: "#1e1e1e", lineColor: "#5b7da8",
          tertiaryColor: "#f6f9fd", fontFamily: "Segoe UI, sans-serif" } });
}
applyTheme(localStorage.getItem(themeKey) ?? "light");
if (DEMO) demoBanner();

const $ = (s) => document.querySelector(s);
let entries = [];
let selectedId = null;
let filterText = "";
let lastFindings = [];

// — Cube-Zustand (Slice/Dice/Drill-down) —
const cube = { kinds: new Set(), conv: new Set(), focus: false, depth: 2 };
// — Navigations-Historie (Modell-Debugger: ← / →) —
const hist = { back: [], fwd: [] };
let editorMode = "inspect"; // "inspect" | "json"

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
  invariant: { Case: "InvariantNode", Fields: { Item: { Description: "", Rule: "SpecsNeedTests" } } },
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
  renderCubeBar();
  renderSidebar();
  renderDashboard();
  renderDesigner();
  renderValidate();
  renderDrift();
  renderDoku();
  renderStatusbar();
}

// — Statusleiste (VS2015): Auswahl, Zähler, Validierungsstand —
function renderStatusbar() {
  const conv = { Aligned: 0, Pending: 0, Diverged: 0, Orphaned: 0 };
  for (const e of entries) conv[e.Convergence] = (conv[e.Convergence] ?? 0) + 1;
  $("#sb-counts").textContent =
    `${entries.length} Knoten · ${conv.Aligned} ✓ · ${conv.Pending} offen` +
    (conv.Diverged ? ` · ${conv.Diverged} abweichend` : "");
  const errors = lastFindings.filter((f) => f.Severity === "Error").length;
  const warns = lastFindings.length - errors;
  const v = $("#sb-validate");
  v.textContent = errors ? `❌ ${errors} Fehler` : warns ? `⚠ ${warns} Warnungen` : "✓ Modell konsistent";
  v.className = errors ? "error" : "";
  $("#sb-sel").textContent = selectedId ? `▸ ${selectedId}` : "";
}

// Re-Render aller Sichten nach Slice/Dice/Fokus-Änderung (ohne Daten neu zu laden).
function rerenderViews() {
  renderCubeBar();
  renderSidebar();
  renderDesigner();
}

// — Cube-Bar: Slice (Knotenarten), Dice (Konvergenz), Drill-down (Fokus) —
function renderCubeBar() {
  const kindsPresent = [...new Set(entries.map((e) => kindOfCase(e.Payload.Case)))].sort();
  const mkChip = (row, label, active, onToggle, dotClass) => {
    const b = document.createElement("button");
    b.className = "chip" + (active ? " active" : "");
    if (dotClass) b.innerHTML = `<span class="dot ${dotClass}"></span>`;
    b.appendChild(document.createTextNode(label));
    b.onclick = onToggle;
    row.appendChild(b);
  };
  const kindRow = $("#cube-kinds");
  kindRow.innerHTML = "";
  for (const k of kindsPresent) {
    mkChip(kindRow, k, cube.kinds.has(k), () => {
      cube.kinds.has(k) ? cube.kinds.delete(k) : cube.kinds.add(k);
      rerenderViews();
    });
  }
  const convRow = $("#cube-conv");
  convRow.innerHTML = "";
  for (const c of ["Aligned", "Pending", "Diverged", "Orphaned"]) {
    mkChip(convRow, "", cube.conv.has(c), () => {
      cube.conv.has(c) ? cube.conv.delete(c) : cube.conv.add(c);
      rerenderViews();
    }, c);
  }
}

// — Referenz-Index: ausgehende und eingehende Verlinkungen pro Knoten —
function outRefs(e) {
  const d = payloadData(e);
  switch (e.Payload.Case) {
    case "ComponentNode": return (d.DependsOn ?? []).map((t) => ({ target: t, label: "hängt ab von" }));
    case "TestNode": return d.SpecRef ? [{ target: d.SpecRef, label: "testet" }] : [];
    case "DecisionNode": return d.Supersedes ? [{ target: d.Supersedes, label: "ersetzt" }] : [];
    case "TermNode": return (d.Relations ?? []).map((r) => ({
      target: r.Fields?.Item,
      label: r.Case === "IsA" ? "ist ein" : r.Case === "PartOf" ? "Teil von" : "bezieht sich auf",
    })).filter((r) => r.target);
    default: return [];
  }
}

function refIndex() {
  const incoming = new Map();
  for (const e of entries) {
    for (const r of outRefs(e)) {
      if (!incoming.has(r.target)) incoming.set(r.target, []);
      incoming.get(r.target).push({ source: e.Id, label: r.label });
    }
  }
  return incoming;
}

// Drill-down: BFS-Nachbarschaft (ungerichtet) um den gewählten Knoten.
function focusNeighborhood() {
  if (!cube.focus || !selectedId) return null;
  const adj = new Map();
  const link = (a, b) => {
    if (!adj.has(a)) adj.set(a, new Set());
    adj.get(a).add(b);
  };
  for (const e of entries) for (const r of outRefs(e)) { link(e.Id, r.target); link(r.target, e.Id); }
  const seen = new Set([selectedId]);
  let frontier = [selectedId];
  for (let hop = 0; hop < cube.depth; hop++) {
    const next = [];
    for (const id of frontier) for (const n of adj.get(id) ?? []) {
      if (!seen.has(n)) { seen.add(n); next.push(n); }
    }
    frontier = next;
  }
  return seen;
}

// Slice/Dice + Fokus: die sichtbare Teilmenge für Graph, UML und Explorer.
function visibleEntries() {
  const focus = focusNeighborhood();
  return entries.filter((e) =>
    (!cube.kinds.size || cube.kinds.has(kindOfCase(e.Payload.Case))) &&
    (!cube.conv.size || cube.conv.has(e.Convergence)) &&
    (!focus || focus.has(e.Id)));
}

function renderSidebar() {
  const q = filterText.toLowerCase();
  const visible = visibleEntries().filter((e) =>
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

let formInstance = null;
let draftEntry = null; // neuer, noch nicht gespeicherter Knoten

function setEditorMode(mode) {
  editorMode = mode;
  $("#inspector").hidden = mode !== "inspect";
  $("#editor-form").hidden = mode !== "form";
  $("#editor-json").hidden = mode !== "json";
  $("#btn-mode-inspect").classList.toggle("active", mode === "inspect");
  $("#btn-mode-form").classList.toggle("active", mode === "form");
  $("#btn-mode-json").classList.toggle("active", mode === "json");
  if (mode === "form") {
    const base = draftEntry ?? entries.find((x) => x.Id === selectedId);
    const box = $("#editor-form");
    box.innerHTML = "";
    if (base) {
      formInstance = buildForm(base, entries, { idEditable: !!draftEntry });
      box.appendChild(formInstance.el);
    } else {
      formInstance = null;
      box.innerHTML = "<p class='insp-hint'>Knoten wählen oder aus der Toolbox anlegen.</p>";
    }
  }
  const editable = mode === "form" ? !!formInstance : mode === "json" && !!$("#editor-json").value;
  $("#btn-save").disabled = !editable;
}

function updateHistButtons() {
  $("#btn-back").disabled = !hist.back.length;
  $("#btn-fwd").disabled = !hist.fwd.length;
}

function select(id, opts = {}) {
  if (!opts.fromHistory && selectedId && id && id !== selectedId) {
    hist.back.push(selectedId);
    if (hist.back.length > 50) hist.back.shift();
    hist.fwd.length = 0;
  }
  selectedId = id;
  draftEntry = null;
  const e = entries.find((x) => x.Id === id);
  $("#editor-title").textContent = id ?? "Kein Knoten gewählt";
  $("#editor-json").value = e ? JSON.stringify(e, null, 2) : "";
  $("#btn-delete").disabled = !e;
  if (e) history.replaceState(null, "", "#" + encodeURIComponent(id));
  setMsg("");
  updateHistButtons();
  setEditorMode(editorMode); // Formular/JSON auf neue Auswahl umbauen
  renderStatusbar();
  renderInspector();
  renderSidebar();
  if (cube.focus) renderDesigner();
}

// — Inspektor: der Modell-Debugger (Details, Verlinkungen, Befunde) —
function renderInspector() {
  const el = $("#inspector");
  el.innerHTML = "";
  const e = entries.find((x) => x.Id === selectedId);
  if (!e) {
    el.innerHTML = "<p class='insp-hint'>Knoten wählen — oder im Graph/UML anklicken. ← / → navigieren wie im Debugger durch die Historie.</p>";
    return;
  }
  const d = payloadData(e);
  const head = document.createElement("div");
  head.className = "insp-head";
  head.innerHTML = `<span class="badge">${kindOfCase(e.Payload.Case)}</span> <span class="dot ${e.Convergence}"></span> ${e.Convergence}`;
  const editBtn = document.createElement("button");
  editBtn.textContent = "✏️ Bearbeiten";
  editBtn.onclick = () => setEditorMode("form");
  head.appendChild(editBtn);
  el.appendChild(head);

  const fields = { Name: d.Name, Titel: d.Title, Beschreibung: d.Description, Definition: d.Definition, Intent: d.Intent,
    Aussage: d.Statement, Begründung: d.Rationale, Entscheidung: d.Choice, Quelle: d.Source,
    Zweck: d.Purpose };
  for (const [label, val] of Object.entries(fields)) {
    if (!val) continue;
    const p = document.createElement("p");
    p.className = "insp-field";
    p.innerHTML = `<b>${label}:</b> `;
    p.appendChild(document.createTextNode(val));
    el.appendChild(p);
  }
  if (d.Criteria?.length) {
    for (const c of d.Criteria) {
      const p = document.createElement("p");
      p.className = "insp-field insp-crit";
      p.textContent = `GIVEN ${c.Given} WHEN ${c.When} THEN ${c.Then}`;
      el.appendChild(p);
    }
  }

  const chipList = (title, items, render) => {
    const h = document.createElement("h4");
    h.textContent = `${title} (${items.length})`;
    el.appendChild(h);
    const row = document.createElement("div");
    row.className = "chip-row";
    if (!items.length) row.innerHTML = "<span class='insp-hint'>—</span>";
    for (const it of items) row.appendChild(render(it));
    el.appendChild(row);
  };
  const chip = (id, label) => {
    const b = document.createElement("button");
    b.className = "chip link";
    b.title = label;
    const target = entries.find((x) => x.Id === id);
    b.innerHTML = target ? `<span class="dot ${target.Convergence}"></span>` : "❓ ";
    b.appendChild(document.createTextNode(`${label} → ${id}`));
    b.onclick = () => (target ? select(id) : null);
    return b;
  };
  chipList("Ausgehende Verlinkungen", outRefs(e), (r) => chip(r.target, r.label));
  chipList("Eingehende Verlinkungen", refIndex().get(e.Id) ?? [], (r) => chip(r.source, `wird ${r.label}`));

  const findings = lastFindings.filter((f) => f.EntityId === e.Id);
  if (findings.length) {
    const h = document.createElement("h4");
    h.textContent = `Befunde (${findings.length})`;
    el.appendChild(h);
    for (const f of findings) {
      const div = document.createElement("div");
      div.className = `finding ${f.Severity}`;
      div.textContent = f.Message;
      el.appendChild(div);
    }
  }
}

function setMsg(text, cls = "") {
  const m = $("#editor-msg");
  m.textContent = text;
  m.className = cls;
}

async function save() {
  let entry;
  if (editorMode === "form") {
    if (!formInstance) return;
    entry = formInstance.getValue();
    if (!/^[a-zA-Z0-9][a-zA-Z0-9_-]*$/.test(entry.Id))
      return setMsg("Bitte eine gültige Id vergeben (a-z, 0-9, -, _).", "error");
  } else {
    try { entry = JSON.parse($("#editor-json").value); }
    catch { return setMsg("JSON ist nicht parsebar", "error"); }
  }
  try {
    await api("spot/" + encodeURIComponent(entry.Id), { method: "PUT", body: JSON.stringify(entry) });
    setMsg("Gespeichert ✔", "ok");
    selectedId = entry.Id;
    draftEntry = null;
    await refresh();
    select(entry.Id, { fromHistory: true });
  } catch (err) { setMsg(err.message, "error"); }
}

async function del() {
  if (!selectedId || !confirm(`Knoten '${selectedId}' löschen?`)) return;
  await api("spot/" + encodeURIComponent(selectedId), { method: "DELETE" });
  selectedId = null;
  select(null);
  await refresh();
}

function newNode(kind) {
  draftEntry = {
    Id: `${kind}-`,
    Payload: structuredClone(templates[kind]),
    Convergence: "Pending",
  };
  if (kind === "term") draftEntry.Payload.Fields.Item.Relations = [];
  selectedId = null;
  $("#editor-title").textContent = `Neu: ${kind}`;
  $("#editor-json").value = JSON.stringify(draftEntry, null, 2);
  setEditorMode("form");
  $("#btn-delete").disabled = true;
  setMsg("Felder ausfüllen und Speichern.");
}

async function deriveTests() {
  const r = await api("derive-tests?write=true", { method: "POST" });
  setMsg(`${r.derived.length} Test-Knoten abgeleitet.`, "ok");
  await refresh();
}

// — Panels —

// Dashboard: Monitoring über Modell, Konvergenz, Qualität und offene Arbeit.
async function renderDashboard() {
  const el = $("#tab-dashboard");
  const findings = await api("validate");
  const total = entries.length;
  const conv = { Aligned: 0, Pending: 0, Diverged: 0, Orphaned: 0 };
  const kinds = {};
  for (const e of entries) {
    conv[e.Convergence] = (conv[e.Convergence] ?? 0) + 1;
    const k = kindOfCase(e.Payload.Case);
    kinds[k] = (kinds[k] ?? 0) + 1;
  }
  const errors = findings.filter((f) => f.Severity === "Error").length;
  const warnings = findings.length - errors;
  const pctAligned = total ? Math.round((conv.Aligned / total) * 100) : 0;

  el.innerHTML = "";
  const cards = document.createElement("div");
  cards.className = "cards";
  const card = (num, label, color) => {
    const c = document.createElement("div");
    c.className = "card";
    c.innerHTML = `<div class="card-num"${color ? ` style="color:${color}"` : ""}></div>`;
    c.querySelector(".card-num").textContent = num;
    c.appendChild(document.createTextNode(label));
    cards.appendChild(c);
  };
  card(total, "Knoten im SPOT");
  card(`${pctAligned}%`, "konvergent", "var(--aligned)");
  card(errors, "Fehler", errors ? "var(--diverged)" : "var(--aligned)");
  card(warnings, "Warnungen", warnings ? "var(--pending)" : "var(--aligned)");
  el.appendChild(cards);

  const h = (t) => { const x = document.createElement("h4"); x.textContent = t; el.appendChild(x); };

  h("Konvergenz");
  const bar = document.createElement("div");
  bar.className = "convbar";
  for (const key of ["Aligned", "Pending", "Diverged", "Orphaned"]) {
    if (!conv[key]) continue;
    const seg = document.createElement("div");
    seg.className = `convseg ${key}`;
    seg.style.flex = conv[key];
    seg.title = `${key}: ${conv[key]}`;
    seg.textContent = conv[key];
    bar.appendChild(seg);
  }
  el.appendChild(bar);

  h("Knotenarten");
  const kb = document.createElement("div");
  kb.className = "kind-badges";
  for (const k of Object.keys(kinds).sort()) {
    const b = document.createElement("button");
    b.className = "badge";
    b.textContent = `${k} ${kinds[k]}`;
    b.onclick = () => { filterText = k; $("#filter").value = k; renderSidebar(); };
    kb.appendChild(b);
  }
  el.appendChild(kb);

  const open = entries.filter((e) => e.Convergence !== "Aligned");
  h(`Offene Arbeit (${open.length})`);
  for (const e of open) {
    const b = document.createElement("button");
    b.className = "node-item";
    b.innerHTML = `<span class="dot ${e.Convergence}"></span>`;
    b.appendChild(document.createTextNode(`${e.Id} (${kindOfCase(e.Payload.Case)})`));
    b.onclick = () => select(e.Id);
    el.appendChild(b);
  }
}

// Doku: das generierte Kontextpaket — lebende Dokumentation + LLM-Vorlage.
let dokuText = "";
async function renderDoku() {
  try {
    dokuText = DEMO ? await demoApi("export") : await (await fetch("/api/export")).text();
    $("#doku-md").textContent = dokuText;
  } catch {
    $("#doku-md").textContent = "Export nicht verfügbar.";
  }
}

// Diagramm-Interaktion: Klick = wählen, Doppelklick = bearbeiten,
// Drag von Knoten zu Knoten = Beziehung anlegen (UML-Editor).
function wireDiagramClicks(container) {
  const byUnderscore = new Map(entries.map((e) => [e.Id.replaceAll("-", "_"), e.Id]));
  const nodeIdOf = (target) => {
    let n = target;
    while (n && n !== container) {
      if (n.id) {
        const hit = [...byUnderscore.keys()].find((k) => n.id.includes(k));
        if (hit) return byUnderscore.get(hit);
      }
      n = n.parentNode;
    }
    return null;
  };
  for (const node of container.querySelectorAll("g.node, g.nodes > g")) node.style.cursor = "pointer";

  let dragSrc = null;
  container.onpointerdown = (ev) => { dragSrc = nodeIdOf(ev.target); };
  container.onpointerup = (ev) => {
    const src = dragSrc;
    dragSrc = null;
    if (!src) return;
    const target = nodeIdOf(ev.target);
    if (!target || target === src) { select(src); return; }
    proposeRelation(src, target, ev.clientX, ev.clientY);
  };
  container.ondblclick = (ev) => {
    const id = nodeIdOf(ev.target);
    if (id) { select(id); setEditorMode("form"); }
  };
}

// Drag-Ziel erreicht: passende Beziehung anbieten und nach Bestätigung speichern.
function proposeRelation(srcId, targetId, x, y) {
  const src = entries.find((e) => e.Id === srcId);
  if (!src) return;
  const menu = document.createElement("div");
  menu.id = "link-menu";
  menu.style.left = x + "px";
  menu.style.top = y + "px";
  const close = () => menu.remove();
  const option = (label, apply) => {
    const b = document.createElement("button");
    b.className = "menu-item";
    b.textContent = label;
    b.onclick = async () => {
      close();
      const updated = structuredClone(src);
      apply(updated.Payload.Fields.Item);
      try {
        await api("spot/" + encodeURIComponent(srcId), { method: "PUT", body: JSON.stringify(updated) });
        setMsg(`Beziehung angelegt: ${srcId} → ${targetId} ✔`, "ok");
        await refresh();
        select(srcId, { fromHistory: true });
      } catch (err) { setMsg(err.message, "error"); }
    };
    menu.appendChild(b);
  };
  const header = document.createElement("div");
  header.className = "insp-hint";
  header.style.padding = "4px 10px";
  header.textContent = `${srcId} → ${targetId}`;
  menu.appendChild(header);

  const targetIsTerm = entries.some((e) => e.Id === targetId && e.Payload.Case === "TermNode");
  if (src.Payload.Case === "TermNode" && targetIsTerm) {
    option("ist ein (Generalisierung)", (d) => d.Relations.push({ Case: "IsA", Fields: { Item: targetId } }));
    option("Teil von (Komposition)", (d) => d.Relations.push({ Case: "PartOf", Fields: { Item: targetId } }));
    option("bezieht sich auf (Assoziation)", (d) => d.Relations.push({ Case: "RelatesTo", Fields: { Item: targetId } }));
  } else if (src.Payload.Case === "ComponentNode") {
    option("hängt ab von (Dependency)", (d) => { if (!d.DependsOn.includes(targetId)) d.DependsOn.push(targetId); });
  } else if (src.Payload.Case === "DecisionNode") {
    option("ersetzt (Supersedes)", (d) => { d.Supersedes = targetId; });
  } else {
    const hint = document.createElement("div");
    hint.className = "insp-hint";
    hint.style.padding = "4px 10px";
    hint.textContent = "Für diese Knotenart gibt es keine ziehbare Beziehung.";
    menu.appendChild(hint);
  }
  const cancel = document.createElement("button");
  cancel.className = "menu-item";
  cancel.textContent = "Abbrechen";
  cancel.onclick = close;
  menu.appendChild(cancel);
  document.body.appendChild(menu);
  setTimeout(() => document.addEventListener("click", function once(ev) {
    if (!menu.contains(ev.target)) { close(); }
    document.removeEventListener("click", once);
  }), 0);
}

// ═══ Diagramm-Designer: eine Zeichenfläche, viele Sichten ═══════════════
// Alle Sichten speisen sich aus visibleEntries() — Slice/Dice/Fokus (Cube)
// wirken überall. Drill-down: Grid-Kachel oder Fokus; Roll-up: Fokus aus.
let cy = null;
let pendingLink = null;
let designerDirty = true;
let designerView = localStorage.getItem("cdd-designer-view") ?? "klassen";
const posStore = JSON.parse(localStorage.getItem("cdd-designer-pos") ?? "{}");
const savePositions = () => {
  if (!cy) return;
  const pos = (posStore[designerView] ??= {});
  for (const n of cy.nodes()) pos[n.id()] = n.position();
  localStorage.setItem("cdd-designer-pos", JSON.stringify(posStore));
};
const cssVar = (name) => getComputedStyle(document.documentElement).getPropertyValue(name).trim();

// Element-Bauer je Sicht: Knotenmenge, Kanten, Form.
const viewBuilders = {
  klassen(visible, ids) {
    const els = [];
    for (const e of visible.filter((x) => x.Payload.Case === "TermNode")) {
      const d = payloadData(e);
      const syn = (d.Synonyms ?? []).join(", ");
      els.push({ data: { id: e.Id, label: d.Name + (syn ? `\n«${syn}»` : ""), conv: e.Convergence, shape: "round-rectangle" } });
      for (const r of d.Relations ?? []) {
        if (!ids.has(r.Fields?.Item)) continue;
        const lbl = r.Case === "IsA" ? "ist ein" : r.Case === "PartOf" ? "Teil von" : "";
        els.push({ data: { id: `${e.Id}>${r.Fields.Item}:${r.Case}`, source: e.Id, target: r.Fields.Item,
          label: lbl, arrow: r.Case === "IsA" ? "triangle-backcurve" : r.Case === "PartOf" ? "diamond" : "triangle",
          dash: r.Case === "RelatesTo" ? [6, 3] : [] } });
      }
    }
    return els;
  },
  architektur(visible, ids) {
    const els = [];
    for (const e of visible.filter((x) => x.Payload.Case === "ComponentNode")) {
      els.push({ data: { id: e.Id, label: payloadData(e).Name ?? e.Id, conv: e.Convergence, shape: "round-rectangle" } });
      for (const dep of payloadData(e).DependsOn ?? []) {
        if (!ids.has(dep)) continue;
        const isComp = entries.some((x) => x.Id === dep && x.Payload.Case === "ComponentNode");
        if (!isComp) els.push({ data: { id: dep, label: dep, conv: entries.find((x) => x.Id === dep)?.Convergence ?? "Pending", shape: "ellipse" } });
        els.push({ data: { id: `${e.Id}>${dep}`, source: e.Id, target: dep, label: "nutzt", arrow: "triangle", dash: [] } });
      }
    }
    return els;
  },
  usecase(visible, ids) {
    const els = [];
    for (const e of visible) {
      const d = payloadData(e);
      if (e.Payload.Case === "SpecNode")
        els.push({ data: { id: e.Id, label: d.Title ?? e.Id, conv: e.Convergence, shape: "ellipse" } });
      else if (e.Payload.Case === "ComponentNode")
        els.push({ data: { id: e.Id, label: d.Name ?? e.Id, conv: e.Convergence, shape: "round-rectangle" } });
      else if (e.Payload.Case === "TestNode" && ids.has(d.SpecRef))
        els.push({ data: { id: e.Id, label: "✓ " + e.Id, conv: e.Convergence, shape: "tag" } });
    }
    const have = new Set(els.map((x) => x.data.id));
    for (const e of visible) {
      const d = payloadData(e);
      if (e.Payload.Case === "ComponentNode")
        for (const dep of d.DependsOn ?? [])
          if (have.has(dep)) els.push({ data: { id: `${e.Id}>${dep}`, source: e.Id, target: dep, label: "", arrow: "triangle", dash: [4, 3] } });
      if (e.Payload.Case === "TestNode" && have.has(d.SpecRef))
        els.push({ data: { id: `${e.Id}>${d.SpecRef}`, source: e.Id, target: d.SpecRef, label: "testet", arrow: "triangle", dash: [2, 2] } });
    }
    return els;
  },
  topologie(visible, ids) {
    const els = [];
    for (const e of visible)
      els.push({ data: { id: e.Id, label: `${kindOfCase(e.Payload.Case)}\n${e.Id}`, conv: e.Convergence,
        shape: e.Payload.Case === "ComponentNode" ? "round-rectangle" : e.Payload.Case === "SpecNode" ? "ellipse" : "round-rectangle" } });
    for (const e of visible)
      for (const r of outRefs(e))
        if (ids.has(r.target))
          els.push({ data: { id: `${e.Id}>${r.target}:${r.label}`, source: e.Id, target: r.target, label: r.label, arrow: "triangle", dash: [] } });
    return els;
  },
};

function renderCyView() {
  const visible = visibleEntries();
  const ids = new Set(visible.map((e) => e.Id));
  const elements = viewBuilders[designerView](visible, ids);
  const saved = posStore[designerView] ?? {};
  for (const el of elements) if (!el.data.source && saved[el.data.id]) el.position = { ...saved[el.data.id] };
  if (cy) { cy.destroy(); cy = null; }
  const nodeEls = elements.filter((el) => !el.data.source);
  const allPositioned = nodeEls.length > 0 && nodeEls.every((el) => el.position);
  cy = cytoscape({
    container: $("#designer-cy"),
    elements,
    layout: { name: allPositioned ? "preset" : designerView === "topologie" ? "cose" : "breadthfirst", animate: false, padding: 30, spacingFactor: 1.2 },
    wheelSensitivity: 0.2,
    style: [
      { selector: "node", style: {
          shape: "data(shape)", width: "label", height: "label", padding: "9px",
          "background-color": cssVar("--diagram-node") || "#e3eefb",
          "border-width": 2, label: "data(label)", "text-wrap": "wrap",
          "font-size": "10px", "font-family": "Tahoma, Segoe UI, sans-serif",
          "text-valign": "center", color: cssVar("--fg") || "#1e1e1e" } },
      { selector: 'node[conv = "Aligned"]', style: { "border-color": cssVar("--aligned") } },
      { selector: 'node[conv = "Pending"]', style: { "border-color": cssVar("--pending") } },
      { selector: 'node[conv = "Diverged"]', style: { "border-color": cssVar("--diverged") } },
      { selector: 'node[conv = "Orphaned"]', style: { "border-color": cssVar("--orphaned") } },
      { selector: "node:selected", style: { "background-color": cssVar("--hover") || "#c9def5", "border-width": 3 } },
      { selector: "edge", style: {
          width: 1.5, "line-color": cssVar("--diagram-line") || "#5b7da8",
          "target-arrow-color": cssVar("--diagram-line") || "#5b7da8",
          "target-arrow-shape": "data(arrow)", "line-dash-pattern": "data(dash)",
          "line-style": "solid", "curve-style": "bezier",
          label: "data(label)", "font-size": "8px", color: cssVar("--muted") || "#666",
          "text-rotation": "autorotate", "text-background-color": cssVar("--bg") || "#fff",
          "text-background-opacity": 0.85 } },
    ],
  });
  cy.on("layoutstop", savePositions);
  cy.on("dragfree", "node", savePositions);
  cy.on("tap", "node", (ev) => {
    const id = ev.target.id();
    if (!entries.some((e) => e.Id === id)) return;
    if (pendingLink && pendingLink !== id) {
      const src = pendingLink;
      pendingLink = null;
      const oe = ev.originalEvent ?? { clientX: 240, clientY: 240 };
      proposeRelation(src, id, oe.clientX, oe.clientY);
    } else select(id);
  });
  cy.on("dbltap", "node", (ev) => { select(ev.target.id()); setEditorMode("form"); });
  cy.on("cxttap", "node", (ev) => {
    pendingLink = ev.target.id();
    setMsg(`Beziehung von '${pendingLink}': Ziel-Knoten anklicken (Esc bricht ab) …`);
  });
  cy.on("tap", (ev) => { if (ev.target === cy) pendingLink = null; });
}

// Sequenzdiagramm: Spec-Kriterien als Kontext↔System-Interaktion (mermaid).
async function renderSequenz() {
  const specs = visibleEntries().filter((e) => e.Payload.Case === "SpecNode");
  const sel = $("#seq-spec");
  sel.innerHTML = "";
  for (const sp of specs) {
    const o = document.createElement("option");
    o.value = sp.Id;
    o.textContent = payloadData(sp).Title ?? sp.Id;
    sel.appendChild(o);
  }
  const box = $("#designer-mermaid");
  if (!specs.length) { box.innerHTML = "<p class='insp-hint'>Keine Specs in der aktuellen Sicht.</p>"; return; }
  if (![...sel.options].some((o) => o.value === sel.dataset.chosen)) sel.dataset.chosen = specs[0].Id;
  sel.value = sel.dataset.chosen;
  const spec = specs.find((e) => e.Id === sel.value) ?? specs[0];
  const d = payloadData(spec);
  const esc = (t) => String(t).replaceAll(";", ",");
  const lines = ["sequenceDiagram", "  autonumber", "  participant K as Kontext (Given)", "  participant S as System"];
  for (const c of d.Criteria ?? []) {
    lines.push(`  Note over K,S: GIVEN ${esc(c.Given)}`);
    lines.push(`  K->>S: WHEN ${esc(c.When)}`);
    lines.push(`  S-->>K: THEN ${esc(c.Then)}`);
  }
  try {
    const { svg } = await mermaid.render("seqDiagram", lines.join("\n"));
    box.innerHTML = `<h4>${d.Title ?? spec.Id}</h4>` + svg;
  } catch { box.textContent = "Sequenz konnte nicht gerendert werden."; }
}

// Grid: Kacheln zum Durchklicken — Klick = Drill-down (Fokus auf den Knoten).
function renderGrid() {
  const box = $("#designer-grid");
  box.innerHTML = "";
  for (const e of visibleEntries()) {
    const d = payloadData(e);
    const card = document.createElement("button");
    card.className = "gcard";
    card.innerHTML = `<span class="gkind">${kindMeta[kindOfCase(e.Payload.Case)]?.[0] ?? "•"} ${kindOfCase(e.Payload.Case)}</span>
      <b></b><small></small><span class="dot ${e.Convergence}"></span>`;
    card.querySelector("b").textContent = d.Name ?? d.Title ?? d.Statement ?? d.Description ?? e.Id;
    card.querySelector("small").textContent = e.Id;
    card.onclick = () => {
      select(e.Id);
      cube.focus = true;
      $("#focus-toggle").checked = true;
      designerView = "topologie";
      renderDesigner();
      rerenderViews();
    };
    box.appendChild(card);
  }
  if (!box.children.length) box.innerHTML = "<p class='insp-hint'>Keine Knoten in der aktuellen Sicht.</p>";
}

function renderDesigner() {
  if (!$("#tab-designer").classList.contains("active")) { designerDirty = true; return; }
  designerDirty = false;
  localStorage.setItem("cdd-designer-view", designerView);
  for (const b of document.querySelectorAll(".dview"))
    b.classList.toggle("active", b.dataset.view === designerView);
  const isCy = ["klassen", "architektur", "usecase", "topologie"].includes(designerView);
  $("#designer-cy").hidden = !isCy;
  $("#designer-mermaid").hidden = designerView !== "sequenz";
  $("#designer-grid").hidden = designerView !== "grid";
  $("#seq-spec").hidden = designerView !== "sequenz";
  if (isCy) renderCyView();
  else if (designerView === "sequenz") renderSequenz();
  else renderGrid();
}

async function renderValidate() {
  const findings = await api("validate");
  lastFindings = findings;
  renderInspector();
  renderStatusbar();
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

// — Toolbox (VS-Style): klicken oder in den Editor ziehen —
const kindMeta = {
  spec: ["📋", "Spec"], term: ["🔤", "Begriff (Ontologie)"], component: ["📦", "Component"],
  decision: ["⚖️", "Entscheidung (ADR)"], premise: ["🧭", "Prämisse"], risk: ["⚠️", "Risk"],
  knowledge: ["📚", "Knowledge"], tool: ["🔧", "Tool"], infra: ["🖥️", "Infra"],
  invariant: ["🛡️", "Invariante"],
};
{
  const tb = $("#toolbox-items");
  for (const [kind, [icon, label]] of Object.entries(kindMeta)) {
    const b = document.createElement("button");
    b.className = "tool-item";
    b.draggable = true;
    b.innerHTML = `<span>${icon}</span>`;
    b.appendChild(document.createTextNode(label));
    b.onclick = () => newNode(kind);
    b.addEventListener("dragstart", (ev) => ev.dataTransfer.setData("text/cdd-kind", kind));
    tb.appendChild(b);
  }
  const ed = $("#editor-json");
  ed.addEventListener("dragover", (ev) => {
    if (ev.dataTransfer.types.includes("text/cdd-kind")) {
      ev.preventDefault();
      ed.classList.add("dragover");
    }
  });
  ed.addEventListener("dragleave", () => ed.classList.remove("dragover"));
  ed.addEventListener("drop", (ev) => {
    const kind = ev.dataTransfer.getData("text/cdd-kind");
    if (!kind) return;
    ev.preventDefault();
    ed.classList.remove("dragover");
    newNode(kind);
  });
}

// — Fenstermanagement (VS-Style): Splitter + ein-/ausblendbare Panels —
const layoutKey = "cdd-layout";
const layout = JSON.parse(localStorage.getItem(layoutKey) ?? "{}");
const panels = [...document.querySelectorAll(".panel[data-panel]")];
const saveLayout = () => localStorage.setItem(layoutKey, JSON.stringify(layout));
const resizeTarget = (s) => (s.dataset.inverse ? s.nextElementSibling : s.previousElementSibling);

function renderViewMenu() {
  const m = $("#view-menu");
  m.innerHTML = "";
  for (const p of panels) {
    const hidden = layout.hidden?.includes(p.dataset.panel);
    const b = document.createElement("button");
    b.className = "menu-item";
    b.textContent = `${hidden ? "  " : "✓ "}${p.dataset.title}`;
    b.onclick = () => {
      layout.hidden = (layout.hidden ?? []).filter((x) => x !== p.dataset.panel);
      if (!hidden) layout.hidden.push(p.dataset.panel);
      saveLayout();
      applyPanelVisibility();
    };
    m.appendChild(b);
  }
}

function applyPanelVisibility() {
  for (const p of panels) {
    p.style.display = layout.hidden?.includes(p.dataset.panel) ? "none" : "";
  }
  for (const s of document.querySelectorAll(".splitter")) {
    const t = resizeTarget(s);
    s.style.display = t && t.style.display === "none" ? "none" : "";
  }
  renderViewMenu();
}

for (const s of document.querySelectorAll(".splitter")) {
  const saved = layout.widths?.[s.dataset.resize];
  if (saved) {
    const t = resizeTarget(s);
    t.style.width = saved;
    t.style.flex = "none";
  }
  s.addEventListener("mousedown", (ev) => {
    ev.preventDefault();
    const target = resizeTarget(s);
    const startX = ev.clientX;
    const startW = target.getBoundingClientRect().width;
    const inverse = !!s.dataset.inverse;
    s.classList.add("dragging");
    const move = (e) => {
      const w = Math.max(80, inverse ? startW - (e.clientX - startX) : startW + (e.clientX - startX));
      target.style.width = w + "px";
      target.style.flex = "none";
    };
    const up = () => {
      document.removeEventListener("mousemove", move);
      document.removeEventListener("mouseup", up);
      s.classList.remove("dragging");
      (layout.widths ??= {})[s.dataset.resize] = resizeTarget(s).style.width;
      saveLayout();
    };
    document.addEventListener("mousemove", move);
    document.addEventListener("mouseup", up);
  });
}

for (const btn of document.querySelectorAll(".panel-close")) {
  btn.onclick = () => {
    const p = btn.closest(".panel");
    (layout.hidden ??= []).push(p.dataset.panel);
    saveLayout();
    applyPanelVisibility();
  };
}

$("#btn-view").onclick = (ev) => {
  ev.stopPropagation();
  $("#model-menu").hidden = true;
  const m = $("#view-menu");
  m.hidden = !m.hidden;
};
$("#btn-model-menu").onclick = (ev) => {
  ev.stopPropagation();
  $("#view-menu").hidden = true;
  const m = $("#model-menu");
  m.hidden = !m.hidden;
};
$("#mi-derive").onclick = () => { $("#model-menu").hidden = true; deriveTests(); };
$("#mi-refresh").onclick = () => { $("#model-menu").hidden = true; refresh(); };
document.addEventListener("click", (ev) => {
  if (!ev.target.closest(".menu")) { $("#view-menu").hidden = true; $("#model-menu").hidden = true; }
});
applyPanelVisibility();

// — Wiring —
$("#btn-refresh").onclick = refresh;
$("#btn-save").onclick = save;
$("#btn-delete").onclick = del;
$("#btn-derive").onclick = deriveTests;
$("#btn-mode-inspect").onclick = () => setEditorMode("inspect");
$("#btn-mode-form").onclick = () => setEditorMode("form");
$("#btn-mode-json").onclick = () => setEditorMode("json");
$("#editor-json").addEventListener("input", () => {
  if (editorMode === "json") $("#btn-save").disabled = !$("#editor-json").value;
});
$("#btn-help").onclick = () => { $("#help-overlay").hidden = false; };
$("#help-overlay").onclick = (ev) => {
  if (ev.target.id === "help-overlay" || ev.target.id === "btn-help-close")
    $("#help-overlay").hidden = true;
};
$("#btn-back").onclick = () => {
  const prev = hist.back.pop();
  if (!prev) return;
  if (selectedId) hist.fwd.push(selectedId);
  select(prev, { fromHistory: true });
  updateHistButtons();
};
$("#btn-fwd").onclick = () => {
  const next = hist.fwd.pop();
  if (!next) return;
  if (selectedId) hist.back.push(selectedId);
  select(next, { fromHistory: true });
  updateHistButtons();
};
$("#focus-toggle").onchange = (ev) => {
  cube.focus = ev.target.checked;
  rerenderViews();
};
$("#focus-depth").onchange = (ev) => {
  cube.depth = Number(ev.target.value);
  if (cube.focus) rerenderViews();
};
window.addEventListener("hashchange", () => {
  const id = decodeURIComponent(location.hash.slice(1));
  if (id && entries.some((e) => e.Id === id)) select(id);
});
$("#btn-theme").onclick = () => {
  applyTheme(document.documentElement.dataset.theme === "dark" ? "light" : "dark");
  renderDesigner();
};
for (const t of document.querySelectorAll(".tab")) {
  t.onclick = () => {
    document.querySelectorAll(".tab, .tab-body").forEach((x) => x.classList.remove("active"));
    t.classList.add("active");
    $("#tab-" + t.dataset.tab).classList.add("active");
    if (t.dataset.tab === "designer") {
      if (designerDirty || !cy) renderDesigner();
      else cy.resize();
    }
  };
}
// — Agent-Tab: Prosa → Modelländerung (direkt via Claude oder per Prompt) —
let pendingChanges = null;

function agentStatus(text, cls = "") {
  const s = $("#agent-status");
  s.textContent = text;
  s.className = cls;
}

function showChanges(changes) {
  pendingChanges = changes;
  $("#agent-summary").textContent = changes.summary ?? "";
  const list = $("#agent-changes");
  list.innerHTML = "";
  for (const e of changes.upsert) {
    const exists = entries.some((x) => x.Id === e.Id);
    const d = document.createElement("div");
    d.className = "finding";
    d.textContent = `${exists ? "ändern" : "neu"}: ${e.Id} (${kindOfCase(e.Payload?.Case ?? "?")})`;
    list.appendChild(d);
  }
  for (const id of changes.delete) {
    const d = document.createElement("div");
    d.className = "finding Error";
    d.textContent = `löschen: ${id}`;
    list.appendChild(d);
  }
  $("#agent-result").hidden = false;
}

async function applyChanges() {
  if (!pendingChanges) return;
  let ok = 0;
  const errors = [];
  for (const e of pendingChanges.upsert) {
    try {
      await api("spot/" + encodeURIComponent(e.Id), { method: "PUT", body: JSON.stringify(e) });
      ok++;
    } catch (err) { errors.push(`${e.Id}: ${err.message}`); }
  }
  for (const id of pendingChanges.delete) {
    try {
      await api("spot/" + encodeURIComponent(id), { method: "DELETE" });
      ok++;
    } catch (err) { errors.push(`${id}: ${err.message}`); }
  }
  pendingChanges = null;
  $("#agent-result").hidden = true;
  agentStatus(
    errors.length ? `${ok} Änderungen angewendet, ${errors.length} Fehler: ${errors.join("; ")}` : `✔ ${ok} Änderungen angewendet.`,
    errors.length ? "error" : "ok",
  );
  await refresh();
}

$("#agent-key").value = getApiKey();
$("#agent-key").onchange = (ev) => setApiKey(ev.target.value.trim());

$("#btn-agent-prompt").onclick = async () => {
  const prose = $("#agent-prose").value.trim();
  if (!prose) return agentStatus("Bitte erst eine Änderung beschreiben.", "error");
  await navigator.clipboard.writeText(buildPrompt(prose, dokuText));
  agentStatus("Prompt kopiert ✔ — in eine KI einfügen, die JSON-Antwort unten einfügen.", "ok");
};

$("#btn-agent-run").onclick = async () => {
  const prose = $("#agent-prose").value.trim();
  const apiKey = $("#agent-key").value.trim();
  if (!prose) return agentStatus("Bitte erst eine Änderung beschreiben.", "error");
  if (!apiKey) return agentStatus("Für den Direktaufruf wird ein API-Key benötigt — alternativ den Prompt kopieren.", "error");
  setApiKey(apiKey);
  agentStatus("Claude arbeitet …");
  $("#btn-agent-run").disabled = true;
  try {
    const changes = await callClaude({
      apiKey,
      model: $("#agent-model").value,
      prose,
      contextMd: dokuText,
    });
    agentStatus("Vorschlag erhalten — prüfen und anwenden.", "ok");
    showChanges(changes);
  } catch (err) {
    agentStatus("Fehler: " + err.message, "error");
  } finally {
    $("#btn-agent-run").disabled = false;
  }
};

$("#btn-agent-parse").onclick = () => {
  try {
    showChanges(parseChanges($("#agent-paste").value));
    agentStatus("Antwort gelesen — prüfen und anwenden.", "ok");
  } catch (err) {
    agentStatus("Antwort nicht lesbar: " + err.message, "error");
  }
};

$("#btn-agent-apply").onclick = applyChanges;
$("#btn-agent-discard").onclick = () => {
  pendingChanges = null;
  $("#agent-result").hidden = true;
  agentStatus("Verworfen.");
};

$("#btn-doku-download").onclick = () => {
  const blob = new Blob([dokuText], { type: "text/markdown" });
  const a = document.createElement("a");
  a.href = URL.createObjectURL(blob);
  a.download = "spot-context.md";
  a.click();
  URL.revokeObjectURL(a.href);
};
$("#btn-doku-copy").onclick = async () => {
  await navigator.clipboard.writeText(dokuText);
  setMsg("Kontextpaket kopiert ✔ — direkt an eine KI übergeben.", "ok");
};
for (const b of document.querySelectorAll(".dview")) {
  b.onclick = () => { designerView = b.dataset.view; renderDesigner(); };
}
$("#seq-spec").onchange = (ev) => { ev.target.dataset.chosen = ev.target.value; renderSequenz(); };
$("#filter").oninput = (ev) => {
  filterText = ev.target.value;
  renderSidebar();
};
document.addEventListener("keydown", (ev) => {
  if (ev.key === "Escape" && pendingLink) { pendingLink = null; setMsg(""); }
  if ((ev.ctrlKey || ev.metaKey) && ev.key === "s") {
    ev.preventDefault();
    if (!$("#btn-save").disabled) save();
  }
});

refresh()
  .then(() => {
    // Deeplink: #<knoten-id> öffnet direkt den Inspektor dieses Knotens.
    const id = decodeURIComponent(location.hash.slice(1));
    if (id && entries.some((e) => e.Id === id)) select(id);
    else renderInspector();
  })
  .catch((e) => setMsg("API nicht erreichbar: " + e.message, "error"));
