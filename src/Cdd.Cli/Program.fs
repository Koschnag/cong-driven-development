open System
open Cdd.Core
open Cdd.Core.Spot

let private root = "."

let private usage () =
    printfn "cdd — cong-driven-development"
    printfn ""
    printfn "Usage:"
    printfn "  cdd version              Versionsinfo"
    printfn "  cdd init                 SPOT-Store mit Beispiel-Knoten anlegen"
    printfn "  cdd list                 Alle SPOT-Knoten auflisten"
    printfn "  cdd validate             Modell prüfen (Exit 1 bei Fehlern)"
    printfn "  cdd diff                 Konvergenz-/Drift-Report"
    printfn "  cdd derive-tests [--write]  Tests aus Specs ableiten"
    printfn "  cdd export-context [--out <datei>]  SPOT als LLM-Kontextpaket/Doku (Markdown)"
    printfn "  cdd sync-code [--write]  Round-Trip: Komponenten gegen src/*.fsproj abgleichen"
    printfn "  cdd sync-tests [--write] Round-Trip: Test-Knoten gegen echte Tests (Trait/Marker) messen"
    printfn "  cdd sync-docs [--check]  README-Status aus dem Modell generieren"

/// Seed-Knoten für `cdd init` — zeigt jede Knotenart einmal.
let private seed : SpotEntry list =
    [ { Id = EntityId "spec-login"
        Convergence = Pending
        Payload =
          SpecNode
            { Title = "Login"
              Intent = "Nutzer authentifiziert sich mit E-Mail und Passwort"
              Criteria =
                [ { Given = "ein registrierter Nutzer"
                    When = "korrekte Credentials eingegeben werden"
                    Then = "wird eine Session erstellt" }
                  { Given = "ein registrierter Nutzer"
                    When = "ein falsches Passwort eingegeben wird"
                    Then = "wird die Anmeldung abgelehnt" } ] } }
      { Id = EntityId "risk-bruteforce"
        Convergence = Pending
        Payload =
          RiskNode
            { Statement = "Brute-Force gegen den Login-Endpunkt"
              Likelihood = Spot.Medium
              Impact = Spot.High
              Mitigation = Some "Rate-Limiting + Account-Lockout" } }
      { Id = EntityId "comp-auth"
        Convergence = Pending
        Payload =
          ComponentNode
            { Name = "AuthService"
              DependsOn = [ EntityId "spec-login" ] } }
      { Id = EntityId "term-nutzer"
        Convergence = Aligned
        Payload =
          TermNode
            { Name = "Nutzer"
              Definition = "Person mit registriertem Konto, die sich authentifizieren kann"
              Synonyms = [ "User"; "Account-Inhaber" ]
              Relations = [] } }
      { Id = EntityId "term-session"
        Convergence = Aligned
        Payload =
          TermNode
            { Name = "Session"
              Definition = "Zeitlich begrenzter, authentifizierter Zugriffskontext eines Nutzers"
              Synonyms = [ "Sitzung" ]
              Relations = [ RelatesTo(EntityId "term-nutzer") ] } }
      { Id = EntityId "term-credential"
        Convergence = Aligned
        Payload =
          TermNode
            { Name = "Credential"
              Definition = "Nachweis zur Authentifizierung, z. B. E-Mail + Passwort"
              Synonyms = []
              Relations = [ PartOf(EntityId "term-nutzer") ] } } ]

let private cmdInit () =
    if Store.exists root then
        printfn "SPOT-Store existiert bereits unter %s" (Store.spotDir root)
        0
    else
        seed |> List.iter (Store.save root)
        printfn "SPOT-Store angelegt unter %s (%d Knoten)" (Store.spotDir root) (List.length seed)
        0

let private cmdList () =
    let entries = Store.load root
    if List.isEmpty entries then
        printfn "Kein SPOT-Store. 'cdd init' ausführen."
    else
        for e in entries do
            printfn "%-20s %-10s %A" (idValue e.Id) (kindOf e) e.Convergence
    0

let private cmdValidate () =
    let findings = Store.load root |> Validate.validate
    for f in findings do
        let tag = match f.Severity with Validate.Error -> "ERROR" | Validate.Warning -> "WARN "
        printfn "[%s] %-20s %s" tag (idValue f.EntityId) f.Message
    let errs = Validate.errors findings |> List.length
    let warns = Validate.warnings findings |> List.length
    printfn "%d Fehler, %d Warnungen" errs warns
    if errs > 0 then 1 else 0

let private cmdDiff () =
    let r = Store.load root |> Diff.report
    let section name (xs: SpotEntry list) =
        printfn "%s (%d):" name (List.length xs)
        for e in xs do printfn "  %s" (idValue e.Id)
    section "Aligned" r.Aligned
    section "Pending" r.Pending
    section "Diverged" r.Diverged
    section "Orphaned" r.Orphaned
    0

