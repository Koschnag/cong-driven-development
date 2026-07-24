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
      term "term-eidos" "EIDOS" "Zielarchitektur für doctrine-getriebene, epistemisch typisierte und evidenzgesteuerte Softwareevolution; CDD ist ihr heutiger Kernel" [ "EIDOS Framework" ] [ RelatesTo(EntityId "term-spot") ]
      term "term-signal" "Signal" "Unverändertes Rohereignis aus Feedback, Runtime, Entwicklung, Betrieb, Simulation oder Analyse" [ "Raw Event" ] [ PartOf(EntityId "term-eidos") ]
      term "term-doctrine" "Doctrine" "Versionierte, maschinenlesbare Regeln für Dispatch, Rechte, Assurance, Eskalation, Promotion und Abbruch" [ "Operational Doctrine" ] [ PartOf(EntityId "term-eidos") ]
      term "term-mission-order" "Mission Order" "Typisierter Auftrag mit Lage, Intent, Scope, Einheit, Constraints, Obligations, Berichts- und Abbruchkriterien" [ "Einsatzauftrag" ] [ PartOf(EntityId "term-eidos") ]
      term "term-change-compiler" "Change Compiler" "Transformiert Intent, System-Twin, Policies und Evidenz in prüfbare Änderungskandidaten samt Obligations und Recovery" [ "Semantic Change Compiler" ] [ PartOf(EntityId "term-eidos") ]
      term "term-evidence-pack" "Evidence Pack" "Versions-, zeit- und umgebungsgebündelter Nachweis für einen Kandidaten und seine Assurance Obligations" [ "Evidence-Carrying Change" ] [ PartOf(EntityId "term-eidos") ]
      term "term-system-twin" "System Twin" "Zeitbezogene, provenienzbehaftete Projektion des bekannten Systems einschließlich Unsicherheit und Widerspruch" [ "Semantic System Twin" ] [ RelatesTo(EntityId "term-spot") ]

      // ── Prämissen ──────────────────────────────────────────────────────
      premise "premise-kein-python" "Kein Python — nie." "Ein Stack (.NET/F#), keine Toolchain-Fragmentierung; Typsicherheit durchgängig"
      premise "premise-cloud-first" "Cloud-first: nichts muss lokal laufen." "Thin Clients als Terminals; GitHub (Pages, Codespaces, GHCR, Releases) trägt alles"
      premise "premise-typsicherheit" "Typsicherheit vor Flexibilität." "Illegale SPOT-Zustände sollen nicht repräsentierbar sein — das Typsystem ist das Schema"
      premise "premise-evidence-before-promotion" "Evidence vor Promotion." "Ein Candidate wird nur befördert, wenn alle risikoadaptiven Obligations mit benannter, reproduzierbarer Evidenz erfüllt sind"
      premise "premise-unknown-remains-unknown" "Unknown bleibt unknown." "Fehlende Evidenz ist weder Zustimmung noch der Nachweis, dass ein Bereich nicht betroffen ist"
      premise "premise-chain-is-plan" "Eine Agentenkette ist ein Plan, nicht die Architektur." "Doctrine, Lage, Risiko und verfügbare Capabilities erzeugen pro Mission einen begrenzten Ausführungsplan"

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
      decision "adr-005-eidos-target" "EIDOS als Zielarchitektur über dem CDD-Kernel"
        "CDD besitzt SPOT und ein Konvergenz-Orakel, aber noch kein epistemisches Lagebild, Change Compilation, Mission Dispatch oder Outcome-Lernen"
        "CDD bleibt der überprüfbare Kernel; EIDOS wird als getrenntes, ehrlich als Pending markiertes Architektur- und Forschungsprogramm entwickelt"
        "Neue Capabilities werden zuerst im SPOT spezifiziert; Produktclaims unterscheiden implementierten Ist-Stand und Zielbild"

      // ── Risiken ────────────────────────────────────────────────────────
      risk "risk-mda-drift" "Modell und Code driften auseinander (der MDA-Friedhof)" Medium Critical
        "Konvergenz-Status je Knoten + Round-Trip (Code→Modell) auf der Roadmap"
      risk "risk-spec-vollstaendigkeit" "Spec-Vollständigkeits-Falle: die Spec wird so komplex wie Code" Medium High
        "Specs bleiben auf Intent/Kriterien/Invarianten-Ebene; Agents füllen Lücken, Validierung fängt Drift"
      risk "risk-pflegekosten" "SPOT-Pflege wird teurer als der Code, den er erzeugt" Medium High
        "Alles Ableitbare wird abgeleitet (Tests, Diagramme), nie handgepflegt"
      risk "risk-korrelierte-orakel" "Generator und Validator teilen denselben systematischen Fehler und erzeugen Scheinevidenz" High Critical
        "Kritische Orakel dekorrelieren, unabhängige Harnesses verwenden und Provenienz sowie Validatorversion im Evidence Pack binden"
      risk "risk-autonomie-blast-radius" "Autonome Änderungen überschreiten den freigegebenen Scope oder erreichen produktive Systeme" Medium Critical
        "Capability-Allowlists, kleinste Rechte, ZT2 als erstes Ziel, keine Produktiv-Credentials, harte Budgets und Fail-Closed-Abbruch"
      risk "risk-outcome-kausalitaet" "Das Evolutionary Memory speichert Korrelation als Ursache und verstärkt eine falsche Strategie" High High
        "Intervention, Störfaktoren und Konfidenz getrennt speichern; Strategy-Änderungen nur nach reproduzierbaren Outcome-Vergleichen"

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
      { Id = EntityId "spec-mcp-server"; Convergence = Aligned
        Payload = SpecNode
          { Title = "MCP-Server"
            Intent = "Jeder MCP-Client (Claude Code, Claude Desktop, …) kann den SPOT direkt lesen, validieren und mutieren"
            Criteria =
              [ { Given = "ein verbundener MCP-Client"
                  When = "spot_upsert oder spot_delete aufgerufen wird"
                  Then = "wird die Änderung gespeichert und die Validierung (inkl. Invarianten) zurückgemeldet" } ] } }
      { Id = EntityId "spec-fehlerliste"; Convergence = Aligned
        Payload = SpecNode
          { Title = "Fehlerliste & Widerspruchs-Erkennung"
            Intent = "Inkonsistenzen, Widersprüche und Regelverstöße sind eine klickbare Liste wie in Visual Studio"
            Criteria =
              [ { Given = "eine zyklische IsA/PartOf-Begriffshierarchie"
                  When = "validiert wird"
                  Then = "erscheint ein Widerspruchs-Fehler in der Fehlerliste; Klick springt zum Knoten" }
                { Given = "zwei Begriffe mit gleichem Namen"
                  When = "validiert wird"
                  Then = "wird Mehrdeutigkeit als Warnung gemeldet" } ] } }

      { Id = EntityId "spec-sync-tests"; Convergence = Aligned
        Payload = SpecNode
          { Title = "Test-Konvergenz messen"
            Intent = "Abgeleitete Test-Knoten werden gegen echte automatisierte Tests gemessen statt behauptet"
            Criteria =
              [ { Given = "ein Test-Knoten und ein Test mit Trait(\"spot\", id) oder [spot: id]-Marker"
                  When = "cdd sync-tests läuft"
                  Then = "wird der Knoten Aligned; ohne Marker bleibt er Pending, Abweichung bricht CI" } ] } }
      { Id = EntityId "spec-sync-docs"; Convergence = Aligned
        Payload = SpecNode
          { Title = "Doku-Konvergenz"
            Intent = "Der README-Status wird aus dem Selbstmodell generiert — Doku-Drift ist ein CI-Fehler"
            Criteria =
              [ { Given = "ein veralteter README-Status"
                  When = "cdd sync-docs --check in der CI läuft"
                  Then = "schlägt der Build fehl, bis sync-docs den Status neu generiert hat" }
                { Given = "Prämissen, Entscheidungen und Invarianten im Modell"
                  When = "cdd sync-docs läuft"
                  Then = "wird docs/decisions.md vollständig daraus generiert" } ] } }

      { Id = EntityId "spec-derive-code"; Convergence = Aligned
        Payload = SpecNode
          { Title = "Modell → Code (derive-code)"
            Intent = "Aus unabgedeckten Test-Knoten entstehen implementierbare Test-Skelette mit fertigem Mess-Marker"
            Criteria =
              [ { Given = "ein Test-Knoten ohne Marker im Test-Code"
                  When = "cdd derive-code läuft"
                  Then = "entsteht ein xUnit-Skelett mit Trait(spot, id) und den Kriterien als Vorgabe; abgedeckte Knoten werden übersprungen" } ] } }

      // ── EIDOS-Zielbild: bewusst Pending, bis Code und Orakel existieren ─
      { Id = EntityId "spec-eidos-epistemic-claims"; Convergence = Pending
        Payload = SpecNode
          { Title = "Epistemisch typisierte Claims"
            Intent = "Beobachtung, Aussage, Ableitung, Vorschlag, Ratifikation und Verifikation bleiben unterscheidbar und provenienzbehaftet"
            Criteria =
              [ { Given = "ein Rohsignal und eine maschinelle Interpretation"
                  When = "beide in den System-Twin projiziert werden"
                  Then = "bleiben Originalsignal, Claim, Provenienz, Zeitpunkt, Scope und epistemischer Status getrennt erhalten" }
                { Given = "widersprüchliche Claims oder fehlende Evidenz"
                  When = "ein Lagebild erzeugt wird"
                  Then = "werden Contested und Unknown explizit dargestellt statt zu einer scheinbar sicheren Aussage geglättet" } ] } }
      { Id = EntityId "spec-eidos-mission-order"; Convergence = Pending
        Payload = SpecNode
          { Title = "Doctrine und Mission Orders"
            Intent = "Jede Agentenausführung erhält einen typisierten Auftrag mit Rechten, Budget, Obligations, Reporting und Abbruchbedingungen"
            Criteria =
              [ { Given = "ein klassifiziertes Change Intent und eine versionierte Doctrine"
                  When = "eine Mission disponiert wird"
                  Then = "entsteht eine Mission Order mit Lage, Ziel, Scope, Einheit, Constraints, Erfolg und Abbruch" }
                { Given = "eine Mission mit überschrittenem Budget, fehlender Capability oder verletzter Policy"
                  When = "der Control Plane die Verletzung meldet"
                  Then = "wird fail closed abgebrochen oder an eine zuständige Authority eskaliert" } ] } }
      { Id = EntityId "spec-eidos-change-compiler"; Convergence = Pending
        Payload = SpecNode
          { Title = "Semantic Change Compiler"
            Intent = "Intent, Twin, Policies und Evidenz erzeugen vergleichbare Candidates statt einer unprüfbaren Einzelantwort"
            Criteria =
              [ { Given = "ein Change Intent, ein versionierter System-Twin, Policies und aktuelle Evidenz"
                  When = "der Change Compiler läuft"
                  Then = "entstehen deterministische Candidate-Metadaten mit Semantic Delta, Artefakten, Obligations, Deployment und Recovery" }
                { Given = "mehrere zulässige Candidates"
                  When = "Impact und Risiken bewertet werden"
                  Then = "bleiben Alternativen, Annahmen und verworfene Optionen im Event Ledger nachvollziehbar" } ] } }
      { Id = EntityId "spec-eidos-evidence-pack"; Convergence = Pending
        Payload = SpecNode
          { Title = "Evidence Packs und Promotion"
            Intent = "Promotion ist eine reproduzierbare Policy-Entscheidung über Evidence statt eine Selbstbestätigung des Generators"
            Criteria =
              [ { Given = "ein Candidate mit risikoadaptiven Assurance Obligations"
                  When = "Generator-unabhängige Gates laufen"
                  Then = "bindet das Evidence Pack Ergebnis, Tool- und Policyversion, Umgebung, Zeitpunkt und Artefakt-Hash" }
                { Given = "eine fehlende, veraltete oder rote Obligation"
                  When = "Promotion bewertet wird"
                  Then = "wird der Candidate nicht befördert und der konkrete Nachweisgrund bleibt auditierbar" } ] } }
      { Id = EntityId "spec-eidos-zt2-opslab"; Convergence = Pending
        Payload = SpecNode
          { Title = "Zero-Touch-Sandbox im OpsLab"
            Intent = "Ein klar definierter Change wird autonom bis zu einer isolierten, vollständig replaybaren Sandbox durchgeführt"
            Criteria =
              [ { Given = "eine synthetische versionierte Report-/Submission-Anwendung und eine Mission Order"
                  When = "der EIDOS-Lauf auf ZT2 startet"
                  Then = "erzeugt, prüft und deployt er den Candidate ausschließlich in der Sandbox ohne produktive Credentials" }
                { Given = "ein Gate-Fehler, Timeout oder verletztes Abbruchkriterium"
                  When = "die Mission endet"
                  Then = "bleibt das Zielsystem unverändert und Event Ledger, Evidence und Recovery-Ergebnis sind replaybar" } ] } }

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
      { Id = EntityId "kb-intent-formalization"; Convergence = Aligned
        Payload = KnowledgeNode
          { Title = "Intent Formalization — Grand Challenge"; Source = "https://arxiv.org/abs/2603.17150"
            MediaType = "paper"
            Takeaways = [ "Die Lücke zwischen natürlichem Intent und prüfbarem Verhalten ist der zentrale Engpass"
                          "Spezifikationsqualität braucht eigene Metriken und Interaktion" ] } }
      { Id = EntityId "kb-intent-debt"; Convergence = Aligned
        Payload = KnowledgeNode
          { Title = "Cognitive and Intent Debt"; Source = "https://arxiv.org/abs/2603.22106"
            MediaType = "paper"
            Takeaways = [ "Fehlendes externalisiertes Rationale erzeugt Intent Debt"
                          "Softwaregesundheit umfasst Code, gemeinsames Verständnis und explizites Intent-Wissen" ] } }
      { Id = EntityId "kb-veriact"; Convergence = Aligned
        Payload = KnowledgeNode
          { Title = "VeriAct — Beyond Verifiability"; Source = "https://arxiv.org/abs/2604.00280"
            MediaType = "paper"
            Takeaways = [ "Verifier-Akzeptanz allein garantiert keine korrekte oder vollständige Spezifikation"
                          "Ein unabhängiges Spec-Harness macht Über- und Unterbeschränkung messbar" ] } }
      { Id = EntityId "kb-llm-modulo"; Convergence = Aligned
        Payload = KnowledgeNode
          { Title = "LLM-Modulo Frameworks"; Source = "https://arxiv.org/abs/2402.01817"
            MediaType = "paper"
            Takeaways = [ "LLMs und externe modellbasierte Verifizierer sollen bidirektional gekoppelt werden"
                          "Der externe Verifizierer bleibt Quelle der belastbaren Garantie" ] } }
      { Id = EntityId "kb-darwin-goedel-machine"; Convergence = Aligned
        Payload = KnowledgeNode
          { Title = "Darwin Gödel Machine"; Source = "https://arxiv.org/abs/2505.22954"
            MediaType = "paper"
            Takeaways = [ "Offene Evolution kann Agentenvarianten empirisch über Benchmarks selektieren"
                          "Sandboxing und menschliche Aufsicht bleiben Teil der berichteten Sicherheitsmaßnahmen" ] } }

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
