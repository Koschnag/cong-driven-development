// Cong OS — der chat-primäre Store. Ein Modell, ein Faden, eine Bühne.
// Bewusst schmal: nur was das Cockpit (cockpit.js → thread/omni/rail/stage) wirklich nutzt.
import { createStore } from './core.js';

export function makeStore() {
  return createStore({
    // ── Daten (das eine Modell) ──
    nodes: [], byId: new Map(),
    validate: [], diff: null,

    // ── chat-primärer Spine ──
    mode: 'dev',                     // dev|plan|infra|prod|ask — Engine-Frame, EIN Wort (= Flächen-Vokabel)
    engine: 'claude',                // claude|mistral|ollama (souverän wählbar)
    runState: 'idle',                // idle|running|done|error

    // ── Schiene: externalisiertes Arbeitsgedächtnis ──
    now: null,                       // {label, surface?, verb?} — die eine nächste Aktion
    pins: [],                        // [{ref, kind, label}] — überlebt Reload (localStorage)

    // ── Bühne: genau eine herbeigerufene Fläche ──
    stageOpen: false,
    stageSurface: 'model',           // plan|model|dev|infra|prod|docs|drift|node|settings
    stageArg: null,                  // z. B. Knoten-Id bei surface='node'
    nodeLens: 'inspector',           // inspector|graph (Bühne, Knoten)

    // ── Infra/Prod-Heartbeat (Komodo/Coolify via MCP-Backend) ──
    infra: null, infraOk: false,

    // ── von der Cube-Linse genutzt ──
    cubeRows: 'kind', cubeCols: 'conv',
  });
}
