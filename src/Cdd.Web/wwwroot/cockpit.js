// Cong OS — chat-primärer Cockpit-Controller.
//
// EIN AXIOM: Es gibt genau einen Gesprächsfaden (den Spine). Alles andere — SPOT-Modell,
// Plan, Dev-Sandbox, Infra-Status, Prod — ist eine PROJEKTION, die NEBEN den Faden
// gerufen wird (die Bühne), nie eine konkurrierende Top-Level-Fläche.
//
// Drei Regionen, immer dieselben:
//   rail   (links)  = externalisiertes Arbeitsgedächtnis: NOW (eine nächste Aktion) + Pins.
//   thread (mitte)  = der Faden. Füllt den Bildschirm. Hyperfokus-tauglich.
//   stage  (rechts) = die EINE herbeigerufene Fläche. Standard: zu. Esc schließt.
//
// Navigation = ein Modell: ⌘K (eine Tür) tippen → Befehl/Knoten/Frage. Ziffern rufen Flächen.
import { api, idOf, kindOf, convOf, title, summary, escapeHtml } from './core.js';
import { makeStore } from './store.js';
import { mountThread } from './thread.js';
import { mountOmni } from './omni.js';
import { renderRail } from './rail.js';
import { renderStage, SURFACES } from './stage.js';
import { errorRows } from './dock.js';

const store = makeStore();
let $omni, $rail, $thread, $stage, $status, omni, thread;

const paintRail   = () => renderRail($rail, store, actions);
const paintStage  = () => renderStage($stage, store, actions);
const paintStatus = () => renderStatus();

// ── Aktionen: der gesamte Verb-Wortschatz des Cockpits an einem Ort ──
const actions = {
  // Spine
  ask: (p) => thread && thread.ask(p),                 // an die Engine schicken (der Normalfall)
  say: (role, text, opts) => thread && thread.say(role, text, opts),

  // Bühne: genau eine Fläche zur Zeit, am Faden angeheftet.
  summon(surface, arg) {
    store.set({ stageSurface: surface, stageArg: arg ?? null, stageOpen: true });
    paintStage(); paintRail(); paintStatus();
  },
  closeStage() { store.set({ stageOpen: false }); paintStage(); paintRail(); paintStatus(); },
  toggleStage(surface) {
    const s = store.get();
    if (s.stageOpen && s.stageSurface === surface) actions.closeStage();
    else actions.summon(surface);
  },
  // Einen SPOT-Knoten betrachten = Bühne auf "node" mit Id. Wirft den Faden NIE weg.
  focusNode(id) { actions.summon('node', id); },

  // Pins = persistentes Arbeitsgedächtnis. Toggle, überlebt Reloads (localStorage).
  togglePin(ref) {
    const pins = store.get().pins.slice();
    const i = pins.findIndex(p => p.ref === ref.ref);
    if (i >= 0) pins.splice(i, 1); else pins.unshift(ref);
    store.set({ pins }); persistPins(pins); paintRail();
  },
  isPinned: (ref) => store.get().pins.some(p => p.ref === ref),

  // NOW = die eine nächste Aktion. Vom Faden/Validate gesetzt, in der Schiene gezeigt.
  setNow(now) { store.set({ now }); paintRail(); paintStatus(); },

  // Engine-Wahl + Modus (develop/plan/admin/deploy/ask) — ein Wort, kein Menü-Dschungel.
  setMode(m) { store.set({ mode: m }); paintRail(); paintStatus(); if (omni) omni.reflectMode(); },
  setEngine(e) { store.set({ engine: e }); paintStatus(); },
  setRunState(rs) { store.set({ runState: rs }); paintStatus(); },

  // Eine Engine-Aktion mit fester Semantik (Tests ableiten), als Faden-Turn sichtbar.
  derive: async () => {
    actions.setRunState('running');
    actions.say('system', '▶ Tests aus den Specs ableiten…');
    try { await fetch('/api/derive-tests?write=true', { method: 'POST' }); actions.say('system', '✓ Tests abgeleitet.'); }
    catch (e) { actions.say('system', '✗ ' + e.message); }
    await reload(); actions.setRunState('done');
    actions.setNow({ label: 'Konvergenz prüfen', surface: 'drift' });
  },

  focusOmni: () => omni && omni.focus(),
  reload: () => reload(),
  rerender: () => { paintStage(); },
  // Geführtes Durchklicken: 2–4 anklickbare nächste Schritte aus dem Modellzustand in den Faden.
  suggestNext: () => thread && thread.suggest(nextSteps()),
  toggleTheme() {
    const r = document.documentElement;
    r.dataset.theme = r.dataset.theme === 'light' ? '' : 'light';
    try { localStorage.setItem('congos-theme', r.dataset.theme || ''); } catch {}
  },
};

// ── Status: EINE Zeile Wahrheit, immer sichtbar (kein verstecktes State) ──
function renderStatus() {
  const s = store.get();
  const er = errorRows(store);
  const nErr = er.filter(r => r.sev === 'error').length;
  const nWarn = er.filter(r => r.sev === 'warning').length;
  const run = { idle: '', running: '● läuft…', done: '✓ fertig', error: '✗ Fehler' }[s.runState] || '';
  const now = s.now ? s.now.label : '—';
  $status.innerHTML =
    `<button class="st tok" data-go="drift"><span class="dot ${nErr ? 'Diverged' : 'Aligned'}"></span>${nErr} Diverged · ${nWarn} offen</button>` +
    `<button class="st tok" data-go="infra"><span class="dot ${s.infraOk ? 'Aligned' : 'Pending'}"></span>Infra ${s.infraOk ? 'grün' : '—'}</button>` +
    `<span class="st">⬡ ${s.nodes.length} Knoten · ${escapeHtml(s.mode)} · ${escapeHtml(s.engine)}</span>` +
    `<span class="st">${run}</span>` +
    `<span class="now"><b>Jetzt:</b> ${escapeHtml(now)} <kbd>⌘.</kbd></span>`;
  $status.querySelectorAll('[data-go]').forEach(b => b.onclick = () => actions.summon(b.dataset.go));
}

