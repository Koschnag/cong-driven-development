# SPOT-Kontext

Generiert aus 66 Knoten (`cdd export-context`). Der SPOT-Graph ist die Quelle — dieses Dokument ist Derivat und ersetzt handgepflegte Doku.

**Konvergenz:** Aligned 62 · Pending 4 · Diverged 0 · Orphaned 0

## Ubiquitäre Sprache (Ontologie)

Diese Begriffe sind verbindlich — in Code, Antworten und allen Artefakten:

- **Agent** *(auch: AI-Agent)* — LLM-gestützter Worker, der aus dem SPOT Implementierung, Tests und Doku ableitet
  - bezieht sich auf `term-spot`
- **Cockpit** *(auch: IDE)* — Web-GUI, die den SPOT multidimensional zeigt: Graph, UML, Validierung, Drift
  - bezieht sich auf `term-spot`
- **Drift** — Auseinanderlaufen von Modell und Code — das, was klassische MDA scheitern ließ
  - bezieht sich auf `term-konvergenz`
- **Knoten** *(auch: Entry, Node)* — Eintrag im SPOT-Graphen mit Identität, Nutzlast und Konvergenz-Status
  - Teil von `term-spot`
- **Konvergenz** — Grad der Übereinstimmung zwischen Modell-Knoten und Implementierung (Pending/Aligned/Diverged/Orphaned)
  - bezieht sich auf `term-knoten`
- **Ontologie** *(auch: Begriffsmodell)* — Begriffsnetz der Domäne: Begriffe mit Definition und typisierten Beziehungen
  - Teil von `term-spot`
- **Spec** *(auch: Spezifikation)* — Maschinenlesbarer Vertrag: Intent plus Akzeptanzkriterien in Given/When/Then
  - ist ein `term-knoten`
- **SPOT** *(auch: Single Point of Truth)* — Single Point of Truth — der eine Graph, in dem Modell, Spec, Tests, Risiken, Wissen und Infrastruktur leben
- **Ubiquitäre Sprache** *(auch: Ubiquitous Language)* — Gemeinsames Vokabular von Fachseite, Technik und AI-Agents — definiert in der Ontologie
  - bezieht sich auf `term-ontologie`

## Invarianten (Governance — werden bei jeder Validierung erzwungen)

- **Jeder Begriff der ubiquitären Sprache ist definiert** — jeder Begriff braucht eine Definition
- **Kritische Risiken brauchen eine Mitigation** — kritische Risiken brauchen eine Mitigation
- **Jede Spec hat mindestens einen Test** — jede Spec braucht mindestens einen Test
- **Begriffe heißen term-*** — Ids der Art 'term' beginnen mit 'term-'

## Prämissen (nicht verhandelbar)

- **Cloud-first: nichts muss lokal laufen.** — Thin Clients als Terminals; GitHub (Pages, Codespaces, GHCR, Releases) trägt alles
- **Kein Python — nie.** — Ein Stack (.NET/F#), keine Toolchain-Fragmentierung; Typsicherheit durchgängig
- **Typsicherheit vor Flexibilität.** — Illegale SPOT-Zustände sollen nicht repräsentierbar sein — das Typsystem ist das Schema

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

## Spezifikationen

### Agent-Interface (`spec-agent-interface`, Aligned)
**Intent:** Prosa-Eingaben werden durch eine KI in validierte Modelländerungen übersetzt

- GIVEN eine Prosa-Beschreibung einer Modelländerung WHEN der Agent ausgeführt wird (Claude direkt oder via kopiertem Prompt) THEN entsteht ein prüfbarer Änderungsvorschlag (upsert/delete), der erst nach Bestätigung angewendet wird

### Chat-primaere Cockpit-Shell (`spec-cockpit-shell`, Aligned)
**Intent:** Das Cockpit ist chat-primaer: eine Omnibar als einzige Tuer, eine Menueleiste, die Rail mit Flaechen, der Faden und eine Statuszeile.

- GIVEN das Cockpit ist gegen das Selbstmodell geladen WHEN die Shell rendert THEN sind Omnibar, Menueleiste, Rail-Flaechen, Faden und die Statuszeile mit Knotenzahl da

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

### LLM-Kontextexport (`spec-export-context`, Aligned)
**Intent:** Der SPOT-Graph wird zur Vorlage, aus der ein Agent den Rest baut

- GIVEN ein gefüllter SPOT-Graph WHEN cdd export-context läuft THEN entsteht ein einzelnes Markdown-Bundle mit Ontologie, Prämissen, Entscheidungen, Specs und offenen Risiken
- GIVEN das exportierte Bundle WHEN es einem LLM als Kontext übergeben wird THEN kann es Implementierungsaufgaben ohne Rückfragen zur Domänensprache bearbeiten

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

- **Modell und Code driften auseinander (der MDA-Friedhof)** (Likelihood Medium, Impact Critical) — Mitigation: Konvergenz-Status je Knoten + Round-Trip (Code→Modell) auf der Roadmap
- **SPOT-Pflege wird teurer als der Code, den er erzeugt** (Likelihood Medium, Impact High) — Mitigation: Alles Ableitbare wird abgeleitet (Tests, Diagramme), nie handgepflegt
- **Spec-Vollständigkeits-Falle: die Spec wird so komplex wie Code** (Likelihood Medium, Impact High) — Mitigation: Specs bleiben auf Intent/Kriterien/Invarianten-Ebene; Agents füllen Lücken, Validierung fängt Drift

## Komponenten

- **Cdd.Cli** (`comp-cli`) → hängt ab von `comp-core`
- **Cdd.Core** (`comp-core`)
- **Cdd.Mcp** (`comp-mcp`) → hängt ab von `comp-core`
- **Cdd.Web** (`comp-web`) → hängt ab von `comp-core`

## Wissensquellen

- **Eric Evans — Domain-Driven Design** (book, ISBN 978-0321125217)
  - Ubiquitous Language ist die Brücke zwischen Fachseite und Code
  - Bounded Contexts begrenzen Modellgültigkeit
- **Martin Fowler — Blog** (blog, https://martinfowler.com)
  - Refactoring-Katalog
  - Evolutionäre Architektur
  - Spec-by-Example

## Tools (Agent-Capabilities)

- **GitHub Actions** — CI/CD, Releases, Pages, Container — der Automatisierungs-Arm
- **Mermaid** — Diagramm-Rendering (Graph, UML) aus dem SPOT — https://cdn.jsdelivr.net/npm/mermaid@11

## Offene Arbeit (nicht Aligned)

- `spec-agent-interface-test-1` (test, Pending)
- `spec-export-context-test-2` (test, Pending)
- `spec-gate-selbst-hart` (spec, Pending)
- `spec-mcp-server-test-1` (test, Pending)

