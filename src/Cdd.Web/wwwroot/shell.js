// Cong OS — Workbench-Controller. Vier Regionen (Command-Bar · Explorer|Well|Properties+Copilot · Dock · Status).
// Auswahl (Properties) ist von offenen Dokumenten (Tabs) entkoppelt: Klicken wirft nie das Dokument weg.
import { api, idOf, kindOf, convOf, title, escapeHtml } from './core.js';
import { makeStore, tabKey, tabNodeId } from './store.js';
import { mountMenuBar } from './menubar.js';
import { renderExplorer } from './explorer.js';
import { renderDocTabs, renderDocBody } from './doctabs.js';
import { renderProperties } from './properties.js';
import { renderDock, errorRows, pushOutput as pushOut } from './dock.js';
import { mountCopilot } from './copilot.js';

const store = makeStore();
let $menubar, $explorer, $doctabs, $docbody, $properties, $copilot, $dock, $status, menubar, copilot;

const paintExplorer = () => renderExplorer($explorer, store, actions);
const paintTabs     = () => renderDocTabs($doctabs, store, actions);
const paintBody     = () => renderDocBody($docbody, store, actions);
const paintWell     = () => { paintTabs(); paintBody(); };
const paintProps    = () => renderProperties($properties, store, actions);
const paintDock     = () => renderDock($dock, store, actions);
function markTree() {
  if (!$explorer) return;
  const s = store.get();
  const selId = s.selected ? idOf(s.selected) : null;
  const actId = tabNodeId(s.active);
  $explorer.querySelectorAll('.leaf').forEach(l => {
    l.classList.toggle('sel', l.dataset.id === selId);
    l.classList.toggle('active', l.dataset.id === actId);
  });
}

function openTab(t) {
  const key = tabKey(t);
  let tabs = store.get().openTabs.slice();
  if (!tabs.some(x => tabKey(x) === key)) {
    tabs.push(t);
    if (tabs.length > 12) tabs = tabs.filter((x, i) => tabKey(x) === key || i >= tabs.length - 12);
  }
  store.set({ openTabs: tabs, active: key });
  const nid = tabNodeId(key);
  if (nid) { const n = store.get().byId.get(nid); if (n) store.set({ selected: n }); }
  paintWell(); paintProps(); paintStatus(); markTree();
}

const actions = {
  select(id) { const n = store.get().byId.get(id); if (!n) return; store.set({ selected: n }); paintProps(); markTree(); paintStatus(); },
  openNode(id) { openTab({ kind: 'node', id }); },
  openSingle(kind) { openTab({ kind }); },
  openNodeLens(id, lens) { store.get().lensByTab.set('node:' + id, lens); openTab({ kind: 'node', id }); },
  setActive(key) {
    store.set({ active: key }); const nid = tabNodeId(key);
    if (nid) { const n = store.get().byId.get(nid); if (n) store.set({ selected: n }); }
    paintWell(); paintProps(); paintStatus(); markTree();
  },
  setLens(key, lens) { store.get().lensByTab.set(key, lens); paintBody(); },
  closeTab(key) {
    const s = store.get(); const tabs = s.openTabs.filter(t => tabKey(t) !== key);
    let active = s.active; if (active === key) active = tabs.length ? tabKey(tabs[tabs.length - 1]) : null;
    store.set({ openTabs: tabs, active });
    const nid = tabNodeId(active); if (nid) { const n = store.get().byId.get(nid); if (n) store.set({ selected: n }); }
    paintWell(); paintProps(); paintStatus(); markTree();
  },
  closeActive() { if (store.get().active) actions.closeTab(store.get().active); },
  setPivot(p) { store.set({ treePivot: p }); paintExplorer(); },
  toggleGroup(g) { const c = store.get().collapsed; c.has(g) ? c.delete(g) : c.add(g); paintExplorer(); },
  setDockTab(t) { store.set({ dockTab: t, dockOpen: true }); paintDock(); },
  openDock(t) { store.set({ dockTab: t, dockOpen: true }); paintDock(); },
  toggleDock() { store.set({ dockOpen: !store.get().dockOpen }); paintDock(); },
  toggleTheme() { const r = document.documentElement; r.dataset.theme = r.dataset.theme === 'light' ? '' : 'light'; try { localStorage.setItem('congos-theme', r.dataset.theme || ''); } catch {} },
  focusExplorer() { const f = $explorer.querySelector('.ex-filter'); if (f) f.focus(); },
  focusPalette() { menubar && menubar.focus(); },
  rerender() { paintBody(); },
  derive: async () => {
    actions.pushOutput('▶ derive-tests…'); actions.setRunState('running');
    try { await fetch('/api/derive-tests?write=true', { method: 'POST' }); actions.pushOutput('✓ Tests abgeleitet'); }
    catch (e) { actions.pushOutput('✗ ' + e.message); }
    await reload(); actions.setRunState('done');
    actions.dispatch('Ich habe Tests aus den Specs abgeleitet — prüf die Konvergenz im Board/Drift.');
  },
  dispatch(p) { copilot && copilot.dispatch(p); },
  pushOutput(line) { pushOut(store, line); if (store.get().dockOpen && store.get().dockTab === 'output') paintDock(); },
  setRunState(rs) { store.set({ runState: rs }); paintStatus(); },
  reload: () => reload(),
};

