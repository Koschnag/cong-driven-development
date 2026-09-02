# Vorregistriertes Protokoll: Riftward Scientific Observatory v1

**Status:** Shadow-v1-Methodik; `PublicObservationSnapshotV1` ist ein Publikations-Gate-Draft. Keine Steuerung, keine Promotionsauthority, keine Produktivbehauptung. Keine behauptete Kompatibilitaet mit dem entfernten Ops-Aggregat. Publikation bleibt gesperrt, bis ein von Ops erzeugtes Fixture in CDD exakt roundtrippt.

**Protokoll-ID:** `riftward-observatory-v1`

## Zweck und Drei-Repository-Grenze

Das Observatory veröffentlicht ausschließlich eine additive, sanitizierte
Forschungsprojektion. Die drei Grenzen sind: (1) das operative Riftward-
Produktrepository als alleiniger Writer für Arbeit, Gates und Promotion; (2)
das private `riftward-autopilot-ops` als deploybarer Control Plane und
Raw-Collector; (3) dieses öffentliche CDD-Repository für Methodik, Schema und
Analyse. CDD ist keine Live-Runtime-Abhängigkeit. Ein später eingefrorener
öffentlicher Datensatz oder Release ist ein unveränderliches Artefakt oder ein
optionales viertes Repository, aber nicht Teil dieser Drei-Repository-Grenze.

Shadow v1 liest nach Abschluss vorhandene, autoritative Receipts und erzeugt
keine Aufgaben, Retries, Services, Git-Refs oder Promotions. Eine analytisch
abgeleitete Episode ist exakt nur dann `Accepted`, wenn der strukturelle
CDD-Verifier aus einem rohen Promotion-Event eine opake
`VerifiedPromotionEvidence` erzeugt hat;
Laufzeit, Wachstum, Tests oder Modellturns sind kein Ersatz.

## Erhobene Felder und Quellen

CDD-Core besitzt und validiert den kuenftigen rohen JSON-Draft
`cdd.agentic-sdlc-observatory.public-observation-snapshot.v1` mit
`RunObservations`, `PromotionObservations`, `InterventionObservations`,
`TelemetryGaps`, `Coverage` und `Integrity`. Er enthaelt weder Episodes noch
Aggregate, abgekoppelte Summen oder Ratios. `Episode` entsteht erst als
analytischer Join der rohen Run- und Promotion-Beobachtungen. Historische
Runs ohne Task-Zuordnung behalten `TaskAttributionMissingReason`; CDD erfindet
keine Task-ID.
Öffentlich zulässig sind nur öffentliche Task-, Attempt- und Epoch-IDs,
eine vollständige Multi-Agent-Konfiguration (Scout, Builder, Critic, Reviewer)
oder eine explizite Nichtverfügbarkeit, terminale Disposition, sanitizierte
Promotions-Beobachtungen mit Source-Event-ID, -Sequenz und SHA-256-Hash sowie
Task-, Change-Set-, Promotion-, Candidate-, Commit- und Tree-Identifier sowie
Dauer-, Token-, Kosten-, Repair-, Gate- und Interventionsmesswerte. Mehrere
Episoden duerfen bewusst dieselbe Epoch-ID teilen. Jede Messung traegt
`Observed`, `Estimated`, `Unavailable` oder `NotApplicable`, eine nichtleere
Unit und bei fehlenden Werten einen nichtleeren MissingReason; unbekannt wird
nie zu null normalisiert. Kosten tragen zusaetzlich Currency, Source, Status
und MissingReason. `Accepted` akzeptiert ausschließlich den geschlossenen
Authority-Fall `RequiredGateReceipt`; freie Authority-Strings sind keine
Evidenz. Candidate-Fingerprints und Source-Hashes sind exakt 64 lower-hex
Zeichen; Commit und Tree sind entsprechend dem expliziten Git-Objektalgorithmus
exakt 40 (SHA-1) oder 64 (SHA-256) lower-hex Zeichen. Evidenz-Task und
-Change-Set muessen zur Episode passen, und eine Promotion darf nicht vor dem
beobachteten Run-Ende liegen.

Quellen sind ausschließlich typisierte, bereits sanitizierte Projektionen:
Riftward `RunRecord`, Promotion- und Gate-Receipts, OpenCode-Nutzungs-Export,
Operator-Declaration oder explizite Ableitung. Prompts, Host-Pfade,
Session-IDs, Rohlogs, interne/private IDs, Credentials und Freitext-Evidenz
sind kein Feld des öffentlichen Modells und werden vor der Projektion verworfen.

## Validität und Auswertung

