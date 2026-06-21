module Tests

open System
open System.IO
open System.Net
open System.Text
open System.Threading.Tasks
open Xunit
open Cdd.Core
open Cdd.Core.Spot

let private sampleSpec id criteria =
    { Id = EntityId id
      Convergence = Pending
      Payload =
        SpecNode
          { Title = "T"
            Intent = "i"
            Criteria = criteria } }

let private crit n =
    { Given = sprintf "g%d" n; When = sprintf "w%d" n; Then = sprintf "t%d" n }

[<Fact>]
let ``serialization round-trips a full graph`` () =
    let entries =
        [ sampleSpec "spec-a" [ crit 1; crit 2 ]
          { Id = EntityId "risk-a"
            Convergence = Diverged
            Payload =
              RiskNode
                { Statement = "boom"; Likelihood = High; Impact = Critical
                  Mitigation = Some "fix" } }
          { Id = EntityId "comp-a"
            Convergence = Aligned
            Payload = ComponentNode { Name = "C"; DependsOn = [ EntityId "spec-a" ] } } ]
    let restored =
        entries
        |> List.map (Json.serialize >> Json.deserialize<SpotEntry>)
    Assert.Equal<SpotEntry list>(entries, restored)

[<Fact>]
let ``EntityId serializes as a bare string`` () =
    let json = Json.serialize (EntityId "x")
    Assert.Equal("\"x\"", json)

[<Fact>]
let ``validate flags a spec without criteria`` () =
    let findings = Validate.validate [ sampleSpec "spec-empty" [] ]
    Assert.Contains(findings, fun f ->
        f.Severity = Validate.Error && f.EntityId = EntityId "spec-empty")

[<Fact; Trait("spot", "spec-validate-test-1")>]
let ``validate flags a test referencing an unknown spec`` () =
    let entries =
        [ { Id = EntityId "test-x"
            Convergence = Pending
            Payload = TestNode { SpecRef = EntityId "nope"; Name = "n"; Derived = false } } ]
    Assert.NotEmpty(Validate.validate entries |> Validate.errors)

[<Fact; Trait("spot", "spec-validate-test-2")>]
let ``validate detects a dependency cycle`` () =
    let comp id dep =
        { Id = EntityId id
          Convergence = Pending
          Payload = ComponentNode { Name = id; DependsOn = [ EntityId dep ] } }
    let entries = [ comp "a" "b"; comp "b" "a" ]
    Assert.Contains(Validate.validate entries, fun f ->
        f.Message.Contains "Zyklus")

[<Fact>]
let ``validate accepts a well-formed graph`` () =
    let entries =
        [ sampleSpec "spec-ok" [ crit 1 ]
          { Id = EntityId "spec-ok-test-1"
            Convergence = Aligned
            Payload = TestNode { SpecRef = EntityId "spec-ok"; Name = "n"; Derived = true } } ]
    Assert.Empty(Validate.validate entries |> Validate.errors)

[<Fact; Trait("spot", "spec-derive-tests-test-1")>]
let ``derive-tests creates one test per criterion`` () =
    let derived = Derive.deriveTests [ sampleSpec "spec-a" [ crit 1; crit 2; crit 3 ] ]
    Assert.Equal(3, List.length derived)
    Assert.All(derived, fun e ->
        match e.Payload with
        | TestNode t -> Assert.True t.Derived
        | _ -> failwith "expected a test node")

[<Fact; Trait("spot", "spec-derive-tests-test-2")>]
let ``derive-tests is idempotent`` () =
    let spec = sampleSpec "spec-a" [ crit 1; crit 2 ]
    let firstPass = Derive.deriveTests [ spec ]
    let secondPass = Derive.deriveTests (spec :: firstPass)
    Assert.Empty secondPass

