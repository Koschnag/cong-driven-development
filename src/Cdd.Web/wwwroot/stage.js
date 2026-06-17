// Die Bühne: genau EINE herbeigerufene Fläche, rechts neben dem Faden. Standard: zu.
//
// Die 6 Flächen = die Funktions-Achsen aus der Strategie, jede eine Projektion des EINEN Modells:
//   1 Plan    (F) — SPOT-Plan: Prämissen · Entscheidungen · Risiken · Specs (der axiomatische Kern)
//   2 Modell  (—) — der ganze SPOT (Knoten + OLAP-Cube + Drift), inkl. Einzelknoten-Inspektor
//   3 Dev     (C) — Entwicklung: Specs↔Tests↔Komponenten, „Tests ableiten“ an Ort und Stelle
//   4 Infra   (E) — Homelab/DC: Pi · Celsius · Tower (adoptiert Komodo via MCP-Backend)
//   5 Prod    (D) — Produktion/Hosting (adoptiert Coolify via MCP-Backend)
//   6 Doku    (—) — die lebende Doku = exakt der Kontext, den die Engine sieht
//
// Jede Fläche ist nur eine Sicht; der Faden bleibt die Wahrheit. Esc schließt die Bühne.
import { idOf, kindOf, convOf, title, summary, escapeHtml } from './core.js';
import { renderNodeDetail } from './properties.js';
import { renderGraph } from './graph.js';
import { renderCube } from './cube.js';
import { renderDocs } from './docs.js';
import { errorRows } from './dock.js';

export const SURFACES = [
  { id: 'plan',  icon: '◆', label: 'Plan' },
  { id: 'model', icon: '⬡', label: 'Modell' },
  { id: 'dev',   icon: '◇', label: 'Dev' },
  { id: 'infra', icon: '☁', label: 'Infra' },
  { id: 'prod',  icon: '▲', label: 'Prod' },
  { id: 'docs',  icon: '▤', label: 'Doku' },
];
// Aliase, damit Status/NOW auch „drift“/„node“/„settings“ auf die Bühne legen können.
const SURFACE_BY_ID = Object.fromEntries(SURFACES.map(s => [s.id, s]));

const PLAN_KINDS = ['premise', 'decision', 'risk', 'spec'];
const DEV_KINDS  = ['spec', 'test', 'component'];

export function renderStage(el, store, actions) {
  const s = store.get();
  el.dataset.open = String(s.stageOpen);
  if (!s.stageOpen) { el.innerHTML = ''; return; }

  const surf = s.stageSurface;
  const meta = SURFACE_BY_ID[surf];
  const label = surf === 'node' ? (s.stageArg || 'Knoten')
              : surf === 'drift' ? 'Drift'
              : surf === 'settings' ? 'Settings'
              : (meta ? meta.label : surf);

  el.innerHTML = `
    <div class="stage-head">
      <span class="stage-title">${meta ? meta.icon + ' ' : ''}${escapeHtml(label)}</span>
      <button class="stage-pin" data-pin title="An Pins (⌘P)">📌</button>
      <button class="stage-x" data-x title="Schließen (Esc)">×</button>
    </div>
    <div class="stage-body" id="stage-body"></div>`;

  const body = el.querySelector('#stage-body');
  el.querySelector('[data-x]').onclick = () => actions.closeStage();
  const pinBtn = el.querySelector('[data-pin]');
  pinBtn.style.visibility = surf === 'node' ? 'visible' : 'hidden';
  pinBtn.onclick = () => actions.togglePin({ ref: s.stageArg, kind: 'node', label: s.stageArg });

  switch (surf) {
    case 'plan':  return renderList(body, store, actions, PLAN_KINDS, 'Plan — der axiomatische Kern: erst Prämissen & Entscheidungen, dann Specs & Risiken.');
    case 'dev':   return renderDev(body, store, actions);
    case 'model': return renderModel(body, store, actions);
    case 'infra': return renderInfra(body, store, actions);
    case 'prod':  return renderProd(body, store, actions);
    case 'docs':  return renderDocs(body, store, actions);
    case 'drift': return renderDrift(body, store, actions);
    case 'node':  return renderNode(body, store, actions, s.stageArg);
    case 'settings': return renderSettings(body, store, actions);
    default: body.innerHTML = `<div class="muted pad">${escapeHtml(surf)}</div>`;
  }
}

