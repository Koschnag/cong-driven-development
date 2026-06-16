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
};

// Engine-Stream (SSE) → onEvent({t,...}) je EngineEvent. Liefert die Abort-Funktion.
export function runEngine(body, onEvent) {
  const ctrl = new AbortController();
  (async () => {
    try {
      const res = await fetch('/api/engine/run', {
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
