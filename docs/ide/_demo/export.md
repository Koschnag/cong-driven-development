# SPOT-Kontext

Generiert aus 133 Knoten (`cdd export-context`). Der SPOT-Graph ist die Quelle — dieses Dokument ist Derivat und ersetzt handgepflegte Doku.

**Konvergenz:** Aligned 125 · Pending 8 · Diverged 0 · Orphaned 0

## Ubiquitäre Sprache (Ontologie)

Diese Begriffe sind verbindlich — in Code, Antworten und allen Artefakten:

- **Agent** *(auch: AI-Agent)* — LLM-gestützter Worker, der aus dem SPOT Implementierung, Tests und Doku ableitet
  - bezieht sich auf `term-spot`
- **Change Compiler** *(auch: Semantic Change Compiler)* — Transformiert Intent, System-Twin, Policies und Evidenz in prüfbare Änderungskandidaten samt Obligations und Recovery
  - Teil von `term-eidos`
- **Claim** *(auch: Forschungsclaim)* — Epistemisch typisierte, provenienzbehaftete Aussage: operativ als EIDOS Claim, oeffentlich nur als explizit sanitizierte ResearchClaimNode-Projektion; ihr Status bleibt von Implementierungs- und Marketingreife getrennt
  - ist ein `term-knoten`
- **Cockpit** *(auch: IDE)* — Web-GUI, die den SPOT multidimensional zeigt: Graph, UML, Validierung, Drift
  - bezieht sich auf `term-spot`
- **Course IR** *(auch: Course Intermediate Representation)* — Technologieunabhaengige, datensparsame Zwischenrepraesentation eines Kurses als Quelle fuer validierbare Lernspiel-Artefakte
  - bezieht sich auf `term-system-twin`
- **Doctrine** *(auch: Operational Doctrine)* — Versionierte, maschinenlesbare Regeln für Dispatch, Rechte, Assurance, Eskalation, Promotion und Abbruch
  - Teil von `term-eidos`
- **Drift** — Auseinanderlaufen von Modell und Code — das, was klassische MDA scheitern ließ
  - bezieht sich auf `term-konvergenz`
- **EIDOS** *(auch: EIDOS Framework)* — Doctrine-getriebene, epistemisch typisierte und evidenzgesteuerte Softwareevolution; v0 implementiert den falsifizierbaren Kernel bis zur isolierten ZT2-Sandbox
  - bezieht sich auf `term-spot`
- **Evidence Pack** *(auch: Evidence-Carrying Change)* — Versions-, zeit- und umgebungsgebündelter Nachweis für einen Kandidaten und seine Assurance Obligations
  - Teil von `term-eidos`
- **Knoten** *(auch: Entry, Node)* — Eintrag im SPOT-Graphen mit Identität, Nutzlast und Konvergenz-Status
  - Teil von `term-spot`
- **Konvergenz** — Grad der Übereinstimmung zwischen Modell-Knoten und Implementierung (Pending/Aligned/Diverged/Orphaned)
  - bezieht sich auf `term-knoten`
- **Mission Order** *(auch: Einsatzauftrag)* — Typisierter Auftrag mit Lage, Intent, Scope, Einheit, Constraints, Obligations, Berichts- und Abbruchkriterien
  - Teil von `term-eidos`
- **Ontologie** *(auch: Begriffsmodell)* — Begriffsnetz der Domäne: Begriffe mit Definition und typisierten Beziehungen
  - Teil von `term-spot`
- **Promotion Gate** *(auch: Freigabe-Gate)* — Explizite Authority- und Policy-Entscheidung, die einen geprueften Candidate in einen freigegebenen Zustand ueberfuehrt
  - Teil von `term-eidos`
  - bezieht sich auf `term-evidence-pack`
- **Signal** *(auch: Raw Event)* — Unverändertes Rohereignis aus Feedback, Runtime, Entwicklung, Betrieb, Simulation oder Analyse
  - Teil von `term-eidos`
- **Spec** *(auch: Spezifikation)* — Maschinenlesbarer Vertrag: Intent plus Akzeptanzkriterien in Given/When/Then
  - ist ein `term-knoten`
- **SPOT** *(auch: Single Point of Truth)* — Single Point of Truth — der eine Graph, in dem Modell, Spec, Tests, Risiken, Wissen und Infrastruktur leben
- **System Twin** *(auch: Semantic System Twin)* — Zeitbezogene, provenienzbehaftete Projektion des bekannten Systems einschließlich Unsicherheit und Widerspruch
  - bezieht sich auf `term-spot`
