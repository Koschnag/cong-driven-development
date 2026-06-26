// Properties + Knoten-Detail. EINE Render-Funktion, zwei Einsätze:
//   compact → persistentes rechtes Properties-Dock (folgt `selected`, zerstörungsfrei)
//   full    → die „Inspector"-Linse (Bühne auf 'node')
// Reich: Glyph · Formal-Notation des Knotens (code behind) · alle Felder · Kriterien ·
// ausgehende UND eingehende Relationen · Validate-Befunde · Konvergenz-Bedeutung · Aktionen.
import { idOf, kindOf, convOf, inner, title, summary, fields, refs, KIND_LABEL, glyphSvg, escapeHtml } from './core.js';
import { nodeFormal } from './formal.js';

const CONV_MEANING = {
  Aligned:  'Modell und Code sind synchron.',
  Pending:  'Modell existiert — Code/Test noch nicht abgeleitet.',
  Diverged: 'Implementierung weicht vom Modell ab — der dringendste Befund.',
  Orphaned: 'Code ohne Modell — verwaist, kein Anker im SPOT.',
};

export function renderNodeDetail(el, store, actions, n, opts = {}) {
  if (!n) {
    el.innerHTML = `<div class="muted pad">Kein Knoten gewählt. Klick im Diagramm oder in einer Liste einen Knoten an.</div>`;
    return;
  }
  const compact = !!opts.compact;
  const k = kindOf(n), c = convOf(n), i = inner(n), id = idOf(n);
  const all = store.get().nodes || [];

  // — Felder (kuratiert je Art) + immer die Zusammenfassung —
  const sum = summary(n);
  const flds = fields(n).map(([l, v]) =>
    `<div class="field"><div class="fl">${escapeHtml(l)}</div><div class="fv">${escapeHtml(v)}</div></div>`).join('');
  const crits = (k === 'spec' && Array.isArray(i.Criteria) && i.Criteria.length) ? i.Criteria.map((cr, j) =>
    `<div class="crit"><span class="crit-n">C${j + 1}</span><b>GIVEN</b> ${escapeHtml(cr.Given)} · <b>WHEN</b> ${escapeHtml(cr.When)} · <b>THEN</b> ${escapeHtml(cr.Then)}</div>`).join('') : '';

  // — ausgehende Relationen —
  const out = refs(n);
  const outRels = out.length
    ? out.map(x => `<span class="rel" data-id="${escapeHtml(x.target)}" title="öffnen">${escapeHtml(x.rel)} → <b>${escapeHtml(x.target)}</b></span>`).join('')
    : '<span class="muted">keine</span>';

  // — eingehende Relationen (Dependents): wer zeigt auf diesen Knoten —
  const inc = [];
  all.forEach(m => refs(m).forEach(r => { if (r.target === id) inc.push({ rel: r.rel, source: idOf(m) }); }));
  const inRels = inc.length
    ? inc.map(x => `<span class="rel rel-in" data-id="${escapeHtml(x.source)}" title="öffnen"><b>${escapeHtml(x.source)}</b> ${escapeHtml(x.rel)} →</span>`).join('')
    : '<span class="muted">keine</span>';

  // — Validate-Befunde für genau diesen Knoten —
  const findings = (store.get().validate || []).filter(f => {
    const e = typeof f.EntityId === 'string' ? f.EntityId : f.EntityId?.Item;
    return e === id;
  });
  const findBlock = findings.length ? `<div class="insp-findings">
    <div class="fl">Befunde <span class="group-count">${findings.length}</span></div>
    ${findings.map(f => `<div class="finding ${(f.Severity?.Case || f.Severity || '').toLowerCase()}">
      <span class="dot ${(f.Severity === 'Error' || f.Severity?.Case === 'Error') ? 'Diverged' : 'Pending'}"></span>${escapeHtml(f.Message)}</div>`).join('')}
  </div>` : '';

  el.innerHTML = `<div class="insp${compact ? ' compact' : ''}">
    <div class="insp-head">${glyphSvg(k, 30)}
      <div class="insp-headtext"><h2>${escapeHtml(title(n))}</h2>
        <div class="meta"><span class="kindchip">${KIND_LABEL[k] || k}</span>
          <span class="conv-chip ${c}"><span class="dot ${c}"></span>${c}</span>
          <code>${escapeHtml(id)}</code></div></div></div>

    <div class="insp-formal" title="dieselbe Notation wie die Formal-Sicht (code behind)">${nodeFormal(n, store)}</div>
    <div class="conv-meaning conv-${c}">${escapeHtml(CONV_MEANING[c] || '')}</div>

    ${sum ? `<div class="insp-summary">${escapeHtml(sum)}</div>` : ''}
    ${flds}${crits}

    <div class="field"><div class="fl">Ausgehend</div><div class="fv rels">${outRels}</div></div>
    <div class="field"><div class="fl">Eingehend</div><div class="fv rels">${inRels}</div></div>
    ${findBlock}

    <div class="actions">
      ${compact ? '<button data-act="open">Öffnen</button>' : ''}
      <button data-act="formal">${k === 'invariant' ? '∀ Logik' : 'λ Formal'}</button>
      <button data-act="graph">◈ Graph</button>
      ${k === 'spec' ? '<button data-act="derive">Tests ableiten</button>' : ''}
      <button data-act="ask">🤖 Erklären</button>
    </div></div>`;

  const goNode = (i2) => (actions.focusNode || actions.openNode || actions.select)(i2);
  el.querySelectorAll('.rel').forEach(x => x.onclick = () => goNode(x.dataset.id));
  el.querySelectorAll('.actions button').forEach(b => b.onclick = () => {
    const a = b.dataset.act;
    if (a === 'open') goNode(id);
    else if (a === 'formal') { store.set({ diagramView: k === 'invariant' ? 'formal-logik' : 'formal-typ' }); actions.repaintDiagram && actions.repaintDiagram(); }
    else if (a === 'graph') { store.set({ nodeLens: 'graph' }); actions.rerender && actions.rerender(); }
    else if (a === 'derive') actions.derive();
    else if (a === 'ask') (actions.ask || actions.dispatch)(`Erklär mir den SPOT-Knoten "${id}" (${title(n)}) und seine Rolle im Modell.`);
  });
}

export function renderProperties(el, store, actions) {
  renderNodeDetail(el, store, actions, store.get().selected, { compact: true });
}
