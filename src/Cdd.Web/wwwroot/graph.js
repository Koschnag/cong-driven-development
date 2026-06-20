// Graph-Linse: interaktives Nachbarschafts-Diagramm (Cytoscape, ziehbar wie Enterprise Architect).
// Souverän: lädt Cytoscape dynamisch; offline → Listen-Fallback (kein toter View).
import { idOf, convOf, title, refs, escapeHtml } from './core.js';

let cyLib; // undefined=ungeladen, null=offline, fn=geladen
async function lib() {
  if (cyLib !== undefined) return cyLib;
  if (typeof window !== 'undefined' && window.cytoscape) { cyLib = window.cytoscape; return cyLib; }
  try { cyLib = (await import('https://esm.sh/cytoscape@3.30.2')).default; }
  catch { cyLib = null; }
  return cyLib;
}
const COLOR = { Aligned: '#2ea043', Pending: '#d29922', Diverged: '#f85149', Orphaned: '#a371f7' };

export async function renderGraph(el, store, actions, centerNode) {
  const s = store.get(), nodes = s.nodes, center = centerNode || s.selected;
  el.innerHTML = `<div class="graph-wrap">
    <div class="graph-hint">Nachbarschaft${center ? ' von <code>' + escapeHtml(idOf(center)) + '</code>' : ' (Ausschnitt)'} — ziehbar, Klick öffnet den Knoten.</div>
    <div id="cy"></div></div>`;
  const cy = el.querySelector('#cy');
  const byId = new Map(nodes.map(n => [idOf(n), n]));

  // Nachbarschaft bestimmen
  let ids;
  if (center) {
    const cid = idOf(center); ids = new Set([cid]);
    refs(center).forEach(r => ids.add(r.target));
    nodes.forEach(n => { if (refs(n).some(r => r.target === cid)) ids.add(idOf(n)); });
  } else ids = new Set(nodes.slice(0, 60).map(idOf));
  const sub = [...ids].map(id => byId.get(id)).filter(Boolean);

  const Cy = await lib();
  if (!Cy) { // Offline-Fallback
    cy.outerHTML = `<div>${sub.map(n =>
      `<div class="node-row" data-id="${escapeHtml(idOf(n))}"><span class="dot ${convOf(n)}"></span><span class="id">${escapeHtml(idOf(n))}</span><span class="sm">${escapeHtml(title(n))}</span></div>`).join('')}</div>`;
    el.querySelectorAll('.node-row').forEach(r => r.onclick = () => (actions.focusNode || actions.select)(r.dataset.id));
    return;
  }
  const els = [];
  sub.forEach(n => els.push({ data: { id: idOf(n), label: idOf(n), c: COLOR[convOf(n)] || '#888' } }));
  sub.forEach(n => refs(n).forEach(r => { if (ids.has(r.target)) els.push({ data: { source: idOf(n), target: r.target, label: r.rel } }); }));
  const inst = Cy({
    container: cy, elements: els,
    style: [
      { selector: 'node', style: { 'background-color': 'data(c)', 'label': 'data(label)', 'color': '#9aa7b4', 'font-size': '9px', 'text-valign': 'bottom', 'text-margin-y': '4px', 'width': '20px', 'height': '20px' } },
      { selector: 'edge', style: { 'line-color': '#3a4656', 'target-arrow-color': '#3a4656', 'target-arrow-shape': 'triangle', 'curve-style': 'bezier', 'width': 1, 'label': 'data(label)', 'font-size': '7px', 'color': '#6b7785' } },
      { selector: 'node[id = "' + (center ? idOf(center) : '__none__') + '"]', style: { 'border-width': 3, 'border-color': '#4ea1ff' } },
    ],
    layout: { name: 'cose', animate: false, padding: 24 },
  });
  inst.on('tap', 'node', evt => (actions.focusNode || actions.select)(evt.target.id()));
  const fit = () => { try { inst.resize(); inst.fit(undefined, 24); } catch {} };
  requestAnimationFrame(fit); setTimeout(fit, 120); setTimeout(fit, 400);
}
