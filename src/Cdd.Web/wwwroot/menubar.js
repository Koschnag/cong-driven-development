// VS-Command-Bar: Menüs (jeder Eintrag = eine echte Aktion + Shortcut) + Quick-Launch-Palette.
// Die Palette ersetzt die alte Omnibox: Befehle + Live-Knotensuche (substring id/titel) + „Copilot fragen".
import { idOf, kindOf, convOf, title, escapeHtml } from './core.js';

const RUN = {
  validate: (a) => { a.reload(); a.openDock('errors'); },
  derive:   (a) => a.derive(),
  cube:     (a) => a.openSingle('cube'),
  docs:     (a) => a.openSingle('docs'),
  board:    (a) => a.openSingle('board'),
  settings: (a) => a.openSingle('settings'),
  explorer: (a) => a.focusExplorer(),
  dock:     (a) => a.toggleDock(),
  errors:   (a) => a.openDock('errors'),
  output:   (a) => a.openDock('output'),
  drift:    (a) => a.openDock('drift'),
  theme:    (a) => a.toggleTheme(),
  closeTab: (a) => a.closeActive(),
};
const LABEL = {
  validate: 'Neu laden / Validieren', derive: 'Tests ableiten', cube: 'OLAP-Cube öffnen', docs: 'Doku öffnen',
  board: 'Board öffnen', settings: 'Settings', explorer: 'Explorer fokussieren', dock: 'Bottom-Dock ein/aus',
  errors: 'Error List', output: 'Output', drift: 'Drift', theme: 'Hell/Dunkel', closeTab: 'Tab schließen',
};
const MENUS = [
  ['Model', [['validate', '⌘E'], ['derive', ''], ['cube', ''], ['docs', ''], ['board', '']]],
  ['View',  [['explorer', '⌘1'], ['dock', '⌘J'], ['errors', ''], ['output', ''], ['drift', ''], ['theme', '']]],
  ['Tools', [['settings', '⌘,'], ['closeTab', '⌘W']]],
];

export function mountMenuBar(el, store, actions) {
  el.innerHTML = `
    <div class="logo">◉ <b>Cong</b> OS</div>
    <nav class="menus">${MENUS.map(([name, items], mi) => `
      <div class="menu" data-mi="${mi}"><button class="menu-btn">${name}</button>
        <div class="menu-pop">${items.map(([id, key]) =>
          `<div class="menu-item" data-cmd="${id}"><span>${escapeHtml(LABEL[id] || id)}</span><span class="key">${key}</span></div>`).join('')}</div>
      </div>`).join('')}</nav>
    <div class="palette-wrap">
      <input id="palette" autocomplete="off" spellcheck="false" placeholder="⌘K  ›  Knoten suchen · Befehl · Copilot fragen…">
      <div class="palette-pop" id="palette-pop"></div>
    </div>
    <div class="rightcluster">
      <span class="tier" title="Zugriffsstufe">Operator · Claude</span>
      <button class="iconbtn" data-cmd="theme" title="Hell/Dunkel">🌓</button>
    </div>`;

  // Menüs
  el.querySelectorAll('.menu-item,[data-cmd]').forEach(it => it.onclick = () => {
    const cmd = it.dataset.cmd; if (RUN[cmd]) RUN[cmd](actions);
    el.querySelectorAll('.menu').forEach(m => m.classList.remove('open'));
  });
  el.querySelectorAll('.menu-btn').forEach(b => b.onclick = (e) => {
    e.stopPropagation(); const m = b.closest('.menu'); const was = m.classList.contains('open');
    el.querySelectorAll('.menu').forEach(x => x.classList.remove('open')); if (!was) m.classList.add('open');
  });
  document.addEventListener('click', () => el.querySelectorAll('.menu').forEach(m => m.classList.remove('open')));

  // Palette
  const inp = el.querySelector('#palette'), pop = el.querySelector('#palette-pop');
  let items = [], ai = -1;
  const close = () => { pop.classList.remove('open'); items = []; ai = -1; };
  function build(q) {
    q = q.trim(); if (!q) { close(); return; }
    const lq = q.toLowerCase(); const out = [];
    for (const id in RUN) if ((LABEL[id] || id).toLowerCase().includes(lq) || id.includes(lq))
      out.push({ type: 'cmd', id, label: '▸ ' + (LABEL[id] || id) });
    const nodes = store.get().nodes
      .filter(n => idOf(n).toLowerCase().includes(lq) || (title(n) || '').toLowerCase().includes(lq))
      .slice(0, 12)
      .map(n => ({ type: 'node', id: idOf(n), conv: convOf(n), kind: kindOf(n), label: idOf(n), sub: title(n) }));
    out.push(...nodes);
    out.push({ type: 'ask', q, label: '🤖 Copilot fragen: ' + q });
    items = out; ai = out.length ? 0 : -1; paint();
  }
  function paint() {
    pop.innerHTML = items.map((it, i) => it.type === 'node'
      ? `<div class="pi${i === ai ? ' active' : ''}" data-i="${i}"><span class="dot ${it.conv}"></span><span class="pi-id">${escapeHtml(it.label)}</span><span class="pi-sub">${escapeHtml(it.sub || '')}</span></div>`
      : `<div class="pi${i === ai ? ' active' : ''}" data-i="${i}">${escapeHtml(it.label)}</div>`).join('');
    pop.classList.toggle('open', items.length > 0);
    pop.querySelectorAll('.pi').forEach(d => { d.onmousedown = (e) => { e.preventDefault(); exec(+d.dataset.i); }; });
  }
  function exec(i) {
    const it = items[i]; if (!it) { const q = inp.value.trim(); if (q) actions.dispatch(q); inp.value = ''; close(); return; }
    if (it.type === 'cmd') RUN[it.id](actions);
    else if (it.type === 'node') actions.openNode(it.id);
    else actions.dispatch(it.q);
    inp.value = ''; close();
  }
  inp.oninput = () => build(inp.value);
  inp.onkeydown = (e) => {
    if (e.key === 'ArrowDown') { e.preventDefault(); ai = Math.min(ai + 1, items.length - 1); paint(); }
    else if (e.key === 'ArrowUp') { e.preventDefault(); ai = Math.max(ai - 1, 0); paint(); }
    else if (e.key === 'Enter') { e.preventDefault(); exec(ai); }
    else if (e.key === 'Escape') { inp.value = ''; close(); inp.blur(); }
  };
  inp.onblur = () => setTimeout(close, 120);

  return { focus: () => inp.focus() };
}