[<Fact; Trait("spot", "spec-derive-tests-test-3")>]
let ``derive-tests corrects a stale name instead of leaving it drifted`` () =
    // Ein abgeleiteter Test, dessen Name nicht mehr zum Kriterium an dieser Position passt
    // (z. B. weil ein Kriterium eingefügt/umsortiert wurde).
    let spec = sampleSpec "spec-a" [ crit 1; crit 2 ]
    let stale =
        { Id = EntityId "spec-a-test-1"
          Convergence = Aligned
          Payload = TestNode { SpecRef = EntityId "spec-a"; Name = "VERALTET"; Derived = true } }
    let derived = Derive.deriveTests [ spec; stale ]
    let names =
        derived
        |> List.choose (fun e -> match e.Payload with TestNode t -> Some(idValue e.Id, t.Name) | _ -> None)
        |> Map.ofList
    Assert.Equal("T — when w1 then t1", names.[idValue (EntityId "spec-a-test-1")])  // korrigiert
    Assert.True(Map.containsKey (idValue (EntityId "spec-a-test-2")) names)          // fehlendes Kriterium erzeugt

[<Fact>]
let ``new node kinds round-trip`` () =
    let entries =
        [ { Id = EntityId "premise-a"; Convergence = Pending
            Payload = PremiseNode { Statement = "s"; Rationale = "r" } }
          { Id = EntityId "adr-1"; Convergence = Aligned
            Payload = DecisionNode { Title = "t"; Context = "c"; Choice = "ch"
                                     Consequences = "co"; Supersedes = Some(EntityId "premise-a") } }
          { Id = EntityId "kb-fowler"; Convergence = Pending
            Payload = KnowledgeNode { Title = "Refactoring"; Source = "https://martinfowler.com"
                                      MediaType = "blog"; Takeaways = [ "a"; "b" ] } }
          { Id = EntityId "tool-grep"; Convergence = Pending
            Payload = ToolNode { Name = "grep"; Purpose = "suchen"; Endpoint = None } } ]
    let restored = entries |> List.map (Json.serialize >> Json.deserialize<SpotEntry>)
    Assert.Equal<SpotEntry list>(entries, restored)

[<Fact>]
let ``validate flags decision superseding unknown node`` () =
    let entries =
        [ { Id = EntityId "adr-1"; Convergence = Pending
            Payload = DecisionNode { Title = "t"; Context = "c"; Choice = "ch"
                                     Consequences = "co"; Supersedes = Some(EntityId "ghost") } } ]
    Assert.NotEmpty(Validate.validate entries |> Validate.errors)

[<Fact>]
let ``term nodes round-trip with relations`` () =
    let entries =
        [ { Id = EntityId "term-a"; Convergence = Aligned
            Payload = TermNode { Name = "A"; Definition = "d"; Synonyms = [ "x" ]
                                 Relations = [ IsA(EntityId "term-b"); PartOf(EntityId "term-b") ] } }
          { Id = EntityId "term-b"; Convergence = Aligned
            Payload = TermNode { Name = "B"; Definition = "d"; Synonyms = []; Relations = [] } } ]
    let restored = entries |> List.map (Json.serialize >> Json.deserialize<SpotEntry>)
    Assert.Equal<SpotEntry list>(entries, restored)
    Assert.Empty(Validate.validate entries |> Validate.errors)

[<Fact>]
let ``validate flags term relation to unknown term`` () =
    let entries =
        [ { Id = EntityId "term-a"; Convergence = Pending
            Payload = TermNode { Name = "A"; Definition = "d"; Synonyms = []
                                 Relations = [ RelatesTo(EntityId "ghost") ] } } ]
    Assert.NotEmpty(Validate.validate entries |> Validate.errors)

[<Fact>]
let ``validate warns on term without definition`` () =
    let entries =
        [ { Id = EntityId "term-a"; Convergence = Pending
            Payload = TermNode { Name = "A"; Definition = " "; Synonyms = []; Relations = [] } } ]
    Assert.NotEmpty(Validate.validate entries |> Validate.warnings)

