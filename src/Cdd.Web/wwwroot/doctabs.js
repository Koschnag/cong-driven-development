// Dokument-Well: echte Mehrdokument-Tabs (VS-Editor-Well) + Body-Dispatcher.
// Knoten-Dokument: Linsen-Subleiste Inspector|Graph. Singletons: Cube/Doku/Board/Settings.
import { idOf, kindOf, convOf, title, escapeHtml } from './core.js';
import { tabKey } from './store.js';
import { renderNodeDetail } from './properties.js';
import { renderGraph } from './graph.js';
import { renderCube } from './cube.js';
import { renderDocs } from './docs.js';

const KIND_ICON = { spec: '◇', test: '✓', term: '¶', decision: '⚑', component: '▣', invariant: '∎', premise: '∴', risk: '⚠', knowledge: '▤', tool: '🔧', infra: '☁' };
const SINGLE = { cube: 'OLAP-Cube', docs: 'Doku', board: 'Board', settings: 'Settings' };
const LENSES = [['inspector', 'Inspector'], ['graph', 'Graph']];

export function renderDocTabs(el, store, actions) {
  const s = store.get();
  if (!s.openTabs.length) { el.innerHTML = ''; return; }
  el.innerHTML = s.openTabs.map(t => {
    const key = tabKey(t), active = key === s.active;
    if (t.kind === 'node') {
      const n = s.byId.get(t.id);
      return `<div class="doctab${active ? ' active' : ''}" data-key="${key}"><span class="ti">${KIND_ICON[kindOf(n)] || '•'}</span><span class="dot ${n ? convOf(n) : ''}"></span><span class="dt-id">${escapeHtml(t.id)}</span><span class="x" data-close="${key}">×</span></div>`;
    }
    return `<div class="doctab${active ? ' active' : ''}" data-key="${key}"><span class="ti">▤</span><span class="dt-id">${SINGLE[t.kind] || t.kind}</span><span class="x" data-close="${key}">×</span></div>`;
  }).join('');
  el.querySelectorAll('.doctab').forEach(d => {
    d.onclick = (e) => { if (e.target.classList.contains('x')) return; actions.setActive(d.dataset.key); };
    d.onauxclick = (e) => { if (e.button === 1) { e.preventDefault(); actions.closeTab(d.dataset.key); } };
  });
  el.querySelectorAll('.x').forEach(x => x.onclick = (e) => { e.stopPropagation(); actions.closeTab(x.dataset.close); });
}

export function renderDocBody(el, store, actions) {
  const s = store.get();
  const t = s.openTabs.find(x => tabKey(x) === s.active);
  if (!t) { el.innerHTML = startPage(); wireStart(el, actions); return; }

  if (t.kind === 'node') {
    const n = s.byId.get(t.id);
    if (!n) { el.innerHTML = `<div class="muted pad">Knoten <code>${escapeHtml(t.id)}</code> nicht gefunden.</div>`; return; }
    const lens = s.lensByTab.get(s.active) || 'inspector';
    el.innerHTML = `<div class="lens-subbar">${LENSES.map(([v, l]) =>
      `<button class="lb${lens === v ? ' active' : ''}" data-lens="${v}">${l}</button>`).join('')}</div><div class="lens-body"></div>`;
    el.querySelectorAll('.lb').forEach(b => b.onclick = () => actions.setLens(s.active, b.dataset.lens));
    const body = el.querySelector('.lens-body');
    if (lens === 'graph') renderGraph(body, store, actions, n);
    else renderNodeDetail(body, store, actions, n, { compact: false });
    return;
  }
  if (t.kind === 'cube') { renderCube(el, store, actions); return; }
  if (t.kind === 'docs') { renderDocs(el, store, actions); return; }
  el.innerHTML = `<div class="muted pad">${SINGLE[t.kind] || t.kind} — folgt in einem späteren Bauschritt (Board: Schritt 5, Settings: Schritt 6).</div>`;
}

function startPage() {
  return `<div class="startpage">
    <h1>◉ Cong <b>OS</b></h1>
    <p class="muted">Visual-Studio-orientierte Kommandozentrale über deinem SPOT-Modell.</p>
    <div class="quick">
      <button data-q="explorer">📁 Im Explorer einen Knoten öffnen</button>
      <button data-q="cube">▦ OLAP-Cube</button>
      <button data-q="docs">▤ Doku</button>
      <button data-q="palette">⌘K Suche / Befehle</button>
    </div>
    <p class="muted small">Tipp: 1× Klick im Explorer = Properties rechts · 2× Klick = als Dokument-Tab öffnen.</p>
  </div>`;
}
function wireStart(el, actions) {
  el.querySelectorAll('.quick button').forEach(b => b.onclick = () => {
    const q = b.dataset.q;
    if (q === 'explorer') actions.focusExplorer();
    else if (q === 'palette') actions.focusPalette();
    else actions.openSingle(q);
  });
}
