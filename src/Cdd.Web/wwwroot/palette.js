// EA/MagicDraw-Toolbox, an die Diagrammfläche angedockt. Zwei Gruppen, beide DATA-DRIVEN aus core.js:
//   Knoten     — die 11 Arten in den 4 Symbol-Familien (Klick = anlegen, Shift-Klick = via Engine).
//   Relationen — die 5 ECHT kantentragenden Felder (keine freien Pfeile). „bewaffnen", dann Quelle→Ziel.
// Ehrlich: One-Click erzeugt eine valide minimale SpotEntry mit Convergence:Pending (der wahre Stub-
// Zustand), via PUT /api/spot/{id} — kein LLM, null Modell-Risiko. Premise→Decision: grau „geplant".
import { KINDS, KIND_LABEL, FAMILY, PREFIX, RELATIONS, glyphSvg } from './core.js';

const FAMILIES = [
  ['Classifier', 'authored'],
  ['Judgement',  '«constraint»'],
  ['Artifact',   '«artifact»'],
  ['Derivation', 'Ableitung'],
];

export function renderPalette(el, store, actions) {
  if (!el) return;
  const arm = store.get().armRel;

  const kindBtn = (k) =>
    `<button class="pkind" data-kind="${k}" title="${KIND_LABEL[k] || k} anlegen (Id-Präfix ${PREFIX[k]}…) · Shift-Klick = via Engine mit Inhalt">
       ${glyphSvg(k, 20)}<span class="pk-lbl">${KIND_LABEL[k] || k}</span></button>`;

  const families = FAMILIES.map(([fam, gloss]) => {
    const ks = KINDS.filter(k => FAMILY[k] === fam);
    return `<div class="pfam"><div class="pfam-h">${fam}<span>${gloss}</span></div>${ks.map(kindBtn).join('')}</div>`;
  }).join('');

  const relBtn = (r) =>
    `<button class="prel${arm && arm.rel === r.rel ? ' armed' : ''}" data-rel="${r.rel}" title="ab ${r.from.join('/')}">
       <span class="pr-g">${r.glyph}</span><span class="pr-lbl">${r.rel}</span><span class="pr-from">${r.from.join('/')}</span></button>`;

  el.innerHTML = `
    <div class="pal-h">Toolbox</div>
    <div class="pal-sec">${families}</div>
    <div class="pal-h">Relationen</div>
    <div class="pal-sec pal-rels">${RELATIONS.map(relBtn).join('')}
      <div class="prel planned" title="kein Feld trägt diese Kante — geplant">
        <span class="pr-g">∵</span><span class="pr-lbl">Premise→Decision</span><span class="pr-from">geplant</span></div>
    </div>`;

  el.querySelectorAll('.pkind').forEach(b => b.onclick = (e) => {
    const k = b.dataset.kind;
    if (e.shiftKey) actions.askNew(k); else actions.newNode(k);
  });
  el.querySelectorAll('.prel[data-rel]').forEach(b => b.onclick = () => {
    const r = RELATIONS.find(x => x.rel === b.dataset.rel);
    store.set({ armRel: arm && arm.rel === r.rel ? null : r });
    actions.repaintDiagram();
  });
}