[<Fact; Trait("spot", "spec-export-context-test-1")>]
let ``export-context renders all sections and content`` () =
    let entries =
        [ { Id = EntityId "term-a"; Convergence = Aligned
            Payload = TermNode { Name = "Begriff"; Definition = "Def"; Synonyms = [ "Syn" ]
                                 Relations = [ IsA(EntityId "term-a") ] } }
          sampleSpec "spec-a" [ crit 1 ]
          { Id = EntityId "risk-a"; Convergence = Pending
            Payload = RiskNode { Statement = "Gefahr"; Likelihood = Low; Impact = High
                                 Mitigation = Some "Plan" } } ]
    let md = Export.toMarkdown entries
    Assert.Contains("# SPOT-Kontext", md)
    Assert.Contains("## Ubiquitäre Sprache (Ontologie)", md)
    Assert.Contains("**Begriff** *(auch: Syn)* — Def", md)
    Assert.Contains("GIVEN g1 WHEN w1 THEN t1", md)
    Assert.Contains("**Gefahr** (Likelihood Low, Impact High) — Mitigation: Plan", md)
    Assert.Contains("## Offene Arbeit (nicht Aligned)", md)
    Assert.Contains("`spec-a`", md)

[<Fact; Trait("spot", "spec-context-slice-test-1")>]
let ``context slice keeps core+index always but full detail only for referenced nodes`` () =
    let entries =
        { Id = EntityId "term-a"; Convergence = Aligned
          Payload = TermNode { Name = "Begriff"; Definition = "Def"; Synonyms = []; Relations = [] } }
        :: [ for i in 1..6 -> sampleSpec (sprintf "spec-%d" i) [ crit i; crit (i + 10) ] ]
    let slice = Export.toContextSlice "Arbeite an spec-3 weiter." entries
    // axiomatischer Kern + Index sind IMMER da
    Assert.Contains("## Ubiquitäre Sprache (verbindlich)", slice)
    Assert.Contains("## Index (alle Knoten", slice)
    Assert.Contains("`spec-3`", slice)
    Assert.Contains("`spec-1`", slice)                   // alle im Index
    // volle Kriterien NUR für den im Auftrag genannten Knoten
    Assert.Contains("GIVEN g3 WHEN w3 THEN t3", slice)   // spec-3: voll
    Assert.DoesNotContain("GIVEN g1 WHEN w1 THEN t1", slice) // spec-1: nur Index-Zeile
    // Surface-Cut: bei Skala (6 Specs, 1 genannt) ist der Slice klar kleiner als der Full-Dump
    Assert.True(slice.Length < (Export.toMarkdown entries).Length)

[<Fact; Trait("spot", "spec-governance-test-1")>]
let ``invariant SpecsNeedTests flags untested specs`` () =
    let inv = { Id = EntityId "inv-1"; Convergence = Aligned
                Payload = InvariantNode { Description = "Specs brauchen Tests"; Rule = SpecsNeedTests } }
    let entries = [ inv; sampleSpec "spec-untested" [ crit 1 ] ]
    Assert.Contains(Validate.validate entries |> Validate.errors, fun f ->
        f.EntityId = EntityId "spec-untested" && f.Message.Contains "Invariante")
    let withTest =
        entries @ [ { Id = EntityId "t1"; Convergence = Pending
                      Payload = TestNode { SpecRef = EntityId "spec-untested"; Name = "n"; Derived = true } } ]
    Assert.Empty(Validate.validate withTest |> Validate.errors)

[<Fact>]
let ``invariant CriticalRisksNeedMitigation escalates to error`` () =
    let inv = { Id = EntityId "inv-2"; Convergence = Aligned
                Payload = InvariantNode { Description = "Krit. Risiken mitigieren"; Rule = CriticalRisksNeedMitigation } }
    let risk = { Id = EntityId "risk-x"; Convergence = Pending
                 Payload = RiskNode { Statement = "s"; Likelihood = Low; Impact = Critical; Mitigation = None } }
    Assert.NotEmpty(Validate.validate [ inv; risk ] |> Validate.errors)

