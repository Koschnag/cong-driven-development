# Cong OS — Redesign: vom SPOT-Editor zur souveränen Personal-OS-Kommandozentrale

> **Konzept:** Eine souveräne, KI-getriebene Personal-OS-Shell, in der **eine Eingabezeile (die Omnibox)** den Agenten über **vier Wahrheiten** — SPOT (Struktur), cong.db (Inhalt), dc-model (Infra-Soll), Live-DC (Infra-Ist) — fährt und jedes Ergebnis als **interaktive Artefakt-Karte** rendert. Kein Knotenbaum, kein Terminal, kein App-Wechsel.
> **Name:** **Cong OS** (in der cc5→cc8-Linie). Claim: *„Eine Eingabe. Dein ganzes digitales Leben."*
> **Eine God App für KI-Entwicklung *und* Betrieb:** konzipieren (SPOT) · bauen (Engine/IDE) · testen (Konvergenz) · deployen (DevOps) · betreiben+überwachen (Infra) · über dein Daten-/Wissensleben denken (DWH). Alles F#/.NET, mesh-only auf VM 120, kein Python, über *eine* URL.

## 1. Diagnose (warum das heutige GUI nicht passt)

Heute: **zwei getrennte UIs** (das Modell-Cockpit `cockpit.js` + die losgelöste `engine.html`-Konsole) und die **KI ist ein Fußnoten-Tab** im unteren Dock eines Modell-CRUD-Editors. Dazu ein Bug: `Program.fs` dumpt das *volle* `Export.toMarkdown` als Kontext in *jeden* Run. → Die KI muss ins **Zentrum**, SPOT+DWH+Infra werden ihre **Arbeitsflächen**, und das „VS2015/Enterprise-Architect"-Gefühl (Knotenbaum-zuerst) verschwindet.

## 2. Das Paradigma

**Conversation-first + Command-Palette-Rückgrat → Canvas aus Live-Karten.** Drei Zonen: **Rail · Stage · Copilot**.
- **Eine Omnibox / ⌘K** ist der einzige Eingang. **Deterministisches Routing (keine LLM-Klassifikation):** erstes Zeichen entscheidet — `/`=Befehl · `@`=Person/DWH-Suche · `#`=Tag/Kind · bekannte Knoten-Id=navigieren · sonst=Prompt an den Agenten auf der aktiven Fläche. Ghost-Text zeigt die Regel inline.
- Die **5 Flächen** (Plan/Ideate/Develop/Monitor/Deploy) sind **Agent-Modi**, kein Gate davor — die Fläche ist eine *Eigenschaft des Requests* (Mode-Chip auf der Ergebniskarte), nicht eine Tür. Tippen geht von überall.
- **Mensch = Approver, KI = Default-Actor.** Default-Home = **Develop** (man landet auf Arbeit).

## 3. Shell-Mockup

```
┌──────────────────────────────────────────────────────────────────────────────────────────┐
│ ◉ Cong OS    ⌘K ▸ / befehl · @person · #tag · id navigieren · sonst fragen…              ▾ │
├──────┬───────────────────────────────────────────────────────────────────┬─────────────────┤
│ RAIL │  STAGE  (eine Fläche; Linsen oben rechts) [Inspector|Graph|Kontext]│ COPILOT (rechts)│
│ ○Plan│  Develop ─ spec-login                              ● Pending       │ engine:Claude ▾ │
│ ○Idea│ ┌── FILES ─────────┬──────────────────────────────────────────┐   │ mode:Develop    │
│ ●Dev │ │ src/Login.fs     │ Intent · Criteria (G/W/T) · Convergence   │   │ ▸ you: derive   │
│ ○Mon │ │ src/Auth.fs ⚠drift│ spec✔ test✗ code⚠                         │   │ ▸ ⚙ tool_use    │
│ ○Dep │ │ .spot/spec-login │ Relations: covers→test-login              │   │ ▸ ◫ diff +2/-0  │
│ ──── │ │ tests/Login.Tests│ [Derive Tests][Sync Code][Validate]       │   │   [Apply][✕]    │
│ SPOT │ └──────────────────┴──────────────────────────────────────────┘   │ ▸ ✔ done $0.02  │
│ ◷12⚠3│  diff-karte (KI- oder Mensch-Edit) … (wartet auf Apply)           │ ⌘K fragen ▸     │
├──────┴───────────────────────────────────────────────────────────────────┴─────────────────┤
│ STATUS: ● 12 nodes · ⚠ 3 diverged · DC: cc6 up · backup 6h · mesh ✔ · R-15 bleeding · idle │
└──────────────────────────────────────────────────────────────────────────────────────────┘
```
Die **Statusline (Ambient-Health-Band, immer sichtbar)** ersetzt die Phasen-Leiste; jeder Token klickbar → springt auf seine Fläche. *(Entschieden: dünnes Dauer-Band statt nur-on-demand — bei Bus-Faktor 1 ist stilles Wegschauen das Risiko.)*

