module Tests

open System
open System.IO
open System.Net
open System.Text
open System.Threading.Tasks
open Xunit
open Cdd.Core
open Cdd.Core.Spot
open CourseForge.Core
open FsCheck

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

let private findRepoRoot () =
    let rec loop (dir: DirectoryInfo) =
        if File.Exists(Path.Combine(dir.FullName, "Cdd.slnx")) then dir.FullName
        elif isNull dir.Parent then failwith "Repository root not found"
        else loop dir.Parent
    loop (DirectoryInfo AppContext.BaseDirectory)

let private withSyntheticMoodle includeSensitive action =
    let root = Path.Combine(Path.GetTempPath(), "courseforge-" + Guid.NewGuid().ToString("N"))
    try
        Directory.CreateDirectory(Path.Combine(root, "sections", "section_1")) |> ignore
        Directory.CreateDirectory(Path.Combine(root, "sections", "section_2")) |> ignore
        File.WriteAllText(
            Path.Combine(root, "moodle_backup.xml"),
            """<moodle_backup><information>
                 <original_course_id>demo-101</original_course_id>
                 <original_course_fullname>Generic Demo Course</original_course_fullname>
                 <original_course_shortname>DEMO101</original_course_shortname>
               </information></moodle_backup>""")
        File.WriteAllText(
            Path.Combine(root, "sections", "section_1", "section.xml"),
            """<section id="1"><id>1</id><name>Foundations</name></section>""")
        File.WriteAllText(
            Path.Combine(root, "sections", "section_2", "section.xml"),
            """<section id="2"><id>2</id><name>Transfer</name></section>""")
        if includeSensitive then
            File.WriteAllText(Path.Combine(root, "users.xml"), "<users><user>private</user></users>")
        action root
    finally
        if Directory.Exists root then Directory.Delete(root, true)

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
    Assert.False(md.EndsWith(Environment.NewLine + Environment.NewLine, StringComparison.Ordinal))

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

[<Fact; Trait("spot", "spec-research-claim-ledger-test-1")>]
let ``research claim projection preserves status provenance and derivation`` () =
    let source =
        { Id = EntityId "kb-source"; Convergence = Aligned
          Payload =
            KnowledgeNode
              { Title = "Source"
                Source = "https://example.org/research"
                MediaType = "paper"
                Takeaways = [] } }
    let claim =
        { Id = EntityId "claim-gates"; Convergence = Aligned
          Payload =
            ResearchClaimNode
              { Statement = "Harder independent gates can justify bounded autonomy."
                Status = Proposed
                Scope = "AI-assisted software evolution"
                Provenance =
                  { SourceRefs = [ source.Id ]
                    DerivedFrom = [ source.Id ]
                    RecordedAt = "2026-07-27T00:00:00Z"
                    Method = "conceptual synthesis" }
                Rationale = Some "Requires comparative experiments." } }
    let restored = Json.serialize claim |> Json.deserialize<SpotEntry>
    Assert.Equal(claim, restored)
    Assert.Empty(Validate.validate [ source; claim ] |> Validate.errors)

[<Fact; Trait("spot", "spec-research-claim-ledger-test-2")>]
let ``verified research claims fail closed without valid public evidence provenance`` () =
    let claim =
        { Id = EntityId "claim-unproven"; Convergence = Pending
          Payload =
            ResearchClaimNode
              { Statement = "Unproven"
                Status = Verified
                Scope = "test"
                Provenance =
                  { SourceRefs = []
                    DerivedFrom = []
                    RecordedAt = "not-a-date"
                    Method = "" }
                Rationale = None } }
    let errors = Validate.validate [ claim ] |> Validate.errors
    Assert.Contains(errors, fun finding -> finding.Message.Contains "ISO-8601")
    Assert.Contains(errors, fun finding -> finding.Message.Contains "mindestens eine benannte Quelle")

[<Fact; Trait("spot", "spec-courseforge-import-test-1")>]
let ``courseforge imports only generic Moodle course metadata`` () =
    withSyntheticMoodle false (fun root ->
        match MoodleFolder.importExtractedFolder Defaults.importLimits root with
        | Error errors -> failwithf "unexpected errors: %A" errors
        | Ok imported ->
            Assert.Equal("Generic Demo Course", imported.Course.Title)
            Assert.Equal("DEMO101", imported.Course.ShortName)
            Assert.Equal(2, imported.Course.Sections.Length)
            Assert.Equal(64, imported.SourceFingerprint.Length))

[<Fact; Trait("spot", "spec-courseforge-import-test-2")>]
let ``courseforge excludes sensitive Moodle data and enforces quotas`` () =
    withSyntheticMoodle true (fun root ->
        match MoodleFolder.importExtractedFolder Defaults.importLimits root with
        | Error errors -> failwithf "unexpected errors: %A" errors
        | Ok imported ->
            Assert.Contains(SensitiveDataExcluded, imported.Findings)
            Assert.DoesNotContain("private", Json.serialize imported)
        let oneFileOnly = { Defaults.importLimits with MaxFiles = 1 }
        match MoodleFolder.importExtractedFolder oneFileOnly root with
        | Error errors ->
            Assert.Contains(errors, function FileLimitExceeded _ -> true | _ -> false)
        | Ok _ -> failwith "file quota should reject the folder")

[<Fact>]
let ``courseforge prohibits DTDs in untrusted Moodle metadata`` () =
    withSyntheticMoodle false (fun root ->
        File.WriteAllText(
            Path.Combine(root, "moodle_backup.xml"),
            """<!DOCTYPE x [<!ENTITY probe SYSTEM "file:///not-allowed">]>
               <moodle_backup><information><original_course_fullname>&probe;</original_course_fullname></information></moodle_backup>""")
        match MoodleFolder.importExtractedFolder Defaults.importLimits root with
        | Error errors ->
            Assert.Contains(errors, function InvalidMetadata _ -> true | _ -> false)
        | Ok _ -> failwith "DTD processing should be prohibited")

[<Fact; Trait("spot", "spec-courseforge-gameplan-test-1")>]
let ``courseforge creates a deterministic authoring-gated game plan`` () =
    withSyntheticMoodle false (fun root ->
        let imported =
            MoodleFolder.importExtractedFolder Defaults.importLimits root
            |> function Ok value -> value | Error errors -> failwithf "%A" errors
        let first = GamePlanBuilder.create imported
        let second = GamePlanBuilder.create imported
        Assert.Equal(first, second)
        Assert.Equal(8, first.Missions.Length)
        Assert.All(first.Missions, fun mission -> Assert.True mission.NeedsAuthoring))

[<Fact; Trait("spot", "spec-feedback-evolution-test-1")>]
let ``public feedback can only create a proposal with assurance obligations`` () =
    let signal =
        { Id = "signal-1"
          Kind = FeatureRequest
          Summary = "Add another learning mechanic"
          Reproduction = None
          BuildVersion = "0.8.0"
          ContainsPersonalData = false }
    match Evolution.triage signal with
    | Candidate proposal ->
        Assert.True proposal.ProposalOnly
        Assert.Contains(HumanPromotion, proposal.Obligations)
        Assert.Contains(AccessibilityReview, proposal.Obligations)
        let requestedAt = DateTimeOffset(2026, 7, 27, 12, 0, 0, TimeSpan.Zero)
        let intent = EidosAdapter.toChangeIntent requestedAt proposal
        Assert.Equal(requestedAt, intent.RequestedAt)
        Assert.Equal(Medium, intent.Hazard.Highest)
        Assert.Contains("human promotion required", intent.Constraints)
        Assert.Contains("public-feedback", intent.Scope)
    | result -> failwithf "unexpected triage result: %A" result

[<Fact; Trait("spot", "spec-feedback-evolution-test-2")>]
let ``sensitive and security feedback never enters autonomous evolution`` () =
    let baseSignal =
        { Id = "signal-2"
          Kind = BugReport
          Summary = "A report"
          Reproduction = None
          BuildVersion = "0.8.0"
          ContainsPersonalData = true }
    Assert.Equal(RejectedSensitive, Evolution.triage baseSignal)
    Assert.Equal(
        EscalatedSecurity,
        Evolution.triage { baseSignal with Kind = SecurityOrPrivacy; ContainsPersonalData = false })

[<Fact; Trait("spot", "spec-research-snapshots-test-1")>]
let ``research snapshot workflow is versioned with the repository`` () =
    let workflow = Path.Combine(findRepoRoot (), ".github", "workflows", "research-snapshot.yml")
    Assert.True(File.Exists workflow)
    let content = File.ReadAllText workflow
    Assert.Contains("research-snapshot", content)
    Assert.Contains("draft: true", content)

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

// ===== Gate: das harte Grün-Gate (spec-gate-selbst-hart) =====

[<Fact>]
let ``Gate.parseTrx liest passed/failed aus TRX-Countern`` () =
    let xml = "<ResultSummary outcome=\"Completed\"><Counters total=\"5\" executed=\"5\" passed=\"5\" failed=\"0\" passedButRunAborted=\"0\" /></ResultSummary>"
    let r = Gate.parseTrx xml
    Assert.Equal(5, r.Passed)
    Assert.Equal(0, r.Failed)
    Assert.True(Gate.istGruen r)

[<Fact>]
let ``Gate.istGruen: rot und leer sind nicht gruen, nur passed>0 und failed=0`` () =
    Assert.False(Gate.istGruen { Gate.Passed = 3; Failed = 1; Skipped = 0 })
    Assert.False(Gate.istGruen { Gate.Passed = 0; Failed = 0; Skipped = 0 })   // „No test"
    Assert.True (Gate.istGruen { Gate.Passed = 3; Failed = 0; Skipped = 0 })

[<Fact; Trait("spot", "spec-gate-selbst-hart-test-1")>]
let ``Gate: failwith-TODO-Skelett (Marker da, Lauf rot) bleibt Pending — kein Aligned durch Marker-Erschleichung`` () =
    let spec = sampleSpec "spec-x" [ crit 1 ]
    let testNode =
        { Id = EntityId "spec-x-test-1"
          Convergence = Pending
          Payload = TestNode { SpecRef = EntityId "spec-x"; Name = "T — when w1 then t1"; Derived = true } }
    let covered = Set.singleton "spec-x-test-1"             // Marker IST präsent (failwith-Skelett)
    let rot   : Gate.TrxResult = { Passed = 0; Failed = 1; Skipped = 0 }
    let gruen : Gate.TrxResult = { Passed = 1; Failed = 0; Skipped = 0 }
    // Der Cheat: das alte Marker-Orakel würde fälschlich Aligned liefern.
    Assert.Equal(Aligned, (Sync.SetzeSpecAligned covered testNode).Convergence)
    // Das harte Gate hält es bei rot auf Pending …
    Assert.Equal(Pending, (Gate.setzeAlignedWennGruen rot covered testNode).Convergence)
    // … und promoviert erst bei echtem Grün.
    Assert.Equal(Aligned, (Gate.setzeAlignedWennGruen gruen covered testNode).Convergence)
    // gateGruen kombiniert echten Lauf + strukturelle Validierung.
    Assert.False(Gate.gateGruen rot [ spec; testNode ])
    Assert.True (Gate.gateGruen gruen [ spec; testNode ])

[<Fact>]
let ``Gate-Property: ein nicht-gruener Lauf macht einen Test-Knoten NIE Aligned`` () =
    let node =
        { Id = EntityId "spec-x-test-1"
          Convergence = Aligned    // selbst wenn er fälschlich Aligned WAR
          Payload = TestNode { SpecRef = EntityId "spec-x"; Name = "n"; Derived = true } }
    let prop (passed: int) (failed: int) (covered: bool) =
        let trx : Gate.TrxResult = { Passed = abs passed; Failed = abs failed; Skipped = 0 }
        let cov = if covered then Set.singleton "spec-x-test-1" else Set.empty
        FsCheck.Fluent.Prop.Implies(
            not (Gate.istGruen trx),
            (Gate.setzeAlignedWennGruen trx cov node).Convergence <> Aligned)
    Check.QuickThrowOnFailure prop

[<Fact>]
let ``Gate-Property: gruener Lauf + Marker => Aligned`` () =
    let node =
        { Id = EntityId "spec-x-test-1"
          Convergence = Pending
          Payload = TestNode { SpecRef = EntityId "spec-x"; Name = "n"; Derived = true } }
    let prop (p: int) =
        let trx : Gate.TrxResult = { Passed = (abs p % 64) + 1; Failed = 0; Skipped = 0 }
        (Gate.setzeAlignedWennGruen trx (Set.singleton "spec-x-test-1") node).Convergence = Aligned
    Check.QuickThrowOnFailure prop

// ===== EIDOS v0: epistemic claims, mission dispatch, evidence and ZT2 =====

let private eidosTime =
    DateTimeOffset(2026, 7, 24, 12, 0, 0, TimeSpan.Zero)

let private withEidosTemp (work: string -> unit) =
    let dir = Path.Combine(Path.GetTempPath(), "cdd-eidos-test-" + Guid.NewGuid().ToString("N"))
    Directory.CreateDirectory(dir) |> ignore
    try work dir
    finally
        if Directory.Exists dir then Directory.Delete(dir, true)

let private eidosFixtureClaim
    (value: string)
    (status: Eidos.EpistemicStatus)
    (signal: Eidos.Signal)
    : Eidos.Claim =
    let provenance : Eidos.Provenance =
        { SourceSignalIds = [ signal.Id ]
          Actor = "test-owner"
          Method = "fixture"
          ToolVersion = "1"
          RecordedAt = eidosTime }
    Eidos.createClaim status "report" "format" value "contract" (Some 1.0M) provenance

[<Fact; Trait("spot", "spec-eidos-epistemic-claims-test-1")>]
let ``EIDOS keeps raw signal claim provenance time scope and epistemic status separate`` () =
    let signal = Eidos.createSignal "owner" "contract" eidosTime "format v2 requested"
    let claim = eidosFixtureClaim "v2" Eidos.Ratified signal
    let twin = Eidos.projectTwin "v1" eidosTime [ "contract" ] [ signal ] [ claim ]
    Assert.Single twin.Signals |> ignore
    Assert.Single twin.Claims |> ignore
    Assert.Equal(signal.Content, twin.Signals.Head.Content)
    Assert.Equal(signal.ContentHash, Eidos.sha256 twin.Signals.Head.Content)
    Assert.Equal(Eidos.Ratified, twin.Claims.Head.Claim.Status)
    Assert.Equal(Eidos.Ratified, twin.Claims.Head.EffectiveStatus)
    Assert.Equal("contract", twin.Claims.Head.Claim.Scope)
    Assert.Equal<string list>([ signal.Id ], twin.Claims.Head.Claim.Provenance.SourceSignalIds)
    Assert.Equal(eidosTime, twin.Claims.Head.Claim.Provenance.RecordedAt)