[<Fact>]
let ``invariant IdPrefix flags wrong prefixes`` () =
    let inv = { Id = EntityId "inv-3"; Convergence = Aligned
                Payload = InvariantNode { Description = "Begriffe heißen term-*"; Rule = IdPrefix("term", "term-") } }
    let bad = { Id = EntityId "begriff-x"; Convergence = Aligned
                Payload = TermNode { Name = "X"; Definition = "d"; Synonyms = []; Relations = [] } }
    Assert.NotEmpty(Validate.validate [ inv; bad ] |> Validate.errors)
    let good = { bad with Id = EntityId "term-x" }
    Assert.Empty(Validate.validate [ inv; good ] |> Validate.errors)

[<Fact>]
let ``invariant round-trips through json`` () =
    let inv = { Id = EntityId "inv-4"; Convergence = Aligned
                Payload = InvariantNode { Description = "d"; Rule = IdPrefix("spec", "spec-") } }
    Assert.Equal(inv, Json.serialize inv |> Json.deserialize<SpotEntry>)

[<Fact; Trait("spot", "spec-sync-tests-test-1")>]
let ``sync-tests measures coverage via markers`` () =
    let testNode id =
        { Id = EntityId id; Convergence = Pending
          Payload = TestNode { SpecRef = EntityId "spec-x"; Name = "n"; Derived = true } }
    let entries = [ testNode "spec-x-test-1"; testNode "spec-x-test-2" ]
    let covered = Set.ofList [ "spec-x-test-1" ]
    let mismatches, updated = Sync.syncTests covered entries
    Assert.Equal(1, List.length mismatches)   // test-1: Pending → Aligned
    Assert.Equal(Aligned, (updated |> List.find (fun e -> e.Id = EntityId "spec-x-test-1")).Convergence)
    Assert.Equal(Pending, (updated |> List.find (fun e -> e.Id = EntityId "spec-x-test-2")).Convergence)

[<Fact>]
let ``scanTestMarkers finds traits and comment markers`` () =
    let tmp = Path.Combine(Path.GetTempPath(), "cdd-mk-" + System.Guid.NewGuid().ToString("N"))
    try
        Directory.CreateDirectory tmp |> ignore
        File.WriteAllText(Path.Combine(tmp, "a.fs"), """[<Fact; Trait("spot", "spec-a-test-1")>]""")
        File.WriteAllText(Path.Combine(tmp, "b.mjs"), "// [spot: spec-b-test-1]\nconsole.log(1)")
        let found = Sync.scanTestMarkers tmp
        Assert.Contains("spec-a-test-1", found)
        Assert.Contains("spec-b-test-1", found)
    finally
        if Directory.Exists tmp then Directory.Delete(tmp, true)

[<Fact; Trait("spot", "spec-derive-code-test-1")>]
let ``derive-code generates skeletons only for uncovered test nodes`` () =
    let entries =
        [ sampleSpec "spec-x" [ crit 1 ]
          { Id = EntityId "spec-x-test-1"; Convergence = Pending
            Payload = TestNode { SpecRef = EntityId "spec-x"; Name = "T — when w1 then t1"; Derived = true } }
          { Id = EntityId "spec-x-test-2"; Convergence = Aligned
            Payload = TestNode { SpecRef = EntityId "spec-x"; Name = "schon abgedeckt"; Derived = true } } ]
    let code = Generate.testSkeletons (Set.ofList [ "spec-x-test-2" ]) entries
    Assert.Contains("Trait(\"spot\", \"spec-x-test-1\")", code)
    Assert.DoesNotContain("spec-x-test-2", code)
    Assert.Contains("GIVEN g1 WHEN w1 THEN t1", code)
    Assert.Contains("failwith", code)
    // Vollständig abgedeckt → freundlicher Hinweis statt Stubs
    let none = Generate.testSkeletons (Set.ofList [ "spec-x-test-1"; "spec-x-test-2" ]) entries
    Assert.DoesNotContain("failwith", none)

