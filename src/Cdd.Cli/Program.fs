open System
open System.Diagnostics
open System.IO
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
    printfn "  cdd derive-code [--out <datei>]  Test-Skelette für unabgedeckte Test-Knoten generieren"
    printfn "  cdd eidos run [--out <ordner>] [--fault <name>]  ZT2-OpsLab vollständig ausführen"
    printfn "  cdd eidos replay <run-ordner>  Ledger, Evidence und Sandbox erneut prüfen"
    printfn "  cdd eidos benchmark [--out <ordner>]  reproduzierbaren Fault-Injection-Benchmark erzeugen"
    printfn "  cdd autopilot init <plan.json> [--workspace <ordner>]  persistenten SDLC-Run anlegen"
    printfn "  cdd autopilot next <run-ordner>  nächste typisierte Harness-Aktion als JSON ausgeben"
    printfn "  cdd autopilot record <run-ordner> <observation.json>  Agent-/Review-Beobachtung aufnehmen"
    printfn "  cdd autopilot gate <run-ordner> [--cwd <ordner>]  erwartetes deterministisches Gate ausführen"
    printfn "  cdd autopilot checkpoint <run-ordner> [--cwd <ordner>]  Git-Checkpoint prüfen und aufnehmen"
    printfn "  cdd autopilot drive <run-ordner> <adapter> --cwd <ordner> [--max-steps <n>]  Controller-Loop fahren"
    printfn "  cdd autopilot status <run-ordner>  Status, Evaluation und nächste Aktion ausgeben"

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
    let projects = Sync.scanRepo root
    if List.isEmpty projects then
        printfn "Keine Projekte unter %s gefunden (src, tools, apps)." root
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
        let decisionsPath = "docs/decisions.md"
        let decisionsNew = Store.load root |> Export.decisionsMarkdown
        let decisionsOld =
            if System.IO.File.Exists decisionsPath then System.IO.File.ReadAllText decisionsPath else ""
        let readmeAktuell = updated = readme
        let decisionsAktuell = decisionsNew = decisionsOld
        if readmeAktuell && decisionsAktuell then
            printfn "README-Status und docs/decisions.md sind aktuell."
            0
        elif check then
            if not readmeAktuell then eprintfn "README-Status ist veraltet."
            if not decisionsAktuell then eprintfn "docs/decisions.md ist veraltet."
            eprintfn "'cdd sync-docs' ausführen und committen."
            1
        else
            if not readmeAktuell then
                System.IO.File.WriteAllText(path, updated)
                printfn "README-Status neu generiert."
            if not decisionsAktuell then
                System.IO.File.WriteAllText(decisionsPath, decisionsNew)
                printfn "docs/decisions.md neu generiert."
            0


let private cmdDeriveCode out =
    let covered = Sync.scanTestMarkers "tests"
    let code = Store.load root |> Generate.testSkeletons covered
    match out with
    | Some path ->
        System.IO.File.WriteAllText(path, code)
        printfn "Test-Skelette geschrieben: %s" path
    | None -> printf "%s" code
    0

let private cmdEidosRun out faultName =
    let outputRoot = out |> Option.defaultValue root |> System.IO.Path.GetFullPath
    match Eidos.parseFault faultName with
    | None ->
        eprintfn "Unbekannter Fault '%s'. Erlaubt: none, failed-gate, failed-unit-gate, stale-evidence, correlated-validator, missing-evidence, artifact-mismatch, policy-mismatch, tampered-pack, budget-exceeded." faultName
        1
    | Some fault ->
        let run = Eidos.runOpsLab outputRoot DateTimeOffset.UtcNow fault
        let runDir = System.IO.Path.Combine(outputRoot, ".eidos", "runs", run.RunId)
        printfn "EIDOS OpsLab: %A" run.Status
        printfn "Run: %s" run.RunId
        printfn "Candidate: %s" run.Candidate.Id
        printfn "Evidence: %s" run.EvidencePack.Id
        printfn "Replay: %b" run.Metrics.ReplayVerified
        printfn "Artefakte: %s" runDir
        0

