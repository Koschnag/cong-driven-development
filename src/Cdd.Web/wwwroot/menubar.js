// VS-2022-Classic-Menüleiste: Identität + Auffindbarkeit. KEINE neue Navigation —
// jeder Eintrag dispatcht eine BESTEHENDE Aktion (die Omnibar ⌘K bleibt die echte Tür).
import { SURFACES } from './stage.js';

export function mountMenubar(el, store, actions) {
  const MENUS = [
    ['Datei', [
      ['Neu laden / Validieren', () => { actions.reload(); actions.openDock('errors'); }],
      ['Tests ableiten', () => actions.derive()],
    ]],
    ['Ansicht', [
      ['💬  Chat', () => actions.setMain('chat')],
      ['◈  Diagramm', () => actions.setMain('diagram')],
      ['—', null],
      ...SURFACES.map(s => [`${s.icon}  ${s.label}`, () => actions.summon(s.id)]),
      ['—', null],
      ['Fehlerliste / Ausgabe (⌘J)', () => actions.toggleDock()],
      ['Faden Vollbild (⌘0)', () => actions.closeStage()],
    ]],
    ['Modell', [
      ['▶ Loop bis Konvergenz', () => actions.loop()],
      ['Drift / Konvergenz', () => actions.summon('drift')],
      ['Modell-Übersicht', () => actions.summon('model')],
      ['Doku', () => actions.summon('docs')],
    ]],
    ['Engine', [
      ['Claude Code', () => actions.setEngine('claude')],
      ['Mistral (EU)', () => actions.setEngine('mistral')],
      ['Ollama (lokal)', () => actions.setEngine('ollama')],
    ]],
    ['Extras', [
      ['Einstellungen', () => actions.summon('settings')],
      ['Hell / Dunkel', () => actions.toggleTheme()],
    ]],
    ['Hilfe', [
      ['Was ist Cong OS?', () => actions.ask('Was ist Cong OS, was bist du, und wie arbeite ich am besten mit dir?')],
    ]],
  ];

  el.innerHTML = MENUS.map(([name, items], mi) => `
    <div class="vs-menu" data-mi="${mi}">
      <button class="vs-menu-btn">${name}</button>
      <div class="vs-menu-pop">${items.map(([label, fn], ii) =>
        fn ? `<div class="vs-menu-item" data-mi="${mi}" data-ii="${ii}">${label}</div>`
           : `<div class="vs-menu-sep"></div>`).join('')}</div>
    </div>`).join('');

  el.querySelectorAll('.vs-menu-btn').forEach(b => b.onclick = (e) => {
    e.stopPropagation();
    const m = b.closest('.vs-menu'); const was = m.classList.contains('open');
    el.querySelectorAll('.vs-menu').forEach(x => x.classList.remove('open'));
    if (!was) m.classList.add('open');
  });
  el.querySelectorAll('.vs-menu-item').forEach(it => it.onclick = () => {
    const fn = MENUS[+it.dataset.mi][1][+it.dataset.ii][1];
    el.querySelectorAll('.vs-menu').forEach(x => x.classList.remove('open'));
    if (fn) fn();
  });
  document.addEventListener('click', () => el.querySelectorAll('.vs-menu').forEach(m => m.classList.remove('open')));
}
