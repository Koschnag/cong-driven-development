// Der Spine: EIN Gesprächsfaden, der den Bildschirm füllt.
//
// Jeder Engine-Lauf wird inline als Turn gerendert: you → (tool/result-Karten) → assistant.
// Knoten-Ids im Text werden klickbar → rufen die Bühne (focusNode), ohne den Faden zu verlieren.
// Das untere Eingabefeld ist der Standardfokus: tippen + Enter ist die niedrigste Reibung.
import { runEngine, escapeHtml } from './core.js';

// Wandelt SPOT-Ids im Fließtext in klickbare Chips (Cross-Refs ins Modell).
function linkifyIds(text, ids, onId) {
  // sichere Token-für-Token-Ersetzung über bekannte Ids (keine Regex-Injection).
  let html = escapeHtml(text);
  for (const id of ids) {
    if (!id || id.length < 3) continue;
    const safe = escapeHtml(id).replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    html = html.replace(new RegExp('(^|[^\\w-])(' + safe + ')(?=[^\\w-]|$)', 'g'),
      (_m, pre, hit) => `${pre}<span class="idchip" data-id="${escapeHtml(id)}">${hit}</span>`);
  }
  return html;
}

export function mountThread(el, store, actions) {
  el.innerHTML = `
    <div class="thread-scroll" id="th-scroll"></div>
    <div class="composer">
      <div class="composer-modes" id="th-modes"></div>
      <div class="composer-row">
        <textarea id="th-in" rows="1" placeholder="Auftrag oder Frage…  (Enter senden · ⇧Enter Zeile · ⌘K Tür)"></textarea>
        <button id="th-send" title="Senden (Enter)">▶</button>
      </div>
    </div>`;
  const scroll = el.querySelector('#th-scroll');
  const input = el.querySelector('#th-in');
  const modesEl = el.querySelector('#th-modes');
  let abort = null, curTurn = null;

  // Modi = die Funktions-Achsen als EIN Wort. Wählt nur den System-Frame der Engine.
  // Labels identisch zu den Flächen (eine Vokabel, kein Develop/Dev-Drift) — Asperger: ein Wort pro Konzept.
  const MODES = [
    ['plan', 'Plan'],
    ['dev', 'Dev'],
    ['infra', 'Infra'],
    ['prod', 'Prod'],
    ['ask', 'Fragen'],
  ];
  function paintModes() {
    const m = store.get().mode;
    modesEl.innerHTML = MODES.map(([id, lbl]) =>
      `<button class="mode${m === id ? ' on' : ''}" data-mode="${id}">${lbl}</button>`).join('');
    modesEl.querySelectorAll('.mode').forEach(b => b.onclick = () => actions.setMode(b.dataset.mode));
  }
  store.subscribe(() => paintModes());
  paintModes();

  const ids = () => store.get().nodes.map(n => (typeof n.Id === 'string' ? n.Id : n?.Id?.Item)).filter(Boolean);
  const atBottom = () => scroll.scrollHeight - scroll.scrollTop - scroll.clientHeight < 80;
  function stick(was) { if (was) scroll.scrollTop = scroll.scrollHeight; }

  // Ein Turn = ein Block im Faden. role: you|assistant|system. Gibt das Element zurück.
  function say(role, text, opts = {}) {
    const was = atBottom();
    const div = document.createElement('div');
    div.className = 'turn ' + role;
    const body = role === 'assistant'
      ? linkifyIds(text || '', ids(), null)
      : escapeHtml(text || '');
    div.innerHTML =
      `<div class="turn-role">${role === 'you' ? 'du' : role === 'assistant' ? 'cong os' : '·'}</div>` +
      `<div class="turn-body">${body}</div>`;
    scroll.appendChild(div); wireIds(div); stick(was);
    return div;
  }
  function wireIds(scope) {
    scope.querySelectorAll('.idchip').forEach(c => c.onclick = () => actions.focusNode(c.dataset.id));
  }

  // Eine Werkzeug-/Ergebnis-Karte, eingerückt unter dem laufenden Turn (Agenten-Aktivität sichtbar).
  function addStep(turn, kind, label, body) {
    const was = atBottom();
    const steps = turn.querySelector('.turn-steps') || (() => {
      const s = document.createElement('div'); s.className = 'turn-steps'; turn.appendChild(s); return s;
    })();
    const c = document.createElement('div');
    c.className = 'step ' + kind;
    c.innerHTML = `<span class="step-lbl">${escapeHtml(label)}</span>` +
      (body ? `<pre>${escapeHtml(String(body).slice(0, 4000))}</pre>` : '');
    steps.appendChild(c); stick(was);
    return c;
  }

  // Geführtes Durchklicken: eine Reihe anklickbarer nächster Schritte als eigener Turn.
  // ADHD: immer eine sichtbare nächste Aktion, nie eine leere Seite. Klick führt aus.
  function suggest(steps) {
    if (!steps || !steps.length) return;
    const was = atBottom();
    const div = document.createElement('div');
    div.className = 'turn suggest';
    div.innerHTML = `<div class="turn-role">weiter</div><div class="turn-body"><div class="chips"></div></div>`;
    const chips = div.querySelector('.chips');
    steps.forEach(st => {
      const b = document.createElement('button');
      b.className = 'chip'; b.textContent = st.label;
      b.onclick = () => st.run();
      chips.appendChild(b);
    });
    scroll.appendChild(div); stick(was);
    return div;
  }

  // Streaming-Assistententext akkumuliert in EINEM Bubble pro Turn.
  function appendAssistant(turn, delta) {
    const was = atBottom();
    let b = turn.querySelector('.assistant-bubble');
    if (!b) {
      b = document.createElement('div'); b.className = 'assistant-bubble'; b.dataset.raw = '';
      turn.appendChild(b);
    }
    b.dataset.raw += delta;
    b.innerHTML = linkifyIds(b.dataset.raw, ids(), null);
    wireIds(b); stick(was);
  }

  function handle(turn, ev) {
    switch (ev.t) {
      case 'started': actions.setRunState('running'); turn.classList.add('running'); break;
      case 'text': appendAssistant(turn, ev.text); break;
      case 'tool': addStep(turn, 'tool', '🔧 ' + (ev.name || 'tool'), ev.input || ''); break;
      case 'toolresult': addStep(turn, 'result', '↳ Ergebnis', (ev.text || '').slice(0, 1200)); break;
      case 'done':
        turn.classList.remove('running');
        actions.setRunState('done');
        if (ev.cost) addStep(turn, 'cost', `✓ fertig · $${(+ev.cost).toFixed(4)}`, '');
        Promise.resolve(actions.reload()).then(() => actions.suggestNext && actions.suggestNext());
        break;
      case 'error':
        turn.classList.remove('running'); turn.classList.add('failed');
        addStep(turn, 'error', '⚠ Fehler', ev.error || '');
        actions.setRunState('error');
        break;
    }
  }

  function ask(prompt) {
    if (!prompt || !prompt.trim()) return;
    if (abort) abort();
    say('you', prompt);
    const turn = document.createElement('div');
    turn.className = 'turn assistant running';
    turn.innerHTML = `<div class="turn-role">cong os</div>`;
    scroll.appendChild(turn); scroll.scrollTop = scroll.scrollHeight;
    curTurn = turn;
    const s = store.get();
    abort = runEngine(
      { Prompt: prompt, Surface: s.mode, Engine: s.engine, Model: '' },
      (ev) => handle(turn, ev));
  }

  function focusInput() { input.focus(); }
  function welcome() {
    const n = store.get().nodes.length;
    say('system',
      `Cong OS bereit. Ein Faden, eine Tür (⌘K). ${n} SPOT-Knoten geladen. ` +
      `Klick unten einen Vorschlag, drück ⌘. für das Nächste, oder tipp einen Auftrag. Ziffern 1–6 rufen Flächen, Esc schließt die Bühne.`);
    focusInput();
  }

  // Eingabe: Enter sendet (niedrigste Reibung), ⇧Enter = Zeilenumbruch. Auto-grow.
  el.querySelector('#th-send').onclick = () => { ask(input.value); input.value = ''; grow(); };
  input.addEventListener('keydown', e => {
    if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); ask(input.value); input.value = ''; grow(); }
  });
  input.addEventListener('input', grow);
  function grow() { input.style.height = 'auto'; input.style.height = Math.min(input.scrollHeight, 160) + 'px'; }

  return { ask, say, suggest, focusInput, welcome };
}
