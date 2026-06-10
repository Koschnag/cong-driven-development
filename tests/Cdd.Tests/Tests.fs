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
let ``store saves and loads round-trip`` () =
    let tmp = Path.Combine(Path.GetTempPath(), "cdd-test-" + System.Guid.NewGuid().ToString("N"))
    try
        let entry = sampleSpec "spec-store" [ crit 1 ]
        Store.save tmp entry
        let loaded = Store.load tmp
        Assert.Equal<SpotEntry list>([ entry ], loaded)
    finally
        if Directory.Exists tmp then Directory.Delete(tmp, true)
