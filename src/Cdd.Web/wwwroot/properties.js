// Properties + Knoten-Detail. EINE Render-Funktion, zwei Einsätze:
//   compact → persistentes rechtes Properties-Dock (folgt `selected`, zerstörungsfrei)
//   full    → die „Inspector"-Linse im Dokument-Well
import { idOf, kindOf, convOf, inner, title, fields, refs, KIND_LABEL, escapeHtml } from './core.js';

export function renderNodeDetail(el, store, actions, n, opts = {}) {
  if (!n) {
    el.innerHTML = `<div class="muted pad">Kein Knoten gewählt. Klick links im <b>Solution Explorer</b> einen Knoten an (1× = Properties, 2× = öffnen).</div>`;
    return;
  }
  const compact = !!opts.compact;
  const k = kindOf(n), c = convOf(n), i = inner(n);
  const flds = fields(n).map(([l, v]) =>
    `<div class="field"><div class="fl">${escapeHtml(l)}</div><div class="fv">${escapeHtml(v)}</div></div>`).join('');
  const crits = (k === 'spec' && Array.isArray(i.Criteria)) ? i.Criteria.map(cr =>
    `<div class="crit"><b>GIVEN</b> ${escapeHtml(cr.Given)}<br><b>WHEN</b> ${escapeHtml(cr.When)}<br><b>THEN</b> ${escapeHtml(cr.Then)}</div>`).join('') : '';
  const r = refs(n);
  const rels = r.length
    ? r.map(x => `<span class="rel" data-id="${escapeHtml(x.target)}" title="2× öffnet den Knoten">${escapeHtml(x.rel)} → ${escapeHtml(x.target)}</span>`).join('')
    : '<span class="muted">keine</span>';

  el.innerHTML = `<div class="insp${compact ? ' compact' : ''}">
    <h2>${escapeHtml(title(n))}</h2>
    <div class="meta">
      <span class="kindchip">${KIND_LABEL[k] || k}</span>
      <span><span class="dot ${c}"></span> ${c}</span>
      <code>${escapeHtml(idOf(n))}</code>
    </div>
    ${flds}${crits}
    <div class="field"><div class="fl">Beziehungen</div><div class="fv rels">${rels}</div></div>
    <div class="actions">
      ${compact ? '<button data-act="open">Im Dokument öffnen</button>' : ''}
      <button data-act="graph">◈ Graph</button>
      ${k === 'spec' ? '<button data-act="derive">Tests ableiten</button>' : ''}
      <button data-act="ask">🤖 Erklären</button>
    </div></div>`;

  // Navigation eines Knotens = Bühne auf diesen Knoten legen (chat-primär).
  // Fallbacks halten die Kompatibilität zur alten Workbench-API.
  const goNode = (id) => (actions.focusNode || actions.openNode || actions.select)(id);
  el.querySelectorAll('.rel').forEach(x => {
    x.onclick = () => goNode(x.dataset.id);
  });
  el.querySelectorAll('.actions button').forEach(b => b.onclick = () => {
    const a = b.dataset.act;
    if (a === 'open') goNode(idOf(n));
    else if (a === 'graph') { store.set({ nodeLens: 'graph' }); actions.rerender && actions.rerender(); }
    else if (a === 'derive') actions.derive();
    else if (a === 'ask') (actions.ask || actions.dispatch)(`Erklär mir den SPOT-Knoten "${idOf(n)}" (${title(n)}) und seine Rolle im Modell.`);
  });
}

export function renderProperties(el, store, actions) {
  renderNodeDetail(el, store, actions, store.get().selected, { compact: true });
}
