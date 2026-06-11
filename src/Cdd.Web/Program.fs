open System
open System.IO
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Cdd.Core
open Cdd.Core.Spot

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

    printfn "CDD Web — SPOT-Root: %s" root
    app.Run()
    0