let private cmdEidosReplay runDir =
    let result = Eidos.replayOpsLab (System.IO.Path.GetFullPath runDir)
    for name, passed in result.Checks do
        printfn "[%s] %s" (if passed then "OK  " else "FAIL") name
    for reason in result.Reasons do eprintfn "%s" reason
    printfn "Replay %s: %b" result.RunId result.Verified
    if result.Verified then 0 else 1

let private cmdEidosBenchmark out =
    let report = Eidos.runBenchmark ()
    printf "%s" (Eidos.benchmarkMarkdown report)
    match out with
    | None -> ()
    | Some outputRoot ->
        let outputRoot = System.IO.Path.GetFullPath outputRoot
        System.IO.Directory.CreateDirectory outputRoot |> ignore
        System.IO.File.WriteAllText(
            System.IO.Path.Combine(outputRoot, "eidos-benchmark.json"),
            Json.serialize report)
        System.IO.File.WriteAllText(
            System.IO.Path.Combine(outputRoot, "eidos-benchmark.md"),
            Eidos.benchmarkMarkdown report)
        printfn ""
        printfn "Benchmark-Artefakte: %s" outputRoot
    if report.Eidos.Correct = report.Eidos.Total then 0 else 1

let private printAutopilotErrors errors =
    for error in errors do eprintfn "Autopilot: %s" error

let private actionExitCode = function
    | Autopilot.Escalate _ -> 2
    | _ -> 0

let private cmdAutopilotInit planPath workspace =
    try
        let plan = Json.deserialize<Autopilot.RunPlan> (File.ReadAllText(Path.GetFullPath planPath))
        match Autopilot.initialize (workspace |> Option.defaultValue root) DateTimeOffset.UtcNow plan with
        | Error errors -> printAutopilotErrors errors; 1
        | Ok(runDirectory, run) ->
            printfn "%s" (Json.serialize
                {| runDirectory = runDirectory
                   runId = run.RunId
                   status = run.Status
                   nextAction = Autopilot.nextAction run |})
            0
    with ex ->
        eprintfn "Autopilot-Plan konnte nicht geladen werden: %s" ex.Message
        1

let private cmdAutopilotNext runDirectory =
    match Autopilot.load runDirectory with
    | Error errors -> printAutopilotErrors errors; 1
    | Ok run ->
        let action = Autopilot.nextAction run
        printfn "%s" (Json.serialize action)
        actionExitCode action

let private cmdAutopilotRecord runDirectory observationPath =
    try
        let observation =
            Json.deserialize<Autopilot.RunObservation>
                (File.ReadAllText(Path.GetFullPath observationPath))
        match Autopilot.record runDirectory DateTimeOffset.UtcNow observation with
        | Error errors -> printAutopilotErrors errors; 1
        | Ok run ->
            let action = Autopilot.nextAction run
            printfn "%s" (Json.serialize
                {| runId = run.RunId
                   status = run.Status
                   evaluation = Autopilot.evaluate run
                   nextAction = action |})
            actionExitCode action
    with ex ->
        eprintfn "Autopilot-Beobachtung konnte nicht geladen werden: %s" ex.Message
        1

let private boundedDetail (value: string) =
    let value = if isNull value then "" else value.Trim()
    if value.Length <= 8000 then value
    else value.Substring(0, 8000) + "\n… output truncated; evidence digest covers the full output"

