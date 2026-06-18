// Cong OS core — Signals-Store, API, SPOT-Helfer.
// SPOT-Knoten-Form (verifiziert): {Id:"...", Payload:{Case:"SpecNode", Fields:{Item:{...}}}, Convergence:"Aligned"}

/* ── Signals-Store (der fehlende observable state, ~kein Framework) ── */
export function createStore(initial) {
  let state = { ...initial };
  const subs = new Set();
  return {
    get: () => state,
    set: (patch) => { state = { ...state, ...patch }; subs.forEach(f => f(state)); },
    subscribe: (f) => { subs.add(f); return () => subs.delete(f); },
  };
}

/* ── API über die existierenden Routen ── */
export const api = {
  spot:     () => fetch('/api/spot').then(r => r.json()),
  validate: () => fetch('/api/validate').then(r => r.json()),
  diff:     () => fetch('/api/diff').then(r => r.json()),
  exportMd: () => fetch('/api/export').then(r => r.text()),
  // Modell-Historie aus git (.spot/-Commits) — Zeitreise.
  history:     (limit = 60) => fetch('/api/history?limit=' + limit).then(r => r.json()),
  nodeHistory: (id) => fetch('/api/history/' + encodeURIComponent(id)).then(r => r.json()),
  // @-Gedächtnis (Wahrheit #2): Volltextsuche über die sanitisierte cong-memory.db (nur sensitive=0).
  dwh:         (q, limit = 24) => fetch('/api/dwh/search?q=' + encodeURIComponent(q) + '&limit=' + limit).then(r => r.json()),
  // RAG: semantische Suche (Ollama nomic-embed + Cosine), gleiche sensitive=0-Garantie.
  dwhSemantic: (q, limit = 16) => fetch('/api/dwh/semantic?q=' + encodeURIComponent(q) + '&limit=' + limit).then(r => r.json()),
  // Laufzeit-Provider (Engines + API-Keys über die GUI; Key wird NIE im Klartext geliefert).
  providers:      () => fetch('/api/providers').then(r => r.json()),
  saveProvider:   (id, p) => fetch('/api/providers/' + encodeURIComponent(id), { method: 'PUT', headers: { 'content-type': 'application/json' }, body: JSON.stringify(p) }).then(r => r.json()),
  deleteProvider: (id) => fetch('/api/providers/' + encodeURIComponent(id), { method: 'DELETE' }),
};

// Engine-Stream (SSE) → onEvent({t,...}) je Ereignis. Liefert die Abort-Funktion.
// url parametrisiert: /api/engine/run (ein Turn) oder /api/loop/run (Loop bis Konvergenz, cdd-mapper).
export function runEngine(body, onEvent, url = '/api/engine/run') {
  const ctrl = new AbortController();
  (async () => {
    try {
      const res = await fetch(url, {
        method: 'POST', headers: { 'content-type': 'application/json' },
        body: JSON.stringify(body), signal: ctrl.signal,
      });
      if (!res.ok || !res.body) { onEvent({ t: 'error', error: 'HTTP ' + res.status }); return; }
      const reader = res.body.getReader(); const dec = new TextDecoder(); let buf = '';
      for (;;) {
        const { done, value } = await reader.read(); if (done) break;
        buf += dec.decode(value, { stream: true });
        let i; while ((i = buf.indexOf('\n\n')) >= 0) {
          const chunk = buf.slice(0, i); buf = buf.slice(i + 2);
          const line = chunk.startsWith('data: ') ? chunk.slice(6) : null;
          if (line) { try { onEvent(JSON.parse(line)); } catch { /* event: done */ } }
        }
      }
    } catch (e) { if (e.name !== 'AbortError') onEvent({ t: 'error', error: e.message }); }
  })();
  return () => ctrl.abort();
}

// Loop bis Konvergenz: treibt cdd-mapper über /api/loop/run, dieselben SSE-Ereignisse wie die Engine.
export const runLoop = (body, onEvent) => runEngine(body, onEvent, '/api/loop/run');

