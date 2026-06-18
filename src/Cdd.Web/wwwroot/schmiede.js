// Die Schmiede: EINE Prosa-Spielidee → Pending-Specs (Auto) → dein Review/Korrektur (das EINE Gate)
// → „Alle konvergieren" (der bestehende Loop). Du schreibst das WAS, das System baut + testet das WIE.
// Ehrliche Grenze sichtbar: Logik = getestete Specs · du validierst die Kriterien · Kunst/Audio/Balance = [ASSET]/manuell.
import { api, runEngine, idOf, kindOf, convOf, inner, title, escapeHtml } from './core.js';

const isAsset = (n) => (title(n) || '').trim().toUpperCase().startsWith('[ASSET]');

export function renderSchmiede(el, store, actions) {
  const s = store.get();
  const st = s.schmiedeStatus || 'idle';
  const log = s.schmiedeLog || [];
  const specs = (s.schmiedeSpecs || []).map(id => s.byId.get(id)).filter(Boolean);
  const repaint = () => renderSchmiede(el, store, actions);
  const busy = st === 'generating' || st === 'converging';
  const nonAsset = specs.filter(n => !isAsset(n));

  el.innerHTML =
    `<div class="stage-hint">Die Schmiede — Prosa-Idee → Specs → dein Review → Konvergenz. <b>Du schreibst das WAS, das System baut das WIE.</b></div>` +
    `<div class="schm-boundary"><b>Automatisch:</b> Logik/Regeln als getestete F#-Specs · <b>du validierst:</b> die generierten Given/When/Then · <b>manuell/Platzhalter:</b> Kunst, Audio, Balance-Gefühl (<code>[ASSET]</code>).</div>` +
    `<textarea class="schm-idea" id="schm-idea" ${busy ? 'disabled' : ''} placeholder="Beschreibe EINE Spielidee in Prosa — z. B.: Stehende Einheiten kosten Unterhalt — pro Sammel-Intervall verbraucht jede lebende Einheit 1 Nahrung; bei 0 Nahrung kann keine neue Einheit entstehen.">${escapeHtml(s.schmiedeIdea || '')}</textarea>` +
    `<div class="stage-actions">
       <button id="schm-gen" ${busy ? 'disabled' : ''}>⚒ Specs schmieden</button>
       ${nonAsset.length ? `<button id="schm-conv" ${busy ? 'disabled' : ''}>▶ Alle konvergieren (${nonAsset.length})</button>` : ''}
     </div>` +
    (log.length ? `<div class="schm-log">${log.map(l => `<div class="schm-line ${l.t}">${escapeHtml(l.text)}</div>`).join('')}</div>` : '') +
    (specs.length ? `<div class="schm-h">Generierte Specs — review &amp; korrigieren (${specs.length})</div>` + specs.map(n => specCard(n)).join('') : '');

  const ta = el.querySelector('#schm-idea');
  if (ta) ta.oninput = () => store.set({ schmiedeIdea: ta.value });
  const gen = el.querySelector('#schm-gen');
  if (gen) gen.onclick = () => generate(store, actions, repaint);
  const conv = el.querySelector('#schm-conv');
  if (conv) conv.onclick = () => converge(store, actions, repaint, nonAsset.length);

  // Spec-Karten: validieren/korrigieren (PUT) oder verwerfen (DELETE) — solange Pending.
  el.querySelectorAll('.sc-card').forEach(card => {
    const id = card.dataset.id;
    const node = store.get().byId.get(id);
    card.querySelector('.sc-save').onclick = async () => {
      if (!node) return;
      const item = {
        ...inner(node),
        Title: card.querySelector('.sc-title').value,
        Intent: card.querySelector('.sc-intent').value,
        Criteria: [...card.querySelectorAll('.sc-crit')].map(c => ({
          Given: c.querySelector('.sc-g').value, When: c.querySelector('.sc-w').value, Then: c.querySelector('.sc-t').value
        })).filter(c => c.Given || c.When || c.Then),
      };
      const updated = { ...node, Payload: { ...node.Payload, Fields: { ...node.Payload.Fields, Item: item } } };
      if (await actions.upsert(updated)) { await actions.reload(); repaint(); }
    };
    card.querySelector('.sc-del').onclick = async () => {
      await fetch('/api/spot/' + encodeURIComponent(id), { method: 'DELETE' });
      const specs2 = (store.get().schmiedeSpecs || []).filter(x => x !== id);
      store.set({ schmiedeSpecs: specs2 });
      await actions.reload(); repaint();
    };
    card.querySelector('.sc-open').onclick = () => actions.focusNode(id);
  });
}