[<Fact; Trait("spot", "spec-sync-docs-test-2")>]
let ``decisionsMarkdown documents premises decisions and invariants`` () =
    let entries =
        [ { Id = EntityId "premise-x"; Convergence = Aligned
            Payload = PremiseNode { Statement = "Kein Python"; Rationale = "Ein Stack" } }
          { Id = EntityId "adr-x"; Convergence = Aligned
            Payload = DecisionNode { Title = "F#"; Context = "K"; Choice = "C"
                                     Consequences = "Q"; Supersedes = None } }
          { Id = EntityId "inv-x"; Convergence = Aligned
            Payload = InvariantNode { Description = "Specs getestet"; Rule = SpecsNeedTests } } ]
    let md = Export.decisionsMarkdown entries
    Assert.Contains("## Prämissen", md)
    Assert.Contains("Kein Python", md)
    Assert.Contains("### F# · `adr-x`", md)
    Assert.Contains("Geltende Invarianten", md)
    Assert.Contains("Specs getestet", md)

[<Fact; Trait("spot", "spec-sync-docs-test-1")>]
let ``statusMarkdown reflects aligned and pending specs`` () =
    let entries =
        [ { sampleSpec "spec-fertig" [ crit 1 ] with Convergence = Aligned }
          sampleSpec "spec-offen" [ crit 1 ] ]
    let md = Export.statusMarkdown entries
    Assert.Contains("✅ **T**", md)
    Assert.Contains("🔜 **T**", md)
    Assert.Contains("generiert", md)

[<Fact; Trait("spot", "spec-roundtrip-sync-test-1")>]
let ``sync-code compares model components against code projects`` () =
    let comp name deps =
        { Id = EntityId ("comp-" + name); Convergence = Pending
          Payload = ComponentNode { Name = name; DependsOn = deps } }
    let entries =
        [ comp "Core" []
          comp "Cli" [ EntityId "comp-Core" ]
          comp "Ghost" [] ]
    let projects : Sync.CodeProject list =
        [ { Name = "Core"; References = [] }
          { Name = "Cli"; References = [ "Core" ] }
          { Name = "Neu"; References = [] } ]
    let results, updated = Sync.compare projects entries
    let statusOf name = results |> List.find (fun r -> r.Name = name) |> fun r -> r.Status
    Assert.Equal(Aligned, statusOf "Core")
    Assert.Equal(Aligned, statusOf "Cli")
    Assert.Equal(Orphaned, statusOf "Neu")     // Code ohne Modell
    Assert.Equal(Pending, statusOf "Ghost")    // Modell ohne Code
    let updatedCore = updated |> List.find (fun e -> e.Id = EntityId "comp-Core")
    Assert.Equal(Aligned, updatedCore.Convergence)

[<Fact>]
let ``sync-code detects diverged dependencies`` () =
    let entries =
        [ { Id = EntityId "comp-a"; Convergence = Aligned
            Payload = ComponentNode { Name = "A"; DependsOn = [] } }
          { Id = EntityId "comp-b"; Convergence = Aligned
            Payload = ComponentNode { Name = "B"; DependsOn = [] } } ]   // Modell: B hängt von nichts ab
    let projects : Sync.CodeProject list =
        [ { Name = "A"; References = [] }
          { Name = "B"; References = [ "A" ] } ]                          // Code: B → A
    let results, _ = Sync.compare projects entries
    let b = results |> List.find (fun r -> r.Name = "B")
    Assert.Equal(Diverged, b.Status)

