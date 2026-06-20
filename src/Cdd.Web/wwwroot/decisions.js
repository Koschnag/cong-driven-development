// Entscheidungen (ADR-Stil) + Prämissen — die getypten SPOT-Knoten sichtbar gemacht.
// Kein Prosa-Wiki: Kontext · Wahl · Konsequenzen + Ablöse-Lineage (Supersedes, beidseitig).
import { idOf, kindOf, convOf, inner, title, escapeHtml } from './core.js';

export function renderDecisions(el, store, actions) {
  const nodes = store.get().nodes;
  const decisions = nodes.filter(n => kindOf(n) === 'decision');
  const premises = nodes.filter(n => kindOf(n) === 'premise');
  // inverse Supersedes: welche Entscheidung löst X ab?
  const supersededBy = new Map();
  decisions.forEach(d => { const sup = inner(d).Supersedes; if (sup) supersededBy.set(sup, idOf(d)); });

  const adr = (d) => {
    const i = inner(d), id = idOf(d), c = convOf(d);
    const replacedBy = supersededBy.get(id);
    const status = replacedBy
      ? `<span class="adr-status old">abgelöst von <code class="xref" data-id="${escapeHtml(replacedBy)}">${escapeHtml(replacedBy)}</code></span>`
      : `<span class="adr-status active">aktiv</span>`;
    const f = (label, v) => v ? `<div class="adr-f"><span>${label}</span>${escapeHtml(v)}</div>` : '';
    return `<div class="adr">
      <div class="adr-head"><span class="dot ${c}"></span><b>${escapeHtml(i.Title || id)}</b><code class="adr-id">${escapeHtml(id)}</code>${status}</div>
      ${f('Kontext', i.Context)}${f('Entscheidung', i.Choice)}${f('Konsequenzen', i.Consequences)}
      ${i.Supersedes ? `<div class="adr-f"><span>Löst ab</span><code class="xref" data-id="${escapeHtml(i.Supersedes)}">${escapeHtml(i.Supersedes)}</code></div>` : ''}
    </div>`;
  };
  const prem = (p) => {
    const i = inner(p);
    return `<div class="node-row" data-id="${escapeHtml(idOf(p))}"><span class="dot ${convOf(p)}"></span><span class="id">${escapeHtml(idOf(p))}</span><span class="sm">${escapeHtml(i.Statement || title(p))}</span></div>`;
  };

  el.innerHTML =
    `<div class="stage-hint">Entscheidungen (ADR) — Kontext · Wahl · Konsequenzen + Ablöse-Lineage. Getypte Knoten, kein Prosa-Wiki.</div>` +
    (decisions.length ? decisions.map(adr).join('') : '<div class="rail-empty pad">— keine Entscheidungen —</div>') +
    `<div class="stage-hint">Prämissen — worauf die Entscheidungen aufsetzen.</div>` +
    (premises.length ? premises.map(prem).join('') : '<div class="rail-empty pad">— keine Prämissen —</div>');

  el.querySelectorAll('.xref').forEach(x => x.onclick = () => actions.focusNode(x.dataset.id));
  el.querySelectorAll('.node-row').forEach(r => r.onclick = () => actions.focusNode(r.dataset.id));
}