/* ── SPOT-Helfer (defensiv gegen Encoding-Varianten) ── */
export const idOf = (n) => (typeof n?.Id === 'string' ? n.Id : (n?.Id?.Item ?? String(n?.Id ?? '')));
export const inner = (n) => n?.Payload?.Fields?.Item ?? (Array.isArray(n?.Payload?.Fields) ? n.Payload.Fields[0] : n?.Payload?.Fields) ?? {};
export const kindOf = (n) => (n?.Payload?.Case || '').replace(/Node$/, '').toLowerCase(); // spec|term|risk|...
export const convOf = (n) => (typeof n?.Convergence === 'string' ? n.Convergence : (n?.Convergence?.Case ?? 'Pending'));

export const KINDS = ['spec','test','risk','infra','component','premise','decision','knowledge','tool','term','invariant'];
export const CONV  = ['Aligned','Pending','Diverged','Orphaned'];
export const KIND_LABEL = { spec:'Spec', test:'Test', risk:'Risk', infra:'Infra', component:'Component', premise:'Prämisse', decision:'Entscheidung', knowledge:'Wissen', tool:'Tool', term:'Begriff', invariant:'Invariante' };

export function title(n) {
  const i = inner(n);
  switch (kindOf(n)) {
    case 'spec': case 'decision': case 'knowledge': return i.Title || idOf(n);
    case 'risk': return i.Statement || idOf(n);
    case 'premise': return i.Statement || idOf(n);
    case 'invariant': return i.Description || idOf(n);
    case 'infra': return i.Resource || idOf(n);
    default: return i.Name || idOf(n); // test/component/tool/term
  }
}
export function summary(n) {
  const i = inner(n);
  switch (kindOf(n)) {
    case 'spec': return i.Intent || '';
    case 'risk': return `Likelihood ${i.Likelihood} · Impact ${i.Impact}` + (i.Mitigation ? ` · ${i.Mitigation}` : '');
    case 'decision': return i.Choice || '';
    case 'premise': return i.Rationale || '';
    case 'term': return i.Definition || '';
    case 'knowledge': return `${i.MediaType || ''} ${i.Source || ''}`.trim();
    case 'tool': return i.Purpose || '';
    case 'component': return (i.DependsOn?.length ? '→ ' + i.DependsOn.join(', ') : '');
    case 'test': return 'covers ' + (i.SpecRef || '?');
    case 'infra': return i.Provider || '';
    default: return '';
  }
}
// Ausgehende Kanten: [{rel, target}]
export function refs(n) {
  const i = inner(n), out = [];
  const tid = (x) => (typeof x === 'string' ? x : x?.Item ?? x?.Fields?.Item);
  switch (kindOf(n)) {
    case 'term': (i.Relations || []).forEach(r => out.push({ rel: r.Case || 'rel', target: tid(r.Fields?.Item ?? r.Fields) })); break;
    case 'component': (i.DependsOn || []).forEach(t => out.push({ rel: 'DependsOn', target: tid(t) })); break;
    case 'test': if (i.SpecRef) out.push({ rel: 'covers', target: tid(i.SpecRef) }); break;
    case 'decision': if (i.Supersedes) out.push({ rel: 'supersedes', target: tid(i.Supersedes) }); break;
  }
  return out.filter(e => e.target);
}
// Voller Inhalt als Feld-Liste (für den Inspector)
export function fields(n) {
  const i = inner(n), f = [];
  const add = (l, v) => { if (v != null && v !== '' && !(Array.isArray(v) && !v.length)) f.push([l, v]); };
  switch (kindOf(n)) {
    case 'spec': add('Intent', i.Intent); break;
    case 'risk': add('Statement', i.Statement); add('Likelihood', i.Likelihood); add('Impact', i.Impact); add('Mitigation', i.Mitigation); break;
    case 'decision': add('Kontext', i.Context); add('Entscheidung', i.Choice); add('Konsequenzen', i.Consequences); break;
    case 'premise': add('Statement', i.Statement); add('Rationale', i.Rationale); break;
    case 'term': add('Definition', i.Definition); add('Synonyme', (i.Synonyms || []).join(', ')); break;
    case 'knowledge': add('Quelle', i.Source); add('Typ', i.MediaType); add('Takeaways', (i.Takeaways || []).join(' · ')); break;
    case 'tool': add('Zweck', i.Purpose); add('Endpoint', i.Endpoint); break;
    case 'infra': add('Provider', i.Provider); add('Resource', i.Resource); break;
    case 'invariant': add('Regel', i.Rule?.Case ?? i.Rule); break;
  }
  return f;
}
export const escapeHtml = (s) => String(s ?? '').replace(/[&<>]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;' }[c]));

/* ──────────────────────────────────────────────────────────────────────────
   SPOT-Notation — EIN kohärentes Symbolsystem statt Unicode-Sammelsurium.
   Register: UML/SysML-Stereotyp-Notation (kein Piktogramm). Drei orthogonale
   Variablen: (1) äußere FORM = ontologische Familie, (2) innere MARKE = die Art
   (formale Notation, die Cong fließend liest), (3) KONVERGENZ = nur am Rand.
   RIGOR: die Marken sind eine MOTIVIERTE Beschriftung, KEIN Beweis. Konditional:
   WENN eine Test-Kante als Typurteil gelesen wird, DANN ist ⊢ korrekt.
   ────────────────────────────────────────────────────────────────────────── */
export const FAMILY = {
  spec: 'Classifier', term: 'Classifier', component: 'Classifier',     // first-class authored
  invariant: 'Judgement', risk: 'Judgement', premise: 'Judgement',     // «constraint» (OCL/SysML)
  knowledge: 'Artifact', tool: 'Artifact', infra: 'Artifact',          // «artifact» (Dinge-in-der-Welt)
  test: 'Derivation', decision: 'Derivation',                          // Operation, die ein Urteil erzeugt
};
// Familie → native Cytoscape-Form (die äußere Silhouette, GPU-gezeichnet, scharf bei jedem Zoom).
export const SHAPE = {
  spec: 'round-rectangle', term: 'round-rectangle', component: 'round-rectangle',
  invariant: 'cut-rectangle', risk: 'cut-rectangle', premise: 'cut-rectangle',
  knowledge: 'barrel', tool: 'barrel', infra: 'barrel',
  test: 'hexagon', decision: 'hexagon',
};
export const HUE = {
  spec: '#5B8DEF', term: '#7BA7F0', component: '#9FC0F5',
  invariant: '#E0A13C', risk: '#C9762F', premise: '#D98A3D',
  knowledge: '#6E8CA0', tool: '#4FB3A8', infra: '#5AA0B5',
  test: '#9B7BE0', decision: '#B58CE6',
};
// Innere Marke = formale Notation mit korrekter Bedeutung:
// ⊢ ableiten (Spec/Test) · ∈ Element (Term) · ⊸ Schnittstelle (Component) · ⊨ gilt-in-allen-Modellen
// (Invariant) · ◇ modal „möglich" (Risk) · ∵ weil/Annahme (Premise) · λ Funktion (Tool) · 𝒦 (Knowledge)
// · ◰ Deployment-Knoten (Infra) · ⋎ Verzweigung (Decision).
export const MARK = {
  spec: '⊢', term: '∈', component: '⊸', invariant: '⊨', risk: '◇',
  premise: '∵', knowledge: '𝒦', tool: 'λ', infra: '◰', test: '⊨', decision: '⋎',
};
// Familien-Silhouette als 20×20-Pfad (4 Formen, über die 11 Arten wiederverwendet).
const OUTLINE = {
  Classifier: "<rect x='2.5' y='4.5' width='15' height='11' rx='2.5'/>",
  Judgement:  "<path d='M2.5 4.5 H14 L17.5 8 V15.5 H2.5 Z'/>",
  Artifact:   "<path d='M4 4 H12.5 L16 7.5 V16 H4 Z M12.5 4 V7.5 H16'/>",
  Derivation: "<path d='M6 4.5 H14 L17.5 10 L14 15.5 H6 L2.5 10 Z'/>",
};
// Innere Marke als winziges Inline-SVG (data-uri, kein Build-Step). encodeURIComponent statt base64.
export function markUri(kind) {
  const g = MARK[kind] || '?', c = HUE[kind] || '#9aa7b4';
  const svg = `<svg xmlns='http://www.w3.org/2000/svg' width='40' height='40'>`
    + `<text x='20' y='21.5' fill='${c}' font-family='ui-serif,Cambria,Georgia,serif' font-size='23' `
    + `font-weight='600' text-anchor='middle' dominant-baseline='central'>${g}</text></svg>`;
  return 'data:image/svg+xml;utf8,' + encodeURIComponent(svg);
}
// Voller Glyph (Silhouette + Marke) für Palette/Schiene/Menü — eine visuelle Sprache überall.
export function glyphSvg(kind, size = 20) {
  const c = HUE[kind] || '#9aa7b4', fam = FAMILY[kind] || 'Classifier';
  return `<svg viewBox='0 0 20 20' width='${size}' height='${size}' class='spot-glyph' aria-hidden='true'>`
    + `<g fill='none' stroke='${c}' stroke-width='1.3' stroke-linejoin='round'>${OUTLINE[fam]}</g>`
    + `<text x='10' y='10.6' fill='${c}' font-size='8.5' font-family='ui-serif,Georgia,serif' `
    + `text-anchor='middle' dominant-baseline='central'>${MARK[kind]}</text></svg>`;
}

// Flächen-Icons (Plan/Modell/Dev/Infra/Prod/Doku/Entscheidungen/Historie) im selben monolinen
// Register wie die Knoten-Glyphen — stroke=currentColor (folgt dem Button), kohärent statt Emoji.
const SURF_PATH = {
  plan:      "<circle cx='10' cy='10' r='6'/><circle cx='10' cy='10' r='2'/>",
  model:     "<path d='M10 3 L16 6.5 V13.5 L10 17 L4 13.5 V6.5 Z'/>",
  dev:       "<path d='M7 6 L3 10 L7 14 M13 6 L17 10 L13 14'/>",
  infra:     "<rect x='4' y='4' width='12' height='4.5' rx='1'/><rect x='4' y='11.5' width='12' height='4.5' rx='1'/><circle cx='6.6' cy='6.2' r='.7'/><circle cx='6.6' cy='13.7' r='.7'/>",
  prod:      "<path d='M10 16 V5 M6 9 L10 5 L14 9'/><path d='M6 16 H14'/>",
  docs:      "<path d='M5.5 3 H12 L14.5 5.5 V17 H5.5 Z'/><path d='M7.5 9 H12.5 M7.5 12 H12.5'/>",
  decisions: "<path d='M10 17 V11 M10 11 L5 6 M10 11 L15 6'/><circle cx='5' cy='5' r='1.4'/><circle cx='15' cy='5' r='1.4'/>",
  history:   "<circle cx='10' cy='10' r='6'/><path d='M10 6.5 V10 L12.5 12'/>",
};
export function surfaceIcon(id, size = 18) {
  const p = SURF_PATH[id] || "<circle cx='10' cy='10' r='5'/>";
  return `<svg viewBox='0 0 20 20' width='${size}' height='${size}' class='surf-svg' aria-hidden='true'>`
    + `<g fill='none' stroke='currentColor' stroke-width='1.4' stroke-linejoin='round' stroke-linecap='round'>${p}</g></svg>`;
}

/* ──────────────────────────────────────────────────────────────────────────
   Toolbox — minimale gültige Knoten + legale Relationen, DETERMINISTISCH (kein LLM).
   Präfixe gegen das Live-Modell verifiziert (nicht erfunden). term- ist die EINZIGE
   per Invariante erzwungene; der Rest ist Konvention. Convergence:Pending = der
   wahrhaftige Stub-Zustand (NICHT Aligned — keine Konvergenz faken).
   ────────────────────────────────────────────────────────────────────────── */
export const PREFIX = {
  spec: 'spec-', test: 'test-', risk: 'risk-', infra: 'infra-', component: 'comp-',
  premise: 'premise-', decision: 'adr-', knowledge: 'kb-', tool: 'tool-', term: 'term-', invariant: 'inv-',
};
const CASE = {
  spec: 'SpecNode', test: 'TestNode', risk: 'RiskNode', infra: 'InfraNode', component: 'ComponentNode',
  premise: 'PremiseNode', decision: 'DecisionNode', knowledge: 'KnowledgeNode', tool: 'ToolNode',
  term: 'TermNode', invariant: 'InvariantNode',
};
// ASCII-Slug: Store.isValidId erlaubt nur [a-zA-Z0-9_-] → Umlaute/Spaces transliterieren, sonst 400.
export function slugify(s) {
  return String(s || '').toLowerCase()
    .replace(/ä/g, 'ae').replace(/ö/g, 'oe').replace(/ü/g, 'ue').replace(/ß/g, 'ss')
    .normalize('NFD').replace(/[̀-ͯ]/g, '')
    .replace(/[^a-z0-9]+/g, '-').replace(/^-+|-+$/g, '').slice(0, 40) || 'neu';
}
// Minimale valide Payload-Item je Art (Feldform exakt aus Spot.fs).
const SKELETON = {
  spec:      t => ({ Title: t, Intent: '', Criteria: [] }),
  test:      t => ({ SpecRef: '', Name: t, Derived: false }),
  risk:      t => ({ Statement: t, Likelihood: 'Medium', Impact: 'Medium', Mitigation: null }),
  infra:     t => ({ Resource: t, Provider: '', Config: {} }),
  component: t => ({ Name: t, DependsOn: [] }),
  premise:   t => ({ Statement: t, Rationale: '' }),
  decision:  t => ({ Title: t, Context: '', Choice: '', Consequences: '', Supersedes: null }),
  knowledge: t => ({ Title: t, Source: '', MediaType: 'link', Takeaways: [] }),
  tool:      t => ({ Name: t, Purpose: '', Endpoint: null }),
  term:      t => ({ Name: t, Definition: '', Synonyms: [], Relations: [] }),
  invariant: t => ({ Description: t, Rule: 'TermsNeedDefinition' }),
};
export function makeEntry(kind, titleText, id) {
  return { Id: id, Payload: { Case: CASE[kind], Fields: { Item: SKELETON[kind](titleText) } }, Convergence: 'Pending' };
}
// Knoten mit ersetztem inneren Item (reine JSON-Chirurgie, immutabel).
function withItem(node, item) {
  return { ...node, Payload: { ...node.Payload, Fields: { ...node.Payload.Fields, Item: item } } };
}
// Legale kantentragende Felder — KEINE freien Pfeile, nur was die Domäne wirklich speichert.
export const RELATIONS = [
  { rel: 'IsA',        from: ['term'],      glyph: '◁',  via: 'termrel' },
  { rel: 'PartOf',     from: ['term'],      glyph: '◆',  via: 'termrel' },
  { rel: 'RelatesTo',  from: ['term'],      glyph: '⋯',  via: 'termrel' },
  { rel: 'DependsOn',  from: ['component'], glyph: '⇠',  via: 'idlist' },
  { rel: 'covers',     from: ['test'],      glyph: '⊢',  via: 'specref' },
  { rel: 'Supersedes', from: ['decision'],  glyph: '⊳',  via: 'idset', field: 'Supersedes' },
];
// Quelle splicen → voller neuer Knoten (zum Zurück-PUTen).
export function spliceRelation(node, relDef, targetId) {
  const item = inner(node);
  if (relDef.via === 'termrel') {
    const rels = (item.Relations || []).slice();
    if (!rels.some(r => r.Case === relDef.rel && (r.Fields?.Item ?? r.Fields) === targetId))
      rels.push({ Case: relDef.rel, Fields: { Item: targetId } });
    return withItem(node, { ...item, Relations: rels });
  }
  if (relDef.via === 'idlist') {
    const arr = (item.DependsOn || []).slice();
    if (!arr.includes(targetId)) arr.push(targetId);
    return withItem(node, { ...item, DependsOn: arr });
  }
  if (relDef.via === 'specref') return withItem(node, { ...item, SpecRef: targetId });
  if (relDef.via === 'idset') return withItem(node, { ...item, [relDef.field]: targetId });
  return node;
}
