// Cube-Linse: OLAP-Pivot über den SPOT (Slice/Dice/Drill). Knotenart × Konvergenz, frei pivotierbar.
import { idOf, kindOf, convOf, summary, KINDS, CONV, KIND_LABEL, escapeHtml } from './core.js';

export function renderCube(el, store, actions) {
  const s = store.get(), nodes = s.nodes;
  const rowDim = s.cubeRows || 'kind', colDim = s.cubeCols || 'conv';
  const colVals = colDim === 'kind' ? KINDS : CONV;
  const rowValsAll = rowDim === 'kind' ? KINDS : CONV;
  const dimOf = (n, d) => d === 'kind' ? kindOf(n) : convOf(n);
  const lbl = (d, v) => d === 'kind' ? (KIND_LABEL[v] || v) : v;

  const cnt = {};
  nodes.forEach(n => { const r = dimOf(n, rowDim), c = dimOf(n, colDim); (cnt[r] = cnt[r] || {})[c] = (cnt[r][c] || 0) + 1; });
  const rows = rowValsAll.filter(r => cnt[r]);

  const head = `<tr><th></th>${colVals.map(c => `<th>${escapeHtml(lbl(colDim, c))}</th>`).join('')}<th>Σ</th></tr>`;
  const body = rows.map(r => {
    const tot = colVals.reduce((a, c) => a + (cnt[r]?.[c] || 0), 0);
    return `<tr><th>${escapeHtml(lbl(rowDim, r))}</th>${colVals.map(c => {
      const v = cnt[r]?.[c] || 0;
      return `<td class="cell${v ? '' : ' zero'}" data-r="${r}" data-c="${c}">${v || '·'}</td>`;
    }).join('')}<td><b>${tot}</b></td></tr>`;
  }).join('');

  el.innerHTML = `
    <div class="cube-ctrl">OLAP-Cube · Zeilen
      <select id="cu-r"><option value="kind"${rowDim === 'kind' ? ' selected' : ''}>Knotenart</option><option value="conv"${rowDim === 'conv' ? ' selected' : ''}>Konvergenz</option></select>
      Spalten <select id="cu-c"><option value="conv"${colDim === 'conv' ? ' selected' : ''}>Konvergenz</option><option value="kind"${colDim === 'kind' ? ' selected' : ''}>Knotenart</option></select>
      <span class="muted">— Zelle anklicken zum Drill-Down</span></div>
    <table class="cube"><thead>${head}</thead><tbody>${body}</tbody></table>
    <div class="cube-drill" id="cu-drill"><div class="muted">Wähle eine Zelle.</div></div>`;

  el.querySelector('#cu-r').onchange = e => { store.set({ cubeRows: e.target.value }); actions.rerender(); };
  el.querySelector('#cu-c').onchange = e => { store.set({ cubeCols: e.target.value }); actions.rerender(); };
  el.querySelectorAll('td.cell').forEach(td => td.onclick = () => {
    const hit = nodes.filter(n => dimOf(n, rowDim) === td.dataset.r && dimOf(n, colDim) === td.dataset.c);
    const dr = el.querySelector('#cu-drill');
    dr.innerHTML = `<h3 style="color:var(--fg2)">${lbl(rowDim, td.dataset.r)} × ${lbl(colDim, td.dataset.c)} — ${hit.length} Knoten</h3>` +
      hit.map(n => `<div class="node-row" data-id="${escapeHtml(idOf(n))}"><span class="dot ${convOf(n)}"></span><span class="id">${escapeHtml(idOf(n))}</span><span class="sm">${escapeHtml(summary(n))}</span></div>`).join('');
    const go = actions.focusNode || actions.openNode || actions.select;
    dr.querySelectorAll('.node-row').forEach(r => { r.onclick = () => go(r.dataset.id); });
  });
}