let private executeWithInput
    (program: string)
    (arguments: string list)
    (workingDirectory: string)
    (timeoutSeconds: int)
    (standardInput: string option) =
    let startInfo = ProcessStartInfo(program)
    startInfo.WorkingDirectory <- Path.GetFullPath workingDirectory
    startInfo.RedirectStandardOutput <- true
    startInfo.RedirectStandardError <- true
    startInfo.RedirectStandardInput <- standardInput.IsSome
    startInfo.UseShellExecute <- false
    startInfo.CreateNoWindow <- true
    for argument in arguments do startInfo.ArgumentList.Add argument
    use child = new Process()
    child.StartInfo <- startInfo
    let timer = Stopwatch.StartNew()
    if not (child.Start()) then failwithf "Prozess %s konnte nicht gestartet werden." program
    let stdout = child.StandardOutput.ReadToEndAsync()
    let stderr = child.StandardError.ReadToEndAsync()
    match standardInput with
    | Some input ->
        child.StandardInput.Write(input)
        child.StandardInput.Close()
    | None -> ()
    let completed = child.WaitForExit(timeoutSeconds * 1000)
    if not completed then
        try child.Kill true with _ -> ()
        child.WaitForExit()
    let output = stdout.GetAwaiter().GetResult()
    let error = stderr.GetAwaiter().GetResult()
    timer.Stop()
    let exitCode = if completed then child.ExitCode else -1
    exitCode, timer.ElapsedMilliseconds, output, error, completed

let private execute program arguments workingDirectory timeoutSeconds =
    executeWithInput program arguments workingDirectory timeoutSeconds None

let private cmdAutopilotGate runDirectory workingDirectory =
    match Autopilot.load runDirectory with
    | Error errors -> printAutopilotErrors errors; 1
    | Ok run ->
        match Autopilot.nextAction run with
        | Autopilot.ExecuteGate request ->
            try
                let exitCode, duration, stdout, stderr, completed =
                    execute request.Gate.Program request.Gate.Arguments workingDirectory request.Gate.TimeoutSeconds
                let evidencePayload =
                    {| gateId = request.Gate.Id
                       validatorId = request.Gate.ValidatorId
                       candidateDigest = request.CandidateDigest
                       program = request.Gate.Program
                       arguments = request.Gate.Arguments
                       timeoutSeconds = request.Gate.TimeoutSeconds
                       completed = completed
                       exitCode = exitCode
                       stdout = stdout
                       stderr = stderr |}
                let evidence = Json.serialize evidencePayload
                let fullDetail = boundedDetail evidence
                let evidenceDigest = Eidos.sha256 evidence
                let observation =
                    Autopilot.GateObserved
                        { GateId = request.Gate.Id
                          ValidatorId = request.Gate.ValidatorId
                          CandidateDigest = request.CandidateDigest
                          Passed = completed && exitCode = 0
                          ExitCode = exitCode
                          EvidenceDigest = evidenceDigest
                          DurationMilliseconds = duration
                          Detail = fullDetail }
                match Autopilot.record runDirectory DateTimeOffset.UtcNow observation with
                | Error errors -> printAutopilotErrors errors; 1
                | Ok updated ->
                    printfn "%s" (Json.serialize
                        {| gate = request.Gate.Id
                           passed = completed && exitCode = 0
                           evidenceDigest = evidenceDigest
                           nextAction = Autopilot.nextAction updated |})
                    if completed && exitCode = 0 then 0 else 1
            with ex ->
                eprintfn "Gate %s konnte nicht ausgeführt werden: %s" request.Gate.Id ex.Message
                1
        | action ->
            eprintfn "Kein Gate erwartet. Nächste Aktion: %s" (Json.serialize action)
            1

let private gitValue workingDirectory arguments =
    let exitCode, _, stdout, stderr, completed = execute "git" arguments workingDirectory 20
    if completed && exitCode = 0 then Ok(stdout.Trim())
    else Error(boundedDetail stderr)