Alle Versuche, auch `Discarded`, `Superseded` und `Unresolved`, verbleiben im
Datensatz. Wachstum (Commits, Dateien, Laufzeit, Tokens oder Agentturns) wird
nicht als Produktivität oder Erfolg interpretiert. Der Kern verweigert negative
Werte, doppelte Attempt- oder autoritative Source-Events sowie doppelte
vollstaendige Candidate/Commit/Tree-Bindungen, widersprüchliche Outcome-/Promotion-Paare,
Gate-Failures über Gate-Runs sowie beobachtete/geschätzte Werte ohne Wert und
Quelle. Aggregation ist deterministisch und weist Vollständigkeit getrennt nach
Qualitätsklasse aus. Ein unbekannter Wert wird nie als beobachtete Null und
eine Teilmenge nie als vollständige Coverage ausgegeben. Derselbe Tree darf
in unterschiedlichen vollstaendigen Bindungen vorkommen; Tree-Gleichheit allein
ist kein Duplikat.

`PublicObservationSnapshotV1` ist gegenwaertig nur der CDD-eigene
Publikations-Gate-Draft. Das Golden Fixture in CDD beweist Parser-/Serializer-
Stabilitaet und Legacy-Missingness, aber keine Producer-Kompatibilitaet. Erst
ein von `riftward-autopilot-ops` emittiertes, sanitisiertes Fixture, das exakt
parst und roundtrippt, darf dieses Gate schliessen.

Hauptbedrohungen: selektive Erfassung, Price-/Provider-Drift, Fehlzuordnung
einer Promotion, nicht vergleichbare Aufgaben, Korrelation von Gate und
Evaluator, veränderte Harness-/Budget-/Modellbedingungen sowie menschliche
Intervention. Jeder Vergleich bindet deshalb Task-Fixture, Protokoll,
Modell/Harness, Budget und Quellenstand; ohne diese Gleichheit gibt es keinen
Effekt- oder Kausalclaim.

## Einordnung und offene Standards

Riftward bearbeitet keine isolierte „KI schreibt ein Spiel“-Behauptung, sondern
eine longitudinale Einzelfallstudie kontinuierlicher Software-Evolution.
SWE-Milestone nutzt dafür Milestone-DAGs und berichtet neben Ergebniswerten
Kosten, Dauer, Turns und Output-Token. TheBotCompany untersucht mehrtägige
Entwicklung mit Strategy-, Execution- und Verification-Phasen sowie
asynchroner menschlicher Aufsicht. Diese Arbeiten sind Vergleichspunkte, aber
kein Beleg für Priorität oder Überlegenheit von Riftward.

Die Ereignishülle orientiert sich konzeptionell an W3C PROV
(Entity/Activity/Agent und Derivation). Eine spätere OTLP-Projektion darf die
OpenTelemetry-CI/CD-Spans für Pipeline- und Task-Runs abbilden; deren Status ist
aktuell Release Candidate und sie bleiben Austauschformat, nicht
Promotionsauthority. Eingefrorene Forschungsreleases sollen als RO-Crate mit
Schema, Dataset Card, Missingness, Protokoll, Analysecode und
Integritäts-Commitments paketiert werden.

Für Gameplay-Resultate reichen Build- und Unit-Gates ausdrücklich nicht:
GameDevBench zeigt die zusätzliche Schwierigkeit multimodaler
Game-Development-Aufgaben; OpenGame-Bench trennt Build Health, Visual Usability
und Intent Alignment. Riftward benötigt daher später reproduzierbare
Runtime-/Capture-Evidenz und externe Playtestsignale als eigene Outcome-Ebene.

## Ablationen und Ralph-Baseline

Die spätere Auswertung kontrastiert mindestens einen Minimal-Ralph-Arm mit
einem methodisch definierten CDD-Arm, bei gleicher Task-Reihenfolge, Gates,
Budget, Modell- und Harness-Deklaration. Ablationen entfernen einzeln Gate-Bindung,
Repair-Klassifikation oder Promotion-Receipt-Bindung. Berichtet werden
vollständig: akzeptierte und verworfene Änderungen, Fehlpromotions,
Vollständigkeit, Dauer, Tokens, Kosten, Gates, Repairs und Interventionen.
Kleine oder unvollständige Samples bleiben deskriptiv; Shadow v1 begründet
keinen allgemeinen Autonomie-, Qualitäts- oder Produktivitätsclaim.

## Quellen

- `research/protocols/loop-engineering-v1.md` (Ralph-Baseline und Fault-Injection)
- `PUBLICATION_POLICY.md` (öffentliche Daten- und Prüfungsschranke)
- `Cdd.Core.Riftward.RunRecord` (sanitisierte, terminale Run-Projektion)
- [SWE-Milestone](https://swe-milestone.com/)
- [TheBotCompany paper](https://arxiv.org/abs/2603.25928)
- [OpenTelemetry CI/CD spans](https://opentelemetry.io/docs/specs/semconv/cicd/cicd-spans/)
- [W3C PROV-DM](https://www.w3.org/TR/prov-dm/)
- [RO-Crate 1.3](https://www.researchobject.org/ro-crate/specification/1.3/)
- [GameDevBench](https://arxiv.org/abs/2602.11103)
- [OpenGame](https://arxiv.org/abs/2604.18394)