function specCard(n) {
  const i = inner(n), asset = isAsset(n), id = idOf(n);
  const crits = (i.Criteria || []);
  return `<div class="sc-card${asset ? ' asset' : ''}" data-id="${escapeHtml(id)}">
    <div class="sc-head"><code>${escapeHtml(id)}</code>
      ${asset ? '<span class="sc-badge">[ASSET] · Platzhalter, kein Code</span>' : '<span class="conv-chip Pending">Pending</span>'}
      <button class="sc-open" title="im Inspector öffnen">↗</button></div>
    <input class="sc-title" value="${escapeHtml(i.Title || '')}" placeholder="Titel">
    <textarea class="sc-intent" placeholder="Intent — das Warum im Spielsinn">${escapeHtml(i.Intent || '')}</textarea>
    ${asset ? '' : `<div class="sc-crits">${(crits.length ? crits : [{}]).map(c => `<div class="sc-crit">
        <input class="sc-g" value="${escapeHtml(c.Given || '')}" placeholder="GIVEN — Ausgangszustand">
        <input class="sc-w" value="${escapeHtml(c.When || '')}" placeholder="WHEN — Aktion / Tick">
        <input class="sc-t" value="${escapeHtml(c.Then || '')}" placeholder="THEN — prüfbarer Zustand">
      </div>`).join('')}</div>`}
    <div class="sc-actions"><button class="sc-save">Speichern</button><button class="sc-del">Verwerfen</button></div>
  </div>`;
}

function pushLog(store, repaint, t, text) {
  const log = (store.get().schmiedeLog || []).slice();
  log.push({ t, text }); if (log.length > 200) log.shift();
  store.set({ schmiedeLog: log }); repaint();
}

function generate(store, actions, repaint) {
  const idea = (store.get().schmiedeIdea || '').trim();
  if (!idea) return;
  const before = new Set(store.get().nodes.filter(n => kindOf(n) === 'spec').map(idOf));
  store.set({ schmiedeStatus: 'generating', schmiedeLog: [{ t: 'system', text: '⚒ Schmiede läuft — Prosa → Pending-Specs (nur Modell, kein Code)…' }], schmiedeSpecs: [] });
  repaint();
  runEngine({ Idea: idea }, (ev) => {
    switch (ev.t) {
      case 'text': if (ev.text && ev.text.trim()) pushLog(store, repaint, 'text', ev.text.trim()); break;
      case 'tool': pushLog(store, repaint, 'tool', '⚙ ' + (ev.name || '') + '  ' + (ev.input || '').replace(/\s+/g, ' ').slice(0, 90)); break;
      case 'error': pushLog(store, repaint, 'error', '⚠ ' + (ev.error || '')); break;
      case 'done':
        actions.reload().then(() => {
          const fresh = store.get().nodes.filter(n => kindOf(n) === 'spec' && convOf(n) === 'Pending' && !before.has(idOf(n)));
          store.set({ schmiedeStatus: fresh.length ? 'review' : 'idle', schmiedeSpecs: fresh.map(idOf) });
          pushLog(store, repaint, 'system', fresh.length ? `✓ ${fresh.length} Pending-Spec(s) generiert — bitte reviewen, dann konvergieren.` : 'Keine neuen Specs entstanden.');
        });
        break;
    }
  }, '/api/schmiede/generate');
}

function converge(store, actions, repaint, count) {
  store.set({ schmiedeStatus: 'converging' });
  pushLog(store, repaint, 'system', `▶ Loop bis Konvergenz — ${count} Spec(s), das Gate entscheidet (echtes dotnet test).`);
  runEngine({ MaxSpecs: count, MaxAttempts: 3 }, (ev) => {
    switch (ev.t) {
      case 'spec': pushLog(store, repaint, 'tool', '◆ ' + ev.id + ' — ' + (ev.title || '')); break;
      case 'attempt': pushLog(store, repaint, 'text', '  Versuch ' + ev.n + '/' + ev.max + ' → claude -p'); break;
      case 'gate': pushLog(store, repaint, ev.ok ? 'system' : 'text', ev.ok ? '  ✓ Gate grün' + (ev.skipped ? ' (übersprungen)' : '') : '  … Gate noch rot'); break;
      case 'spec_done': pushLog(store, repaint, 'system', (ev.konvergiert ? '✓ ' : '✗ ') + ev.id + ' nach ' + ev.versuche + ' Versuch(en)'); break;
      case 'done':
        actions.reload().then(() => { store.set({ schmiedeStatus: 'review' }); pushLog(store, repaint, 'system', `✓ Konvergenz fertig — ${ev.konvergiert}/${ev.total} grün. Modell + Tests aktualisiert.`); });
        break;
      case 'error': pushLog(store, repaint, 'error', '⚠ ' + (ev.error || '')); break;
    }
  }, '/api/loop/run');
}