let private cmdDeriveTests write =
    let entries = Store.load root
    let derived = Derive.deriveTests entries
    if List.isEmpty derived then
        printfn "Keine neuen Tests abzuleiten."
    else
        for e in derived do
            match e.Payload with
            | TestNode t -> printfn "+ %-24s %s" (idValue e.Id) t.Name
            | _ -> ()
        if write then
            derived |> List.iter (Store.save root)
            printfn "%d Test-Knoten geschrieben." (List.length derived)
        else
            printfn "%d Test-Knoten ableitbar (--write zum Persistieren)." (List.length derived)
    0

/// Version aus Directory.Build.props (AssemblyInformationalVersion, ohne Build-Hash).
let private version () =
    let info =
        System.Reflection.Assembly.GetExecutingAssembly()
            .GetCustomAttributes(typeof<System.Reflection.AssemblyInformationalVersionAttribute>, false)
    match info with
    | [| :? System.Reflection.AssemblyInformationalVersionAttribute as a |] ->
        a.InformationalVersion.Split('+').[0]
    | _ -> "unknown"


let private cmdSyncCode write =
    let projects = Sync.scanProjects "src"
    if List.isEmpty projects then
        printfn "Keine .fsproj unter ./src gefunden."
        0
    else
        let entries = Store.load root
        let results, updated = Sync.compare projects entries
        for r in results do
            printfn "%-10A %-24s %s" r.Status (idValue r.Id) r.Detail
        if write then
            updated
            |> List.filter (fun e -> entries |> List.exists (fun o -> o.Id = e.Id && o <> e))
            |> List.iter (fun e -> Store.save root e; printfn "aktualisiert: %s" (idValue e.Id))
        let drift = results |> List.exists (fun r -> r.Status = Diverged || r.Status = Orphaned)
        if drift then 1 else 0


let private cmdSyncTests write =
    let covered = Sync.scanTestMarkers "tests"
    let entries = Store.load root
    let mismatches, updated = Sync.syncTests covered entries
    if List.isEmpty mismatches then
        printfn "Test-Konvergenz synchron (%d Marker gefunden)." (Set.count covered)
        0
    else
        for id, stored, measured in mismatches do
            printfn "%-28s gespeichert: %-8A gemessen: %A" (idValue id) stored measured
        if write then
            updated
            |> List.filter (fun e -> entries |> List.exists (fun o -> o.Id = e.Id && o <> e))
            |> List.iter (fun e -> Store.save root e; printfn "aktualisiert: %s" (idValue e.Id))
            0
        else
            printfn "%d Abweichungen — mit --write übernehmen." (List.length mismatches)
            1

let private docMarkerStart = "<!-- spot:status -->"
let private docMarkerEnd = "<!-- /spot:status -->"

let private cmdSyncDocs check =
    let path = "README.md"
    let readme = System.IO.File.ReadAllText path
    let s = readme.IndexOf docMarkerStart
    let e = readme.IndexOf docMarkerEnd
    if s < 0 || e < s then
        eprintfn "Fehler: Marker %s … %s nicht im README gefunden." docMarkerStart docMarkerEnd
        1
    else
        let generated = Store.load root |> Export.statusMarkdown
        let updated =
            readme.Substring(0, s + docMarkerStart.Length)
            + "\n" + generated
            + readme.Substring(e)
        if updated = readme then
            printfn "README-Status ist aktuell."
            0
        elif check then
            eprintfn "README-Status ist veraltet — 'cdd sync-docs' ausführen und committen."
            1
        else
            System.IO.File.WriteAllText(path, updated)
            printfn "README-Status neu generiert."
            0

[<EntryPoint>]
let main argv =
    try
        match argv with
        | [| "version" |]            -> printfn "cdd %s" (version ()); 0
        | [| "init" |]               -> cmdInit ()
        | [| "list" |]               -> cmdList ()
        | [| "validate" |]           -> cmdValidate ()
        | [| "diff" |]               -> cmdDiff ()
        | [| "derive-tests" |]       -> cmdDeriveTests false
        | [| "derive-tests"; "--write" |] -> cmdDeriveTests true
        | [| "sync-code" |]          -> cmdSyncCode false
        | [| "sync-code"; "--write" |] -> cmdSyncCode true
        | [| "sync-tests" |]         -> cmdSyncTests false
        | [| "sync-tests"; "--write" |] -> cmdSyncTests true
        | [| "sync-docs" |]          -> cmdSyncDocs false
        | [| "sync-docs"; "--check" |] -> cmdSyncDocs true
        | [| "export-context" |] ->
            printf "%s" (Store.load root |> Export.toMarkdown)
            0
        | [| "export-context"; "--out"; path |] ->
            System.IO.File.WriteAllText(path, Store.load root |> Export.toMarkdown)
            printfn "Kontextpaket geschrieben: %s" path
            0
        | _                          -> usage (); 0
    with ex ->
        eprintfn "Fehler: %s" ex.Message
        1
