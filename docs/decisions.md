# Prämissen & Entscheidungen

*Generiert aus dem SPOT-Selbstmodell (`cdd sync-docs`) — Hand-Edits werden überschrieben.*

## Prämissen (nicht verhandelbar)

### Eine Agentenkette ist ein Plan, nicht die Architektur.
*Doctrine, Lage, Risiko und verfügbare Capabilities erzeugen pro Mission einen begrenzten Ausführungsplan* · `premise-chain-is-plan`

### Cloud-first: nichts muss lokal laufen.
*Thin Clients als Terminals; GitHub (Pages, Codespaces, GHCR, Releases) trägt alles* · `premise-cloud-first`

### Evidence vor Promotion.
*Ein Candidate wird nur befördert, wenn alle risikoadaptiven Obligations mit benannter, reproduzierbarer Evidenz erfüllt sind* · `premise-evidence-before-promotion`

### Python-freier Vertrauenskern, polyglotte Adapter.
*CDD-Domäne, Promotion und Persistenz bleiben .NET/F#; austauschbare externe Tooladapter dürfen ihre native Sprache nutzen, ohne zur Kernel- oder Runtime-Abhängigkeit zu werden* · `premise-kein-python`

### Oeffentliche Artefakte sind generisch, synthetisch und vor Veroeffentlichung sanitisiert.
*Forschung muss reproduzierbar sein, ohne personenbezogene Kurs-, Nutzer-, Infrastruktur- oder Betriebsdaten offenzulegen.* · `premise-public-sanitized`

### Typsicherheit vor Flexibilität.
*Illegale SPOT-Zustände sollen nicht repräsentierbar sein — das Typsystem ist das Schema* · `premise-typsicherheit`

### Unknown bleibt unknown.
*Fehlende Evidenz ist weder Zustimmung noch der Nachweis, dass ein Bereich nicht betroffen ist* · `premise-unknown-remains-unknown`

## Entscheidungen (ADRs)

### F# für die Domain · `adr-001-fsharp`
- **Kontext:** Das SPOT-Modell braucht Summen-Typen, Pattern-Matching und Unveränderlichkeit
- **Entscheidung:** F# mit Discriminated Unions als Modellsprache; C# nur für IO-Adapter
- **Konsequenzen:** Kleinere Community, dafür beweisbar korrektere Modelle und Lean-4-Anschlussfähigkeit

### Ein JSON-File pro Knoten · `adr-002-json-store`
- **Kontext:** Der SPOT muss git-diffbar, mergebar und ohne Server nutzbar sein
- **Entscheidung:** Persistenz als .spot/<id>.json via FSharp.SystemTextJson
- **Konsequenzen:** Kein Query-Layer; bei Wachstum später SQLite/Index möglich, Format bleibt Austauschformat

### GitHub-native Infrastruktur · `adr-003-github-only`
- **Kontext:** Eigene Domains/Server erzeugen Pflegekosten und private Abhängigkeiten
- **Entscheidung:** Pages für die Demo, Actions für CI/CD, GHCR für Container, Releases für Binaries
- **Konsequenzen:** Demo-Modus braucht localStorage statt Backend; volle Version via Codespaces/Container

### Lizenz MPL-2.0 · `adr-004-mpl2`
- **Kontext:** Offenheit gewünscht, aber Datei-Copyleft statt viralem Projekt-Copyleft
- **Entscheidung:** MPL-2.0
- **Konsequenzen:** Kommerzielle Nutzung möglich, Änderungen an CDD-Dateien bleiben offen

### EIDOS als Zielarchitektur über dem CDD-Kernel · `adr-005-eidos-target`
- **Kontext:** CDD besitzt SPOT und ein Konvergenz-Orakel, aber noch kein epistemisches Lagebild, Change Compilation, Mission Dispatch oder Outcome-Lernen
- **Entscheidung:** CDD bleibt der überprüfbare Kernel; EIDOS wird als getrenntes, ehrlich als Pending markiertes Architektur- und Forschungsprogramm entwickelt
- **Konsequenzen:** Neue Capabilities werden zuerst im SPOT spezifiziert; Produktclaims unterscheiden implementierten Ist-Stand und Zielbild

