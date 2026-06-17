// Die EINE Tür: Omnibar (⌘K). Fusioniert vier Dinge in ein Eingabefeld, immer gleich:
//   1) Befehle (Flächen rufen, Theme, Validieren, Tests ableiten…)
//   2) SPOT-Knoten-Suche (id/titel substring) → Bühne
//   3) Pins (Präfix "pin ")
//   4) freie Frage/Auftrag → geht an den Faden (Default, wenn nichts matcht)
//
// Ein Modell, kein Menü-Baum: alles ist hier, Enter führt aus. Asperger-freundlich:
// vollständige, vorhersagbare Liste; ADHD-freundlich: Tippen→Tun in einem Schritt.
import { idOf, kindOf, convOf, title, escapeHtml } from './core.js';
import { SURFACES } from './stage.js';

// Befehle = Verben mit fester Semantik. Jedes hat ein Wort + Tastenhinweis.
const COMMANDS = (a) => [
  ...SURFACES.map((s, i) => ({ id: 'go:' + s.id, label: `${s.icon} ${s.label} öffnen`, key: `⌘${i + 1}`, run: () => a.summon(s.id) })),
  { id: 'reload', label: '↻ Modell neu laden / validieren', key: '', run: () => { a.reload(); a.summon('drift'); } },
  { id: 'derive', label: '✓ Tests aus Specs ableiten', key: '', run: () => a.derive() },
  { id: 'now', label: '→ Das Nächste tun (Jetzt)', key: '⌘.', run: () => a.runNow() },
  { id: 'fullscreen', label: '▭ Faden Vollbild (Bühne zu)', key: '⌘0', run: () => a.closeStage() },
  { id: 'theme', label: '🌓 Hell / Dunkel', key: '', run: () => a.toggleTheme() },
];

export function mountOmni(el, store, actions) {
  el.innerHTML = `
    <div class="omni-logo">◉ <b>Cong</b> OS</div>
    <div class="omni-box">
      <input id="omni-in" autocomplete="off" spellcheck="false"
             placeholder="⌘K  ·  Auftrag · Knoten · Befehl · „pin …“">
      <div class="omni-pop" id="omni-pop"></div>
    </div>
    <div class="omni-right">
      <select id="omni-engine" title="Engine (souverän wählbar)">
        <option value="claude">Claude Code</option>
        <option value="mistral">Mistral (EU)</option>
        <option value="ollama">Ollama (lokal)</option>
      </select>
      <span id="omni-mode" class="omni-mode" title="aktiver Modus">develop</span>
    </div>`;

  const inp = el.querySelector('#omni-in');
  const pop = el.querySelector('#omni-pop');
  const engineSel = el.querySelector('#omni-engine');
  const modeEl = el.querySelector('#omni-mode');
  let items = [], ai = -1;

  engineSel.value = store.get().engine;
  engineSel.onchange = () => actions.setEngine(engineSel.value);
  const reflectMode = () => { modeEl.textContent = store.get().mode; };
  reflectMode();

  const close = () => { pop.classList.remove('open'); items = []; ai = -1; };

  function build(q) {
    q = q.trim();
    if (!q) { close(); return; }
    const lq = q.toLowerCase();
    const out = [];

    // Pins-Präfix
    if (lq.startsWith('pin ')) {
      const term = lq.slice(4).trim();
      store.get().nodes
        .filter(n => idOf(n).toLowerCase().includes(term) || (title(n) || '').toLowerCase().includes(term))
        .slice(0, 10)
        .forEach(n => out.push({ type: 'pin', id: idOf(n), conv: convOf(n), label: '📌 anheften: ' + idOf(n), sub: title(n) }));
      items = out; ai = out.length ? 0 : -1; paint(); return;
    }

    // Befehle
    for (const c of COMMANDS(actions))
      if (c.label.toLowerCase().includes(lq) || c.id.includes(lq))
        out.push({ type: 'cmd', cmd: c, label: c.label, key: c.key });

    // Knoten
    store.get().nodes
      .filter(n => idOf(n).toLowerCase().includes(lq) || (title(n) || '').toLowerCase().includes(lq))
      .slice(0, 10)
      .forEach(n => out.push({ type: 'node', id: idOf(n), conv: convOf(n), kind: kindOf(n), label: idOf(n), sub: title(n) }));

    // Immer als letzte Option: an den Faden schicken (der Default-Sinn von Cong OS).
    out.push({ type: 'ask', q, label: '▶ An den Faden: ' + q });

    items = out; ai = 0; paint();
  }

  function paint() {
    pop.innerHTML = items.map((it, i) => {
      const on = i === ai ? ' active' : '';
      if (it.type === 'node' || it.type === 'pin')
        return `<div class="oi${on}" data-i="${i}"><span class="dot ${it.conv}"></span><span class="oi-id">${escapeHtml(it.label)}</span><span class="oi-sub">${escapeHtml(it.sub || '')}</span></div>`;
      return `<div class="oi${on}" data-i="${i}"><span class="oi-id">${escapeHtml(it.label)}</span>${it.key ? `<span class="oi-key">${it.key}</span>` : ''}</div>`;
    }).join('');
    pop.classList.toggle('open', items.length > 0);
    pop.querySelectorAll('.oi').forEach(d => d.onmousedown = (e) => { e.preventDefault(); exec(+d.dataset.i); });
  }

  function exec(i) {
    const it = items[i];
    if (!it) { const q = inp.value.trim(); if (q) actions.ask(q); reset(); return; }
    if (it.type === 'cmd') it.cmd.run();
    else if (it.type === 'node') actions.focusNode(it.id);
    else if (it.type === 'pin') actions.togglePin({ ref: it.id, kind: 'node', label: it.id });
    else actions.ask(it.q);
    reset();
  }
  function reset() { inp.value = ''; close(); inp.blur(); }

  inp.oninput = () => build(inp.value);
  inp.onkeydown = (e) => {
    if (e.key === 'ArrowDown') { e.preventDefault(); ai = Math.min(ai + 1, items.length - 1); paint(); }
    else if (e.key === 'ArrowUp') { e.preventDefault(); ai = Math.max(ai - 1, 0); paint(); }
    else if (e.key === 'Enter') { e.preventDefault(); exec(ai); }
    else if (e.key === 'Escape') { reset(); }
  };
  inp.onblur = () => setTimeout(close, 120);

  return {
    focus: (prefill) => { inp.focus(); if (prefill) { inp.value = prefill; build(prefill); } },
    reflectMode,
  };
}
