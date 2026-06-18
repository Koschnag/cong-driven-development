// Linke Schiene = externalisiertes Arbeitsgedächtnis. Drei Blöcke, immer in dieser Reihenfolge:
//   1) JETZT  — die EINE nächste Aktion. Ein Klick führt sie aus. Nie eine Sackgasse.
//   2) FLÄCHEN — die 6 herbeirufbaren Projektionen (Plan/Dev/Infra/Prod/Modell/Doku). Ziffer = Taste.
//   3) PINS    — was du im Blick behalten willst. Überlebt Reload. Klick = Bühne, × = lösen.
// ADHD: das Gedächtnis liegt sichtbar außerhalb des Kopfes. Asperger: feste Struktur, jedes Mal gleich.
import { idOf, convOf, title, escapeHtml, surfaceIcon } from './core.js';
import { SURFACES } from './stage.js';

export function renderRail(el, store, actions) {
  const s = store.get();
  const now = s.now;

  const nowBlock = `
    <div class="rail-sec rail-now">
      <button class="now-card" data-now>
        <div class="now-k">JETZT  <kbd>⌘.</kbd></div>
        <div class="now-label">${escapeHtml(now ? now.label : 'bereit')}</div>
        <div class="now-go">${now && (now.surface || now.verb) ? 'ausführen →' : 'tippen ⌘K'}</div>
      </button>
    </div>`;

  const surfBlock = `
    <div class="rail-sec">
      <div class="rail-h">Flächen</div>
      ${SURFACES.map((su, i) => {
        const on = s.stageOpen && s.stageSurface === su.id;
        return `<button class="surf${on ? ' on' : ''}" data-surf="${su.id}">
          <span class="surf-ic">${surfaceIcon(su.id)}</span><span class="surf-lbl">${su.label}</span>
          <kbd>${i + 1}</kbd></button>`;
      }).join('')}
    </div>`;

  const pins = s.pins || [];
  const pinBlock = `
    <div class="rail-sec">
      <div class="rail-h">Pins <kbd>⌘P</kbd></div>
      ${pins.length ? pins.map(p => {
        const n = s.byId.get(p.ref);
        const conv = n ? convOf(n) : 'Pending';
        const lbl = n ? title(n) : p.label;
        return `<div class="pin" data-pin="${escapeHtml(p.ref)}" title="${escapeHtml(lbl)}">
          <span class="dot ${conv}"></span><span class="pin-id">${escapeHtml(p.ref)}</span>
          <span class="pin-x" data-unpin="${escapeHtml(p.ref)}">×</span></div>`;
      }).join('') : '<div class="rail-empty">leer — ⌘P zum Anheften</div>'}
    </div>`;

  const titleBar = `<div class="tw-title"><span class="tw-grip">⠿</span><span class="tw-label">Projektmappen-Explorer — SPOT</span></div>`;
  el.innerHTML = titleBar + nowBlock + surfBlock + pinBlock;

  el.querySelector('[data-now]').onclick = () => actions.runNow();
  el.querySelectorAll('[data-surf]').forEach(b => b.onclick = () => actions.toggleStage(b.dataset.surf));
  el.querySelectorAll('.pin').forEach(p => p.onclick = (e) => {
    if (e.target.dataset.unpin) return;
    actions.focusNode(p.dataset.pin);
  });
  el.querySelectorAll('[data-unpin]').forEach(x => x.onclick = (e) => {
    e.stopPropagation(); actions.togglePin({ ref: x.dataset.unpin });
  });
}