// ── Liste von Knoten gefiltert nach Arten, nach Konvergenz gruppiert ──
function renderList(el, store, actions, kinds, hint) {
  const nodes = store.get().nodes.filter(n => kinds.includes(kindOf(n)));
  el.innerHTML = `<div class="stage-hint">${escapeHtml(hint)}</div>` + nodeRows(nodes);
  wireRows(el, actions);
}

function renderDev(el, store, actions) {
  const nodes = store.get().nodes.filter(n => DEV_KINDS.includes(kindOf(n)));
  const pending = nodes.filter(n => kindOf(n) === 'spec' && convOf(n) === 'Pending').length;
  el.innerHTML =
    `<div class="stage-hint">Dev — Specs ↔ Tests ↔ Komponenten. Konvergenz ist der Workflow.</div>` +
    `<div class="stage-actions"><button data-act="derive">✓ Tests ableiten${pending ? ` (${pending} offen)` : ''}</button>
       <button data-act="reload">↻ Validieren</button></div>` +
    nodeRows(nodes);
  el.querySelector('[data-act="derive"]').onclick = () => actions.derive();
  el.querySelector('[data-act="reload"]').onclick = () => actions.reload();
  wireRows(el, actions);
}

// ── Modell: Cube oben (slice/dice), Knotenliste unten — der ganze SPOT ──
function renderModel(el, store, actions) {
  el.innerHTML = `<div class="stage-cube"></div><div class="stage-hint">Alle Knoten</div><div class="stage-nodes"></div>`;
  renderCube(el.querySelector('.stage-cube'), store, actions);
  const list = el.querySelector('.stage-nodes');
  list.innerHTML = nodeRows(store.get().nodes);
  wireRows(list, actions);
}

// ── Drift: die vier Konvergenz-Eimer = der Reconciler-Blick ──
function renderDrift(el, store, actions) {
  const rows = errorRows(store);
  const d = store.get().diff || {};
  const banner = rows.filter(r => r.sev !== 'info').length === 0
    ? `<div class="ok-banner">✓ Modell sauber — keine Divergenz.</div>` : '';
  el.innerHTML = banner + ['Diverged', 'Orphaned', 'Pending', 'Aligned'].map(b => {
    const ns = d[b] || [];
    if (!ns.length) return '';
    return `<div class="drift-bucket"><div class="drift-head"><span class="dot ${b}"></span>${b} <span class="group-count">${ns.length}</span></div>${nodeRows(ns)}</div>`;
  }).join('');
  wireRows(el, actions);
}

// ── Einzelner Knoten: Inspector (zerstörungsfrei) + Graph-Toggle ──
function renderNode(el, store, actions, id) {
  const n = store.get().byId.get(id);
  if (!n) { el.innerHTML = `<div class="muted pad">Knoten <code>${escapeHtml(id || '')}</code> nicht gefunden.</div>`; return; }
  const lens = store.get().nodeLens || 'inspector';
  el.innerHTML = `<div class="lens-subbar">
      <button class="lb${lens === 'inspector' ? ' active' : ''}" data-lens="inspector">Inspector</button>
      <button class="lb${lens === 'graph' ? ' active' : ''}" data-lens="graph">Graph</button>
    </div><div class="lens-body"></div>`;
  el.querySelectorAll('.lb').forEach(b => b.onclick = () => { store.set({ nodeLens: b.dataset.lens }); renderStage(document.querySelector('#stage'), store, actions); });
  const lb = el.querySelector('.lens-body');
  if (lens === 'graph') renderGraph(lb, store, actions, n);
  else renderNodeDetail(lb, store, actions, n, { compact: false });
}

