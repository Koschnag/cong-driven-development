// Historie: git log über .spot/ — die Modell-Zeitreise (jeder Knoten ein git-diffbares JSON-File).
// Klick auf einen Commit lässt den Faden die Änderung erklären (chat-primär bewahrt).
import { api, escapeHtml } from './core.js';

export function renderHistory(el, store, actions) {
  el.innerHTML = `
    <div class="stage-hint">Modell-Historie — jeder SPOT-Knoten ist ein git-diffbares JSON-File, also IST <code>git log</code> über <code>.spot/</code> die Historie des Modells. Klick einen Commit → der Faden erklärt die Änderung.</div>
    <div class="hist-list" id="hist-list"><div class="muted pad">lade…</div></div>`;
  const list = el.querySelector('#hist-list');
  api.history(80).then(commits => {
    if (!Array.isArray(commits) || !commits.length) {
      list.innerHTML = '<div class="muted pad">Keine git-Historie (kein Repo / kein git).</div>'; return;
    }
    list.innerHTML = commits.map(c =>
      `<div class="hist-row" data-h="${escapeHtml(c.Short)}" data-subj="${escapeHtml(c.Subject)}">
        <span class="hist-date">${escapeHtml(c.Date)}</span>
        <code class="hist-hash">${escapeHtml(c.Short)}</code>
        <span class="hist-subj">${escapeHtml(c.Subject)}</span>
        <span class="hist-author">${escapeHtml(c.Author)}</span></div>`).join('');
    list.querySelectorAll('.hist-row').forEach(r => r.onclick = () =>
      actions.ask(`Was änderte sich im Modell mit Commit ${r.dataset.h} („${r.dataset.subj}")? Fasse die SPOT-Änderung knapp zusammen.`));
  }).catch(() => { list.innerHTML = '<div class="muted pad">Historie nicht erreichbar.</div>'; });
}
