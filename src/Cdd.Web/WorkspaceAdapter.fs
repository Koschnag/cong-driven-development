namespace Cdd.Web

open System
open System.Diagnostics
open System.IO
open System.Text.Json
open Cdd.Core.Studio

/// Read-only adapter for a repository workspace. It translates Git and the
/// deliberately small `.ai` interchange format into CDD's vendor-neutral core
/// observations. It never returns local paths, environment values or secrets.
module WorkspaceAdapter =

    let private textOr (fallback: string) (value: string) =
        if String.IsNullOrWhiteSpace value then fallback else value.Trim()

    let private property (name: string) (element: JsonElement) =
        match element.TryGetProperty name with
        | true, value -> Some value
        | _ -> None

    let private stringProperty (name: string) (element: JsonElement) =
        property name element
        |> Option.bind (fun value ->
            if value.ValueKind = JsonValueKind.String then value.GetString() |> Option.ofObj
            else None)

    let private stringArrayProperty (name: string) (element: JsonElement) =
        property name element
        |> Option.filter (fun value -> value.ValueKind = JsonValueKind.Array)
        |> Option.map (fun value ->
            value.EnumerateArray()
            |> Seq.choose (fun item ->
                if item.ValueKind = JsonValueKind.String then item.GetString() |> Option.ofObj
                else None)
            |> Seq.toList)
        |> Option.defaultValue []

    let private readJson (path: string) (reader: JsonElement -> 'T) (fallback: 'T) =
        try
            use document = JsonDocument.Parse(File.ReadAllText path)
            reader document.RootElement
        with _ -> fallback

    let private runGit root (arguments: string list) =
        try
            let startInfo = ProcessStartInfo("git")
            startInfo.WorkingDirectory <- root
            startInfo.RedirectStandardOutput <- true
            startInfo.RedirectStandardError <- true
            startInfo.UseShellExecute <- false
            startInfo.CreateNoWindow <- true
            for argument in arguments do
                startInfo.ArgumentList.Add argument
            use child = Process.Start startInfo
            let output = child.StandardOutput.ReadToEndAsync()
            let errors = child.StandardError.ReadToEndAsync()
            if child.WaitForExit 4000 && child.ExitCode = 0 then
                errors.GetAwaiter().GetResult() |> ignore
                Some(output.GetAwaiter().GetResult().Trim())
            else
                try child.Kill true with _ -> ()
                None
        with _ -> None

    let private intOrZero (value: string) =
        match Int32.TryParse value with
        | true, parsed -> parsed
        | _ -> 0

    let private sanitizeRemote (remote: string) =
        match Uri.TryCreate(remote, UriKind.Absolute) with
        | true, uri when not (String.IsNullOrEmpty uri.UserInfo) ->
            let builder = UriBuilder uri
            builder.UserName <- ""
            builder.Password <- ""
            builder.Uri.AbsoluteUri
        | _ -> remote

    let private observeGit root =
        match runGit root [ "rev-parse"; "--show-toplevel" ] with
        | None ->
            { Available = false; Branch = ""; Commit = ""; CommitTitle = ""
              CommittedAt = ""; Remote = ""; DirtyFiles = 0; Ahead = 0; Behind = 0 }
        | Some _ ->
            let value args = runGit root args |> Option.defaultValue ""
            let ahead, behind =
                match runGit root [ "rev-list"; "--left-right"; "--count"; "HEAD...@{upstream}" ] with
                | Some counts ->
                    match counts.Split([| ' '; '\t' |], StringSplitOptions.RemoveEmptyEntries) with
                    | parts when parts.Length = 2 -> intOrZero parts.[0], intOrZero parts.[1]
                    | _ -> 0, 0
                | None -> 0, 0
            let dirty =
                value [ "status"; "--porcelain" ]
                |> fun output -> if String.IsNullOrWhiteSpace output then 0 else output.Split('\n').Length
            { Available = true
              Branch = value [ "branch"; "--show-current" ] |> textOr "detached"
              Commit = value [ "rev-parse"; "--short=12"; "HEAD" ]
              CommitTitle = value [ "log"; "-1"; "--pretty=%s" ]
              CommittedAt = value [ "log"; "-1"; "--pretty=%cI" ]
              Remote = value [ "remote"; "get-url"; "origin" ] |> sanitizeRemote
              DirtyFiles = dirty
              Ahead = ahead
              Behind = behind }

    let private readProjectId (root: string) =
        let path = Path.Combine(root, ".ai", "config.json")
        if File.Exists path then
            readJson path (stringProperty "projectId" >> Option.defaultValue "") ""
        else ""

    let private readName (root: string) (projectId: string) =
        let readme = Path.Combine(root, "README.md")
        let heading =
            try
                if File.Exists readme then
                    File.ReadLines readme
                    |> Seq.tryFind (fun line -> line.TrimStart().StartsWith("# ", StringComparison.Ordinal))
                    |> Option.map (fun line -> line.Trim().Substring(2).Trim())
                else None
            with _ -> None
        heading
        |> Option.filter (String.IsNullOrWhiteSpace >> not)
        |> Option.defaultValue (
            if projectId.StartsWith("project-", StringComparison.OrdinalIgnoreCase) then projectId.Substring 8
            elif not (String.IsNullOrWhiteSpace projectId) then projectId
            else Path.GetFileName root)

    let private readWorkItems (root: string) =
        let tasks = Path.Combine(root, ".ai", "tasks")
        if not (Directory.Exists tasks) then []
        else
            Directory.GetFiles(tasks, "*.json", SearchOption.TopDirectoryOnly)
            |> Array.sort
            |> Array.choose (fun path ->
                readJson path (fun item ->
                    stringProperty "id" item
                    |> Option.map (fun id ->
                        { Id = id
                          Title = stringProperty "title" item |> Option.defaultValue id
                          Status = stringProperty "status" item |> Option.defaultValue "unknown"
                          Objective = stringProperty "objective" item |> Option.defaultValue ""
                          RequiredGates = stringArrayProperty "requiredGates" item })) None)
            |> Array.toList

    let private readRuns (root: string) =
        let runs = Path.Combine(root, ".ai", "runtime", "runs")
        if not (Directory.Exists runs) then []
        else
            Directory.GetDirectories runs
            |> Array.sortDescending
            |> Array.choose (fun directory ->
                let manifest = Path.Combine(directory, "run.json")
                if not (File.Exists manifest) then None
                else
                    readJson manifest (fun item ->
                        stringProperty "runId" item
                        |> Option.map (fun id ->
                            { Id = id
                              Status = stringProperty "status" item |> Option.defaultValue "unknown"
                              StartedAt = stringProperty "startedAtUtc" item |> Option.defaultValue ""
                              FinishedAt = stringProperty "finishedAtUtc" item
                              HasSummary = File.Exists(Path.Combine(directory, "summary.json")) })) None)
            |> Array.toList

    let private countSpot (root: string) =
        let directory = Path.Combine(root, ".spot")
        if Directory.Exists directory then Directory.GetFiles(directory, "*.json").Length else 0

    let observe (root: string) (observedAt: DateTimeOffset) : WorkspaceObservation =
        let safeRoot = Path.GetFullPath root
        let projectId = readProjectId safeRoot
        let sources =
            [ let gitMarker = Path.Combine(safeRoot, ".git")
              if Directory.Exists gitMarker || File.Exists gitMarker then "git"
              if File.Exists(Path.Combine(safeRoot, ".ai", "config.json")) then ".ai/config.json"
              if Directory.Exists(Path.Combine(safeRoot, ".ai", "tasks")) then ".ai/tasks/*.json"
              if Directory.Exists(Path.Combine(safeRoot, ".ai", "runtime", "runs")) then ".ai/runtime/runs/*/run.json"
              if Directory.Exists(Path.Combine(safeRoot, ".spot")) then ".spot/*.json" ]
        { Id = textOr (Path.GetFileName safeRoot) projectId
          Name = readName safeRoot projectId
          Git = observeGit safeRoot
          WorkItems = readWorkItems safeRoot
          Runs = readRuns safeRoot
          SpotNodes = countSpot safeRoot
          Sources = sources
          ObservedAt = observedAt }

    let snapshot (root: string) (observedAt: DateTimeOffset) =
        observe root observedAt |> Cdd.Core.Studio.projectWorkspace