// ── Datenladung: SPOT + Validate + Diff → Store. Setzt NOW aus dem Zustand ──
async function reload() {
  let nodes = [], validate = [], diff = null;
  try { nodes = await api.spot(); } catch (e) { actions.say('system', '✗ /api/spot: ' + e.message); }
  try { validate = await api.validate(); } catch {}
  try { diff = await api.diff(); } catch {}
  const byId = new Map(nodes.map(n => [idOf(n), n]));
  store.set({ nodes, byId, validate, diff });
  deriveNow();
  paintRail(); paintStage(); paintStatus();
}

// NOW first-principles ableiten: der dringendste offene Punkt im Modell ist die nächste Aktion.
function deriveNow() {
  const s = store.get();
  const diverged = s.nodes.filter(n => convOf(n) === 'Diverged');
  const pendingSpec = s.nodes.filter(n => kindOf(n) === 'spec' && convOf(n) === 'Pending');
  if (diverged.length) actions.setNow({ label: `${diverged.length}× Diverged auflösen`, surface: 'drift' });
  else if (pendingSpec.length) actions.setNow({ label: `Tests für ${pendingSpec.length} Spec(s) ableiten`, surface: null, verb: 'derive' });
  else actions.setNow({ label: 'Modell sauber — neuen Auftrag tippen (⌘K)', surface: null });
}

// Geführte nächste Schritte (mehr als die EINE NOW-Aktion): anklickbare Vorschläge, aus dem Zustand abgeleitet.
function nextSteps() {
  const s = store.get();
  const out = [];
  const diverged = s.nodes.filter(n => convOf(n) === 'Diverged');
  const pendingSpec = s.nodes.filter(n => kindOf(n) === 'spec' && convOf(n) === 'Pending');
  if (diverged.length) out.push({ label: `⚠ ${diverged.length}× Diverged auflösen`, run: () => actions.summon('drift') });
  if (pendingSpec.length) out.push({ label: `✓ Tests für ${pendingSpec.length} Spec(s) ableiten`, run: () => actions.derive() });
  out.push({ label: '◆ Plan ansehen', run: () => actions.summon('plan') });
  out.push({ label: '⬡ Modell öffnen', run: () => actions.summon('model') });
  out.push({ label: '☁ Infra-Status', run: () => actions.summon('infra') });
  return out.slice(0, 4);
}

// ── Pins persistieren (Arbeitsgedächtnis überlebt Reload) ──
function persistPins(pins) { try { localStorage.setItem('congos-pins', JSON.stringify(pins)); } catch {} }
function loadPins() { try { return JSON.parse(localStorage.getItem('congos-pins') || '[]'); } catch { return []; } }

function boot() {
  $omni = document.querySelector('#omni');
  $rail = document.querySelector('#rail');
  $thread = document.querySelector('#thread');
  $stage = document.querySelector('#stage');
  $status = document.querySelector('#status');

  try { const th = localStorage.getItem('congos-theme'); if (th) document.documentElement.dataset.theme = th; } catch {}
  store.set({ pins: loadPins() });

  thread = mountThread($thread, store, actions);
  omni = mountOmni($omni, store, actions);

  // EIN Tastaturmodell. Überall gleich. ⌘+Taste ruft genau eine Sache.
  window.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') { if (store.get().stageOpen) { actions.closeStage(); e.preventDefault(); } return; }
    if (!(e.metaKey || e.ctrlKey)) return;
    const k = e.key.toLowerCase();
    if (k === 'k') { e.preventDefault(); omni.focus(); }                          // die eine Tür
    else if (k === '.') { e.preventDefault(); runNow(); }                          // tu, was JETZT dran ist
    else if (k === 'enter') { e.preventDefault(); thread.focusInput(); }           // zurück zum Tippen
    else if (k >= '1' && k <= '6') { e.preventDefault(); actions.toggleStage(SURFACES[+k - 1].id); } // Flächen
    else if (k === '0') { e.preventDefault(); actions.closeStage(); }              // Faden Vollbild
    else if (k === 'p') { e.preventDefault(); omni.focus('pin '); }
    else if (k === ',') { e.preventDefault(); actions.summon('settings'); }
  });

  paintRail(); paintStage(); paintStatus();
  reload().then(() => {
    thread.welcome();
    actions.suggestNext();
  });
  pollInfra();
}

// Das "Was-jetzt": ein Klick/Tastendruck führt die nächste Aktion aus (nie ein Sackgassen-Zustand).
function runNow() {
  const now = store.get().now;
  if (!now) return;
  if (now.verb === 'derive') actions.derive();
  else if (now.surface) actions.summon(now.surface);
  else actions.focusOmni();
}
actions.runNow = runNow;

// Infra-Heartbeat (Komodo/DC via MCP-Backend; hier defensiv, kein toter View wenn offline).
async function pollInfra() {
  try {
    const r = await fetch('/api/infra/status');
    if (r.ok) { const j = await r.json(); store.set({ infra: j, infraOk: !!(j && j.ok) }); }
  } catch { store.set({ infraOk: false }); }
  paintStatus();
  setTimeout(pollInfra, 30000);
}

boot();
