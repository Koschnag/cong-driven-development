# CDD Open Control Plane: Forschungs- und Tool-Landschaft

> **Stand 2026-08-23 · Position:** CDD arbeitet an einer Frontier-Fragestellung,
> besitzt aber noch nicht die empirische Reife, Datenmenge oder externe
> Validierung der großen KI-Labore. Dieses Dokument trennt Konvergenz der Ideen,
> mögliche Eigenleistung und noch offene Beweisarbeit.

## 1. Forschungsposition

CDD untersucht nicht primär, wie ein besseres Basismodell trainiert wird. Die
Forschungsfrage liegt eine Ebene darüber:

> Wie wird aus wechselbaren Modellen, Werkzeugen und Laufzeitumgebungen eine
> nachvollziehbare, langlaufende und risikoadaptiv autonome
> Software-Evolutionskette, deren Promotion durch unabhängige Evidenz statt
> Selbstbestätigung des Generators begrenzt wird?

Das überschneidet sich deutlich mit aktueller Frontier-Forschung:

| Beobachtung der Frontier | Entsprechung in CDD | Ehrlicher Stand |
|---|---|---|
| Anthropic modelliert Vertrauen als Zusammenspiel aus **Modell, Harness, Tools und Umgebung**. | EIDOS trennt taktische Agent Unit, Mission Order, Capabilities, Doctrine und System Twin. | Domänenmodell und kleiner ZT2-Kernel vorhanden; keine Millionen produktiver Sessions. |
| Anthropic misst reale Autonomie über Laufdauer, Genehmigungen, Unterbrechungen und Erfahrung der Nutzer. | CDD will Autonomie pro Mission über Risiko, Intervention, Evidence und Outcome messen. | Sanitisierte Riftward-Baseline-Aggregation (`Cdd.Core.Riftward`) implementiert; reale longitudinale Riftward-Läufe fehlen weiterhin. |
| OpenAI macht Repository, UI, Logs, Metriken und Architekturregeln für Agenten direkt les- und prüfbar. | SPOT, testbare Invarianten, Browser-Gates, Run-Ledger und Control-Plane-Projektionen. | Browser- und Kernel-Gates existieren; Laufzeit-Telemetrie und Multi-Repo-Daten sind erst ein Vertical Slice. |
| OpenAI trennt den Codex-Harness über einen App-Server von CLI, IDE und weiteren Clients. | CDD behandelt Harness und Oberfläche als Adapter hinter offenen Verträgen. | CDD hat CLI, Web, REST und MCP; weitere Adapter sind Roadmap. |
| OpenAI und Anthropic betonen valide Evals, Defense in Depth, Stopbedingungen und menschliche Kontrolle. | Evidence Packs, korrelationsarme Orakel, Promotion Gate, harte Abort-Kriterien und benannte Authority. | Synthetischer ZT2-Benchmark; externe Validität noch nicht gezeigt. |
| METR misst einen Time Horizon über die menschliche Vergleichsdauer von Aufgaben bei definierter Erfolgswahrscheinlichkeit. | CDD soll Taskschwierigkeit, Zuverlässigkeit und Intervention statt bloßer Terminal-Uptime messen. | Duplikatsichere, missions- und protokollgetrennte Baseline-Aggregation vorhanden; noch keine kalibrierte Riftward-Task-Suite oder statistisch belastbare Wiederholung. |

Primärquellen:

- [Anthropic: Trustworthy agents in practice](https://www.anthropic.com/research/trustworthy-agents)
- [Anthropic: Measuring AI agent autonomy in practice](https://www.anthropic.com/research/measuring-agent-autonomy)
- [Anthropic: Building effective agents](https://www.anthropic.com/engineering/building-effective-agents)
- [Anthropic: Demystifying evals for AI agents](https://www.anthropic.com/engineering/demystifying-evals-for-ai-agents)
- [OpenAI: Harness engineering in an agent-first world](https://openai.com/index/harness-engineering/)
- [OpenAI: Unrolling the Codex agent loop](https://openai.com/index/unrolling-the-codex-agent-loop/)
- [OpenAI: Unlocking the Codex harness](https://openai.com/index/unlocking-the-codex-harness/)
- [OpenAI: Practices for Governing Agentic AI Systems](https://openai.com/index/practices-for-governing-agentic-ai-systems/)
- [OpenAI: SWE-Lancer](https://openai.com/index/swe-lancer/)
- [METR: Task-Completion Time Horizons](https://metr.org/time-horizons/)

### Bewegt sich CDD „in derselben Liga“?

**Auf Ebene des Problemraums: ja.** CDD konvergiert unabhängig mit zentralen
Erkenntnissen der Labore: Harness Engineering, maschinenlesbare Umgebungen,
Defense in Depth, Agent-/UI-Trennung, laufende Beobachtung und valide Orakel.

**Auf Ebene der Evidenz: noch nicht.** Anthropic und OpenAI verfügen über große
Teams, Frontier-Modelle, erhebliche Rechenmittel und Auswertungen sehr vieler
realer Sessions. CDD hat einen typisierten Kernel, derzeit 65+ Tests, einen
synthetischen Zehn-Fall-Benchmark, Browser-E2E und kleine Referenzfälle. Das ist
ein Forschungsprototyp, keine vergleichbare empirische Grundlage.

Die mögliche Eigenleistung ist nicht „noch ein Coding Agent“, sondern die
integrierte Architektur aus:

1. typisiertem SPOT und epistemischen Claim-Zuständen,
2. Semantic Change Compiler und expliziten Alternativkandidaten,
3. Doctrine, Mission Orders und begrenzten Capabilities,
4. unabhängigen Evidence Packs und risikoadaptiver Promotion,
5. formalen und empirischen Assurance-Verfahren in einem Portfolio,
6. Outcome- und Evolutionsschleife über den vollständigen SDLC und Ops.

Eine Prioritäts- oder Neuheitsbehauptung folgt daraus nicht. Dafür braucht es
einen systematischen Literaturreview, Vergleichsimplementierungen und externe
Begutachtung.

Auch ein zwei Tage offenes Terminal wäre noch kein Zwei-Tage-Time-Horizon:
METR bezeichnet damit die geschätzte menschliche Bearbeitungsdauer einer
Aufgabe, die das System mit einer bestimmten Wahrscheinlichkeit schafft. Uptime,
Task-Komplexität und zuverlässige Autonomie müssen getrennt berichtet werden.

## 2. Produktgrenze: Was CDD baut und was es integriert

CDD ist der semantische und governance-orientierte Control Plane. Es soll nicht
Editor, Diagrammrenderer, Workflow-Engine, Forge, Telemetrie-Backend und
Artifact Registry neu implementieren.

```text
10  Intent & Authority       Mensch · Organisation · normative Grenzen
09  CDD Semantic Plane      SPOT · Claims · Doctrine · Promotion · Evolution
08  Assurance Plane         Typen · Tests · Modelle · Beweise · Policy · Runtime
07  Execution Plane         Agent Harness · Workflow Engine · CI/CD · Ops
06  Open Contracts          MCP · LSP · GLSP · OSLC · CDEvents · OTLP · OCI
05  Replaceable Products    Theia · GitHub/Forgejo · Temporal · OPA · Grafana …
```

| Kategorie | Entscheidung | Kandidaten / offene Naht |
|---|---|---|
| Semantischer Twin, Claims, Intent und Konvergenz | **Bauen** | CDD.Core / SPOT |
| Doctrine, Mission Dispatch, Evidence und Promotion | **Bauen** | EIDOS-Kernel; OPA nur als Policy-Adapter |
| Studio-Shell | **Adoptieren, nicht forken** | [Eclipse Theia](https://theia-ide.org/docs/) als prüfbarer FOSS-Kandidat; heutiges CDD Web bleibt schlanker Prototyp |
| Textuelle Sprachdienste | **Adaptieren** | [LSP](https://microsoft.github.io/language-server-protocol/) |
| Typisierte grafische Editoren | **Adaptieren** | [Eclipse GLSP](https://eclipse.dev/glsp/documentation/overview/) |
| Lifecycle-Verlinkung | **Adaptieren** | [OSLC](https://open-services.net/) zwischen Requirements, Architektur, Quality und Change |
| Agent-Werkzeuge und Kontext | **Adaptieren** | MCP, CLI und REST; OpenHands, SWE-agent, Codex, Claude Code und OpenCode bleiben austauschbare Worker |
| Mehrtägige, wiederaufnehmbare Ausführung | **Adoptieren** | [Temporal](https://docs.temporal.io/) als bevorzugter Kandidat; Argo für Kubernetes-DAGs beobachten |
| Events | **Adaptieren** | [CloudEvents](https://cloudevents.io/) + [CDEvents](https://cdevents.dev/docs/) |
| Traces, Metriken und Logs | **Adoptieren** | [OpenTelemetry/OTLP](https://opentelemetry.io/docs/); Backends bleiben ersetzbar |
| Capability-/Promotion-Policy | **Adoptieren** | [Open Policy Agent](https://www.openpolicyagent.org/docs) hinter CDD Doctrine |
| Artefakte und Provenienz | **Adaptieren** | OCI/ORAS, SLSA und in-toto Attestations |
| Reproduzierbare Umgebung | **Adoptieren** | Nix/Dev Containers; Umgebung ist Teil der Evidence |

### Wann Temporal sinnvoll wird

Ein Shell-Loop kann Stunden laufen, ist aber kein belastbarer mehrtägiger
Prozess. Sobald CDD Runs über Prozessabstürze, Rechnerneustarts, Rate Limits und
Wartezeiten hinweg fortsetzen soll, braucht es persistente Workflow-Historie,
idempotente Activities, Leases, Retry-Policies, Deadlines und Compensation.
Diese generische Durability sollte CDD übernehmen. CDD selbst entscheidet
weiterhin, **welche** Mission zulässig ist, **welche** Evidenz zählt und **ob**
promoviert werden darf.

Der lokale `Cdd.Core.Autopilot`-Vertical-Slice füllt bewusst die Schicht davor:
Er persistiert den deterministischen Missionszustand, erzeugt genau eine nächste
Harness-Aktion, begrenzt Resume/Fresh-Start/Repair und führt CLI-Gates aus. Das
macht einen abgebrochenen Prozess fortsetzbar, ersetzt aber noch keine
Workflow-Historie, Leases, Timer und idempotenten Activities über mehrere Hosts.
Diese Grenze und der offene Adaptervertrag sind in
[full-agentic-sdlc.md](full-agentic-sdlc.md) beschrieben.

## 3. Formale und mathematische Assurance

Theoretische Informatik, Mathematik und Philosophie sind kein Schmuck. Sie
helfen an unterschiedlichen Fehlergrenzen und werden deshalb selektiv gewählt:

| Frage | Geeignetes Verfahren | Typischer CDD-Einsatz |
|---|---|---|
| Sind ungültige Zustände repräsentierbar? | F#-Typen und statische Analyse | immer an Domänengrenzen |
| Gilt eine Eigenschaft für viele Eingaben? | Property-based Testing / FsCheck | Parser, Compiler, Policy- und Transformationsinvarianten |
| Sind Rollen, Beziehungen und Rechte konsistent? | Alloy | Capability- und Traceability-Modelle |
| Bleiben Safety und Liveness bei Retry, Lease, Crash und Konkurrenz erhalten? | [TLA+/TLC](https://docs.tlapl.us/) | dauerhafte Orchestrierung und Promotion-Protokolle |
| Erfüllt ein kritischer Algorithmus Vor-/Nachbedingungen und terminiert er? | Dafny/F*/SMT | kleine hochintegre Transformationskerne |
| Braucht eine tragende Aussage einen kernel-geprüften Beweis? | [Lean 4](https://lean-lang.org/doc/reference/latest/) | wenige load-bearing Invarianten |
| Ist die Norm selbst richtig, fair oder gewollt? | explizite Prämissen, Argumentation, benannte Authority | Ethik, Ästhetik, Produktziel und irreversible Entscheidungen |

Ein Beweis des Modells beweist nicht automatisch die Implementierung. Deshalb
bleiben Conformance-Tests, Runtime-Evidenz, Fault Injection und Observability
Teil derselben Assurance-Kette.

## 4. FOSS-, Abstraktions- und Austauschbarkeitsregeln

1. **Offenes kanonisches Format:** CDD-Domänenzustand bleibt versionierbar,
   exportierbar und ohne proprietären Dienst lesbar.
2. **Ports vor SDKs:** Ein Anbieter-SDK endet im IO-Adapter; der Kernel kennt
   nur typisierte Beobachtungen, Aufträge und Evidenz.
3. **Capability statt Vollzugriff:** Adapter erhalten kleinste Rechte pro
   Mission. Ein Protokoll wie MCP ist keine Autorisierung.
4. **Event plus Provenienz:** Eine Projektion darf neu aufgebaut werden. Rohes
   Ereignis, Interpretation und ratifizierte Wahrheit bleiben getrennt.
5. **FOSS-first, nicht FOSS-blind:** Proprietäre Modelle und Dienste sind
   zulässige Adapter, wenn ein offener Ersatzpfad und Datenexport existieren.
6. **Kein Framework wird SPOT:** Auch Theia, Temporal, GitHub, OPA oder ein
   Modell bleibt austauschbar und besitzt nicht die CDD-Domänenautorität.

## 5. Forschungsprogramm bis zu belastbarer Vergleichbarkeit

Riftward wird als longitudinale Fallstudie genutzt, nicht als vorweggenommener
Erfolgsbeweis. Jeder Run soll mindestens erfassen:

- Taskklasse, Schwierigkeit, Scope und vorab fixierte Akzeptanzkriterien,
- Modell, Harness, Kontextstrategie, Tool- und Policyversion,
- Tokens, Requests, Kosten, Laufzeit, Energie-/Hardwareindikatoren,
- menschliche Eingriffe, Genehmigungen, Abbrüche und Eskalationsgründe,
- Gate-Ergebnisse, korrelierte Orakel, Fehlpromotionen und Regressionen,
- Recovery, Reproduzierbarkeit und Outcome nach Integration,
- verworfene Kandidaten, Unsicherheit und negative Resultate.

### Evidence Fitness statt Proxy-Erfolg

Ein grüner Build beantwortet die Frage „baut es?“, nicht automatisch „erreicht
es die behauptete Produkteigenschaft?“. Ein Unit-Test kann eine Regel prüfen,
ohne den realen Datenweg, die Last oder die Hardwaregrenze zu treffen. Ein
Budget dokumentiert eine Absicht, aber noch keine gemessene Einhaltung.

CDD soll deshalb für jede tragende Assurance Obligation die Passung zwischen
Claim und Evidence explizit machen:

| Claim-Dimension | Im Evidence Pack zu binden |
|---|---|
| Gegenstand | exakter Claim und Candidate-/Commit-Digest |
| Systemgrenze | betroffene Runtime, Adapter, Hardware und ausgeschlossene Schichten |
| Szenario | repräsentativer Workload, Szene, Seed und Konfiguration |
| Last | Menge, Konkurrenz, Dauer, Warm-up und Stichprobenumfang |
| Messung | Rohwerte, p50/p95/p99, Unsicherheit und Abbruchkriterien |
| Abweichung | Proxy-Anteil, fehlende Dimensionen und verbleibendes `unknown` |

Das Spielprojekt liefert dafür den ersten konkreten Forschungsfall: Sein
[repräsentativer Frame und seine Budgets](https://github.com/Koschnag/ai-fantasy-rts-rpg/blob/main/docs/PERFORMANCE_BUDGET.md)
verbindet sichtbare und simulierte Einheiten, Animation, Pfadfindung,
Landschaft, Effekte und Ressourcenmetriken auf derselben Zielhardware. Bis
diese Messung existiert, bleiben die Performancebudgets eine gut strukturierte
Hypothese. Genau diese Trennung soll EIDOS später maschinenlesbar promoten oder
fail-closed als `unknown` erhalten.

### Vergleichsdesign

1. Eine repräsentative Taskmenge vorab versionieren.
2. Baselines mit mindestens einem simplen Loop, OpenHands/SWE-agent-artigem
   Harness und dem CDD/EIDOS-Ansatz ausführen.
3. Mehrere Modelle getrennt vom Harness variieren; Budgets angleichen.
4. Aufgaben mehrfach ausführen und Ambiguität durch unabhängige Menschen
   prüfen, statt nur einen Gold-Patch als Wahrheit anzunehmen.
5. Erfolgsrate, Zeit bis zur belastbaren Evidence, Interventionen,
   Fehlpromotion, Kosten und Ressourcenverbrauch berichten.
6. Repro-Packs, Rohprotokolle in sanitisiertem Umfang und negative Resultate
   veröffentlichen; Claims erst danach von `Proposed` zu `Verified` bewegen.

Erst externe Replikation, reale ZT3+-Erfahrung, breitere Benchmarks und Review
rechtfertigen eine Gleichrangigkeitsbehauptung. Bis dahin lautet die präzise
Position: **Frontier-relevante Forschungsfrage, eigenständige FOSS-Architektur,
frühe Evidenz.**