function paintStatus() {
  const s = store.get(); const er = errorRows(store);
  const nErr = er.filter(r => r.sev === 'error').length, nWarn = er.filter(r => r.sev === 'warning').length;
  const run = { idle: 'bereit', running: '● läuft…', done: '✓ fertig', error: '✗ Fehler' }[s.runState] || '';
  const act = s.active ? (tabNodeId(s.active) || s.active) : '—';
  $status.innerHTML =
    `<span class="st tok" data-go="errors"><span class="dot ${nErr ? 'Diverged' : 'Aligned'}"></span>${nErr} Fehler · ${nWarn} Warn</span>` +
    `<span class="st">⬡ ${s.nodes.length} Knoten</span>` +
    `<span class="st">${run}</span>` +
    `<span class="sp">Cong OS · <code>${escapeHtml(act)}</code></span>`;
  const go = $status.querySelector('[data-go=errors]'); if (go) go.onclick = () => actions.openDock('errors');
}

async function reload() {
  let nodes = [], validate = [], diff = null;
  try { nodes = await api.spot(); } catch (e) { actions.pushOutput('✗ /api/spot: ' + e.message); }
  try { validate = await api.validate(); } catch {}
  try { diff = await api.diff(); } catch {}
  const byId = new Map(nodes.map(n => [idOf(n), n]));
  let sel = store.get().selected; if (sel) sel = byId.get(idOf(sel)) || null;
  store.set({ nodes, byId, validate, diff, selected: sel });
  paintExplorer(); paintWell(); paintProps(); paintDock(); paintStatus();
}

function fmtEv(ev) {
  switch (ev.t) {
    case 'started': return '● ' + (ev.model || 'engine');
    case 'tool': return '🔧 ' + (ev.name || 'tool') + (ev.input ? ' ' + String(ev.input).slice(0, 120) : '');
    case 'toolresult': return '↳ ' + String(ev.text || '').slice(0, 200);
    case 'done': return '✓ done' + (ev.cost ? ` · $${(+ev.cost).toFixed(4)}` : '');
    case 'error': return '⚠ ' + (ev.error || '');
    default: return '';
  }
}

function boot() {
  $menubar = document.querySelector('#menubar');
  $explorer = document.querySelector('#explorer');
  $doctabs = document.querySelector('#doctabs');
  $docbody = document.querySelector('#docbody');
  $properties = document.querySelector('#properties');
  $copilot = document.querySelector('#copilot');
  $dock = document.querySelector('#dock');
  $status = document.querySelector('#status');

  try { const th = localStorage.getItem('congos-theme'); if (th) document.documentElement.dataset.theme = th; } catch {}

  menubar = mountMenuBar($menubar, store, actions);
  copilot = mountCopilot($copilot, store, {
    onEvent: (ev) => {
      const line = fmtEv(ev); if (line) actions.pushOutput(line);
      if (ev.t === 'started') actions.setRunState('running');
      else if (ev.t === 'done') actions.setRunState('done');
      else if (ev.t === 'error') actions.setRunState('error');
    },
  });

  window.addEventListener('keydown', (e) => {
    if (!(e.metaKey || e.ctrlKey)) return;
    const k = e.key.toLowerCase();
    if (k === 'k') { e.preventDefault(); menubar.focus(); }
    else if (k === 'j') { e.preventDefault(); actions.toggleDock(); }
    else if (k === 'e') { e.preventDefault(); actions.openDock('errors'); }
    else if (k === '1') { e.preventDefault(); actions.focusExplorer(); }
    else if (k === ',') { e.preventDefault(); actions.openSingle('settings'); }
    else if (k === 'w') { e.preventDefault(); actions.closeActive(); }
  });

  reload().then(() => {
    const first = store.get().nodes.find(n => kindOf(n) === 'spec') || store.get().nodes[0];
    if (first) actions.openNode(idOf(first));
  });
}

boot();
