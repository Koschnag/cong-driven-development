# Loop Engineering für eine Full-Agentic SDLC Chain

> **Stand:** Die reine, deterministische CDD-Loop-Guard-Foundation ist
> implementiert und getestet. Ihre durchgängige Persistenz im seriellen
> `RunState`, die Scheduler- und CLI-Integration, produktive Publisher-Adapter
> und paralleles Worktree-Scheduling bleiben nächste Ausbaustufen.

## Ergebnis

CDD verbindet die kleine, robuste Iteration eines Ralph-Loops mit einer
typisierten SDLC-Zustandsmaschine. Modelle führen begrenzte Arbeit aus; sie
besitzen weder den langlebigen Zustand noch Promotion-Autorität.

Die folgende Kette ist die Zielarchitektur, nicht der heutige vollständig
integrierte Ausführungsstand:

```text
Mission / Doctrine
  → kleiner Work Slice
  → genau ein Mutator mit Lease
  → deterministische Gates
  → 0..3 read-only Advisory Lanes
  → ein unabhängiger finaler Reviewer
  → Candidate Freeze
  → Checkpoint + Committed-Bytes-Portabilität
  → typisierter Publisher
  → Outcome / nächster Slice
```

Die atomare Iteration ist `(run, slice, lease-attempt, candidate-digest)`. Sie
endet mit genau einem neuen Candidate, einem konkret gebundenen Befund oder
einem terminalen Blocker. Ein weiterer Modellturn ohne neues Subjekt oder neue
Evidenz ist keine produktive Iteration.

## Was wir übernehmen — und was nicht neu ist