// ── Infra: Pi=Infra · Celsius=Services · Tower=Proxmox. Live vom Backend (Komodo via MCP). ──
function renderInfra(el, store, actions) {
  const inf = store.get().infra;
  const hosts = (inf && inf.hosts) || [
    { name: 'pi',      role: 'Infra (DNS · Reverse-Proxy · Tailscale)', state: 'unknown' },
    { name: 'celsius', role: 'Services (Nextcloud · YunoHost · Backups)', state: 'unknown' },
    { name: 'tower',   role: 'Proxmox (VMs · Gaming-VM)', state: 'unknown' },
  ];
  const dot = (st) => st === 'up' || st === 'online' ? 'Aligned' : st === 'down' ? 'Diverged' : 'Pending';
  el.innerHTML =
    `<div class="stage-hint">Homelab <span class="adopt-tag">Adopt: Komodo-MCP</span> — ${inf ? 'live' : 'Backend offline → statischer DC-Plan (kein toter View). Der 🤖-Knopf läuft jetzt schon über den Faden.'}</div>` +
    `<div class="stage-actions"><button data-ask>🤖 „Status aller Hosts holen“</button></div>` +
    hosts.map(h => `<div class="host-row">
        <span class="dot ${dot(h.state)}"></span>
        <div class="host-meta"><div class="host-name">${escapeHtml(h.name)}</div><div class="host-role">${escapeHtml(h.role)}</div></div>
        <span class="host-state">${escapeHtml(h.state)}</span>
      </div>`).join('');
  el.querySelector('[data-ask]').onclick = () =>
    actions.ask('Hol den Status von Pi, Celsius und Tower aus dem Homelab (Komodo) und fasse Auffälligkeiten zusammen.');
}

// ── Prod: Deployments (Coolify via MCP). Defensiv: ohne Backend ein klarer Auftrag-Knopf. ──
function renderProd(el, store, actions) {
  const inf = store.get().infra;
  const apps = (inf && inf.apps) || [];
  el.innerHTML =
    `<div class="stage-hint">Produktion <span class="adopt-tag">Adopt: Coolify-MCP</span> — ${apps.length ? 'live' : 'noch keine Deployments; die Knöpfe schicken einen Auftrag in den Faden, echtes Deploy/Rollback kommt mit Coolify-MCP.'}</div>` +
    `<div class="stage-actions">
       <button data-ask>🤖 „Letzte Deployments + Health zeigen“</button>
       <button data-deploy>▲ Deploy auslösen…</button></div>` +
    (apps.length ? apps.map(a => `<div class="host-row">
        <span class="dot ${a.healthy ? 'Aligned' : 'Diverged'}"></span>
        <div class="host-meta"><div class="host-name">${escapeHtml(a.name)}</div><div class="host-role">${escapeHtml(a.url || '')}</div></div>
        <span class="host-state">${escapeHtml(a.status || '')}</span>
      </div>`).join('') : '<div class="rail-empty pad">— Faden fragen, um Prod zu laden —</div>');
  el.querySelector('[data-ask]').onclick = () =>
    actions.ask('Zeig die letzten Deployments und den Health-Status aus Coolify (Prod) und nenne, was Aufmerksamkeit braucht.');
  el.querySelector('[data-deploy]').onclick = () => actions.focusOmni();
}

function renderSettings(el, store, actions) {
  const s = store.get();
  el.innerHTML = `
    <div class="set">
      <div class="set-row"><span>Engine</span><b>${escapeHtml(s.engine)}</b></div>
      <div class="set-row"><span>Modus</span><b>${escapeHtml(s.mode)}</b></div>
      <div class="set-row"><span>Knoten</span><b>${s.nodes.length}</b></div>
      <div class="set-row"><span>Theme</span><button data-theme>🌓 wechseln</button></div>
      <div class="set-note">Souverän: Engine frei wählbar (Claude · Mistral-EU · Ollama-lokal). Backends adoptiert via MCP. Daten bleiben im eigenen DC.</div>
    </div>`;
  el.querySelector('[data-theme]').onclick = () => actions.toggleTheme();
}

// ── gemeinsame Knoten-Zeilen ──
function nodeRows(nodes) {
  if (!nodes.length) return '<div class="rail-empty pad">— keine —</div>';
  return nodes.map(n => `<div class="node-row" data-id="${escapeHtml(idOf(n))}">
      <span class="dot ${convOf(n)}"></span>
      <span class="id">${escapeHtml(idOf(n))}</span>
      <span class="sm">${escapeHtml(summary(n) || title(n))}</span></div>`).join('');
}
function wireRows(el, actions) {
  el.querySelectorAll('.node-row').forEach(r => {
    r.onclick = () => actions.focusNode(r.dataset.id);
  });
}