let private cmdAutopilotCheckpoint runDirectory workingDirectory =
    match Autopilot.load runDirectory with
    | Error errors -> printAutopilotErrors errors; 1
    | Ok run ->
        match Autopilot.nextAction run with
        | Autopilot.CreateCheckpoint request ->
            match gitValue workingDirectory [ "rev-parse"; "HEAD" ], gitValue workingDirectory [ "status"; "--porcelain" ] with
            | Ok commit, Ok status ->
                let clean = String.IsNullOrWhiteSpace status
                let detail = if clean then "Git checkpoint is clean." else "Uncommitted paths remain:\n" + status
                let observation =
                    Autopilot.CheckpointObserved
                        { SliceId = request.SliceId
                          CandidateDigest = request.CandidateDigest
                          Succeeded = clean
                          CommitHash = commit
                          CleanWorktree = clean
                          Detail = boundedDetail detail }
                match Autopilot.record runDirectory DateTimeOffset.UtcNow observation with
                | Error errors -> printAutopilotErrors errors; 1
                | Ok updated ->
                    printfn "%s" (Json.serialize
                        {| clean = clean
                           commit = commit
                           nextAction = Autopilot.nextAction updated |})
                    if clean then 0 else 1
            | Error error, _ | _, Error error ->
                eprintfn "Git-Checkpoint konnte nicht geprüft werden: %s" error
                1
        | action ->
            eprintfn "Kein Checkpoint erwartet. Nächste Aktion: %s" (Json.serialize action)
            1

let private cmdAutopilotStatus runDirectory =
    match Autopilot.load runDirectory with
    | Error errors -> printAutopilotErrors errors; 1
    | Ok run ->
        let action = Autopilot.nextAction run
        printfn "%s" (Json.serialize
            {| schemaVersion = run.SchemaVersion
               runId = run.RunId
               missionId = run.Plan.MissionId
               status = run.Status
               activeSliceIndex = run.ActiveSliceIndex
               slices = run.SliceExecutions
               blockReasons = run.BlockReasons
               evaluation = Autopilot.evaluate run
               ledgerVerified = Autopilot.verifyLedger run.Ledger
               nextAction = action |})
        actionExitCode action