[<Fact; Trait("spot", "spec-eidos-epistemic-claims-test-2")>]
let ``EIDOS projection exposes contradiction and unknown instead of flattening them`` () =
    let signal = Eidos.createSignal "owner" "contract" eidosTime "two incompatible declarations"
    let first = eidosFixtureClaim "v1" Eidos.Declared signal
    let second = eidosFixtureClaim "v2" Eidos.Declared signal
    let twin =
        Eidos.projectTwin "projection" eidosTime
            [ "contract"; "runtime" ] [ signal ] [ first; second ]
    Assert.Equal(2, twin.Claims.Length)
    Assert.All(twin.Claims, fun claim -> Assert.Equal(Eidos.Contested, claim.EffectiveStatus))
    Assert.Single twin.Conflicts |> ignore
    Assert.Contains("runtime", twin.UnknownScopes)
    Assert.Contains(twin.Findings, fun finding -> finding.StartsWith("Unknown scope:"))

[<Fact; Trait("spot", "spec-eidos-mission-order-test-1")>]
let ``EIDOS dispatch emits a complete typed ZT2 mission order`` () =
    withEidosTemp (fun dir ->
        let run = Eidos.runOpsLab dir eidosTime Eidos.NoFault
        let mission = run.Mission
        Assert.Equal(Eidos.ZT2, mission.TrustZone)
        Assert.NotEmpty mission.Situation
        Assert.NotEmpty mission.Intent.DesiredOutcome
        Assert.NotEmpty mission.Scope
        Assert.NotEmpty mission.Unit
        Assert.NotEmpty mission.Constraints
        Assert.NotEmpty mission.Obligations
        Assert.NotEmpty mission.Success
        Assert.NotEmpty mission.Abort
        Assert.Equal(run.Doctrine.Version, mission.DoctrineVersion))

[<Fact; Trait("spot", "spec-eidos-mission-order-test-2")>]
let ``EIDOS control plane fails closed on budget capability and policy violations`` () =
    withEidosTemp (fun dir ->
        let run = Eidos.runOpsLab dir eidosTime Eidos.NoFault
        match Eidos.controlCheck run.Doctrine run.Mission
                  (run.Mission.Budget.MaxDurationSeconds + 1) run.Mission.Unit [] with
        | Eidos.Abort reasons -> Assert.Contains(reasons, fun reason -> reason.Contains("budget"))
        | other -> failwithf "expected Abort, got %A" other
        match Eidos.controlCheck run.Doctrine run.Mission 1 [] [] with
        | Eidos.Abort reasons -> Assert.Contains(reasons, fun reason -> reason.Contains("capabilities"))
        | other -> failwithf "expected Abort, got %A" other
        match Eidos.controlCheck run.Doctrine run.Mission 1 run.Mission.Unit [ "policy denied" ] with
        | Eidos.Escalate(authority, reasons) ->
            Assert.Equal(run.Doctrine.EscalationAuthority, authority)
            Assert.Contains("policy denied", reasons)
        | other -> failwithf "expected Escalate, got %A" other)

[<Fact; Trait("spot", "spec-eidos-change-compiler-test-1")>]
let ``EIDOS change compilation is deterministic and includes complete candidate metadata`` () =
    withEidosTemp (fun firstDir ->
        withEidosTemp (fun secondDir ->
            let first = Eidos.runOpsLab firstDir eidosTime Eidos.NoFault
            let second = Eidos.runOpsLab secondDir eidosTime Eidos.NoFault
            Assert.Equal(first.Candidate.Id, second.Candidate.Id)
            Assert.Equal(first.Candidate.ArtifactHash, second.Candidate.ArtifactHash)
            Assert.Equal(first.Candidate.SemanticDelta, second.Candidate.SemanticDelta)
            Assert.NotEmpty first.Candidate.ArtifactChanges
            Assert.NotEmpty first.Candidate.AssuranceObligations
            Assert.NotEmpty first.Candidate.DeploymentPlan
            Assert.NotEmpty first.Candidate.RecoveryPlan))

[<Fact; Trait("spot", "spec-eidos-change-compiler-test-2")>]
let ``EIDOS retains rejected alternatives assumptions and a replayable compilation ledger`` () =
    withEidosTemp (fun dir ->
        let run = Eidos.runOpsLab dir eidosTime Eidos.NoFault
        Assert.Single run.Compilation.Candidates |> ignore
        Assert.Single run.Compilation.Rejected |> ignore
        Assert.Equal("required-owner-team", run.Compilation.Rejected.Head.Name)
        Assert.NotEmpty run.Candidate.Assumptions
        Assert.Equal<Eidos.RejectedAlternative list>(
            run.Compilation.Rejected,
            run.Candidate.RejectedAlternatives)
        Assert.True(Eidos.verifyLedger run.Compilation.Ledger)
        Assert.Contains(run.Compilation.Ledger, fun event -> event.Kind = "AlternativeRejected"))

[<Fact; Trait("spot", "spec-eidos-evidence-pack-test-1")>]
let ``EIDOS evidence pack binds gate tool policy environment time and artifact`` () =
    withEidosTemp (fun dir ->
        let run = Eidos.runOpsLab dir eidosTime Eidos.NoFault
        let pack = run.EvidencePack
        Assert.True(Eidos.verifyEvidencePack pack)
        Assert.Equal(run.Candidate.Id, pack.CandidateId)
        Assert.Equal(run.Candidate.ArtifactHash, pack.ArtifactHash)
        Assert.Equal(run.Candidate.PolicyVersion, pack.PolicyVersion)
        Assert.StartsWith("zt2:", pack.Environment)
        Assert.All(pack.Records, fun record ->
            Assert.NotEmpty record.ValidatorId
            Assert.NotEmpty record.ToolVersion
            Assert.Equal(pack.Environment, record.Environment)
            Assert.Equal(run.Candidate.ArtifactHash, record.ArtifactHash)
            Assert.Equal(run.Candidate.PolicyVersion, record.PolicyVersion)
            Assert.Equal(Eidos.sha256 record.Details, record.DetailsHash)))

[<Fact; Trait("spot", "spec-eidos-evidence-pack-test-2")>]
let ``EIDOS rejects missing stale red correlated and mismatched evidence with reasons`` () =
    let faults =
        [ Eidos.MissingEvidence
          Eidos.StaleEvidence
          Eidos.FailedGate
          Eidos.CorrelatedValidator
          Eidos.ArtifactMismatch
          Eidos.PolicyMismatch
          Eidos.TamperedPack ]
    for fault in faults do
        withEidosTemp (fun dir ->
            let run = Eidos.runOpsLab dir eidosTime fault
            Assert.Equal(Eidos.Rejected, run.Promotion.Status)
            Assert.NotEmpty run.Promotion.Reasons)

[<Fact; Trait("spot", "spec-eidos-zt2-opslab-test-1")>]
let ``EIDOS OpsLab promotes only into an isolated static credential-free ZT2 sandbox`` () =
    withEidosTemp (fun dir ->
        let run = Eidos.runOpsLab dir eidosTime Eidos.NoFault
        let sandbox = Path.Combine(dir, ".eidos", "runs", run.RunId, "sandbox")
        Assert.Equal(Eidos.RunPromoted, run.Status)
        Assert.True(Directory.Exists sandbox)
        let deployment = File.ReadAllText(Path.Combine(sandbox, "deployment.json"))
        Assert.Contains("\"network\": false", deployment)
        Assert.Contains("\"credentialsMounted\": false", deployment)
        Assert.Contains("\"productionAuthority\": false", deployment)
        Assert.Equal(0, run.Metrics.MechanicalHumanTouches)
        Assert.True(run.Metrics.ReplayVerified))

[<Fact; Trait("spot", "spec-eidos-zt2-opslab-test-2")>]
let ``EIDOS OpsLab gate failure leaves baseline unchanged and remains replayable`` () =
    withEidosTemp (fun dir ->
        let run = Eidos.runOpsLab dir eidosTime Eidos.FailedGate
        let runDir = Path.Combine(dir, ".eidos", "runs", run.RunId)
        Assert.Equal(Eidos.RunRejected, run.Status)
        Assert.False(Directory.Exists(Path.Combine(runDir, "sandbox")))
        Assert.True(run.Metrics.ReplayVerified)
        let replay = Eidos.replayOpsLab runDir
        Assert.True(replay.Verified)
        Assert.All(replay.Checks, fun (_, passed) -> Assert.True(passed)))

[<Fact>]
let ``EIDOS engineering benchmark is deterministic and reports its limited scope`` () =
    let first = Eidos.runBenchmark ()
    let second = Eidos.runBenchmark ()
    Assert.Equal(first, second)
    Assert.Equal(10, first.Eidos.Correct)
    Assert.Equal(0, first.Eidos.UnsafeApprovals)
    Assert.Equal(2, first.LinearBaseline.Correct)
    Assert.Equal(8, first.LinearBaseline.UnsafeApprovals)
    Assert.Contains("not external validity", first.ScopeNote)

[<Fact>]
let ``sync scanRepo includes public example projects`` () =
    let tmp = Path.Combine(Path.GetTempPath(), "cdd-roots-" + Guid.NewGuid().ToString("N"))
    try
        let example = Path.Combine(tmp, "examples", "Reference.Core")
        Directory.CreateDirectory example |> ignore
        File.WriteAllText(Path.Combine(example, "Reference.Core.fsproj"), "<Project></Project>")
        let projects = Sync.scanRepo tmp
        Assert.Contains(projects, fun project -> project.Name = "Reference.Core")
    finally
        if Directory.Exists tmp then Directory.Delete(tmp, true)

[<Fact; Trait("spot", "spec-public-runtime-boundary-test-1")>]
let ``public runtime boundary is fail closed by default`` () =
    let policy =
        PublicRuntimeBoundary.fromEnvironment (fun _ -> "")
    let allowed methodName path =
        PublicRuntimeBoundary.classify methodName path
        |> PublicRuntimeBoundary.isAllowed policy

    Assert.True(allowed "GET" "/api/spot")
    Assert.True(allowed "GET" "/research/")
    Assert.False(allowed "POST" "/api/engine/run")
    Assert.False(allowed "GET" "/api/providers")
    Assert.False(allowed "GET" "/api/dwh/search")
    Assert.False(allowed "GET" "/api/infra/status")
    Assert.False(allowed "GET" "/api/studio/workspaces")

[<Fact; Trait("spot", "spec-public-runtime-boundary-test-2")>]
let ``memory writes require both explicit capabilities`` () =
    let classify methodName = PublicRuntimeBoundary.classify methodName "/api/dwh/index"
    let memoryOnly : PublicRuntimeBoundary.Policy =
        { AllowMutations = false
          EnableMemory = true
          EnableInfra = false
          EnableWorkspaces = false }
    let mutationOnly : PublicRuntimeBoundary.Policy =
        { AllowMutations = true
          EnableMemory = false
          EnableInfra = false
          EnableWorkspaces = false }
    let both = { memoryOnly with AllowMutations = true }

    Assert.False(PublicRuntimeBoundary.isAllowed memoryOnly (classify "POST"))
    Assert.False(PublicRuntimeBoundary.isAllowed mutationOnly (classify "POST"))
    Assert.True(PublicRuntimeBoundary.isAllowed both (classify "POST"))
    Assert.True(PublicRuntimeBoundary.isAllowed memoryOnly (classify "GET"))

[<Fact; Trait("spot", "spec-public-runtime-boundary-test-3")>]
let ``live workspace observations require an explicit independent capability`` () =
    let defaultPolicy = PublicRuntimeBoundary.fromEnvironment (fun _ -> "")
    let enabledPolicy =
        PublicRuntimeBoundary.fromEnvironment (fun key ->
            if key = "CDD_ENABLE_WORKSPACES" then "true" else "")
    let capability = PublicRuntimeBoundary.classify "GET" "/api/studio/workspaces"
    Assert.False(PublicRuntimeBoundary.isAllowed defaultPolicy capability)
    Assert.True(PublicRuntimeBoundary.isAllowed enabledPolicy capability)

[<Fact; Trait("spot", "spec-risk-adaptive-assurance-portfolio-test-1")>]
let ``assurance portfolio selects complementary formal and operational oracles by risk`` () =
    let profile : Studio.MissionProfile =
        { ConcurrentOrDistributed = true
          RelationshipHeavy = true
          HighIntegrity = true
          SecuritySensitive = true
          ProductionChange = true
          RuntimeBehavior = true
          CreativeOrNormative = false }
    let recommendations = Studio.recommendAssurance profile
    let recommendation id = recommendations |> List.find (fun item -> item.Tool.Id = id)
    Assert.True((recommendation "assurance-alloy").Required)
    Assert.True((recommendation "assurance-tla").Required)
    Assert.True((recommendation "assurance-opa").Required)
    Assert.True((recommendation "assurance-slsa").Required)
    Assert.True((recommendation "assurance-runtime").Required)
    Assert.False((recommendation "assurance-lean").Required)

[<Fact; Trait("spot", "spec-risk-adaptive-assurance-portfolio-test-2")>]
let ``normative missions retain named human authority without forcing unrelated formalisms`` () =
    let profile : Studio.MissionProfile =
        { ConcurrentOrDistributed = false
          RelationshipHeavy = false
          HighIntegrity = false
          SecuritySensitive = false
          ProductionChange = false
          RuntimeBehavior = false
          CreativeOrNormative = true }
    let recommendations = Studio.recommendAssurance profile
    Assert.Contains(recommendations, fun item -> item.Tool.Id = "assurance-human" && item.Required)
    Assert.DoesNotContain(recommendations, fun item -> item.Tool.Id = "assurance-tla")
    Assert.DoesNotContain(recommendations, fun item -> item.Tool.Id = "assurance-lean")