## 4. Die 7 Säulen = Projektionen von 2 Wahrheiten auf 5 Flächen (keine 7 Tabs)

| Säule | was sie ist | von KI + Mensch getrieben |
|---|---|---|
| **IDE / Develop** | Agent arbeitet im Repo; Edits + Diffs + Konvergenz als Karten | ClaudeCodeEngine (Read/Edit/Bash git*/dotnet*) · Buttons rufen dieselben MCP-Tools |
| **SPOT** | allgegenwärtige Datenschicht: Rail-Indikator · Inspector · **Graph-Linse** · Kontext-Linse | `mcp__spot__*` · Inspector + ⌘K-Verben · Apply/Discard-Gate |
| **DWH (cong.db)** | `@person`/Datum/Freitext → FTS5 → Treffer-Karten; „promote to SPOT" | neuer `congdb`-MCP (read-only, `sensitive=0` serverseitig) |
| **Chat** | **IST der Copilot-Rail** (Nervensystem), kein separates Programm | EngineEvent-SSE rendert inline · Engine umschaltbar |
| **DevOps** | Pipeline-Linse: build/test/derive/sync als Run-Log (selber Karten-Renderer) | Bash(git*/dotnet*) · `/`-Befehle |
| **Monitoring** | ruhiges Live-Board + Morgen-Health-Karte (rot/amber/grün, kein Tippen) | `infra_validate/drift` read-only · „warum ist cc6 rot" im Copilot |
| **Infra** | typisiertes Infra-Modell + Plan/Apply-Linse (strukturiert, nicht roher stdout) | `infra_plan` frei · `infra_apply` **deploy-gated** (Modal + plan-Token) |

## 5. Multidimensionale Modellansicht + Historie (explizit gewünscht)

