// „Code behind" — dasselbe SPOT-Modell in formaler Notation. Drei EHRLICHE Sichten:
//   typ   — Curry-Howard: Spec = Typ, Test = Bewohner, Konvergenz WIRD GELESEN ALS Typurteil.
//   logik — die Invarianten als prädikatenlogische Sätze; validate = 𝔐 ⊨ Φ.
//   kat   — die FREIE Kategorie auf DependsOn + die Preorder auf IsA (per Konstruktion, kein Gesetz).
//
// RIGOR (Congs Standard): die Notation ist eine MOTIVIERTE Beschriftung, KEIN Beweis. Jede Sicht
// trägt ihren Caveat sichtbar. λ-Kalkül ist als eigene Sicht VERWORFEN (dekorativ) — nur Fußnote.
import { idOf, kindOf, convOf, inner, title, refs, escapeHtml } from './core.js';

// KaTeX, lokal gevendort (window.katex). Offline → lesbarer Monospace-Fallback (kein toter View).
function K(tex, display = false) {
  if (window.katex) { try { return window.katex.renderToString(tex, { displayMode: display, throwOnError: false }); } catch {} }
  return `<code class="tex-fallback">${escapeHtml(tex)}</code>`;
}
const tt = (s) => `\\texttt{${String(s).replace(/[\\{}_#%&$]/g, m => '\\' + m)}}`;

export function renderFormal(el, store, actions, mode) {
  el.classList.add('formal-host');
  const nodes = store.get().nodes;
  if (mode === 'logik') return renderLogik(el, store, actions, nodes);
  if (mode === 'kat')   return renderKat(el, store, actions, nodes);
  return renderTyp(el, store, actions, nodes);
}

const wire = (el, actions) => el.querySelectorAll('[data-id]').forEach(b => b.onclick = () => actions.focusNode(b.dataset.id));

/* ── Typentheorie / Curry-Howard — deine These als Notation ──────────────── */
function renderTyp(el, store, actions, nodes) {
  const specs = nodes.filter(n => kindOf(n) === 'spec');
  const comps = nodes.filter(n => kindOf(n) === 'component');
  const testsOf = (sid) => nodes.filter(n => kindOf(n) === 'test' && (inner(n).SpecRef === sid));

  const judgment = (n) => {
    const c = convOf(n), ts = testsOf(idOf(n));
    const t = ts.length ? `t_{${escapeHtml(idOf(ts[0])).replace(/[^a-zA-Z0-9]/g, '')}}` : 't';
    const T = `\\llbracket ${tt(idOf(n))} \\rrbracket`;
    if (c === 'Aligned')  return K(`\\Gamma \\vdash ${t} : ${T}`, false);
    if (c === 'Pending')  return K(`{?} : ${T}`, false) + ' <span class="fj-note">offenes Loch — Typ deklariert, kein Zeuge</span>';
    if (c === 'Diverged') return K(`${t} : ${T} \\;\\rightsquigarrow\\; \\bot`, false) + ' <span class="fj-note bad">Zeuge bewohnt den Typ nicht</span>';
    return K(`${t} : {?} \\;\\;(\\notin \\Gamma)`, false) + ' <span class="fj-note">Code ohne deklarierten Typ</span>';
  };

  const specBlock = (n) => {
    const i = inner(n), crit = i.Criteria || [];
    const typeDef = crit.length
      ? `\\llbracket ${tt(idOf(n))} \\rrbracket \\;:=\\; ` + crit.map((_, k) => `C_{${k + 1}}`).join(' \\times ')
      : `\\llbracket ${tt(idOf(n))} \\rrbracket \\;:=\\; \\top \\quad (\\text{keine Kriterien})`;
    const critList = crit.map((c, k) =>
      `<div class="fj-crit">${K(`C_{${k + 1}} = G_{${k + 1}} \\times W_{${k + 1}} \\to T_{${k + 1}}`)}` +
      `<span class="fj-gwt"><b>G:</b> ${escapeHtml(c.Given || '')} · <b>W:</b> ${escapeHtml(c.When || '')} · <b>T:</b> ${escapeHtml(c.Then || '')}</span></div>`).join('');
    return `<div class="formal-node conv-${convOf(n)}">
        <div class="fn-head"><button class="fn-id" data-id="${escapeHtml(idOf(n))}">${escapeHtml(idOf(n))}</button>
          <span class="fn-title">${escapeHtml(title(n))}</span><span class="conv-chip ${convOf(n)}">${convOf(n)}</span></div>
        <div class="fn-type">${K(typeDef, true)}</div>
        ${critList ? `<div class="fj-crits">${critList}</div>` : ''}
        <div class="fn-judge">${judgment(n)}</div>
      </div>`;
  };

  const compBlock = (n) => {
    const deps = (inner(n).DependsOn || []);
    if (!deps.length) return '';
    const ctx = deps.map(d => `\\llbracket ${tt(d)} \\rrbracket`).join(',\\; ');
    return `<div class="formal-line"><button class="fn-id" data-id="${escapeHtml(idOf(n))}">${escapeHtml(idOf(n))}</button>` +
      K(`${ctx} \\;\\vdash\\; \\llbracket ${tt(idOf(n))} \\rrbracket`) + `</div>`;
  };

  el.innerHTML = `<div class="formal-wrap">
    <div class="formal-caveat"><b>Lesart, kein Beweis.</b> Bewohnbarkeit entscheidet ein <i>externes Orakel</i>
      (Testlauf / Agent) — „typecheckt" ist ein aus Evidenz <i>assertiertes</i> Urteil, kein syntaktisch
      entscheidbarer Check. Nur der Lean-bewiesene kritische Kern wäre echtes mechanisiertes Curry-Howard.
      Konvergenz <i>wird interpretiert als</i> Typurteilsstatus (sie ist im Modell ein extern gesetztes Label).</div>
    <div class="formal-legend">
      ${K('\\textsf{Aligned}:\\;\\Gamma \\vdash t : \\llbracket S \\rrbracket')} ·
      ${K('\\textsf{Pending}:\\;{?}:\\llbracket S \\rrbracket')} ·
      ${K('\\textsf{Diverged}:\\;t:\\llbracket S \\rrbracket \\rightsquigarrow \\bot')} ·
      ${K('\\textsf{Orphaned}:\\;t:{?}\\,\\notin\\Gamma')}
    </div>
    <h3 class="formal-h">Specs als Typen <span class="muted">— der Vertrag als Proposition, der Test als Bewohner</span></h3>
    ${specs.length ? specs.map(specBlock).join('') : '<div class="muted pad">keine Specs</div>'}
    <h3 class="formal-h">Komponenten als Kontext <span class="muted">— DependsOn ⟶ Kontextannahme</span></h3>
    <div class="formal-ctx">${comps.map(compBlock).join('') || '<div class="muted pad">keine Abhängigkeiten</div>'}</div>
    <div class="formal-foot"><span class="tag">Motivation / Analogie · kein Beweis</span>
      λ-Kalkül ist als eigene Sicht <b>verworfen</b> (kein β-Redex, keine Bindung im Graphen). Einzig tragfähig
      als Bild: den DependsOn-DAG aufzulösen ähnelt dem Normalisieren geschachtelter λ-Abstraktionen
      ${K('\\lambda b.\\,\\lambda c.\\,A(b,c)')} — und ein Zyklus entspräche ${K('\\Omega = (\\lambda x.\\,xx)(\\lambda x.\\,xx)')} ohne Normalform.</div>
  </div>`;
  wire(el, actions);
}