[<Fact; Trait("spot", "spec-studio-workspace-control-plane-test-1")>]
let ``workspace projection prioritizes live work and derives actual lifecycle state`` () =
    let git : Studio.GitObservation =
        { Available = true; Branch = "main"; Commit = "abc"; CommitTitle = "candidate"
          CommittedAt = "2026-08-23T00:00:00Z"; Remote = "https://example.invalid/repo"
          DirtyFiles = 0; Ahead = 0; Behind = 0 }
    let item id status : Studio.WorkItemObservation =
        { Id = id; Title = id; Status = status; Objective = "objective"; RequiredGates = [] }
    let run id status summary : Studio.RunObservation =
        { Id = id; Status = status; StartedAt = id; FinishedAt = None; HasSummary = summary }
    let observation : Studio.WorkspaceObservation =
        { Id = "project"; Name = "Project"; Git = git
          WorkItems = [ item "T-3" "ready"; item "T-2" "blocked"; item "T-1" "running" ]
          Runs = [ run "2026-01" "succeeded" true; run "2026-02" "running" false ]
          AgenticRuns = []
          SpotNodes = 42; Sources = [ "git"; "git"; ".ai/tasks/*.json" ]
          ObservedAt = DateTimeOffset.Parse "2026-08-23T00:00:00Z" }
    let snapshot = Studio.projectWorkspace observation
    Assert.Equal(Studio.Blocked, snapshot.State)
    Assert.Equal(Some "T-1", snapshot.ActiveMission |> Option.map (fun mission -> mission.Id))
    Assert.Equal(1, snapshot.WorkItems.Ready)
    Assert.Equal(1, snapshot.Runs.Running)
    Assert.Equal(1, snapshot.Runs.WithSummary)
    Assert.Equal<string list>([ ".ai/tasks/*.json"; "git" ], snapshot.Sources)

// ===== Full-agentic SDLC controller: slicing, recovery, gates and review =====

let private autopilotTime =
    DateTimeOffset(2026, 8, 23, 12, 0, 0, TimeSpan.Zero)

let private loopGuardPolicy : Autopilot.LoopGuardPolicy =
    { MaxProductAttempts = 2
      MaxInfrastructureAttempts = 2
      MaxSameSessionResumes = 1
      InfrastructureBackoffSeconds = 30 }

let private loopFailureKey subject stage code : Autopilot.LoopFailureKey =
    { RunId = "run-loop"
      SliceId = "slice-loop"
      SubjectDigest = subject
      Stage = stage
      FailureCode = code }

let private createLoopGuard subject =
    match Autopilot.createLoopGuardState subject with
    | Ok state -> state
    | Error errors -> failwith (String.concat "; " errors)

[<Fact; Trait("spot", "spec-loop-engineering-guard-test-1")>]
let ``loop guard keeps administrative holds model free until exact release`` () =
    let hold : Autopilot.AdministrativeHold =
        { HoldId = "release-maintenance"
          Authority = "release-owner"
          Reason = "Guarded maintenance window"
          StartedAtUtc = autopilotTime }
    let initial = createLoopGuard "candidate-a"
    let held =
        match Autopilot.placeAdministrativeHold hold initial with
        | Ok state -> state
        | Error errors -> failwith (String.concat "; " errors)
    let key = loopFailureKey "candidate-a" Autopilot.PromotionStage "resource-busy"
    let afterTicks =
        [ 1..100 ]
        |> List.fold (fun state _ ->
            match Autopilot.observeLoopFailure loopGuardPolicy key Autopilot.HarnessInfrastructureFailure state with
            | Ok(next, Autopilot.WaitWithoutModel(Autopilot.AdministrativeHoldWait "release-maintenance")) ->
                Assert.Equal(state, next)
                next
            | result -> failwithf "Unexpected held-loop result: %A" result) held
    Assert.Empty afterTicks.Failures
    Assert.True(Autopilot.releaseAdministrativeHold "release-maintenance" "wrong-owner" afterTicks |> Result.isError)
    let released =
        match Autopilot.releaseAdministrativeHold "release-maintenance" "release-owner" afterTicks with
        | Ok state -> state
        | Error errors -> failwith (String.concat "; " errors)
    Assert.Equal(Ok Autopilot.Proceed, Autopilot.nextLoopDisposition loopGuardPolicy released)

[<Fact; Trait("spot", "spec-loop-engineering-guard-test-2")>]
let ``loop guard isolates promotion infrastructure failures from reviewer success`` () =
    let key = loopFailureKey "candidate-a" Autopilot.PromotionStage "promotion-lock-busy"
    let firstState =
        match Autopilot.observeLoopFailure loopGuardPolicy key Autopilot.HarnessInfrastructureFailure (createLoopGuard "candidate-a") with
        | Ok(state, Autopilot.WaitWithoutModel(Autopilot.InfrastructureBackoff(observed, 30))) ->
            Assert.Equal(key, observed)
            state
        | result -> failwithf "Unexpected first infrastructure result: %A" result
    let afterReview =
        match Autopilot.observeLoopStageSucceeded "candidate-a" Autopilot.ReviewStage firstState with
        | Ok state -> state
        | Error errors -> failwith (String.concat "; " errors)
    let secondState =
        match Autopilot.observeLoopFailure loopGuardPolicy key Autopilot.HarnessInfrastructureFailure afterReview with
        | Ok(state, Autopilot.WaitWithoutModel(Autopilot.InfrastructureBackoff(_, 60))) -> state
        | result -> failwithf "Unexpected second infrastructure result: %A" result
    Assert.Equal(2, secondState.Failures |> List.exactlyOne |> fun counter -> counter.Attempts)
    match Autopilot.observeLoopFailure loopGuardPolicy key Autopilot.HarnessInfrastructureFailure secondState with
    | Ok(_, Autopilot.CircuitOpen(observed, 3)) -> Assert.Equal(key, observed)
    | result -> failwithf "Expected infrastructure circuit: %A" result

[<Fact; Trait("spot", "spec-loop-engineering-guard-test-3")>]
let ``loop guard bounds protocol and product recovery by exact cause`` () =
    let protocol = loopFailureKey "candidate-a" Autopilot.ReviewStage "missing-terminal"
    let product = loopFailureKey "candidate-a" Autopilot.MutationStage "test-failed"
    let initial = createLoopGuard "candidate-a"
    let protocolOnce =
        match Autopilot.observeLoopFailure loopGuardPolicy protocol Autopilot.AgentProtocolFailure initial with
        | Ok(state, Autopilot.ResumeBoundSession(_, 1)) -> state
        | result -> failwithf "Expected one bound resume: %A" result
    match Autopilot.observeLoopFailure loopGuardPolicy protocol Autopilot.AgentProtocolFailure protocolOnce with
    | Ok(_, Autopilot.CircuitOpen(_, 2)) -> ()
    | result -> failwithf "Expected protocol circuit: %A" result
    let productOnce =
        match Autopilot.observeLoopFailure loopGuardPolicy product Autopilot.AgentProductFailure initial with
        | Ok(state, Autopilot.RetryWithFreshAgent(_, 1)) -> state
        | result -> failwithf "Expected product retry: %A" result
    let productTwice =
        match Autopilot.observeLoopFailure loopGuardPolicy product Autopilot.AgentProductFailure productOnce with
        | Ok(state, Autopilot.RetryWithFreshAgent(_, 2)) -> state
        | result -> failwithf "Expected second product retry: %A" result
    match Autopilot.observeLoopFailure loopGuardPolicy product Autopilot.AgentProductFailure productTwice with
    | Ok(circuitState, Autopilot.CircuitOpen(_, 3)) ->
        match Autopilot.observeLoopFailure loopGuardPolicy product Autopilot.AgentProductFailure circuitState with
        | Ok(replayed, Autopilot.CircuitOpen(_, 3)) -> Assert.Equal(circuitState, replayed)
        | result -> failwithf "Expected idempotent open product circuit: %A" result
    | result -> failwithf "Expected product circuit: %A" result

    let unboundedPolicy =
        { loopGuardPolicy with
            MaxInfrastructureAttempts = 1000
            InfrastructureBackoffSeconds = Int32.MaxValue }
    match Autopilot.observeLoopFailure unboundedPolicy product Autopilot.AgentProductFailure initial with
    | Error errors -> Assert.Contains(errors, fun error -> error.Contains("one-day hard safety bound"))
    | Ok _ -> failwith "unbounded backoff arithmetic must fail closed"

    let forgedPersisted =
        { initial with
            Failures =
              [ { Key = product
                  FailureClass = Autopilot.AgentProductFailure
                  Attempts = Int32.MaxValue } ] }
    Assert.NotEmpty(Autopilot.validateLoopGuardState forgedPersisted)
    Assert.True(Autopilot.nextLoopDisposition loopGuardPolicy forgedPersisted |> Result.isError)

    let zeroCounter =
        { initial with
            Failures =
              [ { Key = product
                  FailureClass = Autopilot.AgentProductFailure
                  Attempts = 0 } ] }
    Assert.Contains(Autopilot.validateLoopGuardState zeroCounter, fun error -> error.Contains("at least one attempt"))
    let duplicateCounter =
        { productTwice with Failures = productTwice.Failures @ productTwice.Failures }
    Assert.Contains(Autopilot.validateLoopGuardState duplicateCounter, fun error -> error.Contains("duplicate failure key"))

    let nonCanonicalCounters =
        { initial with
            Failures =
              [ { Key = loopFailureKey "candidate-a" Autopilot.MutationStage "z-failure"
                  FailureClass = Autopilot.AgentProductFailure
                  Attempts = 1 }
                { Key = loopFailureKey "candidate-a" Autopilot.MutationStage "a-failure"
                  FailureClass = Autopilot.AgentProductFailure
                  Attempts = 1 } ] }
    Assert.Contains(
        Autopilot.validateLoopGuardState nonCanonicalCounters,
        fun error -> error.Contains("canonical order"))

[<Fact; Trait("spot", "spec-loop-engineering-guard-test-4")>]
let ``loop guard subject advance is deterministic and keeps authority holds`` () =
    let hold : Autopilot.AdministrativeHold =
        { HoldId = "operator-hold"
          Authority = "operator"
          Reason = "Reviewing publication policy"
          StartedAtUtc = autopilotTime }
    let failed =
        match Autopilot.observeLoopFailure
                  loopGuardPolicy
                  (loopFailureKey "candidate-a" Autopilot.PublicationStage "transport-error")
                  Autopilot.HarnessInfrastructureFailure
                  (createLoopGuard "candidate-a") with
        | Ok(state, _) -> state
        | Error errors -> failwith (String.concat "; " errors)
    let held =
        match Autopilot.placeAdministrativeHold hold failed with
        | Ok state -> state
        | Error errors -> failwith (String.concat "; " errors)
    let advanced =
        match Autopilot.advanceLoopSubject "candidate-b" held with
        | Ok state -> state
        | Error errors -> failwith (String.concat "; " errors)
    Assert.Empty advanced.Failures
    Assert.Equal(Some hold, advanced.AdministrativeHold)
    let restored = advanced |> Json.serialize |> Json.deserialize<Autopilot.LoopGuardState>
    Assert.Equal(advanced, restored)
    Assert.Equal(
        Autopilot.nextLoopDisposition loopGuardPolicy advanced,
        Autopilot.nextLoopDisposition loopGuardPolicy restored)

let private autopilotPlan recovery : Autopilot.RunPlan =
    let worker id role readOnly : Autopilot.WorkerProfile =
        { Id = id
          Role = role
          Provider = "test-provider"
          Model = "test-model"
          Harness = "test-harness/v1"
          ReadOnly = readOnly
          Capabilities = if readOnly then [ "repo.read" ] else [ "repo.read"; "artifact.write" ] }
    { MissionId = "mission-agentic-sdlc"
      Objective = "Deliver a reviewed and evidenced change."
      Slices =
        [ { Id = "slice-1"
            Stage = Autopilot.Implement
            Title = "Bounded implementation"
            Objective = "Implement one behavior."
            Scope = [ "src/Feature.fs"; "tests/FeatureTests.fs" ]
            AcceptanceCriteria = [ "Behavior is deterministic." ]
            RequiredGateIds = [ "gate-tests" ] } ]
      Workers =
        [ worker "scout-a" Autopilot.Scout true
          worker "builder-a" Autopilot.Builder false
          worker "critic-b" Autopilot.Critic true
          worker "reviewer-c" Autopilot.Reviewer true ]
      Gates =
        [ { Id = "gate-tests"
            Name = "Deterministic tests"
            Program = "dotnet"
            Arguments = [ "test"; "--no-restore" ]
            ValidatorId = "dotnet-test-oracle"
            TimeoutSeconds = 120 } ]
      Recovery = recovery }

let private autopilotTelemetry tools : Autopilot.AgentTelemetry =
    { DurationMilliseconds = 10L
      ToolCalls = tools
      InputTokens = 20L
      OutputTokens = 5L }

let private observeAgent role worker session mode terminal subject output findings =
    Autopilot.AgentTurnObserved
        { WorkerId = worker
          Role = role
          SessionId = session
          DispatchMode = mode
          TerminalMarker = terminal
          Summary = "test observation"
          SubjectDigest = subject
          OutputDigest = output
          Findings = findings
          Telemetry = autopilotTelemetry 2 }

let private applyAutopilot offset observation run =
    match Autopilot.applyObservation (autopilotTime.AddSeconds(float offset)) observation run with
    | Ok updated -> updated
    | Error errors -> failwith (String.concat " | " errors)

let private createAutopilot recovery =
    match Autopilot.create autopilotTime (autopilotPlan recovery) with
    | Ok run -> run
    | Error errors -> failwith (String.concat " | " errors)

[<Fact; Trait("spot", "spec-full-agentic-sdlc-controller-test-1")>]
let ``autopilot executes every slice through separated roles gates review and checkpoint`` () =
    let policy : Autopilot.RecoveryPolicy =
        { MaxSameSessionResumes = 2; MaxFreshStarts = 2; MaxRepairCycles = 2 }
    let initial = createAutopilot policy
    match Autopilot.nextAction initial with
    | Autopilot.DispatchAgent action ->
        Assert.Equal(Autopilot.Scout, action.Worker.Role)
        Assert.Equal(Autopilot.FreshSession, action.Mode)
        Assert.Equal<string list>([ "src/Feature.fs"; "tests/FeatureTests.fs" ], action.Context.Scope)
    | action -> failwithf "expected scout, got %A" action

    let scouted =
        applyAutopilot 1
            (observeAgent Autopilot.Scout "scout-a" "scout-session" Autopilot.FreshSession
                (Some Autopilot.WorkCompleted) None (Some "context-1") []) initial
    let built =
        applyAutopilot 2
            (observeAgent Autopilot.Builder "builder-a" "build-session" Autopilot.FreshSession
                (Some Autopilot.WorkCompleted) None (Some "candidate-1") []) scouted
    match Autopilot.nextAction built with
    | Autopilot.ExecuteGate action ->
        Assert.Equal("gate-tests", action.Gate.Id)
        Assert.Equal("candidate-1", action.CandidateDigest)
    | action -> failwithf "expected gate, got %A" action

    let gated =
        applyAutopilot 3
            (Autopilot.GateObserved
                { GateId = "gate-tests"; ValidatorId = "dotnet-test-oracle"
                  CandidateDigest = "candidate-1"; Passed = true; ExitCode = 0
                  EvidenceDigest = "evidence-1"; DurationMilliseconds = 30L; Detail = "green" }) built
    let critiqued =
        applyAutopilot 4
            (observeAgent Autopilot.Critic "critic-b" "critic-session" Autopilot.FreshSession
                (Some Autopilot.WorkCompleted) (Some "candidate-1") None []) gated
    let reviewed =
        applyAutopilot 5
            (observeAgent Autopilot.Reviewer "reviewer-c" "review-session" Autopilot.FreshSession
                (Some Autopilot.WorkCompleted) (Some "candidate-1") None []) critiqued
    let completed =
        applyAutopilot 6
            (Autopilot.CheckpointObserved
                { SliceId = "slice-1"; CandidateDigest = "candidate-1"; Succeeded = true
                  CommitHash = "abc123"; CleanWorktree = true; Detail = "checkpointed" }) reviewed

    Assert.Equal(Autopilot.Completed, completed.Status)
    Assert.True(Autopilot.verifyLedger completed.Ledger)
    let evaluation = Autopilot.evaluate completed
    Assert.True(evaluation.FullSolve)
    Assert.Equal(1, evaluation.CompletedSlices)
    Assert.Equal(4, evaluation.AgentTurns)
    Assert.Equal(1, evaluation.GateRuns)

