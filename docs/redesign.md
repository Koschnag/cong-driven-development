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

- **Graph/UML/Cube bleiben** — als **Graph-Linse** über der Stage. Frei ziehbar, UML-Klassendiagramm der Ontologie, OLAP-Cube-Navigation (slice/dice/drill).
  - **REVIDIERT (2026-06-18, sanktioniert per AskUserQuestion):** Das Diagramm ist jetzt eine **permanente Split-Mitte** (Diagramm links 1fr + Faden rechts), NICHT mehr nur eine On-Demand-Linse. Die ursprüngliche „Dauer-Splitscreen = Wurzel der zu-schmal-Klage"-These wird damit bewusst zurückgenommen. **Bedingung, die den Widerspruch auflöst:** der Faden kann per ⌘0 (Menü „Faden Vollbild") das Diagramm **einklappen** und den Schirm füllen — das §11-„Hyperfokus"-Versprechen bleibt erreichbar, `data-spine=chat` bleibt wahr. Default = Split (Congs Wahl); Fokus = Faden-Vollbild auf Knopfdruck. Damit sagen Spec und Build wieder dasselbe.
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
- **Engine-Kette (entschieden):** **Claude Code primär** (agentischer Loop, Host-Login) → **Mistral EU als Backup** (wenn Claude nicht erreichbar/Quota) → **lokales Qwen via Ollama als Offline-Fallback** (totaler Internetausfall — souverän, frei). Fällt 1:1 auf das bestehende `IEngine` (`ClaudeCodeEngine` + zwei `OpenAiCompatEngine`-Targets). Für Plan/Ideate/Monitor bleibt Ollama der Default (lokal, frei). Dropdown im Copilot-Header zeigt die aktive Stufe + Degradation ehrlich an.
- **Approval (entschieden — Autopilot-Default):** Develop fährt **`acceptEdits`: die Engine schreibt direkt**, Kontrolle ist *nachgelagert* über die **Develop-Diff-Linse** (Review/Revert nach dem Schreiben) — kein blockierendes Gate vorher, passt zum Low-Reibung-Axiom. **Modal-Gate bleibt** ausschließlich für *irreversible* Akte: ClaudeCode `--permission-prompt-tool` → Orchestrator fängt ab → `PermissionRequest` über die SSE → **GUI-Modal** → `/api/engine/approve`, nur für Deploy-Bash, `infra_apply`, `spot_delete`. Timeout→deny.
- **Session/Memory:** Claude `session_id` pro Surface+Thread in `.agent/sessions.json` → `--resume`. Jeder fertige Turn als Zeile (`system='cdd-agent'`) in `cong.db` → die eigene Historie wird RAG-durchsuchbar.

## 8. Der IDE-Entscheid (entschieden): eigener schlanker Editor im Shell

**Entscheidung umgekehrt** — kein VSCodium-Embed, sondern ein *schlanker eigener Editor* in der Develop-Fläche. Begründung des Nutzers: konsistentes GUI, kein zweites Schwergewicht, eine Codebasis. Trade-off bewusst akzeptiert: LSP-Tiefe baut man nicht nach.
- **Cockpit** zeigt die *Agent-Arbeit* (ToolUse/ToolResult/Diff-Karten), Konvergenz, Inspektion — „der KI beim Arbeiten zusehen", nie ein tmux-Pane.
- **Develop-Editor** = **CodeMirror** im Stage (read + leichtes Edit), Kern ist die **Diff-Linse**: weil Autopilot direkt schreibt (§7), zeigt die Develop-Fläche den *resultierenden Diff* mit `[✓ Übernehmen-im-Modell] [↶ Revert]` post-hoc. Der Editor ist Review-/Inspektions-Werkzeug für die Autopilot-Arbeit, kein Full-IDE-Ersatz. VSCodium bleibt als *optionale* externe Develop-Säule ans selbe Backend andockbar, ist aber nicht der Default.

## 9. Sicherheit

- **`cong.db` `sensitive=1` (klinisch):** `WHERE sensitive=0` serverseitig in `Cdd.Read` **vor** dem MCP-Return; separater Unlock für sensitive; nie Auto-Inject in den Kontext. **Nicht verhandelbar.**
- **Blast-Radius:** statische Allowlist + Approval-Modal für alle irreversiblen Akte; `/api/infra/apply` **nie** außerhalb des Mesh-Tiers; SSH-Keys read-probe vs apply getrennt; mesh-only-Bind (tailscale0, VM 120).
- **Mesh-Degradation ehrlich anzeigen:** Statusline-Token zeigt Reachability; Flächen rendern „offline/last-seen" statt leerer Kacheln.

## 9b. Zugriff & Geräte-Unabhängigkeit (als Teil deiner Cloud)

Ziel: **von jedem Gerät im Browser** bedienbar, integriert in `cong42.de`. Aber die Engine fährt beliebigen Code + Infra → **die gefährliche Fläche darf NICHT public sein.** Lösung = **gestufter Zugriff**, exakt wie der Rest der Cloud (Tier A public+SSO vs Tier B mesh-only):

| Tier | Wie | Geräte | Fläche |
|---|---|---|---|
| **Operator (volle Macht)** | VM 120 an `tailscale0` (`100.64.0.2:5179`) | nur deine Mesh-Geräte | **alles** inkl. Engine-Edit/Bash, Develop, Deploy, `infra_apply` |
| **Consumer (jeder Browser)** | `cockpit.cong42.de` → cc6-Caddy reverse-proxy → VM 120, hinter **Yunohost-SSO + MFA (Pflicht)** | jedes Gerät, überall, ohne VPN (Pixel/iPad/Fremdgerät) | **Read alles** (SPOT/DWH/Monitoring/Historie) + Chat/Ideate (Ollama/Mistral, read-only Tools). **Kein** Write/Bash/Deploy/`infra_apply`. |

