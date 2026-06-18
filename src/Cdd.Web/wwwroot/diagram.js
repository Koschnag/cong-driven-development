// EA/MagicDraw-Diagramm-Fläche: der getypte SPOT-Graph als raumfüllendes Architektur-Diagramm.
// Souverän: nutzt window.cytoscape (lokal gevendort, kein Laufzeit-CDN). Form=Familie, Marke=Art,
// Rand=Konvergenz, Kanten=UML-Relation. Fünf Graph-Sichten + drei Formal-Sichten (code behind).
// Links die EA-Toolbox (palette.js), Klick auf Knoten → Bühne (Inspector).
import { idOf, kindOf, convOf, title, refs, escapeHtml, SHAPE, HUE, markUri } from './core.js';
import { renderPalette } from './palette.js';
import { renderFormal } from './formal.js';

const CONV = { Aligned: '#2ea043', Pending: '#d29922', Diverged: '#f85149', Orphaned: '#a371f7' };

const VIEWS = [
  ['architecture', 'Architektur',   'Komponenten & Abhängigkeiten (DependsOn) — die Systemsicht'],
  ['ontology',     'Ontologie',     'Begriffe & UML-Relationen (IsA · PartOf · RelatesTo)'],
  ['traceability', 'Nachverfolgung','Specs ↔ Tests (SpecRef) — Anforderungs-Abdeckung'],
  ['whole',        'Ganzes Modell', 'alle Knoten & Kanten'],
  ['neighbourhood','Nachbarschaft', 'um den gewählten Knoten'],
];
// „Code behind" — dasselbe Modell in formaler Notation. Drei ehrliche Sichten (λ-Kalkül als
// eigene Sicht verworfen: dekorativ; nur Fußnote in der Typ-Sicht).
const FORMAL_VIEWS = [
  ['formal-typ',   'λ Typen',     'Curry-Howard: Spec = Typ · Test = Bewohner · Konvergenz wird als Typurteil gelesen'],
  ['formal-logik', '∀ Logik',     'Die Invarianten als prädikatenlogische Sätze · validate = 𝔐 ⊨ Φ'],
  ['formal-kat',   '∘ Kategorien','Freie Kategorie auf DependsOn · Preorder auf IsA'],
];
const isFormal = (v) => v && v.startsWith('formal');

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
    els.push({ data: {
      id: idOf(n), label: idOf(n),
      shape: SHAPE[k] || 'ellipse', hue: HUE[k] || '#8aa0b8', mark: markUri(k), conv: c,
    } });
  });
  set.forEach(n => refs(n).forEach(r => {
    if (ids.has(r.target)) els.push({ data: { id: idOf(n) + '·' + r.rel + '·' + r.target, source: idOf(n), target: r.target, rel: r.rel } });
  }));
  const layout = view === 'whole' ? { name: 'cose', animate: false, padding: 30, nodeRepulsion: 9000, idealEdgeLength: 110 }
    : view === 'neighbourhood' ? { name: 'cose', animate: false, padding: 30 }
    : { name: 'breadthfirst', directed: true, spacingFactor: 1.45, padding: 40 };
  return { els, layout, count: set.length };
}

// Symbol-Style: äußere Form aus data(shape), innere Marke als SVG-background-image, Rand trägt die
// Konvergenz (orthogonal zur Art). Convergence braucht NULL extra Bild-Assets (native underlay/border).
const STYLE = [
  { selector: 'node', style: {
    'shape': 'data(shape)',
    'background-color': 'data(hue)', 'background-opacity': 0.13,
    'background-image': 'data(mark)', 'background-fit': 'none', 'background-clip': 'none',
    'background-width': '58%', 'background-height': '58%',
    'border-color': 'data(hue)', 'border-width': 1.75,
    'label': 'data(label)', 'color': '#cdd6e0', 'font-size': '9px', 'font-family': 'ui-monospace, monospace',
    'text-valign': 'bottom', 'text-margin-y': '6px', 'text-wrap': 'wrap', 'text-max-width': '120px',
    'width': '48px', 'height': '48px' } },
  // — Konvergenz NUR am Rand (in Graustufen UND bei 16px unterscheidbar) —
  { selector: 'node[conv="Aligned"]',  style: { 'underlay-color': 'data(hue)', 'underlay-opacity': 0.28, 'underlay-padding': 5 } },
  { selector: 'node[conv="Pending"]',  style: { 'background-opacity': 0.07 } },
  { selector: 'node[conv="Diverged"]', style: { 'border-color': '#C25B6B', 'border-style': 'double', 'border-width': 4.5 } },
  { selector: 'node[conv="Orphaned"]', style: { 'border-style': 'dashed', 'opacity': 0.6 } },
  // — UML-Semantik der Relationen —
  { selector: 'edge', style: {
    'curve-style': 'bezier', 'width': 1.3, 'line-color': '#54647a', 'target-arrow-color': '#54647a',
    'target-arrow-shape': 'vee', 'label': 'data(rel)', 'font-size': '8px', 'color': '#7c8a9a', 'text-rotation': 'autorotate' } },
  { selector: 'edge[rel="IsA"]', style: { 'target-arrow-shape': 'triangle', 'target-arrow-fill': 'hollow', 'line-color': '#8aa0b8', 'target-arrow-color': '#8aa0b8' } },
  { selector: 'edge[rel="PartOf"]', style: { 'source-arrow-shape': 'diamond', 'source-arrow-fill': 'hollow' } },
  { selector: 'edge[rel="RelatesTo"]', style: { 'line-style': 'dotted' } },
  { selector: 'edge[rel="DependsOn"]', style: { 'line-style': 'dashed' } },
  { selector: 'edge[rel="covers"]', style: { 'line-style': 'dotted', 'line-color': '#9B7BE0', 'target-arrow-color': '#9B7BE0' } },
  { selector: 'edge[rel="supersedes"]', style: { 'line-color': '#f85149', 'target-arrow-color': '#f85149', 'width': 2 } },
];