let private cmdAutopilotDrive runDirectory adapter workingDirectory maxSteps =
    if maxSteps <= 0 || maxSteps > 1000 then
        eprintfn "--max-steps muss zwischen 1 und 1000 liegen."
        1
    else
        let rec drive step =
            match Autopilot.load runDirectory with
            | Error errors -> printAutopilotErrors errors; 1
            | Ok run ->
                let before = run.Ledger.Length
                match Autopilot.nextAction run with
                | Autopilot.MissionComplete evaluation ->
                    printfn "%s" (Json.serialize {| event = "MissionComplete"; evaluation = evaluation |})
                    0
                | Autopilot.Escalate reasons ->
                    printfn "%s" (Json.serialize {| event = "Escalate"; reasons = reasons |})
                    2
                | _ when step >= maxSteps ->
                    eprintfn "Autopilot-Schrittbudget %d erreicht; der persistierte Run kann fortgesetzt werden." maxSteps
                    3
                | action ->
                    match action with
                    | Autopilot.ExecuteGate _ ->
                        cmdAutopilotGate runDirectory workingDirectory |> ignore
                        match Autopilot.load runDirectory with
                        | Ok updated when updated.Ledger.Length > before -> drive (step + 1)
                        | _ -> 1
                    | Autopilot.CreateCheckpoint _ ->
                        cmdAutopilotCheckpoint runDirectory workingDirectory |> ignore
                        match Autopilot.load runDirectory with
                        | Ok updated when updated.Ledger.Length > before -> drive (step + 1)
                        | _ -> 1
                    | Autopilot.DispatchAgent _ as action ->
                        try
                            let input = Json.serialize action
                            let exitCode, _, stdout, stderr, completed =
                                executeWithInput adapter [] workingDirectory 3600 (Some input)
                            if not (String.IsNullOrWhiteSpace stderr) then eprintfn "%s" (boundedDetail stderr)
                            if not completed || exitCode <> 0 then
                                eprintfn "Harness-Adapter endete ohne Beobachtung (exit %d). Der Run bleibt unverändert resumierbar." exitCode
                                1
                            else
                                let observation = Json.deserialize<Autopilot.RunObservation> stdout
                                match Autopilot.record runDirectory DateTimeOffset.UtcNow observation with
                                | Error errors -> printAutopilotErrors errors; 1
                                | Ok updated ->
                                    printfn "%s" (Json.serialize
                                        {| event = "ObservationRecorded"
                                           sequence = updated.Ledger.Length
                                           nextAction = Autopilot.nextAction updated |})
                                    drive (step + 1)
                        with ex ->
                            eprintfn "Harness-Adapter konnte nicht verarbeitet werden: %s" ex.Message
                            1
                    | Autopilot.MissionComplete _ | Autopilot.Escalate _ ->
                        failwith "unreachable terminal controller action"
        drive 0

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
        | [| "derive-code" |]        -> cmdDeriveCode None
        | [| "derive-code"; "--out"; path |] -> cmdDeriveCode (Some path)
        | [| "sync-docs" |]          -> cmdSyncDocs false
        | [| "sync-docs"; "--check" |] -> cmdSyncDocs true
        | [| "eidos"; "run" |] -> cmdEidosRun None "none"
        | [| "eidos"; "run"; "--out"; outputRoot |] ->
            cmdEidosRun (Some outputRoot) "none"
        | [| "eidos"; "run"; "--fault"; fault |] ->
            cmdEidosRun None fault
        | [| "eidos"; "run"; "--out"; outputRoot; "--fault"; fault |]
        | [| "eidos"; "run"; "--fault"; fault; "--out"; outputRoot |] ->
            cmdEidosRun (Some outputRoot) fault
        | [| "eidos"; "replay"; runDir |] -> cmdEidosReplay runDir
        | [| "eidos"; "benchmark" |] -> cmdEidosBenchmark None
        | [| "eidos"; "benchmark"; "--out"; outputRoot |] ->
            cmdEidosBenchmark (Some outputRoot)
        | [| "autopilot"; "init"; planPath |] ->
            cmdAutopilotInit planPath None
        | [| "autopilot"; "init"; planPath; "--workspace"; workspace |]
        | [| "autopilot"; "init"; planPath; "--root"; workspace |] ->
            cmdAutopilotInit planPath (Some workspace)
        | [| "autopilot"; "next"; runDirectory |] ->
            cmdAutopilotNext runDirectory
        | [| "autopilot"; "record"; runDirectory; observationPath |] ->
            cmdAutopilotRecord runDirectory observationPath
        | [| "autopilot"; "gate"; runDirectory |] ->
            cmdAutopilotGate runDirectory root
        | [| "autopilot"; "gate"; runDirectory; "--cwd"; workingDirectory |] ->
            cmdAutopilotGate runDirectory workingDirectory
        | [| "autopilot"; "checkpoint"; runDirectory |] ->
            cmdAutopilotCheckpoint runDirectory root
        | [| "autopilot"; "checkpoint"; runDirectory; "--cwd"; workingDirectory |] ->
            cmdAutopilotCheckpoint runDirectory workingDirectory
        | [| "autopilot"; "drive"; runDirectory; adapter; "--cwd"; workingDirectory |] ->
            cmdAutopilotDrive runDirectory adapter workingDirectory 100
        | [| "autopilot"; "drive"; runDirectory; adapter; "--cwd"; workingDirectory; "--max-steps"; value |] ->
            match Int32.TryParse value with
            | true, maxSteps -> cmdAutopilotDrive runDirectory adapter workingDirectory maxSteps
            | _ -> eprintfn "Ungültiges --max-steps: %s" value; 1
        | [| "autopilot"; "status"; runDirectory |] ->
            cmdAutopilotStatus runDirectory
        | [| "export-context" |] ->
            printf "%s" (Store.load root |> Export.toMarkdown)
            0
        | [| "export-context"; "--out"; path |] ->
            System.IO.File.WriteAllText(path, Store.load root |> Export.toMarkdown)
            printfn "Kontextpaket geschrieben: %s" path
            0
        | [||] | [| "help" |] | [| "--help" |] | [| "-h" |] -> usage (); 0
        | _                          ->
            eprintfn "Unbekanntes Kommando oder Argument: %s" (String.concat " " argv)
            usage ()
            1
    with ex ->
        eprintfn "Fehler: %s" ex.Message
        1
