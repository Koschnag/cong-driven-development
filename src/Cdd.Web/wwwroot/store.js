// Cong OS — der Workbench-Store: ein Knoten = ein „Solution"-Artefakt, Konvergenz = der Workflow.
// Trennt bewusst `selected` (treibt nur Properties) von `openTabs/active` (das Dokument-Well) —
// damit Auswählen nie das offene Dokument wegwirft (der VS-Reflex).
import { createStore, idOf, kindOf, convOf, title } from './core.js';

// Die 5 „Flächen" sind jetzt echte Baum-Filter (kinds→surface), keine toten Knöpfe mehr.
export const SURFACE_KINDS = {
  plan:    ['premise', 'decision', 'risk', 'spec'],
  ideate:  ['term', 'knowledge'],
  develop: ['spec', 'test', 'component'],
  monitor: ['invariant', 'risk'],
  deploy:  ['component', 'tool', 'infra'],
};
const SURFACE_ORDER = ['plan', 'ideate', 'develop', 'monitor', 'deploy'];

export function makeStore() {
  return createStore({
    nodes: [], byId: new Map(),
    validate: [], diff: null,
    selected: null,                  // Properties-Fokus (zerstörungsfrei)
    openTabs: [],                    // [{kind:'node'|'cube'|'docs'|'board'|'settings', id?}]
    active: null,                    // tabKey
    lensByTab: new Map(),            // tabKey -> 'inspector'|'graph'
    surface: 'develop',             // bleibt für den Copilot/Engine-Request erhalten
    treePivot: 'kind',               // kind|conv|surface
    treeFilter: '',
    collapsed: new Set(),
    cubeRows: 'kind', cubeCols: 'conv',
    dockOpen: true, dockTab: 'errors', // errors|output|drift
    output: [],
    runState: 'idle',                // idle|running|done|error
  });
}

export const tabKey = (t) => (t.kind === 'node' ? 'node:' + t.id : t.kind);
export const tabNodeId = (key) => (key && key.startsWith('node:') ? key.slice(5) : null);

const KIND_RANK = { spec: 0, test: 1, component: 2, term: 3, decision: 4, premise: 5, invariant: 6, risk: 7, knowledge: 8, tool: 9, infra: 10 };
const CONV_RANK = { Diverged: 0, Orphaned: 1, Pending: 2, Aligned: 3 };

// Knoten zu Baumgruppen [{key,label,count,leaves:[{id,title,conv}]}] gemäß Pivot + Filter.
export function groupTree(store) {
  const s = store.get();
  const f = (s.treeFilter || '').trim().toLowerCase();
  const match = (n) => !f || idOf(n).toLowerCase().includes(f) || (title(n) || '').toLowerCase().includes(f);
  const nodes = s.nodes.filter(match);
  const groups = new Map();
  const push = (key, n) => { (groups.get(key) || groups.set(key, []).get(key)).push(n); };

  if (s.treePivot === 'surface') {
    for (const n of nodes) {
      const k = kindOf(n);
      for (const surf of SURFACE_ORDER) if (SURFACE_KINDS[surf].includes(k)) push(surf, n);
    }
  } else {
    const key = s.treePivot === 'conv' ? convOf : kindOf;
    for (const n of nodes) push(key(n), n);
  }

  const rank = (k) => s.treePivot === 'conv' ? (CONV_RANK[k] ?? 9)
                    : s.treePivot === 'surface' ? SURFACE_ORDER.indexOf(k)
                    : (KIND_RANK[k] ?? 99);
  return [...groups.entries()]
    .map(([key, ns]) => ({
      key, count: ns.length,
      leaves: ns.map(n => ({ id: idOf(n), title: title(n), conv: convOf(n) }))
                .sort((a, b) => a.id.localeCompare(b.id)),
    }))
    .sort((a, b) => rank(a.key) - rank(b.key));
}
