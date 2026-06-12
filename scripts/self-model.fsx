// CDD modelliert sich selbst: generiert das .spot/-Selbstmodell des Projekts.
// Ausführen (nach `dotnet build -c Release`):
//   dotnet fsi scripts/self-model.fsx
// Danach: dotnet run --project src/Cdd.Cli -- derive-tests --write && cdd validate

#r "nuget: FSharp.SystemTextJson, 1.4.36"
#r "../src/Cdd.Core/bin/Release/net9.0/Cdd.Core.dll"

open Cdd.Core
open Cdd.Core.Spot

let term id name definition synonyms relations =
    { Id = EntityId id; Convergence = Aligned
      Payload = TermNode { Name = name; Definition = definition
                           Synonyms = synonyms; Relations = relations } }

let premise id statement rationale =
    { Id = EntityId id; Convergence = Aligned
      Payload = PremiseNode { Statement = statement; Rationale = rationale } }

let decision id title context choice consequences =
    { Id = EntityId id; Convergence = Aligned
      Payload = DecisionNode { Title = title; Context = context; Choice = choice
                               Consequences = consequences; Supersedes = None } }

let risk id statement likelihood impact mitigation =
    { Id = EntityId id; Convergence = Aligned
      Payload = RiskNode { Statement = statement; Likelihood = likelihood
                           Impact = impact; Mitigation = Some mitigation } }

