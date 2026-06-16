// Copilot-Rail: der Agent als Nervensystem. Rendert den EngineEvent-SSE-Stream als Karten.
import { runEngine, escapeHtml } from './core.js';

export function mountCopilot(el, store, opts = {}) {
  el.innerHTML = `
    <div class="cop-head">
      <span>🤖 Copilot</span>
      <select id="cop-engine" title="Engine">
        <option value="claude">Claude Code</option>
        <option value="mistral">Mistral (EU)</option>
        <option value="ollama">Ollama (lokal)</option>
      </select>
      <span id="cop-mode" class="muted" style="margin-left:auto">develop</span>
    </div>
    <div class="cop-stream" id="cop-stream">
      <div class="muted" style="padding:.5rem">Tippe oben in die Omnibox oder hier — der Agent arbeitet in Karten, kein Terminal.</div>
    </div>
    <div class="cop-input">
      <input id="cop-in" placeholder="den Agenten fragen / beauftragen…">
      <button id="cop-send">▶</button>
    </div>`;
  const stream = el.querySelector('#cop-stream');
  const input = el.querySelector('#cop-in');
  const engineSel = el.querySelector('#cop-engine');
  const modeEl = el.querySelector('#cop-mode');
  let abort = null;

  const sub = store.subscribe(s => { modeEl.textContent = s.surface; });

  function addCard(kind, label, body, mono = true) {
    const div = document.createElement('div');
    div.className = 'card ' + kind;
    div.innerHTML = `<div class="lbl">${escapeHtml(label)}</div>` +
      (mono ? `<pre>${escapeHtml(body)}</pre>` : `<div class="body">${escapeHtml(body)}</div>`);
    stream.appendChild(div); stream.scrollTop = stream.scrollHeight;
    return div;
  }

  function handle(ev) {
    if (opts.onEvent) try { opts.onEvent(ev); } catch {}
    switch (ev.t) {
      case 'started': addCard('text', '● ' + (ev.model || ''), '', false); break;
      case 'text': {
        const last = stream.lastElementChild;
        if (last && last.classList.contains('text') && last.dataset.kind === 'asst') last.querySelector('.body').textContent += ev.text;
        else { const c = addCard('text', 'assistant', ev.text, false); c.dataset.kind = 'asst'; }
        stream.scrollTop = stream.scrollHeight; break;
      }
      case 'tool': addCard('tool', '🔧 ' + (ev.name || 'tool'), ev.input || ''); break;
      case 'toolresult': addCard('toolresult', '↳ result', (ev.text || '').slice(0, 4000)); break;
      case 'done': addCard('done', '✓ fertig' + (ev.cost ? ` · $${(+ev.cost).toFixed(4)}` : ''), (ev.result || '').slice(0, 1500), false); break;
      case 'error': addCard('error', '⚠ Fehler', ev.error || ''); break;
      default: addCard('text', '·', ev.line || JSON.stringify(ev));
    }
  }

  function dispatch(prompt) {
    if (!prompt.trim()) return;
    if (abort) abort();
    addCard('text', 'you', prompt, false);
    abort = runEngine(
      { Prompt: prompt, Surface: store.get().surface, Engine: engineSel.value, Model: '' },
      handle);
  }

  el.querySelector('#cop-send').onclick = () => { dispatch(input.value); input.value = ''; };
  input.addEventListener('keydown', e => { if (e.key === 'Enter') { dispatch(input.value); input.value = ''; } });

  return { dispatch, addCard, _cleanup: sub };
}
