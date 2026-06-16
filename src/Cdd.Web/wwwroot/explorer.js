// Solution Explorer: der persistente, immer sichtbare Knoten-Baum (das fehlende Browse-Affordance).
// 1× Klick = select (Properties), 2× Klick = als Dokument öffnen. Pivot Art/Konvergenz/Fläche.
import { KIND_LABEL, escapeHtml } from './core.js';
import { groupTree } from './store.js';

const PIVOTS = [['kind', 'Art'], ['conv', 'Konvergenz'], ['surface', 'Fläche']];
const GROUP_LABEL = (key, pivot) => pivot === 'kind' ? (KIND_LABEL[key] || key)
  : pivot === 'surface' ? key.charAt(0).toUpperCase() + key.slice(1) : key;
const esc = (s) => escapeHtml(s).replace(/"/g, '&quot;');

export function renderExplorer(el, store, actions) {
  const s = store.get();
  el.innerHTML = `
    <div class="ex-head">
      <span class="ex-title">Solution Explorer</span>
      <select class="ex-pivot" title="Gruppieren nach">
        ${PIVOTS.map(([v, l]) => `<option value="${v}"${s.treePivot === v ? ' selected' : ''}>${l}</option>`).join('')}
      </select>
    </div>
    <input class="ex-filter" placeholder="filtern… (id / titel)" value="${esc(s.treeFilter)}" spellcheck="false">
    <div class="ex-tree"></div>`;

  const treeEl = el.querySelector('.ex-tree');
  const filterEl = el.querySelector('.ex-filter');

  function paintTree() {
    const st = store.get();
    const groups = groupTree(store);
    if (!groups.length) { treeEl.innerHTML = '<div class="muted pad">keine Treffer</div>'; return; }
    treeEl.innerHTML = groups.map(g => {
      const open = !st.collapsed.has(g.key);
      const leaves = open ? g.leaves.map(lf =>
        `<div class="leaf${st.active === 'node:' + lf.id ? ' active' : ''}${st.selected && idOfSel(st) === lf.id ? ' sel' : ''}" data-id="${esc(lf.id)}" title="${esc(lf.title || lf.id)}">
           <span class="dot ${lf.conv}"></span><span class="leaf-id">${escapeHtml(lf.id)}</span></div>`).join('') : '';
      return `<div class="group">
        <div class="group-head" data-g="${esc(g.key)}"><span class="caret">${open ? '▾' : '▸'}</span>
          <span class="group-label">${escapeHtml(GROUP_LABEL(g.key, st.treePivot))}</span><span class="group-count">${g.count}</span></div>
        <div class="group-leaves">${leaves}</div></div>`;
    }).join('');
    treeEl.querySelectorAll('.group-head').forEach(h => h.onclick = () => actions.toggleGroup(h.dataset.g));
    treeEl.querySelectorAll('.leaf').forEach(lf => {
      lf.onclick = () => actions.select(lf.dataset.id);
      lf.ondblclick = () => actions.openNode(lf.dataset.id);
    });
  }

  el.querySelector('.ex-pivot').onchange = (e) => actions.setPivot(e.target.value);
  filterEl.oninput = () => { store.set({ treeFilter: filterEl.value }); paintTree(); };
  paintTree();
}

const idOfSel = (st) => st.selected && (typeof st.selected.Id === 'string' ? st.selected.Id : '');