/* ── Prädikatenlogik — die Invarianten sind ihre wahre Heimat ─────────────── */
const INV_FO = {
  SpecsNeedTests: '\\forall s\\,\\big(\\mathsf{Spec}(s) \\rightarrow \\exists t\\,(\\mathsf{Test}(t) \\wedge \\mathsf{Covers}(t,s))\\big)',
  CriticalRisksNeedMitigation: '\\forall r\\,\\big(\\mathsf{Risk}(r) \\wedge \\mathsf{Impact}(r){=}\\mathsf{Critical} \\rightarrow \\exists m\\,\\mathsf{Mitigates}(m,r)\\big)',
  TermsNeedDefinition: '\\forall x\\,\\big(\\mathsf{Term}(x) \\rightarrow \\mathsf{Defined}(x)\\big)',
};
function renderLogik(el, store, actions, nodes) {
  const invs = nodes.filter(n => kindOf(n) === 'invariant');
  const findings = store.get().validate || [];
  const N = nodes.length;

  const block = (n) => {
    const i = inner(n);
    const rule = i.Rule?.Case ?? i.Rule;              // beide Encodings: nackter String ODER {Case,Fields}
    const desc = i.Description || '';
    let fo = INV_FO[rule];
    if (rule === 'IdPrefix') {
      const f = i.Rule?.Fields || {};
      fo = `\\forall x\\,\\big(\\mathsf{Kind}(x){=}${tt(f.kind || '?')} \\rightarrow \\mathsf{prefix}(\\mathrm{id}(x)){=}${tt(f.prefix || '?')}\\big)`;
    }
    if (!fo) fo = `\\textsf{${escapeHtml(rule || 'Regel')}}`;
    // Verletzungen: validate liefert „Invariante verletzt (<desc>): …" mit EntityId als Zeuge.
    const viol = findings.filter(x => typeof x.Message === 'string' && x.Message.includes(`Invariante verletzt (${desc})`));
    const ok = viol.length === 0;
    const witnesses = viol.map(x => `<button class="witness" data-id="${escapeHtml(x.EntityId)}">${escapeHtml(x.EntityId)}</button>`).join(' ');
    return `<div class="formal-node ${ok ? 'sat' : 'unsat'}">
        <div class="fn-head"><button class="fn-id" data-id="${escapeHtml(idOf(n))}">${escapeHtml(idOf(n))}</button>
          <span class="fn-title">${escapeHtml(desc)}</span></div>
        <div class="fn-type">${K('\\Phi \\;\\equiv\\; ' + fo, true)}</div>
        <div class="fn-judge">${ok
          ? K('\\mathfrak{M} \\models \\Phi') + ' <span class="fj-note good">erfüllt</span>'
          : K('\\mathfrak{M} \\not\\models \\Phi') + ` <span class="fj-note bad">Gegenzeuge:</span> ${witnesses}`}</div>
      </div>`;
  };

  el.innerHTML = `<div class="formal-wrap">
    <div class="formal-caveat"><b>Endliche Struktur — entscheidbar.</b> Der SPOT-Graph ist eine endliche
      FO-Struktur ${K('\\mathfrak{M}')} (Domäne = die ${N} Knoten; die Art-Prädikate partitionieren sie).
      ${K('\\forall/\\exists')} über endlicher Domäne sind <i>entscheidbare beschränkte Quantoren</i> —
      Gödel/Tarski gelten hier <b>nicht</b> und werden nicht angeführt. <code>cdd validate</code> berechnet
      ${K('\\mathfrak{M} \\models \\Phi')}; eine Verletzung <i>ist</i> der falsifizierende Zeuge.
      Defined/Critical/Mitigates sind fixierte Interpretationen aus Knotenfeldern, keine Theoreme.</div>
    <h3 class="formal-h">Invarianten als Sätze <span class="muted">— Governance by Invariance</span></h3>
    ${invs.length ? invs.map(block).join('') : '<div class="muted pad">keine Invarianten im Modell</div>'}
  </div>`;
  wire(el, actions);
}