[<Fact>]
let ``sync scanProjects reads fsproj references`` () =
    let tmp = Path.Combine(Path.GetTempPath(), "cdd-sync-" + System.Guid.NewGuid().ToString("N"))
    try
        Directory.CreateDirectory(Path.Combine(tmp, "A")) |> ignore
        Directory.CreateDirectory(Path.Combine(tmp, "B")) |> ignore
        File.WriteAllText(Path.Combine(tmp, "A", "A.fsproj"), "<Project></Project>")
        File.WriteAllText(Path.Combine(tmp, "B", "B.fsproj"),
            """<Project><ItemGroup><ProjectReference Include="..\A\A.fsproj" /></ItemGroup></Project>""")
        let ps = Sync.scanProjects tmp |> List.sortBy (fun p -> p.Name)
        Assert.Equal(2, List.length ps)
        Assert.Equal<string list>([ "A" ], (ps |> List.find (fun p -> p.Name = "B")).References)
    finally
        if Directory.Exists tmp then Directory.Delete(tmp, true)

[<Fact; Trait("spot", "spec-fehlerliste-test-1")>]
let ``validate detects contradictory term hierarchy cycles`` () =
    let term id rels =
        { Id = EntityId id; Convergence = Aligned
          Payload = TermNode { Name = id; Definition = "d"; Synonyms = []; Relations = rels } }
    let entries = [ term "term-a" [ IsA(EntityId "term-b") ]; term "term-b" [ PartOf(EntityId "term-a") ] ]
    Assert.Contains(Validate.validate entries |> Validate.errors, fun f -> f.Message.Contains "Widerspruch")
    // RelatesTo-Zyklen sind KEIN Widerspruch (Assoziation ist frei)
    let ok = [ term "term-a" [ RelatesTo(EntityId "term-b") ]; term "term-b" [ RelatesTo(EntityId "term-a") ] ]
    Assert.Empty(Validate.validate ok |> Validate.errors)
    // Vorlauf-Knoten in einen Zyklus (A→B→C→B) markiert NUR B und C als Widerspruch, nicht A
    let kette =
        [ term "term-a" [ IsA(EntityId "term-b") ]
          term "term-b" [ IsA(EntityId "term-c") ]
          term "term-c" [ IsA(EntityId "term-b") ] ]
    let widersprüche =
        Validate.validate kette |> Validate.errors
        |> List.filter (fun f -> f.Message.Contains "Widerspruch")
        |> List.map (fun f -> idValue f.EntityId) |> Set.ofList
    Assert.Equal<Set<string>>(Set.ofList [ "term-b"; "term-c" ], widersprüche)

[<Fact; Trait("spot", "spec-fehlerliste-test-2")>]
let ``validate warns on ambiguous duplicate term names`` () =
    let term id name =
        { Id = EntityId id; Convergence = Aligned
          Payload = TermNode { Name = name; Definition = "d"; Synonyms = []; Relations = [] } }
    let findings = Validate.validate [ term "term-a" "Konto"; term "term-b" "konto " ]
    Assert.Equal(2, findings |> Validate.warnings |> List.filter (fun f -> f.Message.Contains "Mehrdeutigkeit") |> List.length)

[<Fact>]
let ``store rejects path-traversal ids`` () =
    Assert.False(Store.isValidId (EntityId "../evil"))
    Assert.False(Store.isValidId (EntityId "a/b"))
    Assert.False(Store.isValidId (EntityId ""))
    Assert.True(Store.isValidId (EntityId "spec-login_v2"))
    let entry = { sampleSpec "x" [ crit 1 ] with Id = EntityId "../evil" }
    Assert.Throws<System.ArgumentException>(fun () -> Store.save "/tmp" entry) |> ignore