let entries =
    [
      // ── Ontologie: die ubiquitäre Sprache von CDD selbst ──────────────
      term "term-spot" "SPOT" "Single Point of Truth — der eine Graph, in dem Modell, Spec, Tests, Risiken, Wissen und Infrastruktur leben" [ "Single Point of Truth" ] []
      term "term-knoten" "Knoten" "Eintrag im SPOT-Graphen mit Identität, Nutzlast und Konvergenz-Status" [ "Entry"; "Node" ] [ PartOf(EntityId "term-spot") ]
      term "term-spec" "Spec" "Maschinenlesbarer Vertrag: Intent plus Akzeptanzkriterien in Given/When/Then" [ "Spezifikation" ] [ IsA(EntityId "term-knoten") ]
      term "term-konvergenz" "Konvergenz" "Grad der Übereinstimmung zwischen Modell-Knoten und Implementierung (Pending/Aligned/Diverged/Orphaned)" [] [ RelatesTo(EntityId "term-knoten") ]
      term "term-drift" "Drift" "Auseinanderlaufen von Modell und Code — das, was klassische MDA scheitern ließ" [] [ RelatesTo(EntityId "term-konvergenz") ]
      term "term-ontologie" "Ontologie" "Begriffsnetz der Domäne: Begriffe mit Definition und typisierten Beziehungen" [ "Begriffsmodell" ] [ PartOf(EntityId "term-spot") ]
      term "term-ubiquitaere-sprache" "Ubiquitäre Sprache" "Gemeinsames Vokabular von Fachseite, Technik und AI-Agents — definiert in der Ontologie" [ "Ubiquitous Language" ] [ RelatesTo(EntityId "term-ontologie") ]
      term "term-cockpit" "Cockpit" "Web-GUI, die den SPOT multidimensional zeigt: Graph, UML, Validierung, Drift" [ "IDE" ] [ RelatesTo(EntityId "term-spot") ]
      term "term-agent" "Agent" "LLM-gestützter Worker, der aus dem SPOT Implementierung, Tests und Doku ableitet" [ "AI-Agent" ] [ RelatesTo(EntityId "term-spot") ]

      // ── Prämissen ──────────────────────────────────────────────────────
      premise "premise-kein-python" "Kein Python — nie." "Ein Stack (.NET/F#), keine Toolchain-Fragmentierung; Typsicherheit durchgängig"
      premise "premise-cloud-first" "Cloud-first: nichts muss lokal laufen." "Thin Clients als Terminals; GitHub (Pages, Codespaces, GHCR, Releases) trägt alles"
      premise "premise-typsicherheit" "Typsicherheit vor Flexibilität." "Illegale SPOT-Zustände sollen nicht repräsentierbar sein — das Typsystem ist das Schema"

      // ── Entscheidungen (ADRs) ─────────────────────────────────────────
      decision "adr-001-fsharp" "F# für die Domain"
        "Das SPOT-Modell braucht Summen-Typen, Pattern-Matching und Unveränderlichkeit"
        "F# mit Discriminated Unions als Modellsprache; C# nur für IO-Adapter"
        "Kleinere Community, dafür beweisbar korrektere Modelle und Lean-4-Anschlussfähigkeit"
      decision "adr-002-json-store" "Ein JSON-File pro Knoten"
        "Der SPOT muss git-diffbar, mergebar und ohne Server nutzbar sein"
        "Persistenz als .spot/<id>.json via FSharp.SystemTextJson"
        "Kein Query-Layer; bei Wachstum später SQLite/Index möglich, Format bleibt Austauschformat"
      decision "adr-003-github-only" "GitHub-native Infrastruktur"
        "Eigene Domains/Server erzeugen Pflegekosten und private Abhängigkeiten"
        "Pages für die Demo, Actions für CI/CD, GHCR für Container, Releases für Binaries"
        "Demo-Modus braucht localStorage statt Backend; volle Version via Codespaces/Container"
      decision "adr-004-mpl2" "Lizenz MPL-2.0"
        "Offenheit gewünscht, aber Datei-Copyleft statt viralem Projekt-Copyleft"
        "MPL-2.0"
        "Kommerzielle Nutzung möglich, Änderungen an CDD-Dateien bleiben offen"

      // ── Risiken ────────────────────────────────────────────────────────
      risk "risk-mda-drift" "Modell und Code driften auseinander (der MDA-Friedhof)" Medium Critical
        "Konvergenz-Status je Knoten + Round-Trip (Code→Modell) auf der Roadmap"
      risk "risk-spec-vollstaendigkeit" "Spec-Vollständigkeits-Falle: die Spec wird so komplex wie Code" Medium High
        "Specs bleiben auf Intent/Kriterien/Invarianten-Ebene; Agents füllen Lücken, Validierung fängt Drift"
      risk "risk-pflegekosten" "SPOT-Pflege wird teurer als der Code, den er erzeugt" Medium High
        "Alles Ableitbare wird abgeleitet (Tests, Diagramme), nie handgepflegt"

      // ── Komponenten ───────────────────────────────────────────────────
      { Id = EntityId "comp-core"; Convergence = Aligned
        Payload = ComponentNode { Name = "Cdd.Core"; DependsOn = [] } }
      { Id = EntityId "comp-cli"; Convergence = Aligned
        Payload = ComponentNode { Name = "Cdd.Cli"; DependsOn = [ EntityId "comp-core" ] } }
      { Id = EntityId "comp-web"; Convergence = Aligned
        Payload = ComponentNode { Name = "Cdd.Web"; DependsOn = [ EntityId "comp-core" ] } }
      { Id = EntityId "comp-mcp"; Convergence = Aligned
        Payload = ComponentNode { Name = "Cdd.Mcp"; DependsOn = [ EntityId "comp-core" ] } }

      // ── Specs: was CDD kann (Aligned) und können soll (Pending) ───────
      { Id = EntityId "spec-validate"; Convergence = Aligned
        Payload = SpecNode
          { Title = "Modell-Validierung"
            Intent = "Der SPOT-Graph ist jederzeit strukturell konsistent"
            Criteria =
              [ { Given = "ein Knoten mit Referenz auf eine nicht existierende Id"
                  When = "cdd validate läuft"
                  Then = "wird ein Fehler mit Knoten-Id und Ziel gemeldet" }
                { Given = "Komponenten mit zyklischen Abhängigkeiten"
                  When = "cdd validate läuft"
                  Then = "werden alle Zyklus-Teilnehmer als Fehler markiert" } ] } }
      { Id = EntityId "spec-derive-tests"; Convergence = Aligned
        Payload = SpecNode
          { Title = "Spec→Test-Ableitung"
            Intent = "Tests sind Derivat der Spezifikation, nicht handgeschrieben"
            Criteria =
              [ { Given = "eine Spec mit n Akzeptanzkriterien"
                  When = "cdd derive-tests --write läuft"
                  Then = "existiert genau ein Test-Knoten pro Kriterium" }
                { Given = "bereits abgeleitete Tests"
                  When = "derive-tests erneut läuft"
                  Then = "entstehen keine Duplikate (Idempotenz)" } ] } }
      { Id = EntityId "spec-cockpit-uml"; Convergence = Aligned
        Payload = SpecNode
          { Title = "Multidimensionale Sicht"
            Intent = "Ein Modell, mehrere Projektionen — Graph und UML aus demselben SPOT"
            Criteria =
              [ { Given = "Begriffe mit IsA/PartOf/RelatesTo-Beziehungen"
                  When = "der UML-Tab geöffnet wird"
                  Then = "erscheint ein Klassendiagramm mit Generalisierung, Komposition und Assoziation" } ] } }
      { Id = EntityId "spec-export-context"; Convergence = Aligned
        Payload = SpecNode
          { Title = "LLM-Kontextexport"
            Intent = "Der SPOT-Graph wird zur Vorlage, aus der ein Agent den Rest baut"
            Criteria =
              [ { Given = "ein gefüllter SPOT-Graph"
                  When = "cdd export-context läuft"
                  Then = "entsteht ein einzelnes Markdown-Bundle mit Ontologie, Prämissen, Entscheidungen, Specs und offenen Risiken" }
                { Given = "das exportierte Bundle"
                  When = "es einem LLM als Kontext übergeben wird"
                  Then = "kann es Implementierungsaufgaben ohne Rückfragen zur Domänensprache bearbeiten" } ] } }

      { Id = EntityId "spec-agent-interface"; Convergence = Aligned
        Payload = SpecNode
          { Title = "Agent-Interface"
            Intent = "Prosa-Eingaben werden durch eine KI in validierte Modelländerungen übersetzt"
            Criteria =
              [ { Given = "eine Prosa-Beschreibung einer Modelländerung"
                  When = "der Agent ausgeführt wird (Claude direkt oder via kopiertem Prompt)"
                  Then = "entsteht ein prüfbarer Änderungsvorschlag (upsert/delete), der erst nach Bestätigung angewendet wird" } ] } }

      { Id = EntityId "spec-uml-cube"; Convergence = Aligned
        Payload = SpecNode
          { Title = "Modell-Navigation als Würfel"
            Intent = "Der SPOT-Graph ist wie ein OLAP-Cube navigierbar: Slice, Dice, Drill-down, Verlinkungen"
            Criteria =
              [ { Given = "ein gewählter Knoten"
                  When = "der Inspektor geöffnet ist"
                  Then = "sind ein- und ausgehende Verlinkungen klickbar und die Historie per Zurück/Vor navigierbar" }
                { Given = "aktive Filter (Knotenart/Konvergenz) oder Fokus-Modus"
                  When = "Graph oder UML gerendert werden"
                  Then = "zeigen sie nur die gefilterte Teilmenge bzw. die N-Hop-Nachbarschaft" } ] } }

      { Id = EntityId "spec-form-editor"; Convergence = Aligned
        Payload = SpecNode
          { Title = "Formular-Editor"
            Intent = "Knoten werden über Eingabefelder gepflegt — JSON ist Experten-Option, nicht Voraussetzung"
            Criteria =
              [ { Given = "eine Knotenart aus der Toolbox"
                  When = "ein neuer Knoten angelegt wird"
                  Then = "öffnet sich ein Formular mit passenden Feldern und Dropdowns für Referenzen" } ] } }

      { Id = EntityId "spec-vs2015-ea-layout"; Convergence = Aligned
        Payload = SpecNode
          { Title = "VS2015-Design mit EA-Anatomie"
            Intent = "Das Cockpit folgt Visual Studio 2015 (Chrome) und Enterprise Architect (Fensteranordnung)"
            Criteria =
              [ { Given = "das geöffnete Cockpit"
                  When = "ein Knoten gewählt ist"
                  Then = "zeigen Diagramme als zentrales Dokument, Eigenschaften rechts und die blaue Statusleiste Auswahl, Zähler und Validierungsstand" } ] } }

      // ── Invarianten: Governance by Invariance ────────────────────────
      { Id = EntityId "inv-specs-getestet"; Convergence = Aligned
        Payload = InvariantNode
          { Description = "Jede Spec hat mindestens einen Test"
            Rule = SpecsNeedTests } }
      { Id = EntityId "inv-kritische-risiken"; Convergence = Aligned
        Payload = InvariantNode
          { Description = "Kritische Risiken brauchen eine Mitigation"
            Rule = CriticalRisksNeedMitigation } }
      { Id = EntityId "inv-begriffe-definiert"; Convergence = Aligned
        Payload = InvariantNode
          { Description = "Jeder Begriff der ubiquitären Sprache ist definiert"
            Rule = TermsNeedDefinition } }
      { Id = EntityId "inv-term-praefix"; Convergence = Aligned
        Payload = InvariantNode
          { Description = "Begriffe heißen term-*"
            Rule = IdPrefix("term", "term-") } }

      { Id = EntityId "spec-governance"; Convergence = Aligned
        Payload = SpecNode
          { Title = "Governance by Invariance"
            Intent = "Regeln sind Modell-Knoten und werden bei jeder Validierung (lokal + CI) erzwungen"
            Criteria =
              [ { Given = "eine Invariante im SPOT"
                  When = "cdd validate läuft"
                  Then = "werden Verstöße als Fehler am verletzenden Knoten gemeldet" } ] } }
      { Id = EntityId "spec-roundtrip-sync"; Convergence = Aligned
        Payload = SpecNode
          { Title = "Round-Trip: Code → Modell"
            Intent = "Komponenten-Konvergenz wird aus den echten Projekt-Referenzen abgeleitet, nicht behauptet"
            Criteria =
              [ { Given = "src/*.fsproj und Component-Knoten"
                  When = "cdd sync-code läuft"
                  Then = "wird Aligned/Diverged/Orphaned/Pending je Komponente bestimmt und bei Drift Exit 1 geliefert" } ] } }
      { Id = EntityId "spec-uml-dnd"; Convergence = Aligned
        Payload = SpecNode
          { Title = "UML-Editor mit Drag and Drop"
            Intent = "Beziehungen entstehen durch Ziehen zwischen Diagramm-Knoten, Doppelklick öffnet das Formular"
            Criteria =
              [ { Given = "zwei Begriffe im UML-Diagramm"
                  When = "von einem zum anderen gezogen wird"
                  Then = "wird die gewählte Beziehung (IsA/PartOf/RelatesTo) nach Bestätigung gespeichert" } ] } }

      { Id = EntityId "spec-mcp-server"; Convergence = Aligned
        Payload = SpecNode
          { Title = "MCP-Server"
            Intent = "Jeder MCP-Client (Claude Code, Claude Desktop, …) kann den SPOT direkt lesen, validieren und mutieren"
            Criteria =
              [ { Given = "ein verbundener MCP-Client"
                  When = "spot_upsert oder spot_delete aufgerufen wird"
                  Then = "wird die Änderung gespeichert und die Validierung (inkl. Invarianten) zurückgemeldet" } ] } }
      { Id = EntityId "spec-interactive-graph"; Convergence = Aligned
        Payload = SpecNode
          { Title = "Interaktiver Graph"
            Intent = "Diagramm-Elemente sind frei positionierbar wie in Enterprise Architect; Layouts bleiben erhalten"
            Criteria =
              [ { Given = "ein verschobener Knoten im Graph"
                  When = "die Seite neu geladen wird"
                  Then = "behält der Knoten seine Position; Rechtsklick startet eine neue Beziehung" } ] } }

      // ── Knowledge: wovon die Agents lernen sollen ─────────────────────
      { Id = EntityId "kb-fowler-blog"; Convergence = Aligned
        Payload = KnowledgeNode
          { Title = "Martin Fowler — Blog"; Source = "https://martinfowler.com"
            MediaType = "blog"
            Takeaways = [ "Refactoring-Katalog"; "Evolutionäre Architektur"; "Spec-by-Example" ] } }
      { Id = EntityId "kb-evans-ddd"; Convergence = Aligned
        Payload = KnowledgeNode
          { Title = "Eric Evans — Domain-Driven Design"; Source = "ISBN 978-0321125217"
            MediaType = "book"
            Takeaways = [ "Ubiquitous Language ist die Brücke zwischen Fachseite und Code"
                          "Bounded Contexts begrenzen Modellgültigkeit" ] } }

      // ── Tools: Capabilities für Agents ────────────────────────────────
      { Id = EntityId "tool-mermaid"; Convergence = Aligned
        Payload = ToolNode { Name = "Mermaid"; Purpose = "Diagramm-Rendering (Graph, UML) aus dem SPOT"
                             Endpoint = Some "https://cdn.jsdelivr.net/npm/mermaid@11" } }
      { Id = EntityId "tool-github-actions"; Convergence = Aligned
        Payload = ToolNode { Name = "GitHub Actions"; Purpose = "CI/CD, Releases, Pages, Container — der Automatisierungs-Arm"
                             Endpoint = None } }
    ]

let root = __SOURCE_DIRECTORY__ + "/.."
entries |> List.iter (Store.save root)
printfn "%d Knoten geschrieben nach %s/.spot" (List.length entries) root
