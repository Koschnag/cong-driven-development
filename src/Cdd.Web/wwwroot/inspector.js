// Inspector-Linse: ein SPOT-Knoten im Detail (EA/MagicDraw-Properties, nur klarer).
import { idOf, kindOf, convOf, inner, title, fields, refs, KIND_LABEL, escapeHtml } from './core.js';

export function renderInspector(el, store, actions) {
  const n = store.get().selected;
  if (!n) {
    el.innerHTML = `<div class="muted">Kein Knoten gewählt. Tippe eine Id in die Omnibox, oder öffne <b>Cube</b>/<b>Graph</b> und klick dich rein.</div>`;
    return;
  }
  const k = kindOf(n), c = convOf(n), i = inner(n);
  const flds = fields(n).map(([l, v]) =>
    `<div class="field"><div class="fl">${escapeHtml(l)}</div><div class="fv">${escapeHtml(v)}</div></div>`).join('');
  const crits = (k === 'spec' && i.Criteria) ? i.Criteria.map(cr =>
    `<div class="crit">GIVEN ${escapeHtml(cr.Given)} · WHEN ${escapeHtml(cr.When)} · THEN ${escapeHtml(cr.Then)}</div>`).join('') : '';
  const r = refs(n);
  const rels = r.length
    ? r.map(x => `<span class="rel" data-id="${escapeHtml(x.target)}">${escapeHtml(x.rel)} → ${escapeHtml(x.target)}</span>`).join('')
    : '<span class="muted">keine</span>';

  el.innerHTML = `<div class="insp">
    <h2>${escapeHtml(title(n))}</h2>
    <div class="meta">
      <span class="kindchip">${KIND_LABEL[k] || k}</span>
      <span><span class="dot ${c}"></span> ${c}</span>
      <code style="color:var(--fg3)">${escapeHtml(idOf(n))}</code>
    </div>
    ${flds}${crits}
    <div class="field"><div class="fl">Beziehungen</div><div class="fv">${rels}</div></div>
    <div class="actions">
      <button data-act="graph">◈ Im Graph zeigen</button>
      ${k === 'spec' ? '<button data-act="derive">Tests ableiten</button>' : ''}
      <button data-act="ask">🤖 Erklär mir das</button>
    </div></div>`;

  el.querySelectorAll('.rel').forEach(r => r.onclick = () => actions.select(r.dataset.id));
  el.querySelectorAll('.actions button').forEach(b => b.onclick = () => {
    const a = b.dataset.act;
    if (a === 'graph') actions.setLens('graph');
    else if (a === 'derive') actions.derive();
    else if (a === 'ask') actions.dispatch(`Erklär mir den SPOT-Knoten "${idOf(n)}" (${title(n)}) und seine Rolle im Modell.`);
  });
}