| Ansatz | Öffentliche Kernidee | CDD-Erweiterung |
|---|---|---|
| [Ralph](https://github.com/snarktank/ralph) | frischer Kontext pro kleiner Story; Fortschritt in Git und strukturierten Dateien; Tests und Max-Iterations | typisierte Phasen, Failure Keys, Promotion- und Evidence-Bindung |
| [Anthropic: long-running harnesses](https://www.anthropic.com/engineering/effective-harnesses-for-long-running-agents) | Initializer plus inkrementelle Sessions, Fortschrittsdatei, sauberer Handoff | persistierter Controller statt frei interpretierter Fortschrittsprosa |
| [Anthropic: long-running apps](https://www.anthropic.com/engineering/harness-design-long-running-apps) | Planner, Generator, Evaluator; kleine Chunks; Harness-Ablationen | getrennte Mutations-, Advisory-, Review- und Promotion-Autorität |
| [SWE-agent](https://arxiv.org/abs/2405.15793) | Agent-Computer-Interface beeinflusst praktische Leistung | providerneutraler Action/Observation-Vertrag und messbare Harness-Baselines |
| [OpenHands SDK](https://arxiv.org/abs/2511.03690) | komponierbare Agenten, Lifecycle, Sandbox und Routing | semantischer Trust Core oberhalb austauschbarer Runner |
| [Temporal](https://github.com/temporalio/documentation/blob/main/docs/encyclopedia/workflow/workflow-execution/workflow-execution.mdx) | deterministische Workflows, nichtdeterministische Activities, Replay | vorgesehene Durable-Execution-Naht; keine Workflow-Engine im CDD-Kern |

Ralph, Multi-Agent-Review, Sandboxing und durable Workflows sind keine neuen
Erfindungen. Die zu prüfende CDD-Hypothese ist ihre Kombination mit
kandidatgebundener Semantik, unabhängiger Evidence-Promotion,
ursachengebundenen Circuit Breakern und einer sanitisierten longitudinalen
Evaluation. Dieser Claim bleibt `Proposed`, bis das öffentliche Protokoll
wiederholte Baselines liefert.

## Der implementierte Loop-Guard-Vertrag

`Cdd.Core.Autopilot` modelliert:

- `LoopFailureKey`: Run, Slice, unveränderlicher Subject-Digest, Stage und
  maschinenlesbarer Fehlercode;
- `LoopFailureClass`: Produkt-, Infrastruktur- oder Protokollfehler;
- getrennte Budgets für frische Produktversuche, modellfreies Infrastruktur-
  Backoff und begrenzte Session-Resumes;
- `AdministrativeHold` als explizite Autorität ohne Ablaufdatum, getrennt vom
  kurzlebigen Ressourcen-Mutex;
- kanonisch sortierte, serialisierbare Failure Counter;
- Subject-Wechsel, der alte Counter invalidiert, aber einen administrativen
  Hold nicht automatisch freigibt.

Die Disposition ist deterministisch:

| Beobachtung | Erlaubte nächste Aktion |
|---|---|
| Produktbefund | begrenzter frischer Mutator-Turn |
| Infrastrukturfehler / Resource Busy | `WaitWithoutModel`, danach Circuit |
| fehlendes Agent-Protokoll | begrenztes `ResumeBoundSession`, danach Circuit |
| administrativer Hold | beliebig viele modellfreie Ticks; nur exakte Authority darf lösen |
| Erfolg einer Stage | löscht nur Failure Counter dieser Stage und dieses Subjects |
| neuer Candidate | invalidiert alte Candidate-Evidenz und Failure Counter |

Damit kann ein erfolgreicher Reviewer weder einen wiederholten Publisherfehler
löschen noch ein belegter Mutex als neue Produktarbeit interpretiert werden.

## Swarm-Orchestrierung

Ein produktiver Swarm ist kein Stimmenmehrheits-System:

1. Genau ein Builder besitzt eine zeitlich begrenzte Mutation-Lease und einen
   isolierten Worktree.
2. Höchstens zwei bis drei Advisory-Lanes arbeiten read-only gegen denselben
   Candidate-Digest, beispielsweise Security, Test/Correctness und UX/Runtime.
3. Findings werden nach Lane und stabilem Code kanonisch vereinigt; ihre
   Ankunftsreihenfolge darf die Entscheidung nicht ändern.
4. Ein roter erforderlicher Befund erzeugt einen neuen Repair-Candidate und
   invalidiert alle alten Gates und Reviews.
5. Genau ein unabhängiger finaler Reviewer darf den Candidate einfrieren.
6. Checkpoint, Portabilitätsprüfung und Publisher akzeptieren ausschließlich
   exakt gebundene Digests und Receipts.

Parallele Builder, LLM-Voting und selbstmodifizierende Promotion-Policies sind
nicht Teil dieses Designs. Parallelität wird nur eingesetzt, wenn Ownership und
Merge-Semantik deterministisch sind.

## Zielvertrag für produktive Adapter

- Harte Limits für Agentturns, Toolaufrufe, Tokens, Zeit und Publisherversuche
  müssen lokal erzwungen werden; ein Provider-Limit ist kein lokales Budget.
- Persistenter Zustand wird vor und nach jeder Transition atomar geschrieben.
- Ein Crash darf nach Replay dieselbe nächste Aktion erzeugen.
- Wartezustände dispatchen kein Modell.
- Observability ist Evidence, darf aber den Controller nicht unbegrenzt
  blockieren.
- Promotion arbeitet auf eingefrorenen Candidate-Bytes; Infrastruktur-Retry
  wiederholt nicht Builder oder Reviewer.

Heute erzwingt der reine CDD-Loop-Guard Versuchs- und Backoff-Grenzen sowie
fail-closed Replay-Validierung. Ein produktiver Adapter muss die übrigen
Turn-, Tool-, Token-, Zeit-, Kosten- und Publisherbudgets noch durchgängig
persistieren und erzwingen; die öffentliche Foundation behauptet diese
Integration ausdrücklich noch nicht.

## Offene Arbeit

- Loop-Guard-State in das versionierte `RunState`-Schema und Ledger integrieren;
- bestehende Slice-Lease- und Committed-Bytes-Portabilitäts-Actions in den
  echten Scheduler einplanen;
- Publisher-Action/Observation mit Commit-, Tree- und Receipt-Bindung;
- read-only Advisory-Batches mit deterministischer Ergebnisreduktion;
- Crash-Fault-Injection nach jeder Transition;
- wiederholte, sanitisierte Baselines nach
  [`research/protocols/loop-engineering-v1.md`](../research/protocols/loop-engineering-v1.md).

Private Pfade, Infrastruktur, Prompts, Sessions, interne Logs oder Credentials
gehören weder in dieses Design-Dokument noch in öffentliche Forschungsrecords.