[<Fact; Trait("spot", "spec-full-agentic-sdlc-controller-test-2")>]
let ``autopilot resumes premature stops then starts fresh and finally blocks`` () =
    let policy : Autopilot.RecoveryPolicy =
        { MaxSameSessionResumes = 1; MaxFreshStarts = 2; MaxRepairCycles = 1 }
    let initial = createAutopilot policy
    let interrupted session mode (run: Autopilot.RunState) =
        applyAutopilot (run.Ledger.Length + 1)
            (observeAgent Autopilot.Scout "scout-a" session mode None None None []) run

    let first = interrupted "session-1" Autopilot.FreshSession initial
    match Autopilot.nextAction first with
    | Autopilot.DispatchAgent action -> Assert.Equal(Autopilot.ResumeSession "session-1", action.Mode)
    | action -> failwithf "expected resume, got %A" action
    let resumed = interrupted "session-1" (Autopilot.ResumeSession "session-1") first
    match Autopilot.nextAction resumed with
    | Autopilot.DispatchAgent action -> Assert.Equal(Autopilot.FreshSession, action.Mode)
    | action -> failwithf "expected fresh recovery, got %A" action
    let fresh = interrupted "session-2" Autopilot.FreshSession resumed
    let blocked = interrupted "session-2" (Autopilot.ResumeSession "session-2") fresh

    Assert.Equal(Autopilot.Blocked, blocked.Status)
    Assert.Equal(4, blocked.Metrics.PrematureStops)
    Assert.Equal(2, blocked.Metrics.SameSessionResumes)
    Assert.Equal(2, blocked.Metrics.FreshStarts)
    match Autopilot.nextAction blocked with
    | Autopilot.Escalate reasons -> Assert.Contains(reasons, fun reason -> reason.Contains("recovery budget"))
    | action -> failwithf "expected escalation, got %A" action

[<Fact; Trait("spot", "spec-full-agentic-sdlc-controller-test-3")>]
let ``autopilot fails closed on correlated roles and repairs failed gates`` () =
    let policy : Autopilot.RecoveryPolicy =
        { MaxSameSessionResumes = 1; MaxFreshStarts = 2; MaxRepairCycles = 1 }
    let invalid =
        let plan = autopilotPlan policy
        { plan with
            Workers =
                plan.Workers
                |> List.map (fun worker ->
                    if worker.Role = Autopilot.Reviewer then
                        { worker with Id = "builder-a"; ReadOnly = false }
                    else worker) }
    let errors = Autopilot.validatePlan invalid
    Assert.Contains(errors, fun error -> error.Contains("Duplicate worker"))
    Assert.Contains(errors, fun error -> error.Contains("read-only"))
    Assert.Contains(errors, fun error -> error.Contains("identities"))
    let timeoutErrors =
        let plan = autopilotPlan policy
        { plan with Gates = plan.Gates |> List.map (fun gate -> { gate with TimeoutSeconds = 86_401 }) }
        |> Autopilot.validatePlan
    Assert.Contains(timeoutErrors, fun error -> error.Contains("24-hour"))

    let initial = createAutopilot policy
    let scouted =
        applyAutopilot 1
            (observeAgent Autopilot.Scout "scout-a" "s" Autopilot.FreshSession
                (Some Autopilot.WorkCompleted) None (Some "context") []) initial
    let built =
        applyAutopilot 2
            (observeAgent Autopilot.Builder "builder-a" "b" Autopilot.FreshSession
                (Some Autopilot.WorkCompleted) None (Some "candidate-old") []) scouted
    let repairing =
        applyAutopilot 3
            (Autopilot.GateObserved
                { GateId = "gate-tests"; ValidatorId = "dotnet-test-oracle"
                  CandidateDigest = "candidate-old"; Passed = false; ExitCode = 1
                  EvidenceDigest = "red-evidence"; DurationMilliseconds = 20L; Detail = "test failed" }) built
    match Autopilot.nextAction repairing with
    | Autopilot.DispatchAgent action ->
        Assert.Equal(Autopilot.Builder, action.Worker.Role)
        Assert.Contains("test failed", action.Context.OpenFindings)
    | action -> failwithf "expected repair, got %A" action
    let repaired =
        applyAutopilot 4
            (observeAgent Autopilot.Builder "builder-a" "repair" Autopilot.FreshSession
                (Some Autopilot.WorkCompleted) None (Some "candidate-new") []) repairing
    let stale =
        Autopilot.applyObservation (autopilotTime.AddSeconds 5.0)
            (Autopilot.GateObserved
                { GateId = "gate-tests"; ValidatorId = "dotnet-test-oracle"
                  CandidateDigest = "candidate-old"; Passed = true; ExitCode = 0
                  EvidenceDigest = "stale"; DurationMilliseconds = 1L; Detail = "" }) repaired
    match stale with
    | Error errors -> Assert.Contains(errors, fun error -> error.Contains("stale"))
    | Ok _ -> failwith "stale evidence must not mutate the run"

[<Fact; Trait("spot", "spec-full-agentic-sdlc-controller-test-4")>]
let ``autopilot state is persisted atomically and exposes reproducible evaluation`` () =
    let directory = Path.Combine(Path.GetTempPath(), "cdd-autopilot-test-" + Guid.NewGuid().ToString("N"))
    try
        let policy : Autopilot.RecoveryPolicy =
            { MaxSameSessionResumes = 1; MaxFreshStarts = 2; MaxRepairCycles = 1 }
        match Autopilot.initialize directory autopilotTime (autopilotPlan policy) with
        | Error errors -> failwith (String.concat " | " errors)
        | Ok(runDirectory, created) ->
            Assert.True(File.Exists(Path.Combine(runDirectory, "state.json")))
            Assert.True(File.Exists(Path.Combine(runDirectory, "run.json")))
            match Autopilot.load runDirectory with
            | Error errors -> failwith (String.concat " | " errors)
            | Ok loaded ->
                Assert.Equal(created.RunId, loaded.RunId)
                Assert.Equal(Autopilot.nextAction created, Autopilot.nextAction loaded)
                Assert.Equal(Autopilot.evaluate created, Autopilot.evaluate loaded)
    finally
        if Directory.Exists directory then Directory.Delete(directory, true)

[<Fact>]
let ``Studio projects agentic progress without exposing slice scope or prompts`` () =
    let policy : Autopilot.RecoveryPolicy =
        { MaxSameSessionResumes = 1; MaxFreshStarts = 2; MaxRepairCycles = 1 }
    let run = createAutopilot policy
    let projection = Studio.projectAgenticRun run
    Assert.Equal(run.RunId, projection.Id)
    Assert.Equal("Scouting", projection.Phase)
    Assert.Equal("DispatchAgent", projection.NextAction)
    Assert.Equal(Some "Scout", projection.CurrentRole)
    Assert.Equal(Some "test-model", projection.Model)
    let publicJson = Json.serialize projection
    Assert.DoesNotContain("src/Feature.fs", publicJson)
    Assert.DoesNotContain("Inspect only the declared scope", publicJson)

let private leaseIdentity attempt owner worktree : Autopilot.SliceLeaseIdentity =
    { RunId = "run-swarm"
      MissionId = "mission-swarm"
      SliceId = "slice-core"
      Attempt = attempt
      OwnerId = owner
      WorktreeId = worktree }

let private leaseRequest attempt owner worktree scope expires : Autopilot.SliceLeaseRequest =
    { Identity = leaseIdentity attempt owner worktree
      BaseDigest = "base-abc"
      CandidateDigest = None
      Scope = scope
      ExpiresAtUtc = expires }

let private leaseSubject (lease: Autopilot.SliceLease) : Autopilot.SliceLeaseSubject =
    { Identity = lease.Identity
      BaseDigest = lease.BaseDigest
      CandidateDigest = lease.CandidateDigest
      Scope = lease.Scope }

let private acquireLease at history request =
    match Autopilot.acquireSliceLease at history request with
    | Ok lease -> lease
    | Error errors -> failwith (String.concat " | " errors)

[<Fact; Trait("spot", "spec-slice-worktree-lease-test-1")>]
let ``slice leases reject overlapping live ownership and require monotonic attempts`` () =
    let first =
        leaseRequest 1 "builder-a" "worktree-a" [ "src/Feature" ] (autopilotTime.AddMinutes 10.0)
        |> acquireLease autopilotTime []
    Assert.Equal<string list>([ "src/Feature" ], first.Scope)
    let replayed =
        leaseRequest 1 "builder-a" "worktree-a" [ "src/Feature" ] (autopilotTime.AddMinutes 10.0)
        |> acquireLease (autopilotTime.AddMinutes 1.0) [ first ]
    Assert.Equal(first, replayed)

    let conflict =
        leaseRequest 1 "builder-b" "worktree-b" [ "src/Feature/File.fs" ] (autopilotTime.AddMinutes 10.0)
        |> Autopilot.acquireSliceLease (autopilotTime.AddMinutes 1.0) [ first ]
    match conflict with
    | Error errors -> Assert.Contains(errors, fun error -> error.Contains("conflicts with live attempt"))
    | Ok _ -> failwith "overlapping live scope must not acquire a second lease"

    let disjointScopeSameSlice =
        leaseRequest 2 "builder-b" "worktree-b" [ "tests/FeatureTests.fs" ] (autopilotTime.AddMinutes 10.0)
        |> Autopilot.acquireSliceLease (autopilotTime.AddMinutes 1.0) [ first ]
    match disjointScopeSameSlice with
    | Error errors -> Assert.Contains(errors, fun error -> error.Contains("already has a live lease"))
    | Ok _ -> failwith "one slice must never have two simultaneous owners"

    let staleAttempt =
        leaseRequest 1 "builder-b" "worktree-b" [ "src/Feature" ] (autopilotTime.AddMinutes 30.0)
        |> Autopilot.acquireSliceLease (autopilotTime.AddMinutes 11.0) [ first ]
    match staleAttempt with
    | Error errors -> Assert.Contains(errors, fun error -> error.Contains("Expected lease attempt 2"))
    | Ok _ -> failwith "an expired attempt must not be silently revived"

    let second =
        leaseRequest 2 "builder-b" "worktree-b" [ "src/Feature" ] (autopilotTime.AddMinutes 30.0)
        |> acquireLease (autopilotTime.AddMinutes 11.0) [ first ]
    Assert.Equal(2, second.Identity.Attempt)
    Assert.Equal("builder-b", second.Identity.OwnerId)

[<Fact; Trait("spot", "spec-slice-worktree-lease-test-2")>]
let ``slice lease heartbeat extends only the exact live subject`` () =
    let lease =
        leaseRequest 1 "builder-a" "worktree-a" [ "src/Feature.fs" ] (autopilotTime.AddMinutes 10.0)
        |> acquireLease autopilotTime []
    let renewed =
        match Autopilot.heartbeatSliceLease
                  (autopilotTime.AddMinutes 5.0)
                  (autopilotTime.AddMinutes 20.0)
                  (leaseSubject lease)
                  lease with
        | Ok value -> value
        | Error errors -> failwith (String.concat " | " errors)
    Assert.Equal(autopilotTime.AddMinutes 5.0, renewed.HeartbeatAtUtc)
    Assert.Equal(autopilotTime.AddMinutes 20.0, renewed.ExpiresAtUtc)

    match Autopilot.heartbeatSliceLease
              renewed.HeartbeatAtUtc
              (autopilotTime.AddMinutes 30.0)
              (leaseSubject renewed)
              renewed with
    | Error errors -> Assert.Contains(errors, fun error -> error.Contains("advance monotonically"))
    | Ok _ -> failwith "heartbeat timestamps must strictly advance"

    let conflictingSubject =
        { (leaseSubject renewed) with
            Identity = { renewed.Identity with OwnerId = "builder-b" } }
    match Autopilot.heartbeatSliceLease
              (autopilotTime.AddMinutes 6.0)
              (autopilotTime.AddMinutes 30.0)
              conflictingSubject
              renewed with
    | Error errors -> Assert.Contains(errors, fun error -> error.Contains("identity is stale"))
    | Ok _ -> failwith "a different owner must not renew the lease"

    match Autopilot.heartbeatSliceLease
              (autopilotTime.AddMinutes 21.0)
              (autopilotTime.AddMinutes 30.0)
              (leaseSubject renewed)
              renewed with
    | Error errors -> Assert.Contains(errors, fun error -> error.Contains("expired"))
    | Ok _ -> failwith "an expired lease must not be revived by heartbeat"

