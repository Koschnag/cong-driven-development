# Full-Agentic SDLC mit CDD Autopilot

> **Stand:** ausführbarer Vertical Slice. Der typisierte Controller, persistente
> Runs, CLI-Harness, deterministische Gate-Ausführung und die read-only
> Studio-Projektion sind implementiert. Ein dauerhaft betriebener Workflow-
> Executor, produktive Promotion und selbstlernende Dispatch-Policies sind noch
> nicht implementiert.

## Ergebnis und Grenze

CDD behandelt einen Coding-Agenten nicht als den SDLC. Es hält den langlebigen,
entscheidenden Zustand außerhalb des Modells und lässt austauschbare Modelle nur
begrenzte Aktionen ausführen:

```text
Mission + Doctrine
  → begrenzte Work Slices
  → Scout
  → Builder
  → deterministische Gates
  → read-only Critic
  → unabhängiger Reviewer
  → sauberer Checkpoint
  → nächster Slice oder Full Solve
```

Fehlt der vereinbarte Terminalmarker eines Agenten-Turns, ist das **kein
Erfolg**. Der Controller setzt zunächst dieselbe Session fort, startet nach dem
Resume-Budget frisch aus dem persistierten Zustand und blockiert nach dem
Recovery-Budget fail-closed. Rote Gates oder Review-Befunde erzeugen eine
begrenzte Repair-Schleife; weder Builder noch Reviewer dürfen selbst promoten.

Der aktuelle Scope endet am sauberen Git-Checkpoint. Das ist autonome
Candidate-Entwicklung, nicht autonome Produktion.

## Die fünf CDD-Schichten

| Sicht | Implementierter Vertrag |
|---|---|
| **Methodik** | Missionen werden vorab in einzeln prüfbare Slices mit Scope, Kriterien und Gates geschnitten. Eine große Ticket-ID ist kein ausführbarer Auftrag. |
| **Framework** | `Cdd.Core.Autopilot` modelliert Rollen, Lifecycle-Stufen, Aktionen, Beobachtungen, Recovery, Repair, Ledger und Evaluation als F#-Typen. |
| **Harness** | `cdd autopilot` tauscht JSON-Aktionen und -Beobachtungen mit OpenCode, Codex, Claude Code, OpenHands oder einem eigenen Runner aus und kann Gates selbst ohne Shell ausführen. |
| **Plattform** | `.ai/runtime/runs/<run-id>/state.json` ist der atomar gespeicherte Zustand; `run.json` und `summary.json` sind offene Projektionen für andere Werkzeuge. |
| **Studio** | `/workspace.html` zeigt aktive Mission, Slice, Phase, nächsten Controller-Schritt, Worker-Herkunft und Autonomie-Metriken, aber keine lokalen Scope-Pfade oder Prompts. |

Modelle, Harnesses und Workflow-Engines sind Adapter. Der SPOT und der
Controller behalten die semantische Autorität.

## Run-Vertrag

Ein `RunPlan` enthält:

- eine Mission mit beobachtbarem Ziel;
- geordnete `WorkSlice`s über `Discover`, `Specify`, `Design`, `Implement`,
  `Verify`, `Release`, `Operate` oder `Learn`;
- genau einen Scout, Builder, Critic und Reviewer;
- read-only Critic und Reviewer mit vom Builder verschiedenen Identitäten;
- Gate-Programme als `Program` plus `Arguments`, ohne Shell-String;
- Grenzen für Session-Resume, frische Starts und Repair-Zyklen.

Der Beispielplan liegt unter
[`examples/autopilot/full-sdlc-plan.json`](../examples/autopilot/full-sdlc-plan.json).
Ein serialisierbares Agentenereignis liegt unter
[`examples/autopilot/agent-observation.example.json`](../examples/autopilot/agent-observation.example.json).

Der Controller gibt immer genau eine typisierte Aktion zurück:

| Aktion | Harness-Verhalten |
|---|---|
| `DispatchAgent` | Genannten Worker mit exakt dem Context Slice starten oder die benannte Session fortsetzen. |
| `ExecuteGate` | Gate reproduzierbar im Ziel-Workspace ausführen und Evidence-Digest aufnehmen. |
| `CreateCheckpoint` | Erst nach Gates und beiden Reviews einen sauberen Git-Stand bestätigen. |
| `MissionComplete` | Full Solve mit Evaluation melden; keine weitere Mutation. |
| `Escalate` | Run bleibt blockiert und braucht benannte äußere Autorität. |

