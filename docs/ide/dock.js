// Bottom-Dock im VS-Stil: Error List · Output · Drift. Doppelklick auf eine Zeile = „Gehe zu Quelle".
import { idOf, convOf, title, escapeHtml } from './core.js';

const DOCK_TABS = [['errors', 'Error List'], ['output', 'Output'], ['drift', 'Drift']];
const SEV_ICON = { error: '⛔', warning: '⚠', info: 'ℹ' };

// Vereinigung dreier existierender Quellen — alle ohne neues Backend.
export function errorRows(store) {
  const s = store.get(), rows = [];
  // (1) validate() — heute []; defensiv gegen Feldnamen-Varianten.
  (s.validate || []).forEach(f => {
    const sev = String(f.Severity || f.severity || 'Warning').toLowerCase();
    rows.push({ sev: sev === 'error' ? 'error' : 'warning', msg: f.Message || f.message || f.Text || 'Validierung', id: f.EntityId || f.Entity || f.Id || '' });
  });
  // (2) Konvergenz Diverged/Orphaned
  (s.nodes || []).forEach(n => {
    const c = convOf(n);
    if (c === 'Diverged') rows.push({ sev: 'error', msg: 'Diverged — Modell und Code stimmen nicht überein', id: idOf(n) });
    else if (c === 'Orphaned') rows.push({ sev: 'warning', msg: 'Orphaned — kein Bezug im Modell', id: idOf(n) });
  });
  // (3) diff Pending-Bucket als Info (sonst sähe das saubere Modell „leer/kaputt" aus)
  ((s.diff && s.diff.Pending) || []).forEach(n => rows.push({ sev: 'info', msg: 'Pending — Modell existiert, Code/Tests offen', id: idOf(n) }));
  return rows;
}

export function pushOutput(store, line) {
  const o = store.get().output; o.push(line); if (o.length > 600) o.shift();
}

export function renderDock(el, store, actions) {
  const s = store.get();
  const rows = errorRows(store);
  const nErr = rows.filter(r => r.sev === 'error').length;
  const nWarn = rows.filter(r => r.sev === 'warning').length;
  const nInfo = rows.filter(r => r.sev === 'info').length;

  el.classList.toggle('closed', !s.dockOpen);
  el.innerHTML = `
    <div class="dock-tabs">
      ${DOCK_TABS.map(([v, l]) => {
        const b = v === 'errors' ? `<span class="badge">${nErr}⛔ ${nWarn}⚠</span>` : '';
        return `<button class="dock-tab${s.dockTab === v ? ' active' : ''}" data-dt="${v}">${l} ${b}</button>`;
      }).join('')}
      <button class="dock-min" title="Dock ein/aus (⌘J)">${s.dockOpen ? '▾' : '▴'}</button>
    </div>
    <div class="dock-body">${s.dockOpen ? bodyHtml(s, rows, nErr, nWarn, nInfo) : ''}</div>`;

  el.querySelectorAll('.dock-tab').forEach(b => b.onclick = () => actions.setDockTab(b.dataset.dt));
  el.querySelector('.dock-min').onclick = () => actions.toggleDock();
  el.querySelectorAll('[data-id]').forEach(rw => {
    rw.onclick = () => actions.select(rw.dataset.id);
    rw.ondblclick = () => actions.openNode(rw.dataset.id);
  });
}

function bodyHtml(s, rows, nErr, nWarn, nInfo) {
  if (s.dockTab === 'output') {
    const out = (s.output || []);
    return `<pre class="output">${out.length ? escapeHtml(out.join('\n')) : '— keine Engine-/Build-Ausgabe —'}</pre>`;
  }
  if (s.dockTab === 'drift') {
    const d = s.diff || {};
    return ['Diverged', 'Orphaned', 'Pending', 'Aligned'].map(b => {
      const ns = d[b] || [];
      return `<div class="drift-bucket"><div class="drift-head"><span class="dot ${b}"></span>${b} <span class="group-count">${ns.length}</span></div>
        ${ns.map(n => row('info', '', idOf(n), title(n))).join('')}</div>`;
    }).join('');
  }
  // errors
  const banner = (nErr + nWarn === 0)
    ? `<div class="ok-banner">✓ Modell sauber — 0 Fehler, 0 Warnungen${nInfo ? ` · ${nInfo} Pending` : ''}</div>` : '';
  return banner + rows.map(r => row(r.sev, r.msg, r.id)).join('');
}

function row(sev, msg, id, sub) {
  return `<div class="errrow ${sev}" data-id="${escapeHtml(id)}">
    <span class="ei">${SEV_ICON[sev] || '•'}</span>
    <span class="em">${escapeHtml(msg || sub || '')}</span>
    <span class="eid">${escapeHtml(id)}</span></div>`;
}