const LEGEND = `<b>Form</b> = Familie · <b>Marke</b> = Art · <b>Rand</b> = Konvergenz` +
  ` <span style="color:#2ea043">●</span>Aligned <span style="color:#d29922">●</span>Pending` +
  ` <span style="color:#f85149">●</span>Diverged <span style="color:#a371f7">●</span>Orphaned`;

export function renderDiagram(el, store, actions) {
  el.classList.add('dia-host');
  const view = store.get().diagramView || 'architecture';
  const arm = store.get().armRel;
  const hint = (isFormal(view) ? FORMAL_VIEWS : VIEWS).find(x => x[0] === view);

  const vbtn = ([v, l]) => `<button class="dia-v${view === v ? ' on' : ''}" data-v="${v}">${l}</button>`;
  el.innerHTML = `
    <div class="dia-bar">
      ${VIEWS.map(vbtn).join('')}
      <span class="dia-sep"></span>
      ${FORMAL_VIEWS.map(vbtn).join('')}
      <span class="dia-hint">${escapeHtml(hint ? hint[2] : '')}</span>
      ${isFormal(view) ? '' : `<span class="dia-legend">${LEGEND}</span>`}
    </div>
    ${arm ? `<div class="dia-arm">Relation <b>${escapeHtml(arm.rel)}</b> — Quelle wählen → Ziel klicken · <button data-disarm>abbrechen (Esc)</button></div>` : ''}
    <div class="dia-stage">
      ${isFormal(view) ? '' : '<div class="dia-palette" id="dia-palette"></div>'}
      <div class="dia-canvas" id="dia-cy"></div>
    </div>`;

  el.querySelectorAll('.dia-v').forEach(b => b.onclick = () => { store.set({ diagramView: b.dataset.v, armRel: null }); renderDiagram(el, store, actions); });
  const disarm = el.querySelector('[data-disarm]');
  if (disarm) disarm.onclick = () => { store.set({ armRel: null }); renderDiagram(el, store, actions); };

  const host = el.querySelector('#dia-cy');

  // ── Formal-Sicht: dasselbe Modell als „code behind" (KaTeX), kein Cytoscape ──
  if (isFormal(view)) { renderFormal(host, store, actions, view.replace('formal-', '')); return; }

  // ── Graph-Sicht: EA-Toolbox + Cytoscape ──
  renderPalette(el.querySelector('#dia-palette'), store, actions);

  const { els, layout, count } = build(store, view);
  const Cy = window.cytoscape;
  if (!Cy) { host.innerHTML = `<div class="dia-list muted">Cytoscape nicht geladen — ${count} Knoten in dieser Ansicht.</div>`; return; }
  if (!count) { host.innerHTML = `<div class="dia-list muted">Keine Knoten in der Ansicht „${escapeHtml(view)}".</div>`; return; }

  const cy = Cy({ container: host, elements: els, style: STYLE, layout, wheelSensitivity: 0.25, minZoom: 0.2, maxZoom: 3 });

  // Relation „bewaffnet" → zwei-Klick (Quelle, Ziel); sonst Klick öffnet den Knoten.
  if (arm) {
    host.style.cursor = 'crosshair';
    let src = null;
    cy.on('tap', 'node', e => {
      const id = e.target.id(); const n = store.get().byId.get(id);
      if (!src) {
        if (arm.from.includes(kindOf(n))) { src = id; e.target.style({ 'border-color': '#4ea1ff', 'border-width': 4 }); }
        else actions.say && actions.say('system', `✗ ${arm.rel} nicht erlaubt ab ${kindOf(n)} — Quelle muss ${arm.from.join('/')} sein.`);
      } else if (id !== src) {
        actions.addRelation(src, arm, id); store.set({ armRel: null });
      }
    });
  } else {
    cy.on('tap', 'node', e => actions.focusNode(e.target.id()));
  }

  // Container war beim Init evtl. noch 0 px hoch (gerade erst sichtbar) → nachträglich einpassen.
  const fit = () => { try { cy.resize(); cy.fit(undefined, 34); } catch {} };
  requestAnimationFrame(fit); setTimeout(fit, 120); setTimeout(fit, 400);
  const sel = store.get().selected;
  if (sel) { const nd = cy.getElementById(idOf(sel)); if (nd && nd.length) nd.style({ 'border-color': '#007acc', 'border-width': 4 }); }
}