[<Fact; Trait("spot", "spec-slice-worktree-lease-test-3")>]
let ``candidate binding rejects stale digests scope drift and unsafe scope paths`` () =
    let lease =
        leaseRequest 1 "builder-a" "worktree-a" [ "src/Feature.fs" ] (autopilotTime.AddMinutes 10.0)
        |> acquireLease autopilotTime []
    let bound =
        match Autopilot.bindSliceLeaseCandidate
                  (autopilotTime.AddMinutes 1.0)
                  (leaseSubject lease)
                  "candidate-one"
                  lease with
        | Ok value -> value
        | Error errors -> failwith (String.concat " | " errors)
    Assert.Equal(Some "candidate-one", bound.CandidateDigest)
    let roundtrip = Json.deserialize<Autopilot.SliceLease> (Json.serialize bound)
    Assert.Equal(bound, roundtrip)

    match Autopilot.bindSliceLeaseCandidate
              (autopilotTime.AddMinutes 2.0)
              (leaseSubject bound)
              "candidate-one"
              bound with
    | Error errors -> Assert.Contains(errors, fun error -> error.Contains("advance to a new value"))
    | Ok _ -> failwith "rebinding the same candidate must not create an invalid history version"

    match Autopilot.verifySliceLease
              (autopilotTime.AddMinutes 2.0)
              (leaseSubject lease)
              bound with
    | errors when errors |> List.exists (fun error -> error.Contains("candidate digest is stale")) -> ()
    | errors -> failwithf "expected stale candidate rejection, got %A" errors

    let drifted = { (leaseSubject bound) with Scope = [ "tests/FeatureTests.fs" ] }
    Assert.Contains(
        Autopilot.verifySliceLease (autopilotTime.AddMinutes 2.0) drifted bound,
        fun error -> error.Contains("scope is stale"))

    let unsafeRequest =
        leaseRequest 1 "builder-a" "worktree-a" [ "../outside" ] (autopilotTime.AddMinutes 10.0)
    match Autopilot.acquireSliceLease autopilotTime [] unsafeRequest with
    | Error errors -> Assert.Contains(errors, fun error -> error.Contains("not canonical"))
    | Ok _ -> failwith "parent traversal must not enter a lease scope"

    for unsafeScope in
        [ "src/Feature/"; " src/Feature"; "src/Feature "; "src\\Feature"
          "C:\\repo\\Feature"; "\\\\server\\share"; "src/Feature\u0001" ] do
        match Autopilot.acquireSliceLease autopilotTime []
                  (leaseRequest 1 "builder-a" "worktree-a" [ unsafeScope ] (autopilotTime.AddMinutes 10.0)) with
        | Error _ -> ()
        | Ok _ -> failwithf "non-canonical lease scope was accepted: %A" unsafeScope

    let futureHistory =
        { lease with HeartbeatAtUtc = autopilotTime.AddMinutes 2.0 }
    match Autopilot.acquireSliceLease (autopilotTime.AddMinutes 1.0) [ futureHistory ]
              (leaseRequest 2 "builder-b" "worktree-b" [ "tests/Feature.fs" ] (autopilotTime.AddMinutes 10.0)) with
    | Error errors -> Assert.Contains(errors, fun error -> error.Contains("newer than the acquisition observation"))
    | Ok _ -> failwith "controller time must not move behind retained history"

    let contradictory =
        { lease with
            Identity = { lease.Identity with OwnerId = "builder-b"; WorktreeId = "worktree-b" } }
    match Autopilot.acquireSliceLease (autopilotTime.AddMinutes 1.0) [ lease; contradictory ]
              (leaseRequest 1 "builder-a" "worktree-a" [ "src/Feature.fs" ] (autopilotTime.AddMinutes 10.0)) with
    | Error errors -> Assert.Contains(errors, fun error -> error.Contains("conflicting ownership"))
    | Ok _ -> failwith "idempotent replay must not hide contradictory history"

[<Fact>]
let ``slice lease history rejects candidate rollback and shrinking expiry`` () =
    let acquired =
        leaseRequest 1 "builder-a" "worktree-a" [ "src/Feature.fs" ] (autopilotTime.AddMinutes 10.0)
        |> acquireLease autopilotTime []
    let bound =
        Autopilot.bindSliceLeaseCandidate
            (autopilotTime.AddMinutes 1.0)
            (leaseSubject acquired)
            "candidate-one"
            acquired
        |> function
            | Ok lease -> lease
            | Error errors -> failwith (String.concat " | " errors)
    let nextRequest =
        leaseRequest 2 "builder-b" "worktree-b" [ "src/Feature.fs" ] (autopilotTime.AddMinutes 20.0)
    let rollback =
        { bound with CandidateDigest = None; HeartbeatAtUtc = autopilotTime.AddMinutes 2.0 }
    match Autopilot.acquireSliceLease (autopilotTime.AddMinutes 11.0) [ acquired; bound; rollback ] nextRequest with
    | Error errors -> Assert.Contains(errors, fun error -> error.Contains("rolls back the candidate"))
    | Ok _ -> failwith "candidate rollback in retained history must fail closed"

    let shrinkingExpiry =
        { bound with
            HeartbeatAtUtc = autopilotTime.AddMinutes 2.0
            ExpiresAtUtc = autopilotTime.AddMinutes 9.0 }
    match Autopilot.acquireSliceLease (autopilotTime.AddMinutes 11.0) [ acquired; bound; shrinkingExpiry ] nextRequest with
    | Error errors -> Assert.Contains(errors, fun error -> error.Contains("monotonically extend expiry"))
    | Ok _ -> failwith "shrinking expiry in retained history must fail closed"

[<Fact; Trait("spot", "spec-slice-worktree-lease-test-4")>]
let ``outer lease seam roundtrips all transitions and rejects forged or unsolicited observations`` () =
    let request =
        leaseRequest 1 "builder-a" "worktree-a" [ "src/Feature.fs" ] (autopilotTime.AddMinutes 10.0)
    let acquireTransition = Autopilot.AcquireLease([], request)
    let lease =
        match Autopilot.decideSliceLeaseTransition autopilotTime acquireTransition with
        | Ok value -> value
        | Error errors -> failwith (String.concat " | " errors)
    let subject = leaseSubject lease
    let transitions =
        [ acquireTransition
          Autopilot.VerifyLease(lease, subject)
          Autopilot.HeartbeatLease(
              lease,
              subject,
              autopilotTime.AddMinutes 20.0)
          Autopilot.BindLeaseCandidate(lease, subject, "candidate-one") ]

    for transition in transitions do
        let action = Autopilot.DecideSliceLease transition
        let actionRoundtrip =
            Json.deserialize<Autopilot.ControllerAction> (Json.serialize action)
        Assert.Equal(action, actionRoundtrip)

        let outcome =
            Autopilot.decideSliceLeaseTransition (autopilotTime.AddMinutes 1.0) transition
        match outcome with
        | Ok _ -> ()
        | Error errors -> failwithf "expected valid typed lease transition, got %A" errors
        let observation : Autopilot.SliceLeaseTransitionObservation =
            { Transition = transition; Outcome = outcome }
        let runObservation = Autopilot.SliceLeaseTransitionObserved observation
        let observationRoundtrip =
            Json.deserialize<Autopilot.RunObservation> (Json.serialize runObservation)
        Assert.Equal(runObservation, observationRoundtrip)
        Assert.Equal(
            outcome,
            Autopilot.validateSliceLeaseControllerExchange
                (autopilotTime.AddMinutes 1.0)
                action
                runObservation)

    let forgedObservation : Autopilot.SliceLeaseTransitionObservation =
        { Transition = acquireTransition
          Outcome = Error [ "adapter claims rejection" ] }
    match Autopilot.validateSliceLeaseControllerExchange
              autopilotTime
              (Autopilot.DecideSliceLease acquireTransition)
              (Autopilot.SliceLeaseTransitionObserved forgedObservation) with
    | Error errors -> Assert.Contains(errors, fun error -> error.Contains("does not match the CDD decision"))
    | Ok _ -> failwith "an adapter must not forge a lease decision"

    let staleTransition =
        Autopilot.AcquireLease(
            [],
            leaseRequest 1 "builder-b" "worktree-b" [ "tests/Feature.fs" ] (autopilotTime.AddMinutes 10.0))
    let staleObservation : Autopilot.SliceLeaseTransitionObservation =
        { Transition = staleTransition
          Outcome = Autopilot.decideSliceLeaseTransition autopilotTime staleTransition }
    match Autopilot.validateSliceLeaseControllerExchange
              autopilotTime
              (Autopilot.DecideSliceLease acquireTransition)
              (Autopilot.SliceLeaseTransitionObserved staleObservation) with
    | Error errors -> Assert.Contains(errors, fun error -> error.Contains("does not match the requested transition"))
    | Ok _ -> failwith "a response for another transition must not be accepted"

    match Autopilot.validateSliceLeaseControllerExchange
              autopilotTime
              (Autopilot.DecideSliceLease acquireTransition)
              (Autopilot.HumanInterventionObserved "unrelated") with
    | Error errors -> Assert.Contains(errors, fun error -> error.Contains("does not answer"))
    | Ok _ -> failwith "another observation case must not answer a lease action"

    let policy : Autopilot.RecoveryPolicy =
        { MaxSameSessionResumes = 1; MaxFreshStarts = 2; MaxRepairCycles = 1 }
    let run = createAutopilot policy
    match Autopilot.applyObservation
              autopilotTime
              (Autopilot.SliceLeaseTransitionObserved
                  { Transition = acquireTransition
                    Outcome = Autopilot.decideSliceLeaseTransition autopilotTime acquireTransition })
              run with
    | Error errors -> Assert.Contains(errors, fun error -> error.Contains("not scheduled"))
    | Ok _ -> failwith "the serial controller must reject unsolicited lease observations"

[<Fact>]
let ``outer lease seam rejects an invalid current lease even when its subject matches`` () =
    let invalid : Autopilot.SliceLease =
        { Identity =
            { RunId = ""; MissionId = ""; SliceId = ""; Attempt = 0
              OwnerId = ""; WorktreeId = "" }
          BaseDigest = ""
          CandidateDigest = Some ""
          Scope = [ "src/Feature.fs" ]
          AcquiredAtUtc = autopilotTime.AddHours(-48.0)
          HeartbeatAtUtc = autopilotTime.AddMinutes(-1.0)
          ExpiresAtUtc = autopilotTime.AddHours(48.0) }
    let exactSubject = leaseSubject invalid
    let assertRejected transition =
        match Autopilot.decideSliceLeaseTransition autopilotTime transition with
        | Error errors ->
            Assert.Contains(errors, fun error -> error.Contains("attempt must be positive"))
            Assert.Contains(errors, fun error -> error.Contains("no base digest"))
            Assert.Contains(errors, fun error -> error.Contains("blank candidate digest"))
            Assert.Contains(errors, fun error -> error.Contains("24-hour"))
        | Ok _ -> failwith "an invalid current lease must never gain authority"
    assertRejected (Autopilot.VerifyLease(invalid, exactSubject))
    assertRejected
        (Autopilot.HeartbeatLease(invalid, exactSubject, autopilotTime.AddHours 12.0))
    assertRejected
        (Autopilot.BindLeaseCandidate(invalid, exactSubject, "candidate-one"))

let private committedBytesPortabilityRequest () : Autopilot.CommittedBytesPortabilityRequest =
    let acquired =
        leaseRequest 1 "builder-a" "worktree-a" [ "src/Feature.fs" ] (autopilotTime.AddMinutes 10.0)
        |> acquireLease autopilotTime []
    let bound =
        Autopilot.bindSliceLeaseCandidate
            (autopilotTime.AddMinutes 1.0)
            (leaseSubject acquired)
            "candidate-one"
            acquired
        |> function
            | Ok lease -> lease
            | Error errors -> failwith (String.concat " | " errors)
    { Lease = bound
      Subject = leaseSubject bound
      CandidateDigest = "candidate-one"
      TreeDigest = "tree-one"
      ToolDigest = "toolchain-one"
      RequiredCheckIds = [ "fresh-checkout"; "asset-calibration" ] }

let private portabilityBinding candidate tree log : Autopilot.CommittedBytesPortabilityBinding =
    { CandidateDigest = candidate
      TreeDigest = tree
      ToolDigest = "toolchain-one"
      LogDigest = log }

let private successfulPortabilityReport () : Autopilot.CommittedBytesPortabilityReport =
    { Binding = portabilityBinding "candidate-one" "tree-one" "log-success"
      Checks =
        [ { CheckId = "fresh-checkout"; Passed = true }
          { CheckId = "asset-calibration"; Passed = true } ]
      Execution = Autopilot.ProcessExited 0 }

[<Fact; Trait("spot", "spec-committed-bytes-portability-test-1")>]
let ``committed bytes portability roundtrips exact candidate tree tool and log evidence`` () =
    let request = committedBytesPortabilityRequest ()
    let action = Autopilot.EvaluateCommittedBytesPortability request
    let report = successfulPortabilityReport ()
    let observation =
        Autopilot.CommittedBytesPortabilityObserved
            { Request = request
              Report = report
              ClaimedOutcome = Autopilot.Succeeded }

    Assert.Equal(
        action,
        Json.deserialize<Autopilot.ControllerAction> (Json.serialize action))
    Assert.Equal(
        observation,
        Json.deserialize<Autopilot.RunObservation> (Json.serialize observation))

    match Autopilot.validateCommittedBytesPortabilityControllerExchange
              (autopilotTime.AddMinutes 2.0)
              action
              observation with
    | Error errors -> failwith (String.concat " | " errors)
    | Ok evidence ->
        Assert.Equal(Autopilot.Succeeded, evidence.Outcome)
        Assert.Equal(report.Binding, evidence.Binding)
        Assert.Equal<string list>([], evidence.FailedCheckIds)
        Assert.Equal(None, evidence.FailureCode)
        Assert.Equal(
            Ok Autopilot.AcceptPortabilityEvidence,
            Autopilot.portabilityDisposition evidence)

[<Fact; Trait("spot", "spec-committed-bytes-portability-test-2")>]
let ``product failure repairs candidate while infrastructure failure retries adapter lane`` () =
    let request = committedBytesPortabilityRequest ()
    let productReport : Autopilot.CommittedBytesPortabilityReport =
        { Binding = portabilityBinding "candidate-one" "tree-one" "log-product-failure"
          Checks =
            [ { CheckId = "fresh-checkout"; Passed = false }
              { CheckId = "asset-calibration"; Passed = true } ]
          Execution = Autopilot.ProcessExited 1 }
    let infrastructureReport : Autopilot.CommittedBytesPortabilityReport =
        { Binding = portabilityBinding "candidate-one" "tree-one" "log-infrastructure-failure"
          Checks = []
          Execution = Autopilot.AdapterFailed "adapter-timeout" }
    let decide report =
        match Autopilot.decideCommittedBytesPortability
                  (autopilotTime.AddMinutes 2.0)
                  request
                  report with
        | Ok evidence -> evidence
        | Error errors -> failwith (String.concat " | " errors)

    let product = decide productReport
    Assert.Equal(Autopilot.ProductFailure, product.Outcome)
    Assert.Equal<string list>([ "fresh-checkout" ], product.FailedCheckIds)
    Assert.Equal(None, product.FailureCode)
    Assert.Equal(
        Ok Autopilot.RepairProductCandidate,
        Autopilot.portabilityDisposition product)

    let infrastructure = decide infrastructureReport
    Assert.Equal(Autopilot.InfrastructureFailure, infrastructure.Outcome)
    Assert.Equal<string list>([], infrastructure.FailedCheckIds)
    Assert.Equal(Some "adapter-timeout", infrastructure.FailureCode)
    Assert.Equal(
        Ok Autopilot.RetryPortabilityInfrastructure,
        Autopilot.portabilityDisposition infrastructure)