### CDD als reproduzierbares Forschungs-Monorepo · `adr-006-research-monorepo`
- **Kontext:** Paper, Framework, Studio und Referenzprojekt muessen bis Version 1.0 auf denselben versionierten Forschungsstand zeigen.
- **Entscheidung:** Research Track, CDD-Kernel, CourseForge-Referenzprojekt und Studio bleiben bis Version 1.0 in einem oeffentlichen, sanitisierten Monorepo.
- **Konsequenzen:** Releases koennen Code, Claims, Protokolle und Evidenz gemeinsam pinnen; spaetere Distributionen duerfen getrennt paketiert werden.

### Research Studio ist eine read-only SPOT-Projektion · `adr-007-public-research-studio`
- **Kontext:** Ein visuelles Forschungsportal kann schnell zu einer zweiten Wahrheit oder einem unkontrollierten Agenten-Frontend werden
- **Entscheidung:** Dynamische Forschungsobjekte kommen aus dem öffentlichen Snapshot; Feedback erzeugt nur einen vom Menschen zu prüfenden Issue-Entwurf
- **Konsequenzen:** Storytelling bleibt möglich, Status und Evidenz versioniert; Medien brauchen bei Modelländerungen erneute Prüfung

### CDD als offener semantischer Control Plane statt neuer Alles-Engine · `adr-008-open-semantic-control-plane`
- **Kontext:** Editoren, Diagrammwerkzeuge, Agent-Harnesses, Workflow-Engines, Forges und Observability-Systeme decken einzelne SDLC-Schichten ab und müssen austauschbar bleiben
- **Entscheidung:** CDD baut den typisierten semantischen Kern, Doctrine, Evidence-Promotion und Projektionen; Ausführung, Editoren, Diagramme, Telemetrie, Policy und Artefaktspeicher werden über offene Standards und Ports adaptiert
- **Konsequenzen:** SPOT bleibt Domänenwahrheit; Theia, GLSP, LSP, MCP, OSLC, CDEvents, OTLP, OCI/in-toto und Workflow-Engines können unabhängig ersetzt oder schrittweise eingeführt werden

### Deterministischer Controller über austauschbaren Agent-Harnesses · `adr-009-deterministic-autopilot-controller`
- **Kontext:** Langlaufende Coding-Agenten können vorzeitig enden, ihren eigenen Erfolg überschätzen oder bei großen Aufträgen Kontext und Fortschritt verlieren
- **Entscheidung:** CDD hält den langlebigen Run-Zustand, wählt die nächste typisierte Aktion deterministisch und akzeptiert Agentenausgaben nur als Beobachtung; Provider-Harnesses führen die Aktionen aus
- **Konsequenzen:** Agenten bleiben austauschbar und dürfen nicht selbst promoten; CDD benötigt dafür explizite Slice-, Recovery-, Gate-, Review- und Checkpoint-Protokolle

### Evidence Fitness ist Teil der Promotion Policy · `adr-010-representative-evidence-fitness`
- **Kontext:** Builds, Unit-Tests und dokumentierte Budgets können grün sein, obwohl die behauptete Produkteigenschaft unter repräsentativer Last, Zielhardware oder realer Systemgrenze scheitert
- **Entscheidung:** Jede Assurance Obligation benennt Claim, Systemgrenze, Szenario, Last, Umgebung, Metrik und zulässige Proxys; Promotion lehnt Evidence ab, deren Fitness die Obligation nicht erreicht
- **Konsequenzen:** CDD muss fehlende repräsentative Evidence als unknown erhalten, Evidence-Fitness und Abweichungen berichten und darf Proxy-Erfolg nicht zur Outcome-Aussage hochstufen

## Geltende Invarianten (Governance)

- 🛡️ **Jeder Begriff der ubiquitären Sprache ist definiert** — jeder Begriff braucht eine Definition · `inv-begriffe-definiert`
- 🛡️ **Kritische Risiken brauchen eine Mitigation** — kritische Risiken brauchen eine Mitigation · `inv-kritische-risiken`
- 🛡️ **Jede Spec hat mindestens einen Test** — jede Spec braucht mindestens einen Test · `inv-specs-getestet`
- 🛡️ **Begriffe heißen term-*** — Ids der Art 'term' beginnen mit 'term-' · `inv-term-praefix`
