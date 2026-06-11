module Tests

open System.IO
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

[<Fact>]
let ``validate flags a test referencing an unknown spec`` () =
    let entries =
        [ { Id = EntityId "test-x"
            Convergence = Pending
            Payload = TestNode { SpecRef = EntityId "nope"; Name = "n"; Derived = false } } ]
    Assert.NotEmpty(Validate.validate entries |> Validate.errors)

[<Fact>]
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

[<Fact>]
let ``derive-tests creates one test per criterion`` () =
    let derived = Derive.deriveTests [ sampleSpec "spec-a" [ crit 1; crit 2; crit 3 ] ]
    Assert.Equal(3, List.length derived)
    Assert.All(derived, fun e ->
        match e.Payload with
        | TestNode t -> Assert.True t.Derived
        | _ -> failwith "expected a test node")

[<Fact>]
let ``derive-tests is idempotent`` () =
    let spec = sampleSpec "spec-a" [ crit 1; crit 2 ]
    let firstPass = Derive.deriveTests [ spec ]
    let secondPass = Derive.deriveTests (spec :: firstPass)
    Assert.Empty secondPass

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

[<Fact>]
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
let ``store saves and loads round-trip`` () =
    let tmp = Path.Combine(Path.GetTempPath(), "cdd-test-" + System.Guid.NewGuid().ToString("N"))
    try
        let entry = sampleSpec "spec-store" [ crit 1 ]
        Store.save tmp entry
        let loaded = Store.load tmp
        Assert.Equal<SpotEntry list>([ entry ], loaded)
    finally
        if Directory.Exists tmp then Directory.Delete(tmp, true)
