# Session-Handoff — Cong OS / CDD → Produkt-Release & Gegenentwurf

**Stand:** 2026-06-17, Ende einer langen Claude-Code-Session. Dieses Dokument hält
Arc, Entscheidungen, Stand, Plan und Wiederaufnahme fest — damit der Chat nicht der
einzige (flüchtige) Speicher ist. Literales Transkript liegt lokal unter
`~/.claude/projects/-Users-congnguyen-Desktop-old-cong-portfolio/02494628-6276-40f9-8b7f-dd13f4aeb69f.jsonl`.

## Die These (Nordstern)
**Loop Engineering — ja, aber der Loop terminiert auf Konvergenz gegen ein getyptes
Modell, nicht auf „Agent sagt fertig".** Das beantwortet Luckes „My job is to write
loops"-Post Punkt für Punkt: verifizierbares Abbruchkriterium begrenzt den Token-Burn;
getyptes Modell + Hand-Durchstich = Gegenmittel gegen Cognitive Debt (kein Drift, Modell
bleibt Wahrheit); die Spec ist der saubere Intent. Bewiesen an **Runenruf** (echte
Game-Engine, unähnlichste Domäne → Generalitätsbeweis). Ehrlichkeit ist die Antwort auf
„wo endet Marketing": **zeigen was läuft, Rest als Roadmap markieren.**

