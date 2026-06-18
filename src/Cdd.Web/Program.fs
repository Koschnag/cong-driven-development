open System
open System.IO
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Cdd.Core
open Cdd.Core.Spot

/// Anfrage an die Headless-Engine (GUI → /api/engine/run).
type private EngineRunRequest =
    { Prompt  : string
      Surface : string   // plan | ideate | develop | monitor | deploy
      Engine  : string   // claude | mistral | ollama
      Model   : string }

/// Einheitliche JSON-Antworten über Cdd.Core.Json (nicht ASP.NETs Default-Serializer,
/// der F#-DUs nicht versteht).
let private json (value: 'T) : IResult =
    Results.Text(Json.serialize value, "application/json")

let private badRequest (msg: string) : IResult =
    Results.Text(Json.serialize {| error = msg |}, "application/json", statusCode = 400)

/// Lädt den Store und wandelt Lade-Fehler (z. B. korrupte JSON-Files)
/// in saubere 500-JSON-Antworten statt unbehandelter Exceptions.
let private withStore (root: string) (f: SpotEntry list -> IResult) : IResult =
    try
        f (Store.load root)
    with ex ->
        Results.Text(Json.serialize {| error = ex.Message |}, "application/json", statusCode = 500)

[<EntryPoint>]
let main argv =
    // "--root <pfad>" gehört uns; alle übrigen Argumente (z. B. --urls) gehen an ASP.NET.
    let rec extractRoot acc args =
        match args with
        | "--root" :: value :: rest -> Some value, List.rev acc @ rest
        | x :: rest -> extractRoot (x :: acc) rest
        | [] -> None, List.rev acc
    let rootArg, webArgs = extractRoot [] (List.ofArray argv)
    let root =
        rootArg
        |> Option.defaultValue (Directory.GetCurrentDirectory())
        |> Path.GetFullPath

    let builder = WebApplication.CreateBuilder(Array.ofList webArgs)
    let app = builder.Build()

    app.UseDefaultFiles() |> ignore
    app.UseStaticFiles() |> ignore

    app.MapGet("/api/spot", Func<IResult>(fun () ->
        withStore root json)) |> ignore

    app.MapPut("/api/spot/{id}", Func<string, HttpRequest, Task<IResult>>(fun id req ->
        task {
            use reader = new StreamReader(req.Body)
            let! body = reader.ReadToEndAsync()
            try
                let entry = Json.deserialize<SpotEntry> body
                if idValue entry.Id <> id then
                    return badRequest "Id in URL und Body stimmen nicht überein"
                elif not (Store.isValidId entry.Id) then
                    return badRequest "Ungültige Id (erlaubt: a-zA-Z0-9_-)"
                else
                    Store.save root entry
                    return json entry
            with ex ->
                return badRequest (sprintf "JSON ungültig: %s" ex.Message)
        })) |> ignore

    app.MapDelete("/api/spot/{id}", Func<string, IResult>(fun id ->
        if Store.delete root (EntityId id) then Results.NoContent()
        else Results.NotFound())) |> ignore

    app.MapGet("/api/validate", Func<IResult>(fun () ->
        withStore root (Validate.validate >> json))) |> ignore

    app.MapGet("/api/diff", Func<IResult>(fun () ->
        withStore root (Diff.report >> json))) |> ignore

    app.MapGet("/api/export", Func<IResult>(fun () ->
        withStore root (fun entries ->
            Results.Text(Export.toMarkdown entries, "text/markdown")))) |> ignore

    // @-Memory (Wahrheit #2): Volltextsuche über die SANITISIERTE cong-memory.db (FTS5).
    // sensitive=0 ist nicht verhandelbar: die DB enthält bereits NUR sensitive=0-Daten (klinische
    // Daten verlassen den Mac nie); falls CDD_MEMORY_DB doch auf eine DB mit sensitive-Spalte zeigt,
    // wird hart doppelt gefiltert. Read-only. Kein Treffer ohne Conversation (Doppel-Join).
    app.MapGet("/api/dwh/search", Func<HttpRequest, IResult>(fun req ->
        let q = req.Query.["q"].ToString()
        let limit =
            match System.Int32.TryParse(req.Query.["limit"].ToString()) with
            | true, n when n > 0 && n <= 50 -> n
            | _ -> 20
        let dbPath = Environment.GetEnvironmentVariable("CDD_MEMORY_DB")
        if System.String.IsNullOrWhiteSpace q then
            json {| available = true; hits = ([||]: obj[]) |}
        elif System.String.IsNullOrWhiteSpace dbPath || not (System.IO.File.Exists dbPath) then
            json {| available = false; note = "Memory-DB nicht gemountet (CDD_MEMORY_DB)"; hits = ([||]: obj[]) |}
        else
            try
                use conn = new Microsoft.Data.Sqlite.SqliteConnection(sprintf "Data Source=%s;Mode=ReadOnly" dbPath)
                conn.Open()
                let hasSensitive =
                    use chk = conn.CreateCommand()
                    chk.CommandText <- "SELECT COUNT(*) FROM pragma_table_info('messages') WHERE name='sensitive'"
                    System.Convert.ToInt32(chk.ExecuteScalar()) > 0
                let sensFilter = if hasSensitive then " AND m.sensitive=0 AND c.sensitive=0" else ""
                use cmd = conn.CreateCommand()
                cmd.CommandText <-
                    "SELECT c.system, c.title, m.role, m.created_at, " +
                    "snippet(messages_fts, 0, '[', ']', '…', 12), m.conv_id, m.msg_id " +
                    "FROM messages_fts f " +
                    "JOIN messages m ON f.rowid = m.msg_id " +
                    "JOIN conversations c ON m.conv_id = c.conv_id " +
                    "WHERE messages_fts MATCH @q" + sensFilter + " ORDER BY rank LIMIT @limit"
                cmd.Parameters.AddWithValue("@q", q) |> ignore
                cmd.Parameters.AddWithValue("@limit", limit) |> ignore
                use rd = cmd.ExecuteReader()
                let hits = ResizeArray<obj>()
                while rd.Read() do
                    let g (i: int) = if rd.IsDBNull(i) then "" else rd.GetValue(i).ToString()
                    hits.Add(box {| system = g 0; title = g 1; role = g 2; created_at = g 3
                                    snippet = g 4; conv_id = g 5; msg_id = g 6 |})
                json {| available = true; hits = hits.ToArray() |}
            with ex ->
                json {| available = false; note = sprintf "Suche fehlgeschlagen: %s" ex.Message; hits = ([||]: obj[]) |})) |> ignore

    // Infra/Prod-Heartbeat für die Bühne (Komodo/Coolify werden via MCP adoptiert).
    // Bis das MCP-Backend verdrahtet ist: liefert den deklarierten DC-Plan + ok=false,
    // damit die GUI nie einen toten/leeren View zeigt (Souveränität: lokaler Wahrheits-Plan).
    app.MapGet("/api/infra/status", Func<IResult>(fun () ->
        let host name role state =
            {| name = name; role = role; state = state |}
        json {| ok = false
                source = "static-plan"
                hosts =
                    [ host "pi"      "Infra (DNS · Reverse-Proxy · Tailscale)" "unknown"
                      host "celsius" "Services (Nextcloud · YunoHost · Backups)" "unknown"
                      host "tower"   "Proxmox (VMs · Gaming-VM)" "unknown" ]
                apps = ([] : obj list) |})) |> ignore

    // Modell-Historie aus git: weil jeder Knoten ein .spot/-JSON-File ist, IST `git log` die Historie.
    // Read-only, defensiv ([] ohne git). Zeitreise: /{id}/{hash} liefert den Knoten-Stand zum Commit.
    app.MapGet("/api/history", Func<HttpRequest, IResult>(fun req ->
        let lim = match System.Int32.TryParse(req.Query.["limit"].ToString()) with | true, n -> n | _ -> 60
        json (History.model root lim))) |> ignore

    app.MapGet("/api/history/{id}", Func<string, IResult>(fun id ->
        json (History.node root id 100))) |> ignore

    app.MapGet("/api/history/{id}/{hash}", Func<string, string, IResult>(fun id hash ->
        Results.Text(History.nodeAt root id hash, "application/json"))) |> ignore

    app.MapPost("/api/derive-tests", Func<HttpRequest, IResult>(fun req ->
        let write = req.Query.["write"].ToString() = "true"
        withStore root (fun entries ->
            let derived = Derive.deriveTests entries
            if write then derived |> List.iter (Store.save root)
            json {| derived = derived; written = write |}))) |> ignore

    // Headless-Engine auf Terminal-Ebene: streamt EngineEvents als Server-Sent-Events.
    // CDD liefert die Entropie (SPOT-Export) + verbindet die SPOT-Tools via MCP; die
    // LLM-Engine (Claude Code / Mistral / Ollama) macht die Arbeit.
    app.MapPost("/api/engine/run", Func<HttpContext, Task>(fun ctx ->
        task {
            use reader = new StreamReader(ctx.Request.Body)
            let! bodyStr = reader.ReadToEndAsync()
            let req =
                try Json.deserialize<EngineRunRequest> bodyStr
                with _ -> { Prompt = bodyStr; Surface = "develop"; Engine = "claude"; Model = "" }
            // Surface-Cut statt Full-Dump: nur der axiomatische Kern + Index + die im Auftrag
            // genannten Knoten gehen in JEDEN Run (Token-Ökonomie, behebt den Kontext-Dump-Bug).
            let contextMd =
                try Export.toContextSlice req.Prompt (Store.load root)
                with _ -> ""
            ctx.Response.ContentType <- "text/event-stream"
            ctx.Response.Headers.["Cache-Control"] <- Microsoft.Extensions.Primitives.StringValues("no-cache")
            ctx.Response.Headers.["X-Accel-Buffering"] <- Microsoft.Extensions.Primitives.StringValues("no")
            let emit (ev: Engine.EngineEvent) : Task =
                task {
                    do! ctx.Response.WriteAsync(sprintf "data: %s\n\n" (Engine.toGuiJson ev))
                    do! ctx.Response.Body.FlushAsync()
                }
            let kind = Engine.kindOfString req.Engine
            let opts : Engine.EngineOptions =
                { Kind = kind
                  Model = req.Model
                  Cwd = root
                  // Unbeaufsichtigt → bewusst enge Allowlist. Lesen/Editieren/Suchen + git/dotnet + SPOT-MCP.
                  AllowedTools = [ "Read"; "Edit"; "Write"; "Grep"; "Glob"; "Bash(git *)"; "Bash(dotnet *)"; "mcp__spot__*" ]
                  PermissionMode = (if kind = Engine.ClaudeCode then "acceptEdits" else "")
                  McpConfigJson = (if kind = Engine.ClaudeCode then Engine.spotMcpConfig root else "")
                  BaseUrl = (Environment.GetEnvironmentVariable("CDD_ENGINE_BASEURL") |> Option.ofObj |> Option.defaultValue "")
                  ApiKey = (Environment.GetEnvironmentVariable("MISTRAL_API_KEY") |> Option.ofObj |> Option.defaultValue "") }
            do! emit (Engine.Started("", sprintf "%A (Fläche: %s)" kind req.Surface))
            try
                do! (Engine.create kind).Run({ Prompt = req.Prompt; ContextMd = contextMd; Options = opts }, emit)
            with ex ->
                do! emit (Engine.EngineError ex.Message)
            do! ctx.Response.WriteAsync("event: done\ndata: {}\n\n")
            do! ctx.Response.Body.FlushAsync()
        } :> Task)) |> ignore

    // Loop bis Konvergenz: das Cockpit treibt den Mapper (cdd-mapper --go --json) als Subprozess und
    // streamt dessen JSON-Ereignisse als SSE — der Loop wird wie ein Engine-Turn angezeigt.
    // Cong-OS = Steuerzentrale (orchestriert), Cdd.Mapper = Ausführer, Cdd.Core = das Gate. Keine Doppelung.
    app.MapPost("/api/loop/run", Func<HttpContext, Task>(fun ctx ->
        task {
            use reader = new StreamReader(ctx.Request.Body)
            let! bodyStr = reader.ReadToEndAsync()
            let spec, maxSpecs, maxAttempts =
                try
                    let j = System.Text.Json.JsonDocument.Parse(bodyStr).RootElement
                    let getS (n: string) = match j.TryGetProperty(n) with
                                           | true, v when v.ValueKind = System.Text.Json.JsonValueKind.String -> v.GetString()
                                           | _ -> ""
                    let getI (n: string) d = match j.TryGetProperty(n) with
                                             | true, v when v.ValueKind = System.Text.Json.JsonValueKind.Number -> v.GetInt32()
                                             | _ -> d
                    getS "Spec", getI "MaxSpecs" 1, getI "MaxAttempts" 3
                with _ -> "", 1, 3
            ctx.Response.ContentType <- "text/event-stream"
            ctx.Response.Headers.["Cache-Control"] <- Microsoft.Extensions.Primitives.StringValues("no-cache")
            ctx.Response.Headers.["X-Accel-Buffering"] <- Microsoft.Extensions.Primitives.StringValues("no")
            let send (s: string) : Task =
                task {
                    do! ctx.Response.WriteAsync(sprintf "data: %s\n\n" s)
                    do! ctx.Response.Body.FlushAsync()
                }
            do! send (Json.serialize {| t = "started"; model = sprintf "cdd-mapper · Loop bis Konvergenz (max %d Spec, %d Versuche)" maxSpecs maxAttempts |})
            try
                let psi = System.Diagnostics.ProcessStartInfo("cdd-mapper")
                psi.WorkingDirectory <- root
                psi.RedirectStandardOutput <- true
                psi.RedirectStandardError <- true
                psi.UseShellExecute <- false
                let add (s: string) = psi.ArgumentList.Add s
                let addPair (f: string) (v: string) = add f; add v
                add "--root"; add root; add "--go"; add "--json"
                addPair "--max-specs" (string maxSpecs)
                addPair "--max-attempts" (string maxAttempts)
                if spec <> "" then addPair "--spec" spec
                use proc = new System.Diagnostics.Process()
                proc.StartInfo <- psi
                proc.Start() |> ignore
                let mutable go = true
                while go do
                    let! line = proc.StandardOutput.ReadLineAsync()
                    if isNull line then go <- false
                    elif line.StartsWith("{") then do! send line
                proc.WaitForExit()
            with ex ->
                do! send (Json.serialize {| t = "error"; error = sprintf "Loop-Fehler (cdd-mapper im PATH? `dotnet tool install`?): %s" ex.Message |})
            do! ctx.Response.WriteAsync("event: done\ndata: {}\n\n")
            do! ctx.Response.Body.FlushAsync()
        } :> Task)) |> ignore

    printfn "CDD Web — SPOT-Root: %s" root
    app.Run()
    0
