// CDD modelliert sich selbst: seedet und aktualisiert die Kernel-Knoten des
// .spot/-Selbstmodells. Forschungs- und Erweiterungsknoten werden nicht gelöscht.
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
      term "term-eidos" "EIDOS" "Doctrine-getriebene, epistemisch typisierte und evidenzgesteuerte Softwareevolution; v0 implementiert den falsifizierbaren Kernel bis zur isolierten ZT2-Sandbox" [ "EIDOS Framework" ] [ RelatesTo(EntityId "term-spot") ]
      term "term-signal" "Signal" "Unverändertes Rohereignis aus Feedback, Runtime, Entwicklung, Betrieb, Simulation oder Analyse" [ "Raw Event" ] [ PartOf(EntityId "term-eidos") ]
      term "term-doctrine" "Doctrine" "Versionierte, maschinenlesbare Regeln für Dispatch, Rechte, Assurance, Eskalation, Promotion und Abbruch" [ "Operational Doctrine" ] [ PartOf(EntityId "term-eidos") ]
      term "term-mission-order" "Mission Order" "Typisierter Auftrag mit Lage, Intent, Scope, Einheit, Constraints, Obligations, Berichts- und Abbruchkriterien" [ "Einsatzauftrag" ] [ PartOf(EntityId "term-eidos") ]
      term "term-change-compiler" "Change Compiler" "Transformiert Intent, System-Twin, Policies und Evidenz in prüfbare Änderungskandidaten samt Obligations und Recovery" [ "Semantic Change Compiler" ] [ PartOf(EntityId "term-eidos") ]
      term "term-evidence-pack" "Evidence Pack" "Versions-, zeit- und umgebungsgebündelter Nachweis für einen Kandidaten und seine Assurance Obligations" [ "Evidence-Carrying Change" ] [ PartOf(EntityId "term-eidos") ]
      term "term-system-twin" "System Twin" "Zeitbezogene, provenienzbehaftete Projektion des bekannten Systems einschließlich Unsicherheit und Widerspruch" [ "Semantic System Twin" ] [ RelatesTo(EntityId "term-spot") ]
      term "term-research-studio" "Research Studio" "Read-only Briefing-Projektion des öffentlichen SPOT für Claims, Evidenz, Lücken, Grenzen, Teilprojekte, Medien und kontrolliertes Feedback" [ "Research Cockpit"; "Forschungsbriefing" ] [ RelatesTo(EntityId "term-spot") ]
      term "term-control-plane" "Semantic Control Plane" "Anbieterunabhängige CDD-Schicht, die Intent, Systemzustand, Policies, Nachweise, Promotion und Evolution typisiert steuert, während ersetzbare Adapter die Arbeit ausführen" [ "CDD Control Plane"; "Steuerungsebene" ] [ RelatesTo(EntityId "term-spot"); RelatesTo(EntityId "term-doctrine") ]
      term "term-assurance-portfolio" "Assurance-Portfolio" "Risikoadaptive Kombination unabhängiger Typ-, Test-, Modell-, Beweis-, Policy-, Provenienz-, Runtime- und menschlicher Nachweise für eine Mission" [ "Assurance Stack"; "Nachweisportfolio" ] [ RelatesTo(EntityId "term-evidence-pack"); RelatesTo(EntityId "term-promotion-gate") ]
      term "term-evidence-fitness" "Evidence Fitness" "Grad, zu dem ein Nachweis dieselbe Behauptung, Last, Systemgrenze, Umgebung und Fehlermöglichkeit prüft, für die er eine Promotion begründen soll" [ "Representative Evidence"; "Nachweispassung" ] [ RelatesTo(EntityId "term-evidence-pack"); RelatesTo(EntityId "term-assurance-portfolio") ]
      term "term-autopilot-run" "Autopilot Run" "Persistente, replaybare Ausführung einer Mission als Folge begrenzter Work Slices, Agenten-Turns, Gates, Reviews und Checkpoints" [ "Agentic SDLC Run"; "Durable Run" ] [ PartOf(EntityId "term-control-plane"); RelatesTo(EntityId "term-mission-order") ]
      term "term-work-slice" "Work Slice" "Kleinste einzeln prüf- und checkpointbare Änderungseinheit mit Scope, Akzeptanzkriterien und benötigten Gates" [ "Implementation Slice"; "Task Slice" ] [ PartOf(EntityId "term-autopilot-run") ]

      // ── Prämissen ──────────────────────────────────────────────────────
      premise "premise-kein-python" "Python-freier Vertrauenskern, polyglotte Adapter." "CDD-Domäne, Promotion und Persistenz bleiben .NET/F#; austauschbare externe Tooladapter dürfen ihre native Sprache nutzen, ohne zur Kernel- oder Runtime-Abhängigkeit zu werden"
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
      decision "adr-007-public-research-studio" "Research Studio ist eine read-only SPOT-Projektion"
        "Ein visuelles Forschungsportal kann schnell zu einer zweiten Wahrheit oder einem unkontrollierten Agenten-Frontend werden"
        "Dynamische Forschungsobjekte kommen aus dem öffentlichen Snapshot; Feedback erzeugt nur einen vom Menschen zu prüfenden Issue-Entwurf"
        "Storytelling bleibt möglich, Status und Evidenz versioniert; Medien brauchen bei Modelländerungen erneute Prüfung"
      decision "adr-008-open-semantic-control-plane" "CDD als offener semantischer Control Plane statt neuer Alles-Engine"
        "Editoren, Diagrammwerkzeuge, Agent-Harnesses, Workflow-Engines, Forges und Observability-Systeme decken einzelne SDLC-Schichten ab und müssen austauschbar bleiben"
        "CDD baut den typisierten semantischen Kern, Doctrine, Evidence-Promotion und Projektionen; Ausführung, Editoren, Diagramme, Telemetrie, Policy und Artefaktspeicher werden über offene Standards und Ports adaptiert"
        "SPOT bleibt Domänenwahrheit; Theia, GLSP, LSP, MCP, OSLC, CDEvents, OTLP, OCI/in-toto und Workflow-Engines können unabhängig ersetzt oder schrittweise eingeführt werden"
      decision "adr-009-deterministic-autopilot-controller" "Deterministischer Controller über austauschbaren Agent-Harnesses"
        "Langlaufende Coding-Agenten können vorzeitig enden, ihren eigenen Erfolg überschätzen oder bei großen Aufträgen Kontext und Fortschritt verlieren"
        "CDD hält den langlebigen Run-Zustand, wählt die nächste typisierte Aktion deterministisch und akzeptiert Agentenausgaben nur als Beobachtung; Provider-Harnesses führen die Aktionen aus"
        "Agenten bleiben austauschbar und dürfen nicht selbst promoten; CDD benötigt dafür explizite Slice-, Recovery-, Gate-, Review- und Checkpoint-Protokolle"
      decision "adr-010-representative-evidence-fitness" "Evidence Fitness ist Teil der Promotion Policy"
        "Builds, Unit-Tests und dokumentierte Budgets können grün sein, obwohl die behauptete Produkteigenschaft unter repräsentativer Last, Zielhardware oder realer Systemgrenze scheitert"
        "Jede Assurance Obligation benennt Claim, Systemgrenze, Szenario, Last, Umgebung, Metrik und zulässige Proxys; Promotion lehnt Evidence ab, deren Fitness die Obligation nicht erreicht"
        "CDD muss fehlende repräsentative Evidence als unknown erhalten, Evidence-Fitness und Abweichungen berichten und darf Proxy-Erfolg nicht zur Outcome-Aussage hochstufen"

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
      risk "risk-evaluator-drift" "Ein lernendes oder vom Generator abhängiges Orakel driftet und belohnt Scheinerfolg" High Critical
        "Validatorversionen binden, Generator/Validator trennen, unveränderliche Ankerfälle und externe Audits verwenden"
      risk "risk-public-runtime-exposure" "Eine Showcase-Auslieferung legt Schreib-, Memory- oder Runtime-Fähigkeiten unbeabsichtigt offen" Medium Critical
        "App-seitige fail-closed Capability-Grenze, getrennte Opt-ins, generische Fehler, Security-Header und Browser-/Unit-Tests"
      risk "risk-agent-premature-stop" "Ein Worker beendet einen Turn ohne belastbaren Abschluss, während die Mission fälschlich als erledigt erscheint" High High
        "Terminalmarker als Protokollsignal statt Erfolg; begrenztes Resume derselben Session, frischer Recovery-Start mit Checkpoint und danach fail-closed Eskalation"
      risk "risk-proxy-evidence" "Leicht verfügbare Proxy-Evidence ersetzt unbemerkt die repräsentative Prüfung des eigentlichen Claims" High Critical
        "Evidence Obligations binden Claim, Boundary, Szenario, Last, Umgebung und Metrik; Promotion vergleicht die geforderte mit der beobachteten Evidence Fitness und lässt Lücken unknown"

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

      { Id = EntityId "spec-public-runtime-boundary"; Convergence = Aligned
        Payload = SpecNode
          { Title = "Fail-closed public runtime boundary"
            Intent = "Eine öffentliche CDD-Auslieferung darf ohne Betreiberfreigabe weder mutieren noch Memory- oder Runtime-Daten lesen"
            Criteria =
              [ { Given = "keine Capability-Umgebungsvariable gesetzt ist"
                  When = "ein öffentlicher oder privilegierter Pfad klassifiziert wird"
                  Then = "sind nur read-only SPOT- und statische Projektionen erlaubt" }
                { Given = "Memory geschrieben werden soll"
                  When = "nur eine der Freigaben Memory oder Mutation gesetzt ist"
                  Then = "bleibt die Operation gesperrt" }
                { Given = "lokale Workspace-Zustände beobachtet werden sollen"
                  When = "keine explizite Workspace-Capability gesetzt ist"
                  Then = "bleibt die Live-Projektion unabhängig von öffentlichen Metadaten gesperrt" } ] } }

      { Id = EntityId "spec-risk-adaptive-assurance-portfolio"; Convergence = Pending
        Payload = SpecNode
          { Title = "Risikoadaptives Assurance-Portfolio"
            Intent = "CDD wählt komplementäre offene Nachweisverfahren nach Risiko und Systemform, statt einen Formalismus oder das erzeugende Modell zum universellen Orakel zu machen"
            Criteria =
              [ { Given = "eine hochintegre, verteilte oder produktive Mission"
                  When = "Assurance Obligations abgeleitet werden"
                  Then = "werden passende strukturelle, temporale, Policy-, Provenienz- und Runtime-Orakel kombiniert" }
                { Given = "eine kreative oder normative Mission ohne formale Risikomerkmale"
                  When = "Assurance Obligations abgeleitet werden"
                  Then = "bleibt benannte menschliche Autorität erhalten ohne unpassende Formalismen zu erzwingen" } ] } }

      { Id = EntityId "spec-representative-evidence-fitness"; Convergence = Pending
        Payload = SpecNode
          { Title = "Repräsentative Evidence Fitness"
            Intent = "CDD verhindert Promotion durch grüne, aber am eigentlichen Claim vorbeimessende Proxy-Evidence"
            Criteria =
              [ { Given = "eine Mission mit Produkt-, Laufzeit-, Performance- oder Effizienzclaim"
                  When = "Assurance Obligations kompiliert werden"
                  Then = "benennt jede Obligation Claim, Systemgrenze, repräsentatives Szenario, Last, Umgebung, Metrik, Akzeptanzbereich und zulässige Proxys" }
                { Given = "nur Build-, Unit-Test- oder Budget-Evidence für einen Runtime- oder Outcome-Claim"
                  When = "Promotion bewertet wird"
                  Then = "bleibt die repräsentative Obligation unvollständig und der Claim unknown" }
                { Given = "eine repräsentative Messung"
                  When = "das Evidence Pack erzeugt wird"
                  Then = "bindet es Candidate, Commit, Szene oder Workload, Seed, Konfiguration, Hardware oder Umgebung, Rohmetriken, Quantile und bekannte Abweichungen" } ] } }

      { Id = EntityId "spec-studio-workspace-control-plane"; Convergence = Aligned
        Payload = SpecNode
          { Title = "Offene Workspace-Control-Plane-Projektion"
            Intent = "CDD Studio projiziert reale Projekte, Missionen, Runs und Evidenz über ein offenes read-only Adaptermodell, ohne Hostpfade oder Anbieter als Domänenwahrheit offenzulegen"
            Criteria =
              [ { Given = "Git-, Work-Item- und Run-Beobachtungen eines Projekts"
                  When = "der CDD-Kern den Workspace projiziert"
                  Then = "werden Lifecycle, aktive Mission, Evidenzstand und Aufmerksamkeit deterministisch abgeleitet" }
                { Given = "ein verbundener Workspace und das offene Assurance-Portfolio"
                  When = "die Control-Plane-Oberfläche im Browser geöffnet wird"
                  Then = "sind Mission, Runs, Quellen und austauschbare Verträge responsiv sichtbar ohne den lokalen Projektpfad auszugeben" } ] } }

      { Id = EntityId "spec-full-agentic-sdlc-controller"; Convergence = Pending
        Payload = SpecNode
          { Title = "Persistente Full-Agentic-SDLC-Kette"
            Intent = "CDD führt lange Software-Missionen providerneutral, resumierbar und evidenzgesteuert über kleine Work Slices statt über einen unkontrollierten Modell-Loop"
            Criteria =
              [ { Given = "eine Mission mit mehreren begrenzten Work Slices und rollenbezogenen Worker-Profilen"
                  When = "der Autopilot die nächste Aktion bestimmt"
                  Then = "durchläuft jeder Slice Scout, Builder, deterministische Gates, read-only Critic, unabhängigen Reviewer und Checkpoint in einer typisierten Reihenfolge" }
                { Given = "ein Agenten-Turn ohne erwarteten Terminalmarker"
                  When = "die Beobachtung im Run protokolliert wird"
                  Then = "wird dieselbe Session begrenzt fortgesetzt, danach aus dem letzten Checkpoint frisch gestartet und bei erschöpftem Budget fail-closed blockiert" }
                { Given = "fehlende oder rote Gates, korrelierte Rollen oder offene Review-Befunde"
                  When = "der Controller Promotion oder den nächsten Slice bewertet"
                  Then = "wird keine Fertigstellung akzeptiert und eine begrenzte Repair- oder Eskalationsaktion erzeugt" }
                { Given = "ein persistierter Run mit Agenten-, Gate-, Review- und Recovery-Beobachtungen"
                  When = "Status oder Evaluation abgefragt werden"
                  Then = "werden nächste Aktion, vollständiger Solve, Laufkosten, Toolaufrufe, Premature Stops, Recovery, Interventionen und Gate-Erfolg reproduzierbar projiziert" } ] } }

      { Id = EntityId "spec-research-studio"; Convergence = Pending
        Payload = SpecNode
          { Title = "SPOT-projiziertes Research Studio"
            Intent = "Eine Review-Oberfläche zeigt Forschungsstand, Lücken, Grenzen, Medien und Teilprojekte ohne zweite Wahrheit oder automatische Promotion"
            Criteria =
              [ { Given = "der versionierte SPOT-Snapshot geladen ist"
                  When = "das Research Studio geöffnet wird"
                  Then = "werden Claims, Quellen, Risiken, Prämissen, Entscheidungen und Kennzahlen daraus projiziert" }
                { Given = "Feedback formuliert wurde"
                  When = "die Nutzerin oder der Nutzer fortfährt"
                  Then = "wird nur ein prüfbarer GitHub-Issue-Entwurf geöffnet" } ] } }

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

      // ── EIDOS v0: typisierter Kernel + unabhängige Orakel bis ZT2 ─────
      { Id = EntityId "spec-eidos-epistemic-claims"; Convergence = Aligned
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
      { Id = EntityId "spec-eidos-mission-order"; Convergence = Aligned
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
      { Id = EntityId "spec-eidos-change-compiler"; Convergence = Aligned
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
      { Id = EntityId "spec-eidos-evidence-pack"; Convergence = Aligned
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
      { Id = EntityId "spec-eidos-zt2-opslab"; Convergence = Aligned
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
      { Id = EntityId "kb-explicit-provenance"; Convergence = Aligned
        Payload = KnowledgeNode
          { Title = "Responsible Agentic AI Requires Explicit Provenance"; Source = "https://arxiv.org/abs/2605.17169"
            MediaType = "paper"
            Takeaways = [ "Verantwortung braucht über den gesamten Agenten-Lebenszyklus explizite, eingreifbare Provenienz"
                          "Provenienz ist ein Strukturmerkmal, kein optionales Log-Detail" ] } }
      { Id = EntityId "kb-collaborative-requirements"; Convergence = Aligned
        Payload = KnowledgeNode
          { Title = "Collaborative and AI-Supported Requirements Elicitation"; Source = "https://arxiv.org/abs/2606.24060"
            MediaType = "paper"
            Takeaways = [ "Stakeholder-Kollaboration plus AI-Synthese erzeugte im kontrollierten Versuch die bestbewerteten Artefakte"
                          "EIDOS automatisiert mechanische Synthese, nicht normative Stakeholder-Autorität" ] } }
      { Id = EntityId "kb-who-grades-grader"; Convergence = Aligned
        Payload = KnowledgeNode
          { Title = "Who Grades the Grader?"; Source = "https://arxiv.org/abs/2607.12790"
            MediaType = "paper"
            Takeaways = [ "Evolvierende Metriken brauchen unveränderliche Anker und unabhängige äußere Audits"
                          "Entfernte Anker können Evaluatoren in triviale oder spielbare Metriken kollabieren lassen" ] } }
      { Id = EntityId "kb-w3c-prov"; Convergence = Aligned
        Payload = KnowledgeNode
          { Title = "W3C PROV-O"; Source = "https://www.w3.org/TR/prov-o/"
            MediaType = "standard"
            Takeaways = [ "Entity, Activity und Agent bilden einen interoperablen Provenienz-Kern"
                          "EIDOS-v0 bleibt kompakt, soll seine Provenienz später auf PROV abbilden können" ] } }
      { Id = EntityId "kb-slsa-provenance"; Convergence = Aligned
        Payload = KnowledgeNode
          { Title = "SLSA Provenance"; Source = "https://slsa.dev/spec/v1.2/"
            MediaType = "standard"
            Takeaways = [ "Artefakte werden an Builderidentität, Inputs, Zeit und Digests gebunden"
                          "Evidence Packs übernehmen diese Bindungsprinzipien über Builds hinaus" ] } }
      { Id = EntityId "kb-nist-ssdf"; Convergence = Aligned
        Payload = KnowledgeNode
          { Title = "NIST SP 800-218 Secure Software Development Framework"; Source = "https://csrc.nist.gov/pubs/sp/800/218/final"
            MediaType = "standard"
            Takeaways = [ "Security-Praktiken werden risikoorientiert in bestehende Entwicklungsmodelle integriert"
                          "Provenienz, Security-Anforderungen, Risiken und Designentscheidungen sollen verfolgt werden" ] } }
      { Id = EntityId "kb-riftward-representative-frame"; Convergence = Aligned
        Payload = KnowledgeNode
          { Title = "Project Riftward: Representative Frame as Evidence Boundary"; Source = "https://github.com/Koschnag/ai-fantasy-rts-rpg/blob/main/docs/entscheidungen/005-performancebeweis-sprachrollen-und-integration.md"
            MediaType = "longitudinal-case-study"
            Takeaways = [ "Ein dokumentiertes Performancebudget ist eine Hypothese, bis eine repräsentative Szene auf der behaupteten Zielgrenze gemessen wurde"
                          "Der Fall bindet sichtbare und simulierte Einheiten, Pfadfindung, Animation, Landschaft, Effekte, Quantile, RAM, VRAM, Draw Calls und Allokationen an denselben reproduzierbaren Frame" ] } }

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
