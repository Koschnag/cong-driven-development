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
import { api, runLoop, idOf, kindOf, convOf, title, summary, escapeHtml,
         makeEntry, spliceRelation, PREFIX, slugify, KIND_LABEL } from './core.js';
import { makeStore } from './store.js';
import { mountThread } from './thread.js';
import { mountOmni } from './omni.js';
import { renderRail } from './rail.js';
import { renderStage, SURFACES } from './stage.js';
import { renderDiagram } from './diagram.js';
import { errorRows, renderDock } from './dock.js';
import { mountMenubar } from './menubar.js';

const store = makeStore();
let $omni, $rail, $thread, $stage, $status, $menubar, $dock, $maindia, omni, thread;

const paintRail    = () => renderRail($rail, store, actions);
const paintStage   = () => renderStage($stage, store, actions);
const paintStatus  = () => renderStatus();
const paintDock    = () => renderDock($dock, store, actions);
// Split-Mitte: Diagramm (links) UND Chat (rechts) immer sichtbar — kein Umschalten.
const paintDiagram = () => renderDiagram($maindia, store, actions);

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
  setEngine(e) { store.set({ engine: e }); paintStatus(); if (omni) omni.reflectMode && omni.reflectMode(); },
  setRunState(rs) { store.set({ runState: rs }); paintStatus(); },

  // VS-Bottom-Dock (Fehlerliste · Ausgabe · Drift)
  setDockTab(t) { store.set({ dockOpen: true, dockTab: t }); paintDock(); },
  openDock(t) { store.set({ dockOpen: true, dockTab: t }); paintDock(); },
  toggleDock() { store.set({ dockOpen: !store.get().dockOpen }); paintDock(); },
  pushOutput(line) { const o = store.get().output; o.push(line); if (o.length > 400) o.shift(); if (store.get().dockOpen && store.get().dockTab === 'output') paintDock(); },
  // Aliase: dock.js navigiert via select/openNode → chat-primär ist das focusNode (Bühne auf Knoten).
  select: (id) => actions.focusNode(id),
  openNode: (id) => actions.focusNode(id),

  // Eine Engine-Aktion mit fester Semantik (Tests ableiten), als Faden-Turn sichtbar.
  derive: async () => {
    actions.setRunState('running');
    actions.say('system', '▶ Tests aus den Specs ableiten…');
    try { await fetch('/api/derive-tests?write=true', { method: 'POST' }); actions.say('system', '✓ Tests abgeleitet.'); }
    catch (e) { actions.say('system', '✗ ' + e.message); }
    await reload(); actions.setRunState('done');
    actions.setNow({ label: 'Konvergenz prüfen', surface: 'drift' });
  },

  // Loop bis Konvergenz: das Cockpit treibt cdd-mapper (das Modell treibt die Engine), jeder Schritt im Faden,
  // das Gate entscheidet — nicht „Agent sagt fertig". Der sichtbare Gegenentwurf.
  loop: () => {
    actions.setRunState('running');
    actions.say('system', '▶ Loop bis Konvergenz (cdd-mapper) — das Modell treibt die Engine, das Gate entscheidet.');
    runLoop({ MaxSpecs: 1, MaxAttempts: 3 }, (ev) => {
      switch (ev.t) {
        case 'started': actions.say('system', '● ' + (ev.model || '')); break;
        case 'spec': actions.say('system', '◆ ' + ev.id + ' — ' + (ev.title || '')); break;
        case 'attempt': actions.say('system', '  Versuch ' + ev.n + '/' + ev.max + ' → claude -p'); break;
        case 'gate': actions.say('system', ev.ok ? ('  ✓ Gate grün' + (ev.skipped ? ' — claude übersprungen (Token gespart)' : '')) : '  … Gate noch rot'); break;
        case 'spec_done': actions.say('system', (ev.konvergiert ? '✓ ' : '✗ ') + ev.id + ' nach ' + ev.versuche + ' Versuch(en)'); break;
        case 'done': actions.say('system', `✓ Loop fertig — ${ev.konvergiert}/${ev.total} konvergiert`); actions.setRunState('done'); actions.reload(); actions.summon('drift'); break;
        case 'error': actions.say('system', '⚠ ' + (ev.error || '')); actions.setRunState('error'); break;
      }
    });
  },

  // ── @-Gedächtnis (Wahrheit #2): cong.db-Volltextsuche (nur sensitive=0) → Bühne mit Treffer-Karten ──
  async dwhSearch(q, mode) {
    const term = (q || '').trim();
    if (!term) return;
    mode = mode || store.get().dwhMode || 'keyword';
    store.set({ dwhQuery: term, dwhMode: mode, dwhLoading: true, dwhHits: [] });
    actions.summon('memory');
    try {
      const res = await (mode === 'semantic' ? api.dwhSemantic(term, 16) : api.dwh(term, 24));
      store.set({ dwhHits: res.hits || [], dwhAvailable: res.available !== false, dwhNote: res.note || '', dwhLoading: false });
    } catch (e) {
      store.set({ dwhHits: [], dwhAvailable: false, dwhNote: e.message, dwhLoading: false });
    }
    paintStage();
  },

  // ── Toolbox: getypte Modell-Knoten + Relationen anlegen (deterministisch, kein LLM) ──
  repaintDiagram: () => paintDiagram(),
  async upsert(entry) {
    try {
      const r = await fetch('/api/spot/' + encodeURIComponent(idOf(entry)), {
        method: 'PUT', headers: { 'content-type': 'application/json' }, body: JSON.stringify(entry) });
      if (!r.ok) { actions.say('system', '✗ PUT ' + idOf(entry) + ': ' + (await r.text())); return false; }
      return true;
    } catch (e) { actions.say('system', '✗ ' + e.message); return false; }
  },
  async newNode(kind) {
    const t = window.prompt(`Neuer ${KIND_LABEL[kind] || kind} — Titel/Name:`);
    if (t == null || !t.trim()) return;
    const id = PREFIX[kind] + slugify(t);
    if (store.get().byId.get(id)) { actions.say('system', `✗ Id ${id} existiert schon.`); return; }
    const entry = makeEntry(kind, t.trim(), id);
    if (await actions.upsert(entry)) { await reload(); actions.focusNode(id); actions.say('system', `＋ ${id} angelegt (Pending) — über den Faden mit Inhalt füllen.`); }
  },
  askNew(kind) {
    actions.ask(`Lege einen ${KIND_LABEL[kind] || kind}-Knoten im SPOT an (Id-Präfix ${PREFIX[kind]}…): beschreibe Titel/Intent und – bei einer Spec – die Given/When/Then-Kriterien. Erzeuge eine valide SpotEntry und zeig sie mir vor dem Anwenden.`);
  },
  async addRelation(srcId, relDef, dstId) {
    const node = store.get().byId.get(srcId);
    if (!node) return;
    if (!relDef.from.includes(kindOf(node))) { actions.say('system', `✗ ${relDef.rel} nicht erlaubt ab ${kindOf(node)}.`); paintDiagram(); return; }
    const updated = spliceRelation(node, relDef, dstId);
    if (await actions.upsert(updated)) { await reload(); actions.say('system', `→ ${srcId} ${relDef.rel} ${dstId}`); }
    else paintDiagram();
  },

  // Faden-Vollbild: klappt das Diagramm ein (Split bleibt Default-Wahl, aber der Faden KANN den
  // Schirm füllen — §11 „Hyperfokus" wiederhergestellt, data-spine=chat wird wieder wahr).
  toggleDiagram() {
    const collapsed = !store.get().diagramCollapsed;
    store.set({ diagramCollapsed: collapsed });
    const main = document.querySelector('#main');
    if (main) main.classList.toggle('dia-collapsed', collapsed);
    if (!collapsed) paintDiagram();   // Cytoscape neu einpassen (war 0 px breit)
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
  paintRail(); paintStage(); paintStatus(); paintDock();
  paintDiagram();   // Diagramm immer sichtbar (Split-Mitte)
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
  if (pendingSpec.length) out.push({ label: '▶ Loop bis Konvergenz', run: () => actions.loop() });
  out.push({ label: '◆ Plan ansehen', run: () => actions.summon('plan') });
  out.push({ label: '⬡ Modell öffnen', run: () => actions.summon('model') });
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
  $menubar = document.querySelector('#menubar');
  $dock = document.querySelector('#dock');
  $maindia = document.querySelector('#maindia');
  const reopen = document.querySelector('#dia-reopen'); if (reopen) reopen.onclick = () => actions.toggleDiagram();

  try { const th = localStorage.getItem('congos-theme'); if (th) document.documentElement.dataset.theme = th; } catch {}
  const qsTheme = new URLSearchParams(location.search).get('theme');
  if (qsTheme === 'light') document.documentElement.dataset.theme = 'light';
  else if (qsTheme === 'dark') document.documentElement.dataset.theme = '';
  store.set({ pins: loadPins() });

  thread = mountThread($thread, store, actions);
  omni = mountOmni($omni, store, actions);
  mountMenubar($menubar, store, actions);

  // EIN Tastaturmodell. Überall gleich. ⌘+Taste ruft genau eine Sache.
  window.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') {
      if (store.get().armRel) { store.set({ armRel: null }); paintDiagram(); e.preventDefault(); return; }
      if (store.get().stageOpen) { actions.closeStage(); e.preventDefault(); } return;
    }
    if (!(e.metaKey || e.ctrlKey)) return;
    const k = e.key.toLowerCase();
    if (k === 'k') { e.preventDefault(); omni.focus(); }                          // die eine Tür
    else if (k === '.') { e.preventDefault(); runNow(); }                          // tu, was JETZT dran ist
    else if (k === 'j') { e.preventDefault(); actions.toggleDock(); }              // VS-Bottom-Dock
    else if (k === 'enter') { e.preventDefault(); thread.focusInput(); }           // zurück zum Tippen
    else if (k >= '1' && +k <= SURFACES.length) { e.preventDefault(); actions.toggleStage(SURFACES[+k - 1].id); } // Flächen (rechte Bühne)
    else if (k === '0') { e.preventDefault(); actions.toggleDiagram(); }            // Faden Vollbild (Diagramm ein/aus)
    else if (k === 'p') { e.preventDefault(); omni.focus('pin '); }
    else if (k === ',') { e.preventDefault(); actions.summon('settings'); }
  });

  paintRail(); paintStage(); paintStatus(); paintDock();
  reload().then(() => {
    thread.welcome();
    actions.suggestNext();
    // Deep-Links: ?stage=<fläche> ruft eine Bühne · ?dia=<sicht> wählt die Diagramm-/Formal-Sicht.
    const qs = new URLSearchParams(location.search);
    const st = qs.get('stage'); if (st) actions.summon(st);
    const dv = qs.get('dia'); if (dv) { store.set({ diagramView: dv }); paintDiagram(); }
    const nd = qs.get('node'); if (nd) actions.focusNode(nd);
    if (qs.get('full') === '1') actions.toggleDiagram();   // Fokus: Faden Vollbild, Diagramm eingeklappt
    const dwh = qs.get('dwh'); if (dwh) actions.dwhSearch(dwh);   // @-Gedächtnis direkt öffnen
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