/* ── Kategorien — NUR die freie Kategorie / Preorder, präzise benannt ─────── */
function renderKat(el, store, actions, nodes) {
  const comps = nodes.filter(n => kindOf(n) === 'component');
  const terms = nodes.filter(n => kindOf(n) === 'term');
  // Generatoren (DependsOn) + ein Kompositions-Beispiel (2-Pfad).
  const gens = [];
  comps.forEach(c => (inner(c).DependsOn || []).forEach(d => gens.push([idOf(c), d])));
  const depMap = new Map(comps.map(c => [idOf(c), (inner(c).DependsOn || [])]));
  let compoTex = '';
  for (const [a, b] of gens) {
    const bs = depMap.get(b) || [];
    if (bs.length) { compoTex = `${tt(b)}{\\to}${tt(bs[0])} \\;\\circ\\; ${tt(a)}{\\to}${tt(b)} \\;=\\; ${tt(a)} \\to ${tt(bs[0])}`; break; }
  }
  // IsA-Preorder über Begriffe.
  const isa = [];
  terms.forEach(t => refs(t).forEach(r => { if (r.rel === 'IsA') isa.push([idOf(t), r.target]); }));

  const genList = gens.slice(0, 24).map(([a, b]) => `<span class="kat-mor">${K(`${tt(a)} \\to ${tt(b)}`)}</span>`).join('');
  const isaList = isa.slice(0, 24).map(([a, b]) => `<span class="kat-mor">${K(`${tt(a)} \\sqsubseteq ${tt(b)}`)}</span>`).join('');

  el.innerHTML = `<div class="formal-wrap">
    <div class="formal-caveat"><b>Freie Kategorie — kein entdecktes Gesetz.</b> Dies ist die <i>freie</i> Kategorie
      auf dem DependsOn-Graph (bzw. die Preorder auf IsA): Komposition &amp; Identität gelten <i>per Konstruktion</i>,
      sie vermitteln nur transitive Erreichbarkeit. Cross-Kind-Relationen (covers, RelatesTo …) sind <b>keine</b>
      Morphismen und werden weggelassen. Niemals: „der SPOT-Graph <i>ist</i> eine Kategorie".</div>
    <h3 class="formal-h">${K('\\mathcal{C}_{\\mathrm{dep}}')} — freie Kategorie auf DependsOn</h3>
    <div class="kat-row">${K('\\mathrm{Ob}(\\mathcal{C}_{\\mathrm{dep}}) = \\{' + comps.map(c => tt(idOf(c))).join(',\\,') + '\\}', false)}</div>
    <div class="kat-sec">Erzeugende Morphismen ${K('\\mathrm{id}_A \\in \\mathcal{C}\\;\\forall A')}:</div>
    <div class="kat-mors">${genList || '<span class="muted">keine</span>'}</div>
    ${compoTex ? `<div class="kat-sec">Komposition (per Konstruktion assoziativ):</div><div class="kat-row">${K(compoTex, true)}</div>` : ''}
    <h3 class="formal-h">${K('(\\mathrm{Term}, \\sqsubseteq)')} — Preorder auf IsA</h3>
    <div class="kat-mors">${isaList || '<span class="muted">keine IsA-Kanten</span>'}</div>
    <div class="formal-foot"><span class="tag">verworfen</span> Konvergenz als Funktor und validate als
      natürliche Transformation: <b>kein Gehalt</b> — nicht modelliert.</div>
  </div>`;
  wire(el, actions);
}
