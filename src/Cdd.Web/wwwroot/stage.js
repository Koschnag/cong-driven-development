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
import { api, idOf, kindOf, convOf, title, summary, escapeHtml } from './core.js';
import { renderNodeDetail } from './properties.js';
import { renderGraph } from './graph.js';
import { renderCube } from './cube.js';
import { renderDocs } from './docs.js';
import { renderDecisions } from './decisions.js';
import { renderHistory } from './history.js';
import { renderSchmiede } from './schmiede.js';
import { errorRows } from './dock.js';

export const SURFACES = [
  { id: 'schmiede', icon: '⚒', label: 'Schmiede' },
  { id: 'plan',  icon: '◆', label: 'Plan' },
  { id: 'model', icon: '⬡', label: 'Modell' },
  { id: 'dev',   icon: '◇', label: 'Dev' },
  { id: 'infra', icon: '☁', label: 'Infra' },
  { id: 'prod',  icon: '▲', label: 'Prod' },
  { id: 'docs',  icon: '▤', label: 'Doku' },
  { id: 'decisions', icon: '⎇', label: 'Entscheidungen' },
  { id: 'history',   icon: '⟲', label: 'Historie' },
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
              : surf === 'memory' ? '@ Gedächtnis'
              : surf === 'schmiede' ? '⚒ Schmiede'
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
    case 'decisions': return renderDecisions(body, store, actions);
    case 'history':   return renderHistory(body, store, actions);
    case 'drift': return renderDrift(body, store, actions);
    case 'node':  return renderNode(body, store, actions, s.stageArg);
    case 'memory': return renderMemory(body, store, actions);
    case 'schmiede': return renderSchmiede(body, store, actions);
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

// ── Infra: ECHTE Live-Metriken von VM 120 (uptime/load/mem/disk + Docker). Pi/Celsius/Tower: Periphery geplant. ──
function renderInfra(el, store, actions) {
  const inf = store.get().infra || {};
  const h = inf.host;
  const hosts = inf.hosts || [
    { name: 'pi', role: 'Infra (DNS · Reverse-Proxy · Tailscale)', state: 'unknown' },
    { name: 'celsius', role: 'Services (Nextcloud · YunoHost · Backups)', state: 'unknown' },
    { name: 'tower', role: 'Proxmox (VMs · Gaming-VM)', state: 'unknown' },
  ];
  const apps = inf.apps || [];
  const dot = (st) => st === 'up' || st === 'online' ? 'Aligned' : st === 'down' ? 'Diverged' : 'Pending';
  const metrics = h ? `<div class="infra-metrics">
      <span><b>${escapeHtml(h.name)}</b></span><span>⏱ ${escapeHtml(h.uptime || '')}</span>
      <span>load ${escapeHtml(h.load || '')}</span><span>mem ${h.memUsedMb}/${h.memTotalMb} MB</span>
      <span>disk ${escapeHtml(h.diskUsedPct || '')}</span></div>` : '';
  el.innerHTML =
    `<div class="stage-hint">Homelab — ${inf.ok ? `<b>live</b> · ${escapeHtml(inf.source || '')}` : 'Backend offline → Plan'} <span class="adopt-tag">VM 120 live · Pi/Celsius/Tower via Komodo-Periphery (geplant)</span></div>` +
    metrics +
    `<div class="stage-actions"><button data-ask>🤖 „Status aller Hosts holen“</button></div>` +
    hosts.map(x => `<div class="host-row">
        <span class="dot ${dot(x.state)}"></span>
        <div class="host-meta"><div class="host-name">${escapeHtml(x.name)}</div><div class="host-role">${escapeHtml(x.role)}</div></div>
        <span class="host-state">${escapeHtml(x.state)}</span>
      </div>`).join('') +
    (apps.length ? `<div class="stage-hint" style="margin-top:.5rem">Container · ${apps.length} (live)</div>` +
      apps.map(a => `<div class="host-row">
        <span class="dot ${a.healthy ? 'Aligned' : 'Pending'}"></span>
        <div class="host-meta"><div class="host-name">${escapeHtml(a.name)}</div><div class="host-role">${escapeHtml(a.url || '')}</div></div>
        <span class="host-state">${escapeHtml(a.status || '')}</span>
      </div>`).join('') : '');
  el.querySelector('[data-ask]').onclick = () =>
    actions.ask('Hol den Status von Pi, Celsius und Tower aus dem Homelab und fasse Auffälligkeiten zusammen.');
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

// ── @-Gedächtnis: cong.db-Volltextsuche (NUR sensitive=0) als Treffer-Karten. „→ an den Faden" = einordnen. ──
function renderMemory(el, store, actions) {
  const s = store.get();
  const q = s.dwhQuery || '';
  if (s.dwhLoading) { el.innerHTML = `<div class="muted pad">Suche „${escapeHtml(q)}" im Gedächtnis…</div>`; return; }
  if (s.dwhAvailable === false) {
    el.innerHTML = `<div class="stage-hint">@-Gedächtnis (cong.db, nur <code>sensitive=0</code>). <b>${escapeHtml(s.dwhNote || 'nicht verfügbar')}</b></div>`;
    return;
  }
  const hits = s.dwhHits || [];
  const mode = s.dwhMode || 'keyword';
  el.innerHTML =
    `<div class="stage-hint">@-Gedächtnis · „${escapeHtml(q)}" · ${hits.length} Treffer <span class="adopt-tag">cong.db · nur sensitive=0</span></div>` +
    `<div class="stage-actions mem-modes">
       <button data-mode="keyword" class="${mode === 'keyword' ? 'on' : ''}">wörtlich · FTS5</button>
       <button data-mode="semantic" class="${mode === 'semantic' ? 'on' : ''}">semantisch · Vektor</button></div>` +
    (hits.length ? hits.map((h, i) => `<div class="mem-card">
        <div class="mem-meta"><span class="mem-sys">${escapeHtml(h.system || '')}</span>${h.score != null ? `<span class="mem-score">cos ${escapeHtml(String(h.score))}</span>` : ''}<span class="mem-when">${escapeHtml((h.created_at || '').slice(0, 16))}</span></div>
        <div class="mem-title">${escapeHtml(h.title || '(ohne Titel)')}</div>
        <div class="mem-snip">${escapeHtml(h.snippet || '')}</div>
        <div class="mem-actions"><button data-i="${i}">→ an den Faden</button></div>
      </div>`).join('') : '<div class="rail-empty pad">— keine Treffer —</div>');
  el.querySelectorAll('.mem-modes button').forEach(b => b.onclick = () => actions.dwhSearch(q, b.dataset.mode));
  el.querySelectorAll('.mem-card [data-i]').forEach(b => b.onclick = () => {
    const h = (store.get().dwhHits || [])[+b.dataset.i];
    if (h) actions.ask(`Aus meinem Gedächtnis (${h.system}, „${h.title}"): „${h.snippet}". Ordne das ein und sag, ob daraus ein SPOT-Knoten werden sollte.`);
  });
}

function renderSettings(el, store, actions) {
  const s = store.get();
  el.innerHTML = `
    <div class="set">
      <div class="set-row"><span>Engine</span><b>${escapeHtml(s.engine)}</b></div>
      <div class="set-row"><span>Modus</span><b>${escapeHtml(s.mode)}</b></div>
      <div class="set-row"><span>Knoten</span><b>${s.nodes.length}</b></div>
      <div class="set-row"><span>Theme</span><button data-theme>🌓 wechseln</button></div>
      <div class="set-providers muted">Provider laden…</div>
    </div>`;
  el.querySelector('[data-theme]').onclick = () => actions.toggleTheme();
  renderProviders(el.querySelector('.set-providers'), store, actions);
}

// Vorlagen für gängige OpenAI-kompatible Anbieter — Ein-Klick prefill, dann Key eintragen + Speichern.
// (Nur Claude ist vorkonfiguriert; alles hier fügt der Nutzer selbst hinzu.)
const PROVIDER_PRESETS = [
  { id: 'ollama',     label: 'Ollama (lokal)', base: 'http://localhost:11434/v1', model: 'qwen2.5:3b' },
  { id: 'mistral',    label: 'Mistral (EU)',   base: 'https://api.mistral.ai/v1', model: 'mistral-large-latest' },
  { id: 'groq',       label: 'Groq',           base: 'https://api.groq.com/openai/v1', model: 'llama-3.3-70b-versatile' },
  { id: 'openrouter', label: 'OpenRouter',     base: 'https://openrouter.ai/api/v1', model: '' },
  { id: 'together',   label: 'Together',       base: 'https://api.together.xyz/v1', model: '' },
];

// ── Laufzeit-Provider: Engines + API-Keys über die GUI (jeder OpenAI-kompatible Anbieter). ──
async function renderProviders(box, store, actions) {
  if (!box) return;
  let ps;
  try { ps = (await api.providers()).providers || []; }
  catch { box.innerHTML = '<div class="muted">Provider nicht ladbar.</div>'; return; }
  box.classList.remove('muted');
  const dot = (p) => p.builtin ? 'builtin' : (p.keySet ? 'ok' : 'nokey');
  box.innerHTML =
    `<div class="set-h">Engines &amp; Provider <span class="muted">— Keys zur Laufzeit, jeder OpenAI-kompatible Anbieter</span></div>` +
    ps.map(p => `<div class="prov-row">
        <span class="prov-dot ${dot(p)}" title="${p.builtin ? 'eingebaut' : (p.keySet ? 'Key gesetzt' : 'kein Key')}"></span>
        <div class="prov-meta"><b>${escapeHtml(p.label)}</b> <code>${escapeHtml(p.id)}</code>
          <div class="prov-sub">${escapeHtml(p.baseUrl || (p.builtin ? 'eingebaut, kein Key nötig' : ''))}${p.model ? ' · ' + escapeHtml(p.model) : ''}</div></div>
        ${p.builtin ? '' : `<button class="prov-btn" data-edit="${escapeHtml(p.id)}">bearb.</button><button class="prov-btn del" data-del="${escapeHtml(p.id)}">×</button>`}
      </div>`).join('') +
    `<div class="prov-form">
       <div class="set-h2">Provider hinzufügen / bearbeiten</div>
       <div class="prov-presets"><span class="muted">Vorlagen:</span>
         ${PROVIDER_PRESETS.map((p, i) => `<button class="prov-preset" data-preset="${i}">${escapeHtml(p.label)}</button>`).join('')}</div>
       <input class="prov-in" id="pv-id" placeholder="id — z. B. mistral, groq, openrouter">
       <input class="prov-in" id="pv-label" placeholder="Label — z. B. Mistral (EU)">
       <input class="prov-in" id="pv-base" placeholder="Base-URL — z. B. https://api.mistral.ai/v1">
       <input class="prov-in" id="pv-model" placeholder="Modell — z. B. mistral-large-latest">
       <input class="prov-in" id="pv-key" type="password" placeholder="API-Key — leer lässt vorhandenen unverändert" autocomplete="off">
       <button class="prov-save" id="pv-save">Speichern</button>
       <div class="set-note">Keys liegen nur lokal (gitignored) und werden nie im Klartext ausgeliefert.
         Claude (Cloud) primär · dein Backup hier · Ollama lokal als Notfall.</div>
     </div>`;
  const $ = (id) => box.querySelector(id);
  box.querySelectorAll('[data-preset]').forEach(b => b.onclick = () => {
    const p = PROVIDER_PRESETS[+b.dataset.preset]; if (!p) return;
    $('#pv-id').value = p.id; $('#pv-label').value = p.label; $('#pv-base').value = p.base;
    $('#pv-model').value = p.model; $('#pv-key').focus();
  });
  box.querySelectorAll('[data-edit]').forEach(b => b.onclick = () => {
    const p = ps.find(x => x.id === b.dataset.edit); if (!p) return;
    $('#pv-id').value = p.id; $('#pv-label').value = p.label; $('#pv-base').value = p.baseUrl || '';
    $('#pv-model').value = p.model || ''; $('#pv-key').value = ''; $('#pv-key').focus();
  });
  box.querySelectorAll('[data-del]').forEach(b => b.onclick = async () => {
    await api.deleteProvider(b.dataset.del); actions._reloadEngines && actions._reloadEngines(); renderProviders(box, store, actions);
  });
  $('#pv-save').onclick = async () => {
    const id = $('#pv-id').value.trim().toLowerCase().replace(/[^a-z0-9_-]/g, '');
    if (!id || id === 'claude' || id === 'ollama') { actions.say && actions.say('system', '✗ Id leer oder reserviert (claude/ollama).'); return; }
    const p = { Id: id, Label: ($('#pv-label').value.trim() || id), BaseUrl: $('#pv-base').value.trim(), Model: $('#pv-model').value.trim(), ApiKey: $('#pv-key').value };
    const r = await api.saveProvider(id, p);
    if (r && r.ok) { actions.say && actions.say('system', `✓ Provider ${id} gespeichert${r.keySet ? ' (Key ✓)' : ''}.`); actions._reloadEngines && actions._reloadEngines(); renderProviders(box, store, actions); }
    else actions.say && actions.say('system', '✗ ' + ((r && r.error) || 'Speichern fehlgeschlagen'));
  };
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
