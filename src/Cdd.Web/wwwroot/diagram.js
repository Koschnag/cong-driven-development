// EA/MagicDraw-Diagramm-Fläche: der getypte SPOT-Graph als raumfüllendes Architektur-Diagramm.
// Souverän: nutzt window.cytoscape (lokal gevendort, kein Laufzeit-CDN). Form=Art, Rand=Konvergenz,
// Kanten=UML-Relation. Vier Sichten + Nachbarschaft. Klick auf Knoten → Bühne (Inspector).
import { idOf, kindOf, convOf, title, refs, escapeHtml } from './core.js';

const CONV = { Aligned: '#2ea043', Pending: '#d29922', Diverged: '#f85149', Orphaned: '#a371f7' };
// UML-nahe Form je Knotenart
const SHAPE = {
  spec: 'round-rectangle', decision: 'diamond', risk: 'triangle', component: 'rectangle',
  term: 'ellipse', test: 'hexagon', premise: 'tag', invariant: 'barrel',
  tool: 'cut-rectangle', knowledge: 'round-tag', infra: 'barrel',
};
// gedämpfte Füllung je Art-Familie (Rand trägt die Konvergenz, Füllung die Art)
const FILL = {
  spec: '#22303f', test: '#1f3b33', component: '#2a2f3a', tool: '#2a2f3a', infra: '#2a2f3a',
  term: '#1f3340', knowledge: '#1f3340', decision: '#2c2740', premise: '#2c2740',
  risk: '#3a2a2a', invariant: '#33302a',
};

const VIEWS = [
  ['architecture', 'Architektur',   'Komponenten & Abhängigkeiten (DependsOn) — die Systemsicht'],
  ['ontology',     'Ontologie',     'Begriffe & UML-Relationen (IsA · PartOf · RelatesTo)'],
  ['traceability', 'Nachverfolgung','Specs ↔ Tests (SpecRef) — Anforderungs-Abdeckung'],
  ['whole',        'Ganzes Modell', 'alle Knoten & Kanten'],
  ['neighbourhood','Nachbarschaft', 'um den gewählten Knoten'],
];
const VIEW_KINDS = {
  architecture: ['component', 'tool', 'infra'],
  ontology: ['term'],
  traceability: ['spec', 'test'],
};

function pick(store, view) {
  const all = store.get().nodes;
  if (view === 'whole') return all;
  if (view === 'neighbourhood') {
    const c = store.get().selected || all.find(n => kindOf(n) === 'component') || all[0];
    if (!c) return [];
    const cid = idOf(c); const ids = new Set([cid]);
    refs(c).forEach(r => ids.add(r.target));
    all.forEach(n => { if (refs(n).some(r => r.target === cid)) ids.add(idOf(n)); });
    return all.filter(n => ids.has(idOf(n)));
  }
  const kinds = VIEW_KINDS[view] || [];
  return all.filter(n => kinds.includes(kindOf(n)));
}

function build(store, view) {
  const set = pick(store, view);
  const ids = new Set(set.map(idOf));
  const els = [];
  set.forEach(n => {
    const k = kindOf(n), c = convOf(n);
    els.push({ data: { id: idOf(n), label: idOf(n), shape: SHAPE[k] || 'ellipse', fill: FILL[k] || '#252a33', bc: CONV[c] || '#888' } });
  });
  set.forEach(n => refs(n).forEach(r => {
    if (ids.has(r.target)) els.push({ data: { id: idOf(n) + '·' + r.rel + '·' + r.target, source: idOf(n), target: r.target, rel: r.rel } });
  }));
  const layout = view === 'whole' ? { name: 'cose', animate: false, padding: 30, nodeRepulsion: 9000, idealEdgeLength: 90 }
    : view === 'neighbourhood' ? { name: 'cose', animate: false, padding: 30 }
    : { name: 'breadthfirst', directed: true, spacingFactor: 1.3, padding: 36 };
  return { els, layout, count: set.length };
}

const STYLE = [
  { selector: 'node', style: {
    'shape': 'data(shape)', 'background-color': 'data(fill)', 'border-color': 'data(bc)', 'border-width': 2.5,
    'label': 'data(label)', 'color': '#cdd6e0', 'font-size': '9px', 'font-family': 'ui-monospace, monospace',
    'text-valign': 'bottom', 'text-margin-y': '4px', 'text-wrap': 'wrap', 'text-max-width': '110px',
    'width': '30px', 'height': '30px' } },
  { selector: 'edge', style: {
    'curve-style': 'bezier', 'width': 1.3, 'line-color': '#54647a', 'target-arrow-color': '#54647a',
    'target-arrow-shape': 'vee', 'label': 'data(rel)', 'font-size': '8px', 'color': '#7c8a9a', 'text-rotation': 'autorotate' } },
  // UML-Semantik der Relationen
  { selector: 'edge[rel="IsA"]', style: { 'target-arrow-shape': 'triangle', 'target-arrow-fill': 'hollow', 'line-color': '#8aa0b8', 'target-arrow-color': '#8aa0b8' } },
  { selector: 'edge[rel="PartOf"]', style: { 'source-arrow-shape': 'diamond', 'source-arrow-fill': 'hollow' } },
  { selector: 'edge[rel="RelatesTo"]', style: { 'line-style': 'dotted' } },
  { selector: 'edge[rel="DependsOn"]', style: { 'line-style': 'dashed' } },
  { selector: 'edge[rel="covers"]', style: { 'line-style': 'dotted', 'line-color': '#2ea043', 'target-arrow-color': '#2ea043' } },
  { selector: 'edge[rel="supersedes"]', style: { 'line-color': '#f85149', 'target-arrow-color': '#f85149', 'width': 2 } },
];

const LEGEND = `<b>Form</b> = Art · <b>Rand</b> = Konvergenz` +
  ` <span style="color:#2ea043">●</span>Aligned <span style="color:#d29922">●</span>Pending <span style="color:#f85149">●</span>Diverged` +
  ` · <b>Kanten</b> = UML-Relation`;

export function renderDiagram(el, store, actions) {
  el.classList.add('dia-host');
  const view = store.get().diagramView || 'architecture';
  el.innerHTML = `
    <div class="dia-bar">
      ${VIEWS.map(([v, l]) => `<button class="dia-v${view === v ? ' on' : ''}" data-v="${v}">${l}</button>`).join('')}
      <span class="dia-hint">${escapeHtml(VIEWS.find(x => x[0] === view)[2])}</span>
      <span class="dia-legend">${LEGEND}</span>
    </div>
    <div class="dia-canvas" id="dia-cy"></div>`;
  el.querySelectorAll('.dia-v').forEach(b => b.onclick = () => { store.set({ diagramView: b.dataset.v }); renderDiagram(el, store, actions); });

  const host = el.querySelector('#dia-cy');
  const { els, layout, count } = build(store, view);
  const Cy = window.cytoscape;
  if (!Cy) { host.innerHTML = `<div class="dia-list muted">Cytoscape nicht geladen — ${count} Knoten in dieser Ansicht.</div>`; return; }
  if (!count) { host.innerHTML = `<div class="dia-list muted">Keine Knoten in der Ansicht „${escapeHtml(view)}".</div>`; return; }

  const cy = Cy({ container: host, elements: els, style: STYLE, layout, wheelSensitivity: 0.25, minZoom: 0.2, maxZoom: 3 });
  cy.on('tap', 'node', e => actions.focusNode(e.target.id()));
  const sel = store.get().selected;
  if (sel) { const nd = cy.getElementById(idOf(sel)); if (nd && nd.length) nd.style({ 'border-color': '#007acc', 'border-width': 4 }); }
}