[<Fact>]
let ``store delete removes a node`` () =
    let tmp = Path.Combine(Path.GetTempPath(), "cdd-test-" + System.Guid.NewGuid().ToString("N"))
    try
        let entry = sampleSpec "spec-del" [ crit 1 ]
        Store.save tmp entry
        Assert.True(Store.delete tmp (EntityId "spec-del"))
        Assert.Empty(Store.load tmp)
        Assert.False(Store.delete tmp (EntityId "spec-del"))
    finally
        if Directory.Exists tmp then Directory.Delete(tmp, true)

[<Fact>]
let ``store load reports corrupt files instead of crashing`` () =
    let tmp = Path.Combine(Path.GetTempPath(), "cdd-test-" + System.Guid.NewGuid().ToString("N"))
    try
        Store.save tmp (sampleSpec "spec-ok" [ crit 1 ])
        File.WriteAllText(Path.Combine(Store.spotDir tmp, "kaputt.json"), "kein json")
        let ex = Assert.Throws<IOException>(fun () -> Store.load tmp |> ignore)
        Assert.Contains("kaputt.json", ex.Message)
    finally
        if Directory.Exists tmp then Directory.Delete(tmp, true)

[<Fact>]
let ``store saves and loads round-trip`` () =
    let tmp = Path.Combine(Path.GetTempPath(), "cdd-test-" + System.Guid.NewGuid().ToString("N"))
    try
        let entry = sampleSpec "spec-store" [ crit 1 ]
        Store.save tmp entry
        let loaded = Store.load tmp
        Assert.Equal<SpotEntry list>([ entry ], loaded)
    finally
        if Directory.Exists tmp then Directory.Delete(tmp, true)

[<Fact>]
let ``scanRepo findet Projekte auch unter tools und apps`` () =
    let tmp = Path.Combine(Path.GetTempPath(), "cdd-scanrepo-" + System.Guid.NewGuid().ToString("N"))
    Directory.CreateDirectory(Path.Combine(tmp, "src", "A")) |> ignore
    Directory.CreateDirectory(Path.Combine(tmp, "tools", "B")) |> ignore
    File.WriteAllText(Path.Combine(tmp, "src", "A", "A.fsproj"), "<Project/>")
    File.WriteAllText(Path.Combine(tmp, "tools", "B", "B.csproj"),
        """<Project><ItemGroup><ProjectReference Include="..\..\src\A\A.fsproj" /></ItemGroup></Project>""")
    let projekte = Sync.scanRepo tmp
    Directory.Delete(tmp, true)
    Assert.Equal(2, List.length projekte)
    let b = projekte |> List.find (fun p -> p.Name = "B")
    Assert.Equal<string list>([ "A" ], b.References)

