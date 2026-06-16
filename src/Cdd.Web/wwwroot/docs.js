// Doku-Linse: die lebende Doku (Export.toMarkdown) — exakt was die KI als Kontext sieht.
// Knoten-Ids im Text werden klickbar (Cross-Refs ins Modell).
import { idOf } from './core.js';

function md2html(md) {
  const esc = s => s.replace(/[&<>]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;' }[c]));
  const inline = t => esc(t).replace(/\*\*(.+?)\*\*/g, '<b>$1</b>').replace(/`([^`]+)`/g, '<code>$1</code>');
  let html = '', inList = false;
  for (const ln of md.split('\n')) {
    const closeList = () => { if (inList) { html += '</ul>'; inList = false; } };
    if (/^### /.test(ln)) { closeList(); html += `<h3>${inline(ln.slice(4))}</h3>`; }
    else if (/^## /.test(ln)) { closeList(); html += `<h2>${inline(ln.slice(3))}</h2>`; }
    else if (/^# /.test(ln)) { closeList(); html += `<h1>${inline(ln.slice(2))}</h1>`; }
    else if (/^\s*- /.test(ln)) { if (!inList) { html += '<ul>'; inList = true; } html += `<li>${inline(ln.replace(/^\s*- /, ''))}</li>`; }
    else if (ln.trim() === '') { closeList(); }
    else { closeList(); html += `<p>${inline(ln)}</p>`; }
  }
  if (inList) html += '</ul>';
  return html;
}

export async function renderDocs(el, store, actions) {
  el.innerHTML = '<div class="muted">Doku wird aus dem SPOT generiert…</div>';
  let md;
  try { md = await fetch('/api/export').then(r => r.text()); }
  catch { el.innerHTML = '<div class="muted">Doku nicht erreichbar.</div>'; return; }
  el.innerHTML = `<div class="docs">${md2html(md)}</div>`;
  const ids = new Set(store.get().nodes.map(idOf));
  el.querySelectorAll('.docs code').forEach(c => {
    if (ids.has(c.textContent)) { c.classList.add('xref'); c.onclick = () => actions.select(c.textContent); c.ondblclick = () => actions.openNode(c.textContent); }
  });
}