Agentenausgaben werden als Beobachtungen aufgenommen. Für einen Agenten-Turn
sind nur `WorkCompleted`, `ChangesRequested` und `CannotProceed` gültige
Terminalmarker. Ein fehlender Marker wird als Premature Stop gezählt. Critic und
Reviewer müssen außerdem exakt den aktuellen Candidate-Digest referenzieren;
Gate-Evidenz für einen älteren Candidate wird abgelehnt.

## CLI-Ablauf

```bash
# 1. Run im Ziel-Workspace anlegen
cdd autopilot init examples/autopilot/full-sdlc-plan.json --workspace /workspace/project

# 2. Nächste Aktion für den Harness lesen
cdd autopilot next /workspace/project/.ai/runtime/runs/<run-id>

# 3. Agenten-Beobachtung nach dem Turn aufnehmen
cdd autopilot record /workspace/project/.ai/runtime/runs/<run-id> observation.json

# 4. Wenn ExecuteGate erwartet wird, führt CDD das Gate direkt und ohne Shell aus
cdd autopilot gate /workspace/project/.ai/runtime/runs/<run-id> --cwd /workspace/project

# 5. Nach unabhängiger Review den bereits erzeugten sauberen Git-Checkpoint prüfen
cdd autopilot checkpoint /workspace/project/.ai/runtime/runs/<run-id> --cwd /workspace/project

# Status, Ledger-Prüfung, Evaluation und nächste Aktion
cdd autopilot status /workspace/project/.ai/runtime/runs/<run-id>

# Optional: den äußeren Controller-Loop mit einem Adapterprogramm vollständig fahren
cdd autopilot drive /workspace/project/.ai/runtime/runs/<run-id> \
  /opt/cdd/bin/my-harness-adapter --cwd /workspace/project --max-steps 100
```

`record` akzeptiert die mit `Cdd.Core.Json` serialisierte
`RunObservation`. Ein Agent-Adapter sollte die Beobachtung aus seinem nativen
Eventstream erstellen, nicht aus einer freien Abschlussprosa erraten.

`drive` ist der langlebige lokale Outer Loop: Bei `DispatchAgent` schreibt CDD
die komplette `ControllerAction` als JSON auf `stdin` des explizit gewählten
Adapterprogramms und erwartet genau eine `RunObservation` auf `stdout`; Logs
gehören auf `stderr`. Gates und Checkpoints führt beziehungsweise prüft CDD
selbst. Ein Adapterfehler verändert den Run nicht, ein Schrittbudget beendet nur
den Prozess—der persistierte Run bleibt fortsetzbar. Für mehrtägige Ausführung
bleibt ein Temporal-Adapter die vorgesehene Produktionsnaht.

## Adapter für OpenCode, Codex und andere Harnesses

Der Adapter hat fünf kleine Verantwortungen:

1. `DispatchAgent.Worker` auf das native Modell-/Agentenprofil abbilden.
2. Bei `FreshSession` eine neue Session starten; bei `ResumeSession(id)` exakt
   diese Session fortsetzen.
3. Toolaufrufe, Tokens, Dauer, Session-ID, Terminalmarker und Digests in eine
   `AgentTurnObservation` normalisieren.
4. Nur die im `ContextSlice` benannten Inhalte holen. Große Historien werden als
   content-addressed Checkpoint oder über RAG referenziert, nicht erneut in jeden
   Prompt kopiert.
5. Nach jeder Beobachtung wieder `next` abfragen. Der Adapter interpretiert den
   SDLC-Zustand nicht selbst.

Für OpenCode kann ein Adapter beispielsweise dessen JSON-Eventstream und
Session-Fortsetzung verwenden; für Codex oder Claude Code gelten dieselben CDD-
Aktionen mit einem anderen IO-Adapter. Die konkrete CLI-Syntax bleibt außerhalb
des Kernvertrags, weil sie sich unabhängig vom CDD-Modell ändern kann.

Ein robustes Worker-Protokoll trennt Abschluss und Inhalt. Sinngemäß:

```json
{
  "terminal": "WorkCompleted",
  "sessionId": "provider-session-id",
  "subjectDigest": "candidate-under-review",
  "outputDigest": "new-context-or-candidate",
  "findings": [],
  "summary": "short factual report"
}
```

