# Vorregistriertes Protokoll: Loop Engineering v1

**Status:** Entwurf / noch keine bestätigenden Resultate

**Protokoll-ID:** `cdd-loop-engineering-v1`
**Ziel:** Die Wirkung ursachen- und kandidatgebundener Loop Guards gegenüber
einer minimalen statusglobalen Ralph-Schleife messen.

## Hypothese

Ein Guarded Loop reduziert Agenten-Turns nach wiederholtem identischem
Infrastruktur- oder Protokollfehler, ohne die Zahl falscher Promotions zu
erhöhen. Der zugehörige SPOT-Claim bleibt bis zur Auswertung `Proposed`.

## Versuchsarme

- **A — Minimal Ralph:** frische Session pro Iteration, ein kleiner Task,
  Fortschritt in versionierten Artefakten, deterministische Gates und ein
  globales Retry-/Max-Iterations-Budget.
- **B — CDD Guarded Loop:** gleiche Iteration, zusätzlich Failure Key aus Run,
  Slice, Candidate, Stage und Cause; getrennte Produkt-, Infrastruktur- und
  Protokollbudgets; modellfreie Waits; expliziter administrativer Hold;
  kandidatgebundene Review- und Publisher-Evidenz.

Beide Arme verwenden dieselbe Modellversion, Systemanweisung, Toolversion,
Task-Reihenfolge, Token-/Zeit-/Toolbudgets, Hardwareklasse und Gates. Die
Reihenfolge der Arme wird je Task randomisiert.

## Aufgaben und Wiederholungen

- mindestens fünf eindeutige Runs pro Arm und Task;
- mindestens drei kleine, öffentlich reproduzierbare Software-Slices mit
  objektiven Tests;
- mindestens ein positiver, ein echter Produktfehler- und ein reiner
  Infrastrukturpfad;
- keine Auswertung aus einem einzigen Langlauf oder nur erfolgreichen Runs.

## Deterministische Fault-Injection

Jeder Fault wird vorab an eine Transition gebunden:

| Fault | Erwartung für Arm B |
|---|---|
| administrativer Hold über 100 Ticks | null Agentturns; keine automatische Freigabe |
| Publisher-Mutex dauerhaft belegt | null Agentturns während Wait; Circuit nach Budget |
| Mutex `busy → busy → free` | nur Publisher wird wiederholt; kein neuer Build/Review |
| fehlender Terminalmarker | ein begrenztes Session-Resume; danach Circuit |
| Reviewer-Erfolg zwischen Publisherfehlern | Publisher-Counter bleibt erhalten |
| stale Candidate-/Tree-/Receipt-Digest | Publication fail-closed |
| Gate-Prozess rot | Produkt-Repair erlaubt |
| Gate-Adapter nicht erreichbar | modellfreier Infrastruktur-Retry, kein Produkt-Repair |
| zweiter Mutator / überlappende Lease | Dispatch abgelehnt |
| Crash nach jeder Zustands-Transition | Replay erzeugt dieselbe nächste Aktion |
| lokales Agent-/Tool-/Zeitbudget erreicht | harter Stop unabhängig von Rollenerfolgen |

Alle Faults verwenden synthetische Fake-Adapter. Produktive Repositories,
Credentials, interne Hostnamen und private Logs sind ausgeschlossen.

## Primäre Metriken

Pro Run werden ausschließlich sanitizierbare Aggregate erfasst:

- Full Solve und akzeptierte Slices;
- falsche Promotions und stale akzeptierte Evidenz;
- Agentturns nach dem zweiten identischen Failure Key;
- Agentsekunden, Toolaufrufe sowie Input-/Outputtokens;
- Premature Stops, Session-Resumes und frische Starts;
- Recovery-Latenz und Circuit-Open-Latenz;
- Gate-Runs, Gate-Fehler und menschliche Interventionen.

## Vorab definierte Entscheidung

Die Hypothese gilt nicht als unterstützt, wenn irgendein Guarded-Loop-Run eine
falsche Promotion akzeptiert. Bei gleicher Promotionssicherheit wird Arm B nur
dann als effizienter berichtet, wenn der Median der Agentturns nach dem zweiten
identischen Infrastruktur-Failure-Key niedriger ist und Full Solve nicht sinkt.
Effektgrößen, Streuung, negative Runs und Budgetabbrüche werden vollständig
berichtet; bei der kleinen Stichprobe gibt es keinen allgemeinen Modellclaim.

## Reproduzierbarkeit und Publikationsgrenze

Jeder veröffentlichte Snapshot bindet Commit, Protokoll-Digest, Task-Fixture,
Modell-/Harness-Deklaration, Budget und aggregierte Resultate. Session-IDs,
Prompts, Scope-Pfade, interne Digests, Freitextlogs, Personen- und
Infrastrukturdaten werden vor der Aggregation verworfen. Doppelte Run-IDs,
widersprüchliche Records und nichtterminale Runs werden abgelehnt.

## Öffentliche Grundlagen

- [Ralph reference implementation](https://github.com/snarktank/ralph)
- [Anthropic: Effective harnesses for long-running agents](https://www.anthropic.com/engineering/effective-harnesses-for-long-running-agents)
- [Anthropic: Harness design for long-running apps](https://www.anthropic.com/engineering/harness-design-long-running-apps)
- [Anthropic: parallel compiler agents](https://www.anthropic.com/engineering/building-c-compiler)
- [Temporal durable workflow execution](https://github.com/temporalio/documentation/blob/main/docs/encyclopedia/workflow/workflow-execution/workflow-execution.mdx)
- [SWE-agent / Agent-Computer Interface](https://arxiv.org/abs/2405.15793)
- [OpenHands Software Agent SDK](https://arxiv.org/abs/2511.03690)