[<Fact; Trait("spot", "spec-committed-bytes-portability-test-3")>]
let ``portability seam rejects forged outcomes other requests and stale committed bytes`` () =
    let request = committedBytesPortabilityRequest ()
    let action = Autopilot.EvaluateCommittedBytesPortability request
    let successReport = successfulPortabilityReport ()
    let validate report claimed echoedRequest =
        Autopilot.validateCommittedBytesPortabilityControllerExchange
            (autopilotTime.AddMinutes 2.0)
            action
            (Autopilot.CommittedBytesPortabilityObserved
                { Request = echoedRequest
                  Report = report
                  ClaimedOutcome = claimed })

    match validate successReport Autopilot.ProductFailure request with
    | Error errors -> Assert.Contains(errors, fun error -> error.Contains("does not match the CDD decision"))
    | Ok _ -> failwith "an adapter must not forge a portability outcome"

    let otherRequest = { request with TreeDigest = "tree-two" }
    match validate successReport Autopilot.Succeeded otherRequest with
    | Error errors -> Assert.Contains(errors, fun error -> error.Contains("does not match the requested obligation"))
    | Ok _ -> failwith "an observation for another portability request must be rejected"

    let staleReport =
        { successReport with
            Binding = portabilityBinding "candidate-old" "tree-old" "log-stale" }
    match validate staleReport Autopilot.Stale request with
    | Error errors -> Assert.Contains(errors, fun error -> error.Contains("targets stale"))
    | Ok _ -> failwith "stale committed bytes must not become accepted evidence"

[<Fact; Trait("spot", "spec-committed-bytes-portability-test-4")>]
let ``portability reports fail closed on incomplete contradictory or unsolicited evidence`` () =
    let request = committedBytesPortabilityRequest ()
    let incomplete =
        { successfulPortabilityReport () with
            Checks = [ { CheckId = "fresh-checkout"; Passed = true } ] }
    match Autopilot.decideCommittedBytesPortability
              (autopilotTime.AddMinutes 2.0)
              request
              incomplete with
    | Error errors -> Assert.Contains(errors, fun error -> error.Contains("every required check"))
    | Ok _ -> failwith "completed evidence may not omit a required check"

    let contradictory =
        { successfulPortabilityReport () with
            Checks =
                [ { CheckId = "fresh-checkout"; Passed = false }
                  { CheckId = "asset-calibration"; Passed = true } ] }
    match Autopilot.decideCommittedBytesPortability
              (autopilotTime.AddMinutes 2.0)
              request
              contradictory with
    | Error errors -> Assert.Contains(errors, fun error -> error.Contains("zero portability exit code"))
    | Ok _ -> failwith "a zero exit code may not contradict a failed check"

    let pathLikeRequest = { request with ToolDigest = "tools/local/adapter" }
    match Autopilot.decideCommittedBytesPortability
              (autopilotTime.AddMinutes 2.0)
              pathLikeRequest
              (successfulPortabilityReport ()) with
    | Error errors -> Assert.Contains(errors, fun error -> error.Contains("not a path"))
    | Ok _ -> failwith "the semantic contract must not accept adapter-local paths"

    let run =
        createAutopilot
            { MaxSameSessionResumes = 1
              MaxFreshStarts = 2
              MaxRepairCycles = 1 }
    let unsolicited =
        Autopilot.CommittedBytesPortabilityObserved
            { Request = request
              Report = successfulPortabilityReport ()
              ClaimedOutcome = Autopilot.Succeeded }
    match Autopilot.applyObservation autopilotTime unsolicited run with
    | Error errors -> Assert.Contains(errors, fun error -> error.Contains("not scheduled"))
    | Ok _ -> failwith "the serial controller must reject unsolicited portability evidence"

// ===== Longitudinal Riftward evidence: sanitized records and baselines =====

let private riftwardPolicy : Autopilot.RecoveryPolicy =
    { MaxSameSessionResumes = 1; MaxFreshStarts = 2; MaxRepairCycles = 1 }

/// A run whose whole declared configuration is uniform across all roles.
let private riftwardRun missionId provider model harness =
    let basePlan = autopilotPlan riftwardPolicy
    let plan =
        { basePlan with
            MissionId = missionId
            Workers =
                basePlan.Workers
                |> List.map (fun worker ->
                    { worker with Provider = provider; Model = model; Harness = harness }) }
    match Autopilot.create autopilotTime plan with
    | Ok run -> run
    | Error errors -> failwith (String.concat " | " errors)

let private completeRiftwardRun run =
    run
    |> applyAutopilot 1
        (observeAgent Autopilot.Scout "scout-a" "scout-session" Autopilot.FreshSession
            (Some Autopilot.WorkCompleted) None (Some "context-1") [])
    |> applyAutopilot 2
        (observeAgent Autopilot.Builder "builder-a" "build-session" Autopilot.FreshSession
            (Some Autopilot.WorkCompleted) None (Some "candidate-1") [])
    |> applyAutopilot 3
        (Autopilot.GateObserved
            { GateId = "gate-tests"; ValidatorId = "dotnet-test-oracle"
              CandidateDigest = "candidate-1"; Passed = true; ExitCode = 0
              EvidenceDigest = "evidence-1"; DurationMilliseconds = 30L; Detail = "green" })
    |> applyAutopilot 4
        (observeAgent Autopilot.Critic "critic-b" "critic-session" Autopilot.FreshSession
            (Some Autopilot.WorkCompleted) (Some "candidate-1") None [])
    |> applyAutopilot 5
        (observeAgent Autopilot.Reviewer "reviewer-c" "review-session" Autopilot.FreshSession
            (Some Autopilot.WorkCompleted) (Some "candidate-1") None [])
    |> applyAutopilot 6
        (Autopilot.CheckpointObserved
            { SliceId = "slice-1"; CandidateDigest = "candidate-1"; Succeeded = true
              CommitHash = "abc123"; CleanWorktree = true; Detail = "checkpointed" })

let private blockRiftwardRun (run: Autopilot.RunState) =
    let interrupt session mode (current: Autopilot.RunState) =
        applyAutopilot (current.Ledger.Length + 1)
            (observeAgent Autopilot.Scout "scout-a" session mode None None None []) current
    run
    |> interrupt "session-1" Autopilot.FreshSession
    |> interrupt "session-1" (Autopilot.ResumeSession "session-1")
    |> interrupt "session-2" Autopilot.FreshSession
    |> interrupt "session-2" (Autopilot.ResumeSession "session-2")

let private riftwardProtocol = "sha256:evaluation-protocol-v1"

let private observeOrFail run =
    match Riftward.observeRun riftwardProtocol run with
    | Ok record -> record
    | Error errors -> failwith (String.concat " | " errors)

let private aggregateOrFail records =
    match Riftward.aggregate records with
    | Ok aggregates -> aggregates
    | Error errors -> failwith (String.concat " | " errors)

[<Fact; Trait("spot", "spec-riftward-longitudinal-baseline-test-1")>]
let ``riftward records strip sessions scopes prompts and artifacts from terminal runs`` () =
    let running = riftwardRun "mission-riftward" "provider-a" "model-a" "harness-a"
    match Riftward.observeRun " " (running |> blockRiftwardRun) with
    | Error errors -> Assert.Contains(errors, fun error -> error.Contains("protocol digest"))
    | Ok _ -> failwith "a baseline without an evaluation protocol is not comparable"
    match Riftward.observeRun riftwardProtocol running with
    | Error errors -> Assert.Contains(errors, fun error -> error.Contains("terminal"))
    | Ok _ -> failwith "a running snapshot has no stable outcome"

    let record = running |> blockRiftwardRun |> observeOrFail
    Assert.Equal(Autopilot.Blocked, record.Status)
    Assert.Equal("mission-riftward", record.MissionId)
    Assert.Equal("model-a", record.Configuration.Builder.Model)
    Assert.Equal(riftwardProtocol, record.Configuration.EvaluationProtocolDigest)
    Assert.Equal(4, record.Evaluation.PrematureStops)

    let published = Json.serialize record
    Assert.DoesNotContain("session-1", published)
    Assert.DoesNotContain("session-2", published)
    Assert.DoesNotContain("scout-session", published)
    Assert.DoesNotContain("src/Feature.fs", published)
    Assert.DoesNotContain("test observation", published)
    Assert.DoesNotContain("candidate-1", published)
    Assert.DoesNotContain("context-1", published)
    Assert.DoesNotContain("abc123", published)

[<Fact; Trait("spot", "spec-riftward-longitudinal-baseline-test-2")>]
let ``riftward aggregates baselines deterministically per declared configuration`` () =
    let completedA runId =
        riftwardRun "mission-riftward" "provider-a" "model-a" "harness-a"
        |> completeRiftwardRun |> observeOrFail
        |> fun record -> { record with RunId = runId }
    let records =
        [ completedA "run-a-1"
          completedA "run-a-2"
          riftwardRun "mission-riftward" "provider-a" "model-a" "harness-a"
          |> blockRiftwardRun |> observeOrFail |> fun record -> { record with RunId = "run-a-3" }
          riftwardRun "mission-other" "provider-b" "model-b" "harness-b"
          |> completeRiftwardRun |> observeOrFail |> fun record -> { record with RunId = "run-b-1" } ]

    let firstPass = aggregateOrFail records
    let secondPass = aggregateOrFail (List.rev records)
    Assert.Equal<Riftward.BaselineAggregate list>(firstPass, secondPass)
    Assert.Equal(2, firstPass.Length)

    let baselineA = firstPass |> List.find (fun item -> item.Configuration.Builder.Model = "model-a")
    let baselineB = firstPass |> List.find (fun item -> item.Configuration.Builder.Model = "model-b")
    Assert.Equal("mission-other", (List.head firstPass).MissionId)
    Assert.Equal(3, baselineA.Runs)
    Assert.Equal(1, baselineA.Missions)
    Assert.Equal(2, baselineA.FullSolves)
    Assert.Equal(1, baselineA.BlockedRuns)
    Assert.Equal(3, baselineA.AttemptedSlices)
    Assert.Equal(2, baselineA.CompletedSlices)
    Assert.Equal(4, baselineA.PrematureStops)
    Assert.Equal(2, baselineA.GateRuns)
    Assert.Equal(240L, baselineA.InputTokens)
    Assert.Equal(60L, baselineA.OutputTokens)
    Assert.Equal(70L, baselineA.MedianDurationMilliseconds)
    Assert.Equal(1, baselineB.Runs)
    Assert.Equal(1, baselineB.Missions)
    Assert.Equal(1, baselineB.FullSolves)

[<Fact; Trait("spot", "spec-riftward-longitudinal-baseline-test-3")>]
let ``riftward separates mixed configurations and classifies repetition fitness honestly`` () =
    let single =
        riftwardRun "mission-riftward" "provider-a" "model-a" "harness-a"
        |> completeRiftwardRun |> observeOrFail
    let aggregate = aggregateOrFail [ single ] |> List.exactlyOne
    Assert.Equal(Ok Riftward.Anecdotal, Riftward.classify 3 aggregate)
    Assert.Equal(Ok Riftward.Repeated, Riftward.classify 1 aggregate)

    // A differing critic profile must never be merged into the uniform
    // baseline even when the builder model matches.
    let mixedPlan =
        let basePlan = autopilotPlan riftwardPolicy
        { basePlan with MissionId = "mission-riftward"
                        Workers =
                            basePlan.Workers
                            |> List.map (fun worker ->
                                if worker.Role = Autopilot.Critic
                                then { worker with Provider = "provider-c"; Model = "model-c"; Harness = "harness-c" }
                                else { worker with Provider = "provider-a"; Model = "model-a"; Harness = "harness-a" }) }
    let mixed =
        match Autopilot.create autopilotTime mixedPlan with
        | Ok run -> run |> completeRiftwardRun |> observeOrFail
        | Error errors -> failwith (String.concat " | " errors)
    let aggregates = aggregateOrFail [ single; { mixed with RunId = "mixed-run" } ]
    Assert.Equal(2, aggregates.Length)
    Assert.Contains(aggregates, fun item -> item.Configuration.Critic.Provider <> item.Configuration.Scout.Provider)

    let deduplicated = aggregateOrFail [ single; single ] |> List.exactlyOne
    Assert.Equal(1, deduplicated.Runs)
    Assert.Equal(Ok Riftward.Anecdotal, Riftward.classify 2 deduplicated)

    let contradictory = { single with Status = Autopilot.Blocked }
    match Riftward.aggregate [ single; contradictory ] with
    | Error errors -> Assert.Contains(errors, fun error -> error.Contains("contradictory"))
    | Ok _ -> failwith "one RunId must not contribute contradictory records"

    let anotherMission = { single with RunId = "other-mission-run"; MissionId = "mission-other" }
    Assert.Equal(2, aggregateOrFail [ single; anotherMission ] |> List.length)

    let anotherProtocol =
        { single with
            RunId = "other-protocol-run"
            Configuration =
                { single.Configuration with EvaluationProtocolDigest = "sha256:evaluation-protocol-v2" } }
    Assert.Equal(2, aggregateOrFail [ single; anotherProtocol ] |> List.length)

[<Fact>]
let ``riftward rejects untrusted records and malformed run provenance at publication boundary`` () =
    let valid =
        riftwardRun "mission-riftward" "provider-a" "model-a" "harness-a"
        |> completeRiftwardRun
        |> observeOrFail
    let untrusted =
        { valid with
            Status = Autopilot.Running
            InputTokens = -1L
            OutputTokens = -1L
            Configuration =
                { valid.Configuration with
                    Builder = { valid.Configuration.Builder with Provider = " " } }
            Evaluation =
                { valid.Evaluation with
                    CompletedSlices = -1
                    TotalSlices = -1
                    GateRuns = 0
                    GateFailures = 1
                    DurationMilliseconds = -1L } }
    match Riftward.aggregate [ untrusted ] with
    | Error errors ->
        Assert.Contains(errors, fun error -> error.Contains("not terminal"))
        Assert.Contains(errors, fun error -> error.Contains("Builder provider"))
        Assert.Contains(errors, fun error -> error.Contains("negative CompletedSlices"))
        Assert.Contains(errors, fun error -> error.Contains("at least one attempted slice"))
        Assert.Contains(errors, fun error -> error.Contains("more gate failures"))
        Assert.Contains(errors, fun error -> error.Contains("negative input tokens"))
    | Ok _ -> failwith "untrusted records must not become a public baseline"

    let terminal =
        riftwardRun "mission-riftward" "provider-a" "model-a" "harness-a"
        |> blockRiftwardRun
    let malformedRun =
        { terminal with
            Plan =
                { terminal.Plan with
                    Workers =
                        terminal.Plan.Workers
                        |> List.map (fun worker ->
                            if worker.Role = Autopilot.Builder then { worker with Harness = " " }
                            else worker) }
            Metrics = { terminal.Metrics with InputTokens = -1L } }
    match Riftward.observeRun riftwardProtocol malformedRun with
    | Error errors ->
        Assert.Contains(errors, fun error -> error.Contains("Builder harness"))
        Assert.Contains(errors, fun error -> error.Contains("negative input tokens"))
    | Ok _ -> failwith "malformed durable state must fail at the publication boundary"