- **Ubiquitäre Sprache** *(auch: Ubiquitous Language)* — Gemeinsames Vokabular von Fachseite, Technik und AI-Agents — definiert in der Ontologie
  - bezieht sich auf `term-ontologie`

## Invarianten (Governance — werden bei jeder Validierung erzwungen)

- **Jeder Begriff der ubiquitären Sprache ist definiert** — jeder Begriff braucht eine Definition
- **Kritische Risiken brauchen eine Mitigation** — kritische Risiken brauchen eine Mitigation
- **Jede Spec hat mindestens einen Test** — jede Spec braucht mindestens einen Test
- **Begriffe heißen term-*** — Ids der Art 'term' beginnen mit 'term-'

## Prämissen (nicht verhandelbar)

- **Eine Agentenkette ist ein Plan, nicht die Architektur.** — Doctrine, Lage, Risiko und verfügbare Capabilities erzeugen pro Mission einen begrenzten Ausführungsplan
- **Cloud-first: nichts muss lokal laufen.** — Thin Clients als Terminals; GitHub (Pages, Codespaces, GHCR, Releases) trägt alles
- **Evidence vor Promotion.** — Ein Candidate wird nur befördert, wenn alle risikoadaptiven Obligations mit benannter, reproduzierbarer Evidenz erfüllt sind
- **Kein Python — nie.** — Ein Stack (.NET/F#), keine Toolchain-Fragmentierung; Typsicherheit durchgängig
- **Oeffentliche Artefakte sind generisch, synthetisch und vor Veroeffentlichung sanitisiert.** — Forschung muss reproduzierbar sein, ohne personenbezogene Kurs-, Nutzer-, Infrastruktur- oder Betriebsdaten offenzulegen.
- **Typsicherheit vor Flexibilität.** — Illegale SPOT-Zustände sollen nicht repräsentierbar sein — das Typsystem ist das Schema
- **Unknown bleibt unknown.** — Fehlende Evidenz ist weder Zustimmung noch der Nachweis, dass ein Bereich nicht betroffen ist

## Entscheidungen (ADRs)

### F# für die Domain (`adr-001-fsharp`)
- **Kontext:** Das SPOT-Modell braucht Summen-Typen, Pattern-Matching und Unveränderlichkeit
- **Entscheidung:** F# mit Discriminated Unions als Modellsprache; C# nur für IO-Adapter
- **Konsequenzen:** Kleinere Community, dafür beweisbar korrektere Modelle und Lean-4-Anschlussfähigkeit

### Ein JSON-File pro Knoten (`adr-002-json-store`)
- **Kontext:** Der SPOT muss git-diffbar, mergebar und ohne Server nutzbar sein
- **Entscheidung:** Persistenz als .spot/<id>.json via FSharp.SystemTextJson
- **Konsequenzen:** Kein Query-Layer; bei Wachstum später SQLite/Index möglich, Format bleibt Austauschformat

### GitHub-native Infrastruktur (`adr-003-github-only`)
- **Kontext:** Eigene Domains/Server erzeugen Pflegekosten und private Abhängigkeiten
- **Entscheidung:** Pages für die Demo, Actions für CI/CD, GHCR für Container, Releases für Binaries
- **Konsequenzen:** Demo-Modus braucht localStorage statt Backend; volle Version via Codespaces/Container

### Lizenz MPL-2.0 (`adr-004-mpl2`)
- **Kontext:** Offenheit gewünscht, aber Datei-Copyleft statt viralem Projekt-Copyleft
- **Entscheidung:** MPL-2.0
- **Konsequenzen:** Kommerzielle Nutzung möglich, Änderungen an CDD-Dateien bleiben offen

### EIDOS als Zielarchitektur über dem CDD-Kernel (`adr-005-eidos-target`)
- **Kontext:** CDD besitzt SPOT und ein Konvergenz-Orakel, aber noch kein epistemisches Lagebild, Change Compilation, Mission Dispatch oder Outcome-Lernen
- **Entscheidung:** CDD bleibt der überprüfbare Kernel; EIDOS wird als getrenntes, ehrlich als Pending markiertes Architektur- und Forschungsprogramm entwickelt
- **Konsequenzen:** Neue Capabilities werden zuerst im SPOT spezifiziert; Produktclaims unterscheiden implementierten Ist-Stand und Zielbild

### CDD als reproduzierbares Forschungs-Monorepo (`adr-006-research-monorepo`)
- **Kontext:** Paper, Framework, Studio und Referenzprojekt muessen bis Version 1.0 auf denselben versionierten Forschungsstand zeigen.
- **Entscheidung:** Research Track, CDD-Kernel, CourseForge-Referenzprojekt und Studio bleiben bis Version 1.0 in einem oeffentlichen, sanitisierten Monorepo.
- **Konsequenzen:** Releases koennen Code, Claims, Protokolle und Evidenz gemeinsam pinnen; spaetere Distributionen duerfen getrennt paketiert werden.

## Spezifikationen

### Agent-Interface (`spec-agent-interface`, Aligned)
**Intent:** Prosa-Eingaben werden durch eine KI in validierte Modelländerungen übersetzt

- GIVEN eine Prosa-Beschreibung einer Modelländerung WHEN der Agent ausgeführt wird (Claude direkt oder via kopiertem Prompt) THEN entsteht ein prüfbarer Änderungsvorschlag (upsert/delete), der erst nach Bestätigung angewendet wird

### Chat-primaere Cockpit-Shell (`spec-cockpit-shell`, Aligned)
**Intent:** Das Cockpit ist chat-primaer: eine Omnibar als einzige Tuer, eine Menueleiste, die Rail mit Flaechen, der Faden und eine Statuszeile.

- GIVEN das Cockpit ist gegen das Selbstmodell geladen WHEN die Shell rendert THEN sind Omnibar, Menueleiste, Rail-Flaechen, Faden und die Statuszeile mit Knotenzahl da

### Course IR zu authoring-gegatem Spielplan (`spec-courseforge-gameplan`, Aligned)
**Intent:** Ein importierter Kurs erzeugt reproduzierbare Lernmissions-Skelette, ohne fachliche Richtigkeit vorzutaueschen.

- GIVEN derselbe Course IR und Source Fingerprint WHEN der Spielplan mehrfach erzeugt wird THEN entstehen identische Missionen, die bis zur fachlichen Ratifikation NeedsAuthoring behalten

### Datensparsamer Moodle-Folder-Import (`spec-courseforge-import`, Aligned)
**Intent:** Ein generischer Course-IR-Adapter liest nur benoetigte Kursmetadaten und schliesst sensible Moodle-Daten aus.

- GIVEN ein synthetischer, extrahierter Moodle-Backup-Ordner WHEN CourseForge den Ordner importiert THEN entsteht ein deterministischer Course IR mit Kurs und Abschnitten
- GIVEN ein Ordner mit users.xml, fremden Dateien oder ueberschrittenen Quotas WHEN CourseForge den Import prueft THEN werden sensible Daten ausgeschlossen und harte Datei- sowie Groessenlimits fail-closed erzwungen

### Modell → Code (derive-code) (`spec-derive-code`, Aligned)
**Intent:** Aus unabgedeckten Test-Knoten entstehen implementierbare Test-Skelette mit fertigem Mess-Marker

- GIVEN ein Test-Knoten ohne Marker im Test-Code WHEN cdd derive-code läuft THEN entsteht ein xUnit-Skelett mit Trait(spot, id) und den Kriterien als Vorgabe; abgedeckte Knoten werden übersprungen

### Spec→Test-Ableitung (`spec-derive-tests`, Aligned)
**Intent:** Tests sind Derivat der Spezifikation, nicht handgeschrieben

- GIVEN eine Spec mit n Akzeptanzkriterien WHEN cdd derive-tests --write läuft THEN existiert genau ein Test-Knoten pro Kriterium
- GIVEN bereits abgeleitete Tests WHEN derive-tests erneut läuft THEN entstehen keine Duplikate (Idempotenz)

### Getypte Diagramm-Flaeche mit Toolbox (`spec-diagram-surface`, Aligned)
**Intent:** Die Split-Mitte zeigt den getypten SPOT-Graphen als Cytoscape-Diagramm mit mehreren Sichten und der EA-Toolbox.

- GIVEN das Cockpit ist geladen WHEN die Diagramm-Flaeche rendert THEN erscheinen die Diagramm-Sichten, ein Cytoscape-Canvas und die Toolbox mit den Knotenarten

### Semantic Change Compiler (`spec-eidos-change-compiler`, Aligned)
**Intent:** Intent, Twin, Policies und Evidenz erzeugen vergleichbare Candidates statt einer unprüfbaren Einzelantwort

- GIVEN ein Change Intent, ein versionierter System-Twin, Policies und aktuelle Evidenz WHEN der Change Compiler läuft THEN entstehen deterministische Candidate-Metadaten mit Semantic Delta, Artefakten, Obligations, Deployment und Recovery
- GIVEN mehrere zulässige Candidates WHEN Impact und Risiken bewertet werden THEN bleiben Alternativen, Annahmen und verworfene Optionen im Event Ledger nachvollziehbar

### Epistemisch typisierte Claims (`spec-eidos-epistemic-claims`, Aligned)
**Intent:** Beobachtung, Aussage, Ableitung, Vorschlag, Ratifikation und Verifikation bleiben unterscheidbar und provenienzbehaftet

- GIVEN ein Rohsignal und eine maschinelle Interpretation WHEN beide in den System-Twin projiziert werden THEN bleiben Originalsignal, Claim, Provenienz, Zeitpunkt, Scope und epistemischer Status getrennt erhalten
- GIVEN widersprüchliche Claims oder fehlende Evidenz WHEN ein Lagebild erzeugt wird THEN werden Contested und Unknown explizit dargestellt statt zu einer scheinbar sicheren Aussage geglättet

### Evidence Packs und Promotion (`spec-eidos-evidence-pack`, Aligned)
**Intent:** Promotion ist eine reproduzierbare Policy-Entscheidung über Evidence statt eine Selbstbestätigung des Generators

- GIVEN ein Candidate mit risikoadaptiven Assurance Obligations WHEN Generator-unabhängige Gates laufen THEN bindet das Evidence Pack Ergebnis, Tool- und Policyversion, Umgebung, Zeitpunkt und Artefakt-Hash
- GIVEN eine fehlende, veraltete oder rote Obligation WHEN Promotion bewertet wird THEN wird der Candidate nicht befördert und der konkrete Nachweisgrund bleibt auditierbar

### Doctrine und Mission Orders (`spec-eidos-mission-order`, Aligned)
**Intent:** Jede Agentenausführung erhält einen typisierten Auftrag mit Rechten, Budget, Obligations, Reporting und Abbruchbedingungen

- GIVEN ein klassifiziertes Change Intent und eine versionierte Doctrine WHEN eine Mission disponiert wird THEN entsteht eine Mission Order mit Lage, Ziel, Scope, Einheit, Constraints, Erfolg und Abbruch
- GIVEN eine Mission mit überschrittenem Budget, fehlender Capability oder verletzter Policy WHEN der Control Plane die Verletzung meldet THEN wird fail closed abgebrochen oder an eine zuständige Authority eskaliert

### Zero-Touch-Sandbox im OpsLab (`spec-eidos-zt2-opslab`, Aligned)
**Intent:** Ein klar definierter Change wird autonom bis zu einer isolierten, vollständig replaybaren Sandbox durchgeführt

- GIVEN eine synthetische versionierte Report-/Submission-Anwendung und eine Mission Order WHEN der EIDOS-Lauf auf ZT2 startet THEN erzeugt, prüft und deployt er den Candidate ausschließlich in der Sandbox ohne produktive Credentials
- GIVEN ein Gate-Fehler, Timeout oder verletztes Abbruchkriterium WHEN die Mission endet THEN bleibt das Zielsystem unverändert und Event Ledger, Evidence und Recovery-Ergebnis sind replaybar

### LLM-Kontextexport (`spec-export-context`, Aligned)
**Intent:** Der SPOT-Graph wird zur Vorlage, aus der ein Agent den Rest baut

- GIVEN ein gefüllter SPOT-Graph WHEN cdd export-context läuft THEN entsteht ein einzelnes Markdown-Bundle mit Ontologie, Prämissen, Entscheidungen, Specs und offenen Risiken
- GIVEN das exportierte Bundle WHEN es einem LLM als Kontext übergeben wird THEN kann es Implementierungsaufgaben ohne Rückfragen zur Domänensprache bearbeiten

### Feedback zu kontrolliertem EIDOS Change Intent (`spec-feedback-evolution`, Aligned)
**Intent:** Bug- und Feature-Signale duerfen nur pruefbare Vorschlaege erzeugen, die ein expliziter Adapter in risikotypisierte EIDOS Change Intents ohne Promotion-Autoritaet kompiliert.

- GIVEN ein oeffentlicher Feature- oder Bug-Report ohne personenbezogene Daten WHEN das Signal klassifiziert wird THEN entsteht ein ProposalOnly FeedbackChangeProposal und daraus ein risikotypisierter EIDOS Change Intent mit Assurance und menschlichem Promotion Gate
- GIVEN ein Signal mit personenbezogenen Daten oder Security-Bezug WHEN das Signal klassifiziert wird THEN wird es verworfen oder in einen getrennten Security-Prozess eskaliert und erzeugt keinen autonomen Candidate

### Fehlerliste & Widerspruchs-Erkennung (`spec-fehlerliste`, Aligned)
**Intent:** Inkonsistenzen, Widersprüche und Regelverstöße sind eine klickbare Liste wie in Visual Studio

- GIVEN eine zyklische IsA/PartOf-Begriffshierarchie WHEN validiert wird THEN erscheint ein Widerspruchs-Fehler in der Fehlerliste; Klick springt zum Knoten
- GIVEN zwei Begriffe mit gleichem Namen WHEN validiert wird THEN wird Mehrdeutigkeit als Warnung gemeldet

### Formale code-behind-Sicht (`spec-formal-view`, Aligned)
**Intent:** Dasselbe SPOT-Modell ist als formale Notation (Typen/Logik/Kategorien, KaTeX) darstellbar.

- GIVEN die Diagramm-Flaeche WHEN auf eine Formal-Sicht gewechselt wird THEN rendert sie das Modell in formaler Notation mit KaTeX

### Gate-Selbsthärtung (`spec-gate-selbst-hart`, Pending)
**Intent:** Das Konvergenz-Orakel wird auf das eigene Modell angewendet: ein Test-Knoten gilt nur als Aligned, wenn ein echter Test-Marker existiert, nicht durch bloße Behauptung

- GIVEN das Selbst-Modell unter .spot/ und der Testcode unter tests/ WHEN die reflexive Invariante über das eigene Modell geprüft wird THEN hat jeder als Aligned markierte Test-Knoten einen echten Trait-spot-Marker im Testcode, also kein Aligned ohne Test

### Governance by Invariance (`spec-governance`, Aligned)
**Intent:** Regeln sind Modell-Knoten und werden bei jeder Validierung (lokal + CI) erzwungen

- GIVEN eine Invariante im SPOT WHEN cdd validate läuft THEN werden Verstöße als Fehler am verletzenden Knoten gemeldet

### MCP-Server (`spec-mcp-server`, Aligned)
**Intent:** Jeder MCP-Client (Claude Code, Claude Desktop, …) kann den SPOT direkt lesen, validieren und mutieren

- GIVEN ein verbundener MCP-Client WHEN spot_upsert oder spot_delete aufgerufen wird THEN wird die Änderung gespeichert und die Validierung (inkl. Invarianten) zurückgemeldet

### Sanitisiertes öffentliches Research-Claim-Ledger (`spec-research-claim-ledger`, Aligned)
**Intent:** Operative EIDOS-Claims werden nur als schmale, quellengebundene und öffentlich geprüfte Forschungsprojektion im SPOT veröffentlicht.

- GIVEN eine öffentliche Forschungsbehauptung mit benannten Quellen und Ableitungen WHEN sie als ResearchClaimNode serialisiert und validiert wird THEN bleiben epistemischer Status, Scope, Zeitpunkt, Methode, Quellen und Ableitungen reproduzierbar erhalten
- GIVEN ein als Verified oder OutcomeConfirmed markierter Research-Claim WHEN öffentliche Evidenz oder gültige Provenienz fehlt THEN schlägt die Validierung fail-closed fehl

### Reproduzierbare Research Snapshots (`spec-research-snapshots`, Pending)
**Intent:** Regelmaessige Forschungsstaende pinnen Code, Claims, Protokolle, Checksummen und Build-Evidenz auf denselben Commit.

- GIVEN ein gruener, sanitizierter Stand auf main oder ein manueller Snapshot-Auftrag WHEN der Research-Snapshot-Workflow laeuft THEN entsteht ein pruefbares Bundle und bei manueller Freigabe ein Draft-Release statt einer ungeprueften Veroeffentlichung

### Round-Trip: Code → Modell (`spec-roundtrip-sync`, Aligned)
**Intent:** Komponenten-Konvergenz wird aus den echten Projekt-Referenzen abgeleitet, nicht behauptet

- GIVEN src/*.fsproj und Component-Knoten WHEN cdd sync-code läuft THEN wird Aligned/Diverged/Orphaned/Pending je Komponente bestimmt und bei Drift Exit 1 geliefert

### Doku-Konvergenz (`spec-sync-docs`, Aligned)
**Intent:** Der README-Status wird aus dem Selbstmodell generiert — Doku-Drift ist ein CI-Fehler

- GIVEN ein veralteter README-Status WHEN cdd sync-docs --check in der CI läuft THEN schlägt der Build fehl, bis sync-docs den Status neu generiert hat
- GIVEN Prämissen, Entscheidungen und Invarianten im Modell WHEN cdd sync-docs läuft THEN wird docs/decisions.md vollständig daraus generiert

### Test-Konvergenz messen (`spec-sync-tests`, Aligned)
**Intent:** Abgeleitete Test-Knoten werden gegen echte automatisierte Tests gemessen statt behauptet

- GIVEN ein Test-Knoten und ein Test mit Trait("spot", id) oder [spot: id]-Marker WHEN cdd sync-tests läuft THEN wird der Knoten Aligned; ohne Marker bleibt er Pending, Abweichung bricht CI

### Modell-Validierung (`spec-validate`, Aligned)
**Intent:** Der SPOT-Graph ist jederzeit strukturell konsistent

- GIVEN ein Knoten mit Referenz auf eine nicht existierende Id WHEN cdd validate läuft THEN wird ein Fehler mit Knoten-Id und Ziel gemeldet
- GIVEN Komponenten mit zyklischen Abhängigkeiten WHEN cdd validate läuft THEN werden alle Zyklus-Teilnehmer als Fehler markiert

## Risiken

- **Autonome Änderungen überschreiten den freigegebenen Scope oder erreichen produktive Systeme** (Likelihood Medium, Impact Critical) — Mitigation: Capability-Allowlists, kleinste Rechte, ZT2 als erstes Ziel, keine Produktiv-Credentials, harte Budgets und Fail-Closed-Abbruch
- **Ein lernendes oder vom Generator abhängiges Orakel driftet und belohnt Scheinerfolg** (Likelihood High, Impact Critical) — Mitigation: Validatorversionen binden, Generator/Validator trennen, unveränderliche Ankerfälle und externe Audits verwenden
- **Generator und Validator teilen denselben systematischen Fehler und erzeugen Scheinevidenz** (Likelihood High, Impact Critical) — Mitigation: Kritische Orakel dekorrelieren, unabhängige Harnesses verwenden und Provenienz sowie Validatorversion im Evidence Pack binden
- **Modell und Code driften auseinander (der MDA-Friedhof)** (Likelihood Medium, Impact Critical) — Mitigation: Konvergenz-Status je Knoten + Round-Trip (Code→Modell) auf der Roadmap
- **Das Evolutionary Memory speichert Korrelation als Ursache und verstärkt eine falsche Strategie** (Likelihood High, Impact High) — Mitigation: Intervention, Störfaktoren und Konfidenz getrennt speichern; Strategy-Änderungen nur nach reproduzierbaren Outcome-Vergleichen
- **SPOT-Pflege wird teurer als der Code, den er erzeugt** (Likelihood Medium, Impact High) — Mitigation: Alles Ableitbare wird abgeleitet (Tests, Diagramme), nie handgepflegt
- **Personenbezogene Kurs-, Feedback-, Infrastruktur- oder Forschungsdaten gelangen in das oeffentliche Showcase-Repository.** (Likelihood Medium, Impact Critical) — Mitigation: Nur synthetische Fixtures; Metadaten-Allowlist; Secret-Scan; PII-Checkliste; private Security-Meldungen; menschliches Release-Gate.
- **Spec-Vollständigkeits-Falle: die Spec wird so komplex wie Code** (Likelihood Medium, Impact High) — Mitigation: Specs bleiben auf Intent/Kriterien/Invarianten-Ebene; Agents füllen Lücken, Validierung fängt Drift

## Komponenten

- **Cdd.Cli** (`comp-cli`) → hängt ab von `comp-core`
- **Cdd.Core** (`comp-core`)
- **CourseForge.Core** (`comp-courseforge`) → hängt ab von `comp-core`
- **Cdd.Mcp** (`comp-mcp`) → hängt ab von `comp-core`
- **Cdd.Web** (`comp-web`) → hängt ab von `comp-core`

## Wissensquellen

- **No Silver Bullet: Essence and Accidents of Software Engineering** (paper, http://www.cs.unc.edu/techreports/86-020.pdf)
  - Software besitzt essenzielle und akzidentelle Schwierigkeiten
  - Werkzeuge koennen akzidentelle Arbeit reduzieren, ohne die konzeptuelle Essenz aufzuheben
- **Collaborative and AI-Supported Requirements Elicitation** (paper, https://arxiv.org/abs/2606.24060)
  - Stakeholder-Kollaboration plus AI-Synthese erzeugte im kontrollierten Versuch die bestbewerteten Artefakte
  - EIDOS automatisiert mechanische Synthese, nicht normative Stakeholder-Autorität
- **Darwin Gödel Machine** (paper, https://arxiv.org/abs/2505.22954)
  - Offene Evolution kann Agentenvarianten empirisch über Benchmarks selektieren
  - Sandboxing und menschliche Aufsicht bleiben Teil der berichteten Sicherheitsmaßnahmen
- **Eric Evans — Domain-Driven Design** (book, ISBN 978-0321125217)
  - Ubiquitous Language ist die Brücke zwischen Fachseite und Code
  - Bounded Contexts begrenzen Modellgültigkeit
- **Responsible Agentic AI Requires Explicit Provenance** (paper, https://arxiv.org/abs/2605.17169)
  - Verantwortung braucht über den gesamten Agenten-Lebenszyklus explizite, eingreifbare Provenienz
  - Provenienz ist ein Strukturmerkmal, kein optionales Log-Detail
- **Martin Fowler — Blog** (blog, https://martinfowler.com)
  - Refactoring-Katalog
  - Evolutionäre Architektur
  - Spec-by-Example
- **Cognitive and Intent Debt** (paper, https://arxiv.org/abs/2603.22106)
  - Fehlendes externalisiertes Rationale erzeugt Intent Debt
  - Softwaregesundheit umfasst Code, gemeinsames Verständnis und explizites Intent-Wissen
- **Intent Formalization — Grand Challenge** (paper, https://arxiv.org/abs/2603.17150)
  - Die Lücke zwischen natürlichem Intent und prüfbarem Verhalten ist der zentrale Engpass
  - Spezifikationsqualität braucht eigene Metriken und Interaktion
- **LLM-Modulo Frameworks** (paper, https://arxiv.org/abs/2402.01817)
  - LLMs und externe modellbasierte Verifizierer sollen bidirektional gekoppelt werden
  - Der externe Verifizierer bleibt Quelle der belastbaren Garantie
- **Moodle Course Backup** (documentation, https://docs.moodle.org/500/en/Course_backup)
  - Moodle-Kursbackups verwenden die Endung .mbz
  - Backups koennen Nutzer-, Rollen-, Datei-, Kommentar-, Abschluss-, Log- und Bewertungsdaten enthalten
  - Ein generischer Importer muss Metadaten und personenbezogene Inhalte strikt trennen
- **NIST SP 800-218 Secure Software Development Framework** (standard, https://csrc.nist.gov/pubs/sp/800/218/final)
  - Security-Praktiken werden risikoorientiert in bestehende Entwicklungsmodelle integriert
  - Provenienz, Security-Anforderungen, Risiken und Designentscheidungen sollen verfolgt werden
- **SLSA Provenance** (standard, https://slsa.dev/spec/v1.2/)
  - Artefakte werden an Builderidentität, Inputs, Zeit und Digests gebunden
  - Evidence Packs übernehmen diese Bindungsprinzipien über Builds hinaus
- **Software Engineering im KI-Zeitalter: Gegenthese zum Hype** (essay, https://www.linkedin.com/pulse/software-engineering-im-ki-zeitalter-gegenthese-zum-hype-nguyen-imnof/)
  - Der Mensch arbeitet am System: Intent, Spezifikationen, Invarianten und Governance
  - KI reduziert Implementierungsmechanik, nicht automatisch essenzielle Komplexität
  - Autonomie muss an maschinenpruefbare, risikoadaptive Gates gebunden sein
  - Langfristige Grundlagen und explizite Modelle sind wichtiger als kurzfristiges Tool-Wissen
- **VeriAct — Beyond Verifiability** (paper, https://arxiv.org/abs/2604.00280)
  - Verifier-Akzeptanz allein garantiert keine korrekte oder vollständige Spezifikation
  - Ein unabhängiges Spec-Harness macht Über- und Unterbeschränkung messbar
- **W3C PROV-O** (standard, https://www.w3.org/TR/prov-o/)
  - Entity, Activity und Agent bilden einen interoperablen Provenienz-Kern
  - EIDOS-v0 bleibt kompakt, soll seine Provenienz später auf PROV abbilden können
- **Who Grades the Grader?** (paper, https://arxiv.org/abs/2607.12790)
  - Evolvierende Metriken brauchen unveränderliche Anker und unabhängige äußere Audits
  - Entfernte Anker können Evaluatoren in triviale oder spielbare Metriken kollabieren lassen

## Forschungsclaims

Der Status beschreibt die Erkenntnislage, nicht Marketing-Reife oder Implementierungsstand.

### Proposed (`claim-essential-complexity-remains`)
- **Aussage:** Generative KI reduziert vor allem akzidentelle Implementierungsarbeit; Spezifikation, Design und Verifikation bleiben essenzielle Aufgaben.
- **Scope:** AI-gestuetzte Softwareentwicklung
- **Methode:** Konzeptionelle Synthese aus Primaertext und oeffentlicher Gegenthese
- **Erfasst:** 2026-07-27T00:00:00Z
- **Quellen:** `kb-brooks-no-silver-bullet`, `kb-software-engineering-ki-gegenthese`
- **Begründung:** Muss empirisch gegen geeignete Entwicklungsbaselines operationalisiert werden.

### Proposed (`claim-gates-bound-autonomy`)
- **Aussage:** Der zulaessige Autonomiegrad eines Software-Agenten sollte durch unabhaengige, maschinenpruefbare und risikoadaptive Gates begrenzt werden.
- **Scope:** Agentische Softwareevolution
- **Methode:** Architekturhypothese aus Verifikationsliteratur und CDD-Gate-Modell
- **Erfasst:** 2026-07-27T00:00:00Z
- **Quellen:** `kb-software-engineering-ki-gegenthese`, `kb-llm-modulo`, `kb-veriact`
- **Abgeleitet aus:** `claim-essential-complexity-remains`
- **Begründung:** Vergleichbare Kandidaten muessen gegen korrelierte Orakel und einen ungeregelten Agenten getestet werden.

### Proposed (`claim-spot-traceability`)
- **Aussage:** Ein typisierter Single Point of Truth kann die Nachverfolgbarkeit von Intent zu Code, Test, Evidenz und Outcome verbessern.
- **Scope:** CDD/SPOT in kleinen und mittleren Softwareprojekten
- **Methode:** Design-Science-Hypothese aus dem CDD-Prototyp
- **Erfasst:** 2026-07-27T00:00:00Z
- **Quellen:** `kb-software-engineering-ki-gegenthese`
- **Abgeleitet aus:** `spec-roundtrip-sync`, `spec-sync-tests`, `spec-sync-docs`
- **Begründung:** Der aktuelle Prototyp zeigt technische Machbarkeit, aber noch keine allgemeine Wirkung.

## Tools (Agent-Capabilities)

- **GitHub Actions** — CI/CD, Releases, Pages, Container — der Automatisierungs-Arm
- **Mermaid** — Diagramm-Rendering (Graph, UML) aus dem SPOT — https://cdn.jsdelivr.net/npm/mermaid@11

## Offene Arbeit (nicht Aligned)

- `claim-essential-complexity-remains` (claim, Pending)
- `claim-gates-bound-autonomy` (claim, Pending)
- `claim-spot-traceability` (claim, Pending)
- `spec-agent-interface-test-1` (test, Pending)
- `spec-export-context-test-2` (test, Pending)
- `spec-gate-selbst-hart` (spec, Pending)
- `spec-mcp-server-test-1` (test, Pending)
- `spec-research-snapshots` (spec, Pending)