- **Graph/UML/Cube bleiben** — als **Graph-Linse** über der Stage (Cytoscape von Dauer-Splitscreen zur On-Demand-Linse degradiert; *das* war die Wurzel der „zu schmal"-Klage). Frei ziehbar, UML-Klassendiagramm der Ontologie, OLAP-Cube-Navigation (slice/dice/drill).
- **Time-Travel ist geschenkt:** der SPOT liegt als ein JSON-File pro Knoten unter git → jede Änderung ist ein Commit → **Zeit-Slider** scrubbt durch die Modell-Historie. Zweite Achse: **Lebens-/Daten-Historie** (cong.db-Timeline, 93k Events). Du gehst auf einer Zeitachse durch Modell *und* Leben.
- **Doku** ist *generiert* aus dem SPOT (Kontext-Linse = `Export.toMarkdown` wörtlich → der Mensch sieht, was die KI sieht), kein separater Ort.

## 6. Architektur (Modulkarte · Writer-pro-Realm · kein Python · kein JS-Framework)

- **Cdd.Core** (existiert) — SPOT-Domäne, Validate/Diff/Derive/Export/Store. Unverändert als `.spot/`-Writer.
- **Cdd.Engine** (existiert) — `IEngine`+`EngineEvent`. **Erweitern:** (a) `EngineEvent.PermissionRequest of id*tool*input`; (b) `OpenAiCompatEngine` bekommt einen echten **Tool-Loop** (parse tool_calls → MCP → re-feed bis fertig, iter-Cap) — das *eine* wirklich neue Engine-Stück.
- **Cdd.Agent** (NEU) — der Orchestrator: `assembleContext` (surface-geschnitten, token-budgetiert) + `routeEngine: Surface→EngineKind` + Permission-Gate. Die `/api/engine/run`-Logik wandert aus `Program.fs` hierher.
- **Cdd.Dwh** (NEU, ~30 LOC) — ReadOnly-SQLite über `cong.db`, parametrisiertes FTS5, **`AND sensitive=0` serverseitig erzwungen**.
- **Cdd.Infra** (NEU) — `dc-model.fsx` als kompilierte Library (`validate()` 12 Invarianten) + `probeHost` (SSH übers Mesh) + `reconcile` (Soll/Ist/Drift); `plan` read-only, `apply` hinter `confirmToken`.
- **Cdd.Read** (NEU) — *die eine* Projektion (`Entity{Ref(Realm,Kind,Id);Title;Summary;Convergence;Sensitive;Links}`), an die Web **und** Mcp binden → Agent sieht Struktur+Inhalt+Infra als *einen* Kontext.
- **Cdd.Mcp** (existiert, 8 SPOT-Tools) + **congdb-MCP** + **infra-MCP** — getrennte Server = getrennte Allowlists/Blast-Radius.
- **Cdd.Web** (existiert) — Frontend bleibt Vanilla-JS + ein **~3 KB Signals-Store** (subscribe/notify — der echte Defekt war fehlender observable state, nicht fehlendes React). Module: `shell.js` · `inspector.js` · `stream.js` · `store.js`.

**Kern-Routen** (SSE default): READS `/api/entity/{realm}/{kind}/{id}` · `/api/search` · `/api/timeline` · SPOT `/api/spot|validate|diff|export|derive-tests` · ENGINE `/api/engine/run` (SSE) + **`/api/engine/approve`** (NEU) · DWH `/api/dwh/search` · INFRA `/api/infra/{state,risks,validate,drift,plan,apply}` + `/api/monitor/stream`.

## 7. KI-Integration

- **Kontext surface-geschnitten** (behebt den Full-Dump-Bug): Develop = nur referenzierte Knoten + `outRefs/inRefs`-Nachbarn + Source-Files; Monitor = `validate()`+drift, keine Bodies; Ideate = terms+premises+**RAG**-Snippets. Hard ceiling (Ollama ~6k / Claude ~25k), Overflow → RAG statt Inline. „Alle Chats draufwerfen" = **semantisch via RAG** (`sqlite-vec`, kein Python), nicht literale Konkatenation von 84k Messages.
- **Routing (reine Funktion, keine LLM-Klassifikation):** Plan/Ideate/Monitor→**Ollama** (lokal, souverän, frei), Develop/Deploy→**Claude Code** (agentischer Loop, Host-Login), **Mistral** nur Override. Dropdown im Copilot-Header.
- **Approval:** ClaudeCode `--permission-prompt-tool` → Orchestrator fängt ab → `PermissionRequest` über die SSE → **GUI-Modal** (tool + input-diff + Allow/Deny/Allow-for-session) → `/api/engine/approve`. Statische Allowlist = Gate 1, Modal = Gate 2 nur für irreversible Akte (Deploy-Bash, `infra_apply`, `spot_delete`). Timeout→deny.
- **Session/Memory:** Claude `session_id` pro Surface+Thread in `.agent/sessions.json` → `--resume`. Jeder fertige Turn als Zeile (`system='cdd-agent'`) in `cong.db` → die eigene Historie wird RAG-durchsuchbar.

## 8. Der IDE-Entscheid: den Editor NICHT nachbauen → VSCodium

Tiefes Code-Editieren gehört nicht in `Cdd.Web` nachgebaut. **Die Develop-Säule ist gespalten:**
- **Cockpit** zeigt die *Agent-Arbeit* (ToolUse/ToolResult/Diff-Karten), Konvergenz, Inspektion — „der KI beim Arbeiten zusehen", nie ein tmux-Pane.
- **VSCodium** (läufst du eh, souveräner OSS-Build) ist die *Develop-Säule fürs echte Editieren* — verdrahtet ans **selbe** Engine/SPOT/MCP-Backend. Den weltbesten Editor + Terminal + Git rebaust du nicht. *(Begründung: kein Editor-Nachbau, kein Bloat; Cockpit ist chat-primär, VSCodium ist editor-primär — jedes Werkzeug an seiner Stärke, ein Backend.)*

## 9. Sicherheit

- **`cong.db` `sensitive=1` (klinisch):** `WHERE sensitive=0` serverseitig in `Cdd.Read` **vor** dem MCP-Return; separater Unlock für sensitive; nie Auto-Inject in den Kontext. **Nicht verhandelbar.**
- **Blast-Radius:** statische Allowlist + Approval-Modal für alle irreversiblen Akte; `/api/infra/apply` **nie** außerhalb des Mesh-Tiers; SSH-Keys read-probe vs apply getrennt; mesh-only-Bind (tailscale0, VM 120).
- **Mesh-Degradation ehrlich anzeigen:** Statusline-Token zeigt Reachability; Flächen rendern „offline/last-seen" statt leerer Kacheln.

## 10. Phasen-Bauplan

| Phase | Ziel |
|---|---|
| **A — Die neue Shell** | 3-Zonen-Shell ersetzt phasebar+tree+dock; Omnibox+Routing; Copilot rendert den *existierenden* `/api/engine/run`-SSE als Karten; Cytoscape→Linse. **Frontend-only, kein Backend.** Sofort „raus aus dem Terminal". |
| **B — Develop + Approval** | `Cdd.Agent` extrahieren; `PermissionRequest` + `/api/engine/approve` + Modal. Loop Idee→Spec→Derive→Code→Sync→Aligned als Karten. |
| **C — DWH** | `Cdd.Dwh` + congdb-MCP (FTS5, read-only, `sensitive=0`) + DWH-Linse. `@person`-Suche + „promote to SPOT". |
| **D — Infra (Monitor+Deploy)** | `Cdd.Infra` (dc-model als Library) + infra-MCP + Monitor-Board + Plan/Apply-Linse. |
| **E — RAG + Geräte-Durabilität + Self-Model** | `sqlite-vec`-Index (Embeddings via Ollama-HTTP) + semantische Suche; Canvas-State serverseitig (gleiche URL resumed auf Pixel/iPad); `self-model.fsx` als Digital-Twin-Knoten. |

## 11. Erster Schnitt (Phase A — null neue Endpoints)

`wwwroot/`: **NEU** `shell.js` (Rail+⌘1–5, Omnibox+Routing+Ghost-Text, Stage mit Linsen-Tabs, Copilot-Rail, Statusline), `stream.js` (~150 LOC: `/api/engine/run`-SSE → `EngineEvent`→Karte; der `toGuiJson`-Vertrag ist die 1:1-Quelle), `inspector.js` (Node-Renderer + Convergence-Farben + Relations), `store.js` (~3 KB Signals). `index.html` auf `shell.js` verdrahtet; `cockpit.js`-Cytoscape → Graph-Linse; `engine.html` in den Copilot gefaltet + gelöscht.
**Routen (existieren alle):** `/api/spot`, `/api/validate`, `/api/diff`, `/api/export`, `/api/engine/run`.
**Ergebnis:** Cong tippt in *eine* Bar, sieht den Agenten in Karten arbeiten, nie ein tmux-Pane — der Kern-Escape, an Tag eins.

## 12. Offene Entscheidungen (für Cong)

1. **Name** festnageln: **Cong OS** (Empfehlung, cc5→cc8-Linie) vs „Sovereign Personal OS" vs „SPOT Cockpit".
2. **IDE-Säule:** VSCodium-Anbindung (Empfehlung, §8) — oder doch ein Editor im Cockpit?
3. **Autopilot-Default:** `acceptEdits` für low-risk SPOT/File-Edits (auto, undoable), explizites Gate nur für `infra_apply`/`delete` + globaler Autopilot-Toggle (default aus). Passt das zu deinem Low-Reibung-Axiom?
4. **RAG:** `sqlite-vec` (v0.1.x, jung) vor Commit verifizieren (`.NET LoadExtension` auf dem DC-Runtime); Fallback FTS5-only deckt das MVP. Embedding-Modell: `nomic-embed-text` (leicht) zuerst.

---
*Quelle: 4-Linsen-Design-Workflow + adversariale Synthese (5 Agenten), gegen den echten Code gelesen. Baut auf der Engine-Schicht (PR #41).*
