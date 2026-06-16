// Cong OS — Shell-Controller. Eine Omnibox, drei Zonen, fünf Flächen, vier Modell-Linsen.
import { createStore, api, idOf, kindOf, convOf, CONV, escapeHtml } from './core.js';
import { mountCopilot } from './copilot.js';
import { renderInspector } from './inspector.js';
import { renderGraph } from './graph.js';
import { renderCube } from './cube.js';
import { renderDocs } from './docs.js';

const SURFACES = [['plan', 'Plan'], ['ideate', 'Ideate'], ['develop', 'Develop'], ['monitor', 'Monitor'], ['deploy', 'Deploy']];
const LENSES = [['inspector', 'Inspector'], ['graph', 'Graph'], ['cube', 'Cube'], ['docs', 'Doku']];

const store = createStore({
  nodes: [], byId: new Map(), selected: null,
  surface: 'develop', lens: 'inspector', validate: [], diff: null,
  cubeRows: 'kind', cubeCols: 'conv',
});
let copilot, $rail, $tabs, $stage, $status;

const actions = {
  select: (id) => {
    const n = store.get().byId.get(id); if (!n) return;
    const lens = store.get().lens;
    store.set({ selected: n, lens: lens === 'cube' || lens === 'docs' ? 'inspector' : lens });
    renderStage();
  },
  setLens: (l) => { store.set({ lens: l }); renderStage(); },
  dispatch: (p) => { document.querySelector('.copilot').classList.add('open'); copilot.dispatch(p); },
  derive: async () => { try { await fetch('/api/derive-tests?write=true', { method: 'POST' }); } catch {} await reload(); actions.dispatch('Ich habe Tests aus den Specs abgeleitet — schau auf die Konvergenz.'); },
  rerender: () => renderStage(),
};

async function reload() {
  const [nodes, validate, diff] = await Promise.all([
    api.spot(), api.validate().catch(() => []), api.diff().catch(() => null),
  ]);
  const byId = new Map(nodes.map(n => [idOf(n), n]));
  let sel = store.get().selected; if (sel) sel = byId.get(idOf(sel)) || null;
  store.set({ nodes, byId, validate, diff, selected: sel });
  renderRail(); renderStatus(); renderStage();
}

function renderRail() {
  const s = store.get();
  $rail.innerHTML = `<h4>Flächen</h4>` +
    SURFACES.map(([id, l], n) => `<div class="surface ${s.surface === id ? 'active' : ''}" data-s="${id}"><span class="t">${l}</span><span class="k">⌘${n + 1}</span></div>`).join('') +
    `<h4>Substanz</h4>` +
    `<div class="metric"><span>SPOT</span><b>${s.nodes.length}</b><span class="muted">Knoten</span></div>` +
    `<div class="metric"><span>⚠ Diverged</span><b>${s.nodes.filter(n => convOf(n) === 'Diverged').length}</b></div>` +
    `<div class="metric"><span>DWH</span><b>167 MB</b></div>`;
  $rail.querySelectorAll('.surface').forEach(d => d.onclick = () => { store.set({ surface: d.dataset.s }); renderRail(); renderTabs(); renderStatus(); });
}

function renderTabs() {
  const s = store.get();
  $tabs.innerHTML =
    `<span class="title">${SURFACES.find(x => x[0] === s.surface)[1]}${s.selected ? ' · ' + escapeHtml(idOf(s.selected)) : ''}</span>` +
    LENSES.map(([id, l]) => `<button class="lenstab ${s.lens === id ? 'active' : ''}" data-l="${id}">${l}</button>`).join('') +
    `<span class="conv">${CONV.map(c => `<span class="dot ${c}"></span>${s.nodes.filter(n => convOf(n) === c).length}`).join(' &nbsp; ')}</span>`;
  $tabs.querySelectorAll('.lenstab').forEach(b => b.onclick = () => actions.setLens(b.dataset.l));
}

function renderStage() {
  renderTabs();
  const l = store.get().lens;
  if (l === 'inspector') renderInspector($stage, store, actions);
  else if (l === 'graph') renderGraph($stage, store, actions);
  else if (l === 'cube') renderCube($stage, store, actions);
  else if (l === 'docs') renderDocs($stage, store, actions);
}

function renderStatus() {
  const s = store.get();
  const div = s.nodes.filter(n => convOf(n) === 'Diverged').length;
  $status.innerHTML =
    `<span class="tok" data-go="cube"><span class="dot Aligned"></span>${s.nodes.length} nodes</span>` +
    `<span class="tok" data-go="cube"><span class="dot Diverged"></span>${div} diverged</span>` +
    `<span class="tok muted">DC-Monitoring → Phase D</span>` +
    `<span class="tok muted">DWH-Suche → Phase C</span>` +
    `<span class="sp">Cong OS · ${s.surface} · idle</span>`;
  $status.querySelectorAll('[data-go=cube]').forEach(t => t.onclick = () => actions.setLens('cube'));
}

function route(q) {
  q = q.trim(); if (!q) return;
  const s = store.get();
  if (q[0] === '/') {
    const cmd = q.slice(1).trim().toLowerCase();
    if (cmd.startsWith('derive')) return actions.derive();
    if (cmd.startsWith('val') || cmd.startsWith('cube')) return actions.setLens('cube');
    if (cmd.startsWith('doc')) return actions.setLens('docs');
    if (cmd.startsWith('graph')) return actions.setLens('graph');
    return actions.dispatch('/' + cmd);
  }
  if (q[0] === '@') return actions.dispatch(`(DWH-Volltext kommt in Phase C) Im Datenwarehouse suchen: ${q.slice(1)}`);
  if (q[0] === '#') { store.set({ lens: 'cube' }); return renderStage(); }
  if (s.byId.has(q)) return actions.select(q);
  return actions.dispatch(q);
}

async function boot() {
  $rail = document.querySelector('#rail');
  $tabs = document.querySelector('#lenstabs');
  $stage = document.querySelector('#stagebody');
  $status = document.querySelector('#status');
  copilot = mountCopilot(document.querySelector('.copilot'), store);

  const omni = document.querySelector('#omni-in');
  omni.addEventListener('keydown', e => { if (e.key === 'Enter') { route(e.target.value); e.target.value = ''; } });
  window.addEventListener('keydown', e => {
    if ((e.metaKey || e.ctrlKey) && e.key.toLowerCase() === 'k') { e.preventDefault(); omni.focus(); }
    if ((e.metaKey || e.ctrlKey) && e.key >= '1' && e.key <= '5') { e.preventDefault(); store.set({ surface: SURFACES[+e.key - 1][0] }); renderRail(); renderTabs(); renderStatus(); }
  });
  document.querySelector('#cop-toggle').onclick = () => document.querySelector('.copilot').classList.toggle('open');
  document.querySelector('#theme').onclick = () => { const r = document.documentElement; r.dataset.theme = r.dataset.theme === 'light' ? '' : 'light'; };

  await reload();
  const firstSpec = store.get().nodes.find(n => kindOf(n) === 'spec');
  if (firstSpec) { store.set({ selected: firstSpec }); renderStage(); }
}

boot();