// ── OpenAiCompat-Tool-Loop: hermetisch gegen einen Mock-OpenAI-Endpoint (HttpListener) ──
// Beweist die NEUE Logik: tool_call (spot_list) → in-process ausführen → Ergebnis re-feeden → finale Antwort.
[<Fact; Trait("spot", "spec-engine-toolloop-test-1")>]
let ``openai-compat engine drives an agentic tool-loop`` () : Task =
    task {
        // temp .spot-Store mit einem Knoten, den spot_list zurückgeben muss
        let root = Path.Combine(Path.GetTempPath(), "cdd-toolloop-" + Guid.NewGuid().ToString("N"))
        Directory.CreateDirectory(Path.Combine(root, ".spot")) |> ignore
        Store.save root { Id = EntityId "term-x"; Convergence = Pending
                          Payload = TermNode { Name = "X"; Definition = "d"; Synonyms = []; Relations = [] } }

        // freien Port finden
        let probe = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0)
        probe.Start()
        let port = (probe.LocalEndpoint :?> IPEndPoint).Port
        probe.Stop()

        // Mock-OpenAI: 1. Antwort = tool_call spot_list, 2. Antwort = finaler Text
        let responses =
            [ """{"choices":[{"message":{"role":"assistant","content":null,"tool_calls":[{"id":"c1","type":"function","function":{"name":"spot_list","arguments":"{}"}}]}}]}"""
              """{"choices":[{"message":{"role":"assistant","content":"Fertig: term-x gelistet."}}]}""" ]
        use listener = new HttpListener()
        listener.Prefixes.Add(sprintf "http://localhost:%d/" port)
        listener.Start()
        let serverTask =
            task {
                for r in responses do
                    let! ctx = listener.GetContextAsync()
                    let bytes = Encoding.UTF8.GetBytes(r: string)
                    ctx.Response.ContentType <- "application/json"
                    ctx.Response.ContentLength64 <- int64 bytes.Length
                    do! ctx.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length)
                    ctx.Response.OutputStream.Close()
            }

        let events = System.Collections.Generic.List<Engine.EngineEvent>()
        let collect (ev: Engine.EngineEvent) : Task = events.Add(ev); Task.CompletedTask
        let opts : Engine.EngineOptions =
            { Kind = Engine.Ollama; Model = "mock"; Cwd = root; AllowedTools = []
              PermissionMode = ""; McpConfigJson = ""; BaseUrl = sprintf "http://localhost:%d" port; ApiKey = ""; SystemPrompt = "" }
        let runner = Engine.create Engine.Ollama
        do! runner.Run({ Prompt = "Liste die Knoten."; ContextMd = ""; Options = opts }, collect)
        do! serverTask
        listener.Stop()
        try Directory.Delete(root, true) with _ -> ()

        // Der Loop hat: das Tool aufgerufen, in-process ausgeführt (term-x), und final terminiert.
        Assert.Contains(events, fun e -> match e with Engine.ToolUse("spot_list", _) -> true | _ -> false)
        Assert.Contains(events, fun e -> match e with Engine.ToolResult r -> r.Contains "term-x" | _ -> false)
        Assert.Contains(events, fun e -> match e with Engine.Completed(r, _) -> r.Contains "Fertig" | _ -> false)
    }

// ===== Reflexiv: CDD prüft eine Invariante über sein EIGENES Selbst-Modell =====
// Selbstanwendung — das System auf den Prozess selbst gerichtet. Schließt die Klasse
// "Aligned ohne echten Test" (Defekt 4: Sync.fs setzt Test-Knoten Aligned bei Marker-
// Präsenz, nicht bei Grün): ein als Aligned markierter Test-Knoten MUSS einen echten
// [<Trait("spot", id)>]-Marker im Testcode haben. Zusammen mit grüner Suite (CI) folgt:
// Aligned-Test-Knoten ⇒ der zugehörige Test existiert und ist grün. Diese Invariante läuft
// selbst als Test in derselben Suite + CI — das System gatet sich gegen seinen eigenen Drift.
let rec private findeWurzel (dir: string) : string option =
    if isNull dir then None
    elif Directory.Exists(Path.Combine(dir, ".spot")) then Some dir
    else findeWurzel (Path.GetDirectoryName dir)

[<Fact; Trait("spot", "spec-gate-selbst-hart-test-1")>]
let ``Reflexiv — jeder Aligned Test-Knoten im Selbst-Modell hat einen echten Test-Marker`` () =
    match findeWurzel (Directory.GetCurrentDirectory()) with
    | None -> ()  // ohne erreichbares .spot/ (isolierter Checkout) nichts zu prüfen
    | Some wurzel ->
        let modell = Store.load wurzel
        let abgedeckt = Sync.scanTestMarkers (Path.Combine(wurzel, "tests"))
        let verwaiste =
            modell
            |> List.choose (fun e ->
                match e.Payload with
                | TestNode _ when e.Convergence = Aligned
                                  && not (Set.contains (idValue e.Id) abgedeckt) ->
                    Some(idValue e.Id)
                | _ -> None)
        Assert.True(
            List.isEmpty verwaiste,
            sprintf "Aligned Test-Knoten ohne echten Marker (= Aligned ohne Test): %A" verwaiste)
