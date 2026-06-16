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
            let contextMd =
                try Export.toMarkdown (Store.load root)
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

    printfn "CDD Web — SPOT-Root: %s" root
    app.Run()
    0