[<Fact>]
let ``riftward aggregation and repetition classification fail closed at numeric boundaries`` () =
    let valid =
        riftwardRun "mission-riftward" "provider-a" "model-a" "harness-a"
        |> completeRiftwardRun
        |> observeOrFail
    let boundaryRecord runId totalSlices duration inputTokens outputTokens =
        { valid with
            RunId = runId
            Status = Autopilot.Blocked
            Evaluation =
                { valid.Evaluation with
                    FullSolve = false
                    CompletedSlices = 0
                    TotalSlices = totalSlices
                    DurationMilliseconds = duration }
            InputTokens = inputTokens
            OutputTokens = outputTokens }

    match
        Riftward.aggregate
            [ boundaryRecord "int-a" Int32.MaxValue 0L 0L 0L
              boundaryRecord "int-b" Int32.MaxValue 0L 0L 0L ]
    with
    | Error errors -> Assert.Contains(errors, fun error -> error.Contains("overflows AttemptedSlices"))
    | Ok _ -> failwith "overflowing int aggregates must fail as typed errors"

    match
        Riftward.aggregate
            [ boundaryRecord "token-a" 1 0L Int64.MaxValue Int64.MaxValue
              boundaryRecord "token-b" 1 0L Int64.MaxValue Int64.MaxValue ]
    with
    | Error errors ->
        Assert.Contains(errors, fun error -> error.Contains("overflows InputTokens"))
        Assert.Contains(errors, fun error -> error.Contains("overflows OutputTokens"))
    | Ok _ -> failwith "overflowing token aggregates must fail as typed errors"

    let maximumMedian =
        aggregateOrFail
            [ boundaryRecord "median-a" 1 Int64.MaxValue 0L 0L
              boundaryRecord "median-b" 1 Int64.MaxValue 0L 0L ]
        |> List.exactlyOne
    Assert.Equal(Int64.MaxValue, maximumMedian.MedianDurationMilliseconds)

    let baseline = aggregateOrFail [ valid ] |> List.exactlyOne
    Assert.Equal(Ok Riftward.Repeated, Riftward.classify 1 baseline)
    match Riftward.classify 0 baseline with
    | Error errors -> Assert.Contains(errors, fun error -> error.Contains("minimum must be positive"))
    | Ok _ -> failwith "an invalid repetition minimum must fail closed"

    let forgedCounts = { baseline with RunIds = [ "a"; "b" ]; Runs = 0; Missions = 0 }
    match Riftward.classify 2 forgedCounts with
    | Error errors ->
        Assert.Contains(errors, fun error -> error.Contains("Runs must equal"))
        Assert.Contains(errors, fun error -> error.Contains("exactly one mission"))
    | Ok _ -> failwith "forged run counts must never become Repeated"

    let forgedIds = { baseline with RunIds = [ ""; "" ]; Runs = 2 }
    match Riftward.classify 1 forgedIds with
    | Error errors ->
        Assert.Contains(errors, fun error -> error.Contains("blank values"))
        Assert.Contains(errors, fun error -> error.Contains("duplicate RunId"))
    | Ok _ -> failwith "blank or duplicate run ids must never become Repeated"

    let invalidRelations =
        { baseline with
            FullSolves = 2
            BlockedRuns = -1
            AttemptedSlices = 0
            CompletedSlices = 1
            GateRuns = 0
            GateFailures = 1
            MedianDurationMilliseconds = -1L
            InputTokens = -1L }
    match Riftward.classify 1 invalidRelations with
    | Error errors ->
        Assert.Contains(errors, fun error -> error.Contains("negative BlockedRuns"))
        Assert.Contains(errors, fun error -> error.Contains("CompletedSlices cannot exceed"))
        Assert.Contains(errors, fun error -> error.Contains("GateFailures cannot exceed"))
        Assert.Contains(errors, fun error -> error.Contains("negative median duration"))
        Assert.Contains(errors, fun error -> error.Contains("negative input tokens"))
    | Ok _ -> failwith "inconsistent aggregate counters must fail closed"

// ===== Admissible comparisons between sanitized Riftward baselines =====

/// One baseline aggregate over `count` fully completed runs of one declared
/// configuration; `label` keeps RunIds distinct across helper calls.
let private riftwardAggregate label count missionId provider model harness =
    [ for index in 1 .. count ->
        riftwardRun missionId provider model harness
        |> completeRiftwardRun
        |> observeOrFail
        |> fun record -> { record with RunId = sprintf "%s-%s-%d-%s" missionId provider index label } ]
    |> aggregateOrFail
    |> List.exactlyOne

[<Fact; Trait("spot", "spec-riftward-baseline-comparison-test-1")>]
let ``riftward admits repeated same-mission same-protocol contrasts deterministically`` () =
    let left = riftwardAggregate "alpha" 3 "mission-riftward" "provider-a" "model-a" "harness-a"
    let right = riftwardAggregate "beta" 2 "mission-riftward" "provider-b" "model-b" "harness-b"
    let firstAdmission = Riftward.compareBaselines 2 left right
    Assert.Equal(firstAdmission, Riftward.compareBaselines 2 left right)
    match firstAdmission with
    | Error errors -> failwith (String.concat " | " errors)
    | Ok comparison ->
        Assert.Equal("mission-riftward", comparison.MissionId)
        Assert.Equal(riftwardProtocol, comparison.EvaluationProtocolDigest)
        Assert.Equal(2, comparison.MinimumRepetitions)
        Assert.Equal(left, comparison.Left)
        Assert.Equal(right, comparison.Right)

[<Fact; Trait("spot", "spec-riftward-baseline-comparison-test-2")>]
let ``riftward refuses comparisons below the repetition minimum or over invalid aggregates`` () =
    let repeated = riftwardAggregate "alpha" 3 "mission-riftward" "provider-a" "model-a" "harness-a"
    let anecdotal = riftwardAggregate "beta" 1 "mission-riftward" "provider-b" "model-b" "harness-b"
    match Riftward.compareBaselines 3 anecdotal repeated with
    | Error errors -> Assert.Contains(errors, fun error -> error.Contains("left baseline is anecdotal"))
    | Ok _ -> failwith "an anecdotal baseline must never be compared"
    match Riftward.compareBaselines 3 repeated anecdotal with
    | Error errors -> Assert.Contains(errors, fun error -> error.Contains("right baseline is anecdotal"))
    | Ok _ -> failwith "an anecdotal baseline must never be compared"
    let forged = { repeated with Runs = 0 }
    match Riftward.compareBaselines 1 forged repeated with
    | Error errors -> Assert.Contains(errors, fun error -> error.Contains("at least one run"))
    | Ok _ -> failwith "a forged aggregate has no comparable voice"
    match Riftward.compareBaselines 0 repeated anecdotal with
    | Error errors -> Assert.Contains(errors, fun error -> error.Contains("minimum must be positive"))
    | Ok _ -> failwith "a non-positive repetition minimum admits no comparison"

[<Fact; Trait("spot", "spec-riftward-baseline-comparison-test-3")>]
let ``riftward refuses cross-mission cross-protocol and contrast-free comparisons`` () =
    let alpha = riftwardAggregate "alpha" 2 "mission-alpha" "provider-a" "model-a" "harness-a"
    let otherMission = riftwardAggregate "beta" 2 "mission-beta" "provider-b" "model-b" "harness-b"
    match Riftward.compareBaselines 1 alpha otherMission with
    | Error errors -> Assert.Contains(errors, fun error -> error.Contains("compare one mission"))
    | Ok _ -> failwith "baselines from different missions share no scope"
    let otherProtocol =
        { alpha with
            Configuration =
                { alpha.Configuration with EvaluationProtocolDigest = "sha256:evaluation-protocol-v2" } }
    match Riftward.compareBaselines 1 alpha otherProtocol with
    | Error errors -> Assert.Contains(errors, fun error -> error.Contains("evaluation protocol digest"))
    | Ok _ -> failwith "baselines under different protocols share no evidence level"
    let twin = riftwardAggregate "twin" 2 "mission-alpha" "provider-a" "model-a" "harness-a"
    match Riftward.compareBaselines 1 alpha twin with
    | Error errors -> Assert.Contains(errors, fun error -> error.Contains("distinct declared configurations"))
    | Ok _ -> failwith "a configuration cannot contrast with itself"

[<Fact; Trait("spot", "spec-riftward-baseline-comparison-test-4")>]
let ``riftward refuses run ids reused across compared configurations`` () =
    let alpha = riftwardAggregate "alpha" 2 "mission-alpha" "provider-a" "model-a" "harness-a"
    let beta = riftwardAggregate "beta" 2 "mission-alpha" "provider-b" "model-b" "harness-b"
    let reused = { beta with RunIds = alpha.RunIds }
    match Riftward.compareBaselines 1 alpha reused with
    | Error errors -> Assert.Contains(errors, fun error -> error.Contains("reuses RunId"))
    | Ok _ -> failwith "one run must not count in both compared configurations"

// ===== Public scientific observatory: additive, read-only episode publication =====

let private observed unit source value : Observatory.Measured<'value> =
    { Quality = Observatory.Observed; Unit = unit; Value = Some value; Source = Some source; MissingReason = None }

let private unavailable unit reason : Observatory.Measured<'value> =
    { Quality = Observatory.Unavailable; Unit = unit; Value = None; Source = None; MissingReason = Some reason }

let private lowerHex width character = String(character, width)

let private sourceEvent eventId sequence hashCharacter : Observatory.SourceEventIdentity =
    { PublicEventId = eventId
      Sequence = sequence
      HashAlgorithm = Observatory.ContentHashAlgorithm.Sha256
      EventHash = lowerHex 64 hashCharacter }

let private promotionCandidate eventId sequence hashCharacter promotionId taskId changeSetId candidateCharacter commitCharacter treeCharacter : Observatory.PromotionEvidenceCandidate =
    { SourceEvent = sourceEvent eventId sequence hashCharacter
      PublicPromotionId = promotionId
      PublicTaskId = taskId
      PublicChangeSetId = changeSetId
      CandidateFingerprintAlgorithm = Observatory.ContentHashAlgorithm.Sha256
      PublicCandidateFingerprint = lowerHex 64 candidateCharacter
      GitObjectAlgorithm = Observatory.GitObjectAlgorithm.Sha1
      PublicCommitId = lowerHex 40 commitCharacter
      PublicTreeId = lowerHex 40 treeCharacter
      PromotedAtUtc = DateTimeOffset.Parse "2026-08-29T00:00:00Z"
      Authority = Observatory.RequiredGateReceipt }

let private verified candidate =
    Observatory.verifyPromotionEvidence candidate
    |> Result.defaultWith (String.concat " | " >> failwith)

let private observatoryEpisode outcome attempt epoch : Observatory.Episode =
    { PublicTaskId = "task-public-1"
      PublicChangeSetId = "change-set-public-1"
      PublicAttemptId = attempt
      PublicEpochId = epoch
      Agent =
        Observatory.MultiAgentConfiguration
            ("sha256:configuration-public-1",
             [ { Role = "scout"; Provider = "provider"; Model = "model"; Harness = "harness" }
               { Role = "builder"; Provider = "provider"; Model = "model"; Harness = "harness" }
               { Role = "critic"; Provider = "provider"; Model = "model"; Harness = "harness" }
               { Role = "reviewer"; Provider = "provider"; Model = "model"; Harness = "harness" } ])
      Outcome = outcome
      StartedAtUtc = unavailable "utc" "not-published"
      FinishedAtUtc = unavailable "utc" "not-published"
      Metrics =
        { DurationMilliseconds = observed "ms" Observatory.RiftwardRunRecord 100L
          InputTokens = observed "tokens" Observatory.OpenCodeUsageExport 20L
          OutputTokens = unavailable "tokens" "not-reported"
          RepairCycles = observed "cycles" Observatory.RiftwardRunRecord 2
          GateRuns = observed "runs" Observatory.GateReceipt 3
          GateFailures = observed "runs" Observatory.GateReceipt 1
          HumanInterventions = unavailable "interventions" "not-reported" }
      Cost = { Status = Observatory.CostEstimated; Amount = Some 1.25M; Currency = Some "EUR"; Source = Some Observatory.DerivedAggregate; MissingReason = None } }

let private promotionEvidence =
    promotionCandidate "event-public-1" 1L 'a' "promotion-public-1" "task-public-1" "change-set-public-1" 'b' 'c' 'd'
    |> verified

[<Fact; Trait("spot", "spec-scientific-observatory-test-1")>]
let ``scientific observatory keeps missingness and discarded work in deterministic aggregates`` () =
    let accepted = observatoryEpisode (Observatory.Accepted promotionEvidence) "attempt-1" "epoch-shared"
    let discarded = observatoryEpisode (Observatory.NotAccepted Observatory.Discarded) "attempt-2" "epoch-shared"
    match Observatory.aggregate [ discarded; accepted ] with
    | Error errors -> failwith (String.concat " | " errors)
    | Ok aggregate ->
        Assert.Equal(2, aggregate.Episodes)
        Assert.Equal(1, aggregate.Accepted)
        Assert.Equal(1, aggregate.Discarded)
        Assert.Equal(200L, aggregate.DurationMilliseconds.ObservedTotal)
        Assert.Equal(2, aggregate.OutputTokens.Completeness.Unavailable)
        Assert.Equal(2.50M, aggregate.Cost.EstimatedTotal)
        Assert.Equal(2, aggregate.Cost.Completeness.Estimated)