**Server-seitig erzwungen:** die gefährlichen Routen (`/api/engine/run` mit Write-Tools, `/api/infra/apply`, `spot_delete`) prüfen den **Tier** (Bind-Interface / vertrauenswürdiger cc6-Header) und sind **nur im Operator-Tier** erreichbar — nie über den Public-Edge. So ist „jedes Gerät" wahr für das Sichere, und die God-Macht bleibt auf deinen Geräten.

⚠️ **Lektion aus R-04** (codespace heute = Pass-only ohne SSO/MFA, im Modell *kritisch*): den Fehler wiederholen wir nicht — `cockpit.cong42.de` bekommt **SSO + MFA ab Tag eins**, sonst gar nicht public.

**Technisch geräte-unabhängig:** responsive 3-Zonen-Shell (Rail → Icon-Leiste, Copilot → Sheet, Stage → Vollbild am Handy); **Canvas-/Session-State serverseitig auf VM 120** → dieselbe URL nimmt auf jedem Gerät den Faden auf (Phase E). **Eine URL, ein Login, jedes Gerät** — als App in deinem Stack (selbe Domain, selbes SSO, selber Edge, selbes Backup). Der Public-Edge wird mit Phase E scharfgeschaltet; das Tier-Gating muss von Anfang an im Code stehen.

## 10. Phasen-Bauplan

| Phase | Ziel |
|---|---|
| **A — Die neue Shell** | 3-Zonen-Shell ersetzt phasebar+tree+dock; Omnibox+Routing; Copilot rendert den *existierenden* `/api/engine/run`-SSE als Karten; Cytoscape→Linse. **Frontend-only, kein Backend.** Sofort „raus aus dem Terminal". |
| **B — Develop + Autopilot** | `Cdd.Agent` extrahieren; **Autopilot-Default** (`acceptEdits`, §7) + CodeMirror-Editor + **Diff-Linse** (post-hoc Review/Revert, §8); `PermissionRequest` + `/api/engine/approve` + Modal **nur** für irreversible Akte. Loop Idee→Spec→Derive→Code→Sync→Aligned als Karten. |
| **C — DWH** | `Cdd.Dwh` + congdb-MCP (FTS5, read-only, `sensitive=0`) + DWH-Linse. `@person`-Suche + „promote to SPOT". |
| **D — Infra (Monitor+Deploy)** | `Cdd.Infra` (dc-model als Library) + infra-MCP + Monitor-Board + Plan/Apply-Linse. |
| **E — RAG + Geräte-Durabilität + Self-Model** | `sqlite-vec`-Index, **Embeddings lokal via Ollama `nomic-embed-text`** (entschieden — souverän, offline-fähig; Mistral-embed nur optionaler Online-Boost) + semantische Suche; Canvas-State serverseitig (gleiche URL resumed auf Pixel/iPad); `self-model.fsx` als Digital-Twin-Knoten. |

## 11. Erster Schnitt (Phase A — null neue Endpoints)

`wwwroot/` (as-built, Commit `ab477ed`): `shell.js` (Controller: Rail+⌘1–5, Omnibox+Routing, Linsen-Tabs, Statusline, Boot), `core.js` (Signals-Store + `/api/engine/run`-SSE-Client + SPOT-Helfer; `toGuiJson`-Vertrag ist die 1:1-Quelle), `copilot.js` (Engine-Stream als Karten), `inspector.js`/`graph.js`/`cube.js`/`docs.js` (die vier Modell-Linsen), `cockpit.css`. `index.html` auf `shell.js` verdrahtet; alte `cockpit.js`/`agent.js`/`demo.js`/`form.js`/`styles.css`/`engine.html` **gelöscht** (−1079 LOC netto).
**Routen (existieren alle):** `/api/spot`, `/api/validate`, `/api/diff`, `/api/export`, `/api/engine/run`.
**Ergebnis (live auf VM 120, `100.64.0.2:5179`):** Cong tippt in *eine* Bar, sieht den Agenten in Karten arbeiten, klickt durch 4 Linsen über denselben SPOT — nie ein tmux-Pane. Der Kern-Escape, an Tag eins.

## 12. Entscheidungen (2026-06-17 festgelegt)

1. **Name:** **Cong OS** ✓ (cc5→cc8-Linie).
2. **IDE-Säule:** **eigener schlanker Editor** (CodeMirror + Diff-Linse) im Shell ✓ — *nicht* VSCodium (§8 umgekehrt). VSCodium bleibt optional andockbar.
3. **Autopilot-Default:** **`acceptEdits` — Engine schreibt direkt** ✓, Kontrolle nachgelagert über die Develop-Diff-Linse; Modal-Gate nur für irreversible Akte (`infra_apply`/Deploy-Bash/`spot_delete`).
4. **Engine-Kette:** Claude Code primär → Mistral EU Backup → lokales Qwen (Ollama) Offline-Fallback ✓.
5. **RAG-Embeddings:** **lokal, Ollama `nomic-embed-text` + `sqlite-vec`** ✓ (souverän/offline-fähig — vom Offline-Requirement erzwungen); Mistral-embed nur optionaler Online-Boost. `sqlite-vec` (v0.1.x, jung) vor Commit gegen `.NET LoadExtension` auf dem DC-Runtime verifizieren; Fallback FTS5-only deckt das MVP.

---
*Quelle: 4-Linsen-Design-Workflow + adversariale Synthese (5 Agenten), gegen den echten Code gelesen. Baut auf der Engine-Schicht (PR #41).*