## Die vier Repos (das „Programm")
| Repo | Rolle | Stand |
|---|---|---|
| `cong-driven-development` (Branch `cockpit-engine`, PR #41) | **Werkzeug/IDE** — der chat-primäre Cockpit | live auf VM 120 `100.64.0.2:5179` |
| `cdd-programm` (`Cdd.Mapper`) | **Steuerzentrale** — Loop bis Konvergenz, Dashboard | Mapper v1, baut, Gate hat Zähne |
| `runenruf` | **Anwendung/Fallbeispiel** — RTS-RPG, F#/C#/Silk.NET | 74 SPOT-Knoten, 2 Pending, 1 Testprojekt |
| `cdd-fallstudie` | **Beleg** — Wissenschaft | (nicht angefasst) |

## Was gebaut wurde (diese Session, alles in `cong-driven-development`/`cockpit-engine`)
- **Chat-primärer Cockpit „Der Faden"**: Omnibar (⌘K) · Rail (JETZT/Pins) · Thread ·
  Bühne · Statusleiste. Mitte schaltet **Chat ⇄ Diagramm** (zwei 90%-Funktionen).
- **VS-2022-Classic-Gewand**: Dark+Light VS-Paletten, Cascadia/Segoe-Fonts (Cascadia
  lokal gevendort), Menüleiste, Tool-Window-Titel, Bottom-Dock (Fehlerliste/Ausgabe/Drift).
- **EA/MagicDraw-Diagramm**: Form=Knotenart, Rand=Konvergenz, Kanten=UML-Relation;
  Ansichten Architektur/Ontologie/Nachverfolgung/Ganzes; Cytoscape lokal gevendort.
- **Entscheidungen** (ADR + Ablöse-Lineage), **Prämissen**, **Historie** (git-`log` über
  `.spot/` = Modell-Zeitreise; `/api/history`).
- **Engine-Selbstwissen**: `--append-system-prompt` (der Agent weiß, dass er die Cong-OS-
  Engine ist).
- Backend: `History.fs`, `/api/history*`, `/api/infra/status`. Build 0/0.
- **Engine auf VM 120 repariert**: Claude Code installiert + PATH gefixt → Chat läuft live.

## Der Mapper (das Konvergenz-Gate — verifiziert, ehrlich)
`cdd-programm/src/Cdd.Mapper/MapperCore.cs`: findet Pending-Specs → baut GIVEN/WHEN/THEN-
Prompt → `claude -p` headless → **Gate v2**: konvergiert NUR wenn Marker Aligned **UND**
≥1 Testprojekt **UND** `dotnet test` grün (Exit 0 reicht nicht, „No tests" zählt nicht).
Prompt verbietet dem Ausführer, das Gate zu täuschen. **Das ist das Anti-Zirkularitäts-
Prinzip in Code.** Dry-Run an Runenruf gewählt: `spec-fenster` (Cross-Platform-Fenster + GL).

## „Für viele" — Produktentscheidungen (vorbelegt, tragfähig)
- **Verteilform: self-host / „bring your own repo"**, kein SaaS (passt zu Souveränität,
  geringste Reibung). Andere zeigen CDD auf ihr Repo, modellieren, Mapper loopt bis grün,
  sie reviewen das Gate statt den Diff.
- **Positionierung** vs Spec Kit / Kiro / Tessl: deren Specs sind **Prosa**; CDDs Spec ist
  **typisiert + verifizierbar** (Konvergenz-Gate) + souverän + F#/.NET + Chat/Diagramm-Cockpit.
- **Stack-Regeln:** F#/C#/.NET, **kein Python**, MPL-2.0.

## Offene Entscheidungen / Roadmap (ehrlich als Roadmap markiert)
- **Property-Tests (FsCheck) + Lean-Beweise** stehen im Manifest, **noch nicht im Code** —
  die „keine Property widerlegt / Kern bewiesen"-Beine des Gates sind Roadmap, nicht Ist.
- Cockpit↔Mapper-Schluss (Loop-Knopf im Cockpit ruft `Cdd.Mapper`) = nächster Tool-Bau.
- Anbieter-Naht Opus→Mistral (spec'd in `cdd-programm`).

## LinkedIn (gespeichert)
Finaler **Kommentar** zu Luckes Review-Post liegt im Chat (kein „du", geschlechtsneutral,
mit Token-Beat + Spec-Korrektheit-vs-Treue + Durchstich-als-Entdeckung). **Gegenentwurf-Post**
zum Loop-Engineering-Post: wird vom Workflow `cdd-release-gegenentwurf` (run `wf_a44d1937-22d`)
final entworfen — inkl. Urteil „safe to link repo: ja/mit-Fixes/nein".

## Wiederaufnahme (in der Claude-App)
Repo (public), **Branch `cockpit-engine`**: https://github.com/Koschnag/cong-driven-development/tree/cockpit-engine ·
PR #41 · `cdd-programm` (Mapper) · `runenruf` (Fallbeispiel). Memory:
`memory/cong_os_redesign.md`. Dieser Handoff: `docs/SESSION-HANDOFF.md`.

## Autonomer Lauf — ERGEBNIS (2026-06-18, durchgearbeitet)

**Der Beweis steht: Runenruf wurde DURCH das Tool gebaut, nicht von Hand.**

1. **spec-fenster konvergiert durch `cdd-mapper --go`** (runenruf Branch `cdd-mapper/auto`,
   Commits `108dfb2` + `773e631`): claude -p implementierte ehrlich `GlKontext.cs`
   (GL-3.3-Core / ES-3.0), `GlBackend.cs`, einen echten Test mit `Trait("spot",…)` +
   Frame-Loop-Guard — Spec/Tests **nicht** manipuliert, Convergence **nicht** von Hand gesetzt.
   Gate grün: **`dotnet test` 27/27**, Marker Aligned. Das Orakel verbürgte spec-fenster → **Aligned**.
   Re-Run ist **idempotent**: „Gate bereits grün — kein claude-Lauf nötig (Token gespart)",
   und das Modell meldet **„konvergiert — nichts zu tun"** (sauberer Loop-Abbruch).

2. **Tool gehärtet (das machte den Lauf erst ehrlich):**
   - `cdd` + `cdd-mapper` als `dotnet tool` paketiert (`PackAsTool`) — Voraussetzung, sonst
     übersprang der Mapper still die Gate-Schritte (`cdd@d6b34a5`, `cdd-mapper@5e0d679`).
   - Mapper: **Idempotenz** (schon-grüne Spec überspringt claude → Token-Antwort) + **Orakel
     verbürgt Spec→Aligned** nur nach echtem grünem Gate; der Ausführer darf Convergence nie
     setzen. **10/10 Mapper-Tests** (2 neue Orakel-Tests). `cdd-mapper@160391f`.
   - Mapper `--json`-Streaming für die Cockpit-Anzeige.

3. **Cockpit↔Mapper geschlossen** (`cdd@3b3980b`, live VM 120): `POST /api/loop/run` spawnt
   `cdd-mapper --go --json` und streamt als SSE; Frontend `runLoop` + Aktion **„▶ Loop bis
   Konvergenz"** (Modell-Menü + Vorschlag-Chip) zeigt jeden Schritt im Faden, refresht Drift.
   Service-PATH um `~/.dotnet/tools` erweitert. **Aus 4 Repos ist EIN Werkzeug geworden.**

4. **Gegenentwurf-Post** gesichert in `docs/GEGENENTWURF-POST.md` (Entwurf, NICHT gepostet).

**Noch offen (für DEINE Freigabe — outward-facing, bewusst nicht autonom getan):**
- v0.1-Tag / `dotnet tool`-Publish auf nuget.org · der LinkedIn-Post · Merge `cdd-mapper/auto`→main.
- Honesty-Docs vor dem Verlinken (in `GEGENENTWURF-POST.md` gelistet): STATUS.md neu generieren
  + Caption, cdd-programm-README-Widerspruch glätten, Test-Zahlen als `dotnet test`-Verdikt.
- Mehr Runenruf-Specs modellieren (aktuell konvergiert das Modell vollständig → Loop hat nichts
  mehr zu tun; neue Pending-Specs = neues Futter für den Loop).