Fehlt `terminal`, setzt CDD die Session kontrolliert fort. Ein Text wie „sieht
fertig aus“ wird nie als Promotion-Signal verwendet.

## Context Engineering und Rollen

- **Scout:** read-only; erzeugt einen kleinen, content-addressed Context Slice,
  identifiziert Verträge, Tests, Risiken und fremde Änderungen.
- **Builder:** darf nur den Slice-Scope verändern; bekommt Kriterien,
  Context-Digest und offene Repair-Befunde.
- **Critic:** read-only; sucht früh nach logischen Fehlern, Scope Drift und
  fehlenden Tests.
- **Reviewer:** read-only und unabhängig; prüft Candidate plus Gate-Evidenz vor
  dem Checkpoint.

Für Routine-Slices kann ein schnelles Modell arbeiten. Schwierige Architektur-
oder Repair-Slices können ein stärkeres Profil erhalten. Wichtiger als ein
global maximales Reasoning-Level sind getrennte Rollen, kleine Kontexte und ein
unabhängiges Schlussorakel.

## Evaluation

Jeder Run projiziert mindestens:

- Full Solve sowie abgeschlossene/gesamte Slices;
- Agent Turns und Tool Calls;
- Premature Stops, Session-Resumes und frische Starts;
- Repair-Zyklen, Gate-Runs und Gate-Fehler;
- menschliche Interventionen und gemessene Laufdauer.

Für Modellvergleiche müssen Plan, Gates, Budget, Harness- und Toolversionen
fixiert werden. Ein einzelner erfolgreicher Lauf oder lange Terminal-Uptime ist
kein belastbarer Modellvergleich. CDD trennt Modellleistung, Harnessleistung und
Aufgabenschwierigkeit im Run-Vertrag; statistische Wiederholungen und externe
Validierung bleiben Teil des Forschungsprogramms.

### Modellherkunft und Black-Box-Fingerprinting

`WorkerProfile.Provider` und `Model` sind zunächst **deklarierte Provenienz**, kein
Beweis der tatsächlichen Modellgewichte hinter einem Router. Kritische Fragen zu
China, EU-Recht oder US-Politik können Policy-Unterschiede sichtbar machen, sind
aber kein zuverlässiger Herkunftstest: Systemprompt, Hosting-Policy,
Nachtraining, Übersetzung und Router können dasselbe Muster erzeugen.

CDD behandelt solche Versuche deshalb als kontrollierte Behavioral Evals:

1. Dieselben neutralen und kontroversen Fälle in randomisierter Reihenfolge und
   mehreren Paraphrasen gegen bekannte Baselines ausführen.
2. Ablehnung, Begründung, Rechtsraum-Annahmen, Toolnutzung und Konsistenz messen,
   nicht einzelne Formulierungen als Fingerabdruck deuten.
3. Coding-Tasks, Kontextgrenzen, Structured Output, Tool-Fehler und
   Premature-Stop-Raten getrennt vom Policy-Verhalten vergleichen.
4. Modell, Harness, Systemprompt, Budget und Endpoint-Version im Run fixieren.
5. Behavioral Similarity nur als `Inferred` oder `Proposed` führen.

Ein Versuch, versteckte Systemprompts oder interne Identität „auszutricksen“,
liefert überwiegend Halluzinationen und verletzt je nach Methode Vertrauens- oder
Nutzungsgrenzen. Belastbarer sind signierte Provider-Attestation, kontrollierte
Endpoint-/Transportprovenienz und reproduzierbare Baseline-Vergleiche. Ohne
solche Evidenz behauptet CDD keine Herkunft.

## Nächste belastbare Ausbaustufen

1. Ein Temporal-Adapter für Leases, idempotente Activities, mehrtägige Timer und
   Crash-Recovery; der deterministische CDD-Zustand bleibt Workflow-Payload.
2. OTLP-Export der Run-Metriken sowie OCI/in-toto-Attestations für Evidence und
   Checkpoints.
3. Capability-Sandboxes pro Worker statt bloßer deklarierter Profile.
4. Kalibrierte, mehrfach ausgeführte Riftward-Task-Suite mit Baselines und
   negativen Resultaten.
5. ZT3 erst nach belastbarer Recovery-, Policy- und externer Review-Evidenz.