[<Fact; Trait("spot", "spec-scientific-observatory-test-2")>]
let ``scientific observatory verifies promotion structure and episode binding`` () =
    let malformed =
        { promotionCandidate "event-public-x" 2L 'a' "promotion-public-x" "task-public-1" "change-set-public-1" 'b' 'c' 'd' with
            PublicCommitId = "ABCDEF" }
    match Observatory.verifyPromotionEvidence malformed with
    | Error errors -> Assert.Contains(errors, fun error -> error.Contains("40 lower-hex"))
    | Ok _ -> failwith "uppercase or wrong-width git ids must not produce verified evidence"
    let wrongBinding =
        promotionCandidate "event-public-y" 3L 'e' "promotion-public-y" "task-other" "change-set-other" 'f' '1' '2'
        |> verified
    let finished = observed "utc" Observatory.RiftwardRunRecord (DateTimeOffset.Parse "2026-08-30T00:00:00Z")
    let contradictory =
        { observatoryEpisode (Observatory.Accepted wrongBinding) "attempt-binding" "epoch-binding" with
            FinishedAtUtc = finished }
    let bindingErrors = Observatory.validateEpisode contradictory
    Assert.Contains(bindingErrors, fun error -> error.Contains("task id does not match"))
    Assert.Contains(bindingErrors, fun error -> error.Contains("change-set id does not match"))
    Assert.Contains(bindingErrors, fun error -> error.Contains("cannot precede the observed episode finish"))

[<Fact; Trait("spot", "spec-scientific-observatory-test-2")>]
let ``scientific observatory fails closed on invalid measured values`` () =
    let observedWithoutSource =
        { observatoryEpisode (Observatory.NotAccepted Observatory.Discarded) "attempt-2" "epoch-2" with
            Metrics =
                { (observatoryEpisode (Observatory.NotAccepted Observatory.Discarded) "attempt-2" "epoch-2").Metrics with
                    InputTokens = { Quality = Observatory.Observed; Unit = "tokens"; Value = Some 1L; Source = None; MissingReason = None } } }
    let negative =
        { observatoryEpisode (Observatory.NotAccepted Observatory.Discarded) "attempt-3" "epoch-3" with
            Metrics =
                { (observatoryEpisode (Observatory.NotAccepted Observatory.Discarded) "attempt-3" "epoch-3").Metrics with
                    GateFailures = observed "runs" Observatory.GateReceipt -1 } }
    let unavailableWithoutReason =
        { observatoryEpisode (Observatory.NotAccepted Observatory.Discarded) "attempt-4" "epoch-4" with
            Metrics =
                { (observatoryEpisode (Observatory.NotAccepted Observatory.Discarded) "attempt-4" "epoch-4").Metrics with
                    OutputTokens = { Quality = Observatory.Unavailable; Unit = "tokens"; Value = None; Source = None; MissingReason = None } } }
    let costWithoutReason =
        { observatoryEpisode (Observatory.NotAccepted Observatory.Unresolved) "attempt-5" "epoch-5" with
            Cost = { Status = Observatory.CostUnavailable; Amount = None; Currency = None; Source = None; MissingReason = None } }
    match Observatory.aggregate [ observedWithoutSource; negative; unavailableWithoutReason; costWithoutReason ] with
    | Error errors ->
        Assert.Contains(errors, fun error -> error.Contains("InputTokens is Observed but has no source"))
        Assert.Contains(errors, fun error -> error.Contains("GateFailures cannot be negative"))
        Assert.Contains(errors, fun error -> error.Contains("OutputTokens is Unavailable but has no missing reason"))
        Assert.Contains(errors, fun error -> error.Contains("Cost is CostUnavailable but has no missing reason"))
    | Ok _ -> failwith "invalid observatory records must fail closed"

[<Fact; Trait("spot", "spec-scientific-observatory-test-3")>]
let ``scientific observatory rejects duplicate attempts source events and complete bindings`` () =
    let first = observatoryEpisode (Observatory.NotAccepted Observatory.Discarded) "attempt-duplicate" "epoch-1"
    let duplicate = observatoryEpisode (Observatory.NotAccepted Observatory.Superseded) "attempt-duplicate" "epoch-2"
    let reusedSource =
        promotionCandidate "event-public-1" 1L 'a' "promotion-public-2" "task-public-1" "change-set-public-1" 'e' 'f' '1'
        |> verified
        |> fun evidence -> observatoryEpisode (Observatory.Accepted evidence) "attempt-source" "epoch-3"
    match Observatory.aggregate [ first; duplicate; observatoryEpisode (Observatory.Accepted promotionEvidence) "attempt-original" "epoch-4"; reusedSource ] with
    | Error errors ->
        Assert.Contains(errors, fun error -> error.Contains("Duplicate public attempt id"))
        Assert.Contains(errors, fun error -> error.Contains("Duplicate authoritative source event"))
    | Ok _ -> failwith "duplicate attempts and source events must fail closed"

[<Fact; Trait("spot", "spec-scientific-observatory-test-1")>]
let ``scientific observatory deduplicates complete promotion bindings not trees alone`` () =
    let accepted = observatoryEpisode (Observatory.Accepted promotionEvidence) "attempt-accepted-1" "epoch-1"
    let sameBinding =
        promotionCandidate "event-public-2" 2L 'e' "promotion-public-2" "task-public-1" "change-set-public-1" 'b' 'c' 'd'
        |> verified
        |> fun evidence -> observatoryEpisode (Observatory.Accepted evidence) "attempt-accepted-2" "epoch-2"
    let treeOnly =
        promotionCandidate "event-public-3" 3L 'f' "promotion-public-3" "task-public-1" "change-set-public-1" '1' '2' 'd'
        |> verified
        |> fun evidence -> observatoryEpisode (Observatory.Accepted evidence) "attempt-accepted-3" "epoch-3"
    match Observatory.aggregate [ accepted; sameBinding ] with
    | Error errors ->
        Assert.Contains(errors, fun error -> error.Contains("Duplicate candidate/commit/tree binding"))
    | Ok _ -> failwith "one complete accepted binding must not count as multiple promotions"
    match Observatory.aggregate [ accepted; treeOnly ] with
    | Ok aggregate -> Assert.Equal(2, aggregate.Accepted)
    | Error errors -> failwith ("tree reuse alone is not a duplicate binding: " + String.concat " | " errors)

[<Fact; Trait("spot", "spec-scientific-observatory-test-2")>]
let ``riftward observatory provenance is multi-agent rather than builder-only`` () =
    let configuration : Riftward.RunConfiguration =
        { Scout = { Provider = "scout-provider"; Model = "scout-model"; Harness = "h" }
          Builder = { Provider = "builder-provider"; Model = "builder-model"; Harness = "h" }
          Critic = { Provider = "critic-provider"; Model = "critic-model"; Harness = "h" }
          Reviewer = { Provider = "reviewer-provider"; Model = "reviewer-model"; Harness = "h" }
          EvaluationProtocolDigest = "sha256:protocol" }
    let record : Riftward.RunRecord =
        { RunId = "run-public"; MissionId = "mission-public"; Configuration = configuration
          Status = Autopilot.Completed
          Evaluation = { CompletedSlices = 1; TotalSlices = 1; AgentTurns = 1; ToolCalls = 1; PrematureStops = 0; SameSessionResumes = 0; FreshStarts = 1; RepairCycles = 0; GateRuns = 1; GateFailures = 0; HumanInterventions = 0; DurationMilliseconds = 1L; FullSolve = true }
          InputTokens = 1L; OutputTokens = 1L }
    let episode =
        Observatory.fromRiftwardRun "task" "change-set" "attempt" "epoch" { Status = Observatory.CostUnavailable; Amount = None; Currency = None; Source = None; MissingReason = Some "not-published" } record
    Assert.Equal(Observatory.NotAccepted Observatory.Unresolved, episode.Outcome)
    match episode.Agent with
    | Observatory.MultiAgentConfiguration (_, roles) ->
        Assert.Equal(4, roles.Length)
        Assert.Contains(roles, fun role -> role.Role = "builder" && role.Provider = "builder-provider")
        Assert.Contains(roles, fun role -> role.Role = "critic" && role.Provider = "critic-provider")
    | _ -> failwith "Riftward provenance must be multi-agent or explicitly unavailable"

[<Fact; Trait("spot", "spec-scientific-observatory-test-3")>]
let ``draft public observation snapshot golden fixture round-trips without fabricating legacy task attribution`` () =
    let fixture = Path.Combine(findRepoRoot (), "tests", "Cdd.Tests", "Fixtures", "public-observation-snapshot-v1.golden.json")
    let json = File.ReadAllText fixture
    match Observatory.parsePublicObservationSnapshotV1 json with
    | Error errors -> failwith (String.concat " | " errors)
    | Ok snapshot ->
        Assert.Single snapshot.RunObservations |> ignore
        Assert.Null(snapshot.RunObservations.Head.PublicTaskId |> Option.toObj)
        Assert.Equal(Some "legacy-source-omitted-task", snapshot.RunObservations.Head.TaskAttributionMissingReason)
        let emitted = Observatory.serializePublicObservationSnapshotV1 snapshot |> Result.defaultWith (String.concat " | " >> failwith)
        Assert.Equal(json.TrimEnd(), emitted.TrimEnd())
        match Observatory.deriveEpisodeProjections snapshot with
        | Ok [ Observatory.LegacyRunWithoutTaskAttribution (runId, reason) ] ->
            Assert.Equal("run-legacy-1", runId)
            Assert.Equal("legacy-source-omitted-task", reason)
        | Ok _ -> failwith "legacy task attribution must remain an explicit non-episode projection"
        | Error errors -> failwith (String.concat " | " errors)

[<Fact; Trait("spot", "spec-riftward-t053-public-registry-test-1")>]
let ``Riftward T-053 registry fails closed to an immutable public source binding`` () =
    let protocol =
        Path.Combine(findRepoRoot (), "docs", "research", "PROTOCOL.md")
        |> File.ReadAllText
    [ "d7d5f949758a3a38ca4238ceadfbbd83965eb71d"
      "3ce6338f6524b9349af716755c91d01d77cd3b93"
      "riftward-research-observability` / `2.0.1"
      "58b93d5a7ce8b0c1b182030d36eab9f156ff1aa8f2c2d246be54bcd53f3bf1de"
      "Raw-Export"
      "publikationsgesperrt"
      "keine öffentliche `prospective-observed` Messung" ]
    |> List.iter (fun required -> Assert.Contains(required, protocol))
    Assert.DoesNotContain("a8da858d9a25892a4671104c57f5edfe3c789a39", protocol)
    Assert.DoesNotContain("a127ab37de6752a6defd8b9ebcb04c37cba0e3343863b5c10f53a9d109e20a65", protocol)

[<Fact; Trait("spot", "spec-scientific-observatory-test-3")>]
let ``draft snapshot structurally validates and derives accepted episode only by join`` () =
    let run : Observatory.PublicRunObservationV1 =
        { SourceEvent = sourceEvent "run-event-1" 1L '1'
          PublicRunId = "run-public-1"
          PublicTaskId = Some "task-public-1"
          TaskAttributionMissingReason = None
          PublicChangeSetId = Some "change-set-public-1"
          PublicAttemptId = "attempt-public-1"
          PublicEpochId = "epoch-public-1"
          Agent = (observatoryEpisode (Observatory.NotAccepted Observatory.Unresolved) "unused" "unused").Agent
          NonAcceptedDisposition = None
          StartedAtUtc = unavailable "utc" "not-published"
          FinishedAtUtc = unavailable "utc" "not-published"
          Metrics = (observatoryEpisode (Observatory.NotAccepted Observatory.Unresolved) "unused" "unused").Metrics
          Cost = (observatoryEpisode (Observatory.NotAccepted Observatory.Unresolved) "unused" "unused").Cost }
    let promotion : Observatory.PublicPromotionObservationV1 =
        { PublicAttemptId = run.PublicAttemptId
          Evidence = promotionCandidate "promotion-event-1" 2L '2' "promotion-public-1" "task-public-1" "change-set-public-1" '3' '4' '5' }
    let snapshot : Observatory.PublicObservationSnapshotV1 =
        { Schema = Observatory.PublicObservationSnapshotV1Schema
          RunObservations = [ run ]
          PromotionObservations = [ promotion ]
          InterventionObservations = []
          TelemetryGaps = []
          Coverage =
            { WindowStartUtc = DateTimeOffset.Parse "2026-08-29T00:00:00Z"
              WindowEndUtc = DateTimeOffset.Parse "2026-08-29T01:00:00Z"
              Sources = [ { Source = Observatory.RiftwardRunRecord; Status = Observatory.Partial; MissingReason = Some "draft-fixture" } ] }
          Integrity =
            { PublicSnapshotId = "snapshot-public-1"
              ManifestHashAlgorithm = Observatory.ContentHashAlgorithm.Sha256
              ManifestHash = lowerHex 64 '6'
              PreviousManifestHash = None
              GeneratedAtUtc = DateTimeOffset.Parse "2026-08-29T01:00:00Z" } }
    let serialized = Observatory.serializePublicObservationSnapshotV1 snapshot |> Result.defaultWith (String.concat " | " >> failwith)
    Assert.Equal(snapshot, Observatory.parsePublicObservationSnapshotV1 serialized |> Result.defaultWith (String.concat " | " >> failwith))
    match Observatory.deriveEpisodeProjections snapshot with
    | Ok [ Observatory.JoinedEpisode episode ] ->
        match episode.Outcome with
        | Observatory.Accepted _ -> ()
        | _ -> failwith "joined promotion must produce acceptance"
    | Ok _ -> failwith "one attributed run and promotion must derive one joined episode"
    | Error errors -> failwith (String.concat " | " errors)

    let contradictory =
        { snapshot with
            RunObservations = [ { run with NonAcceptedDisposition = Some Observatory.Unresolved } ] }
    match Observatory.validatePublicObservationSnapshotV1 contradictory with
    | Error errors -> Assert.Contains(errors, fun error -> error.Contains("both promotion and terminal non-acceptance"))
    | Ok _ -> failwith "a promoted run cannot also carry a non-accepted disposition"

    let ambiguousRun =
        { run with
            SourceEvent = sourceEvent "run-event-2" 3L '7'
            PublicRunId = "run-public-2" }
    let orphanPromotion =
        { promotion with
            PublicAttemptId = "attempt-missing"
            Evidence = promotionCandidate "promotion-event-2" 4L '8' "promotion-public-2" "task-public-1" "change-set-public-1" '9' 'a' 'b' }
    let malformed =
        { snapshot with
            RunObservations = [ { run with SourceEvent = sourceEvent "run-event-zero" 0L 'c' }; ambiguousRun ]
            PromotionObservations = [ orphanPromotion ] }
    match Observatory.validatePublicObservationSnapshotV1 malformed with
    | Error errors ->
        Assert.Contains(errors, fun error -> error.Contains("sequence must be positive"))
        Assert.Contains(errors, fun error -> error.Contains("Duplicate public run attempt id"))
        Assert.Contains(errors, fun error -> error.Contains("references unknown attempt"))
    | Ok _ -> failwith "ambiguous joins and non-positive source sequences must fail closed"
