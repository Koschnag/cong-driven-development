namespace Cdd.Core

/// Round-Trip-Engineering (Code → Modell): gleicht Component-Knoten gegen die
/// realen Projekt-Referenzen der .fsproj-Dateien ab und setzt Konvergenz-Status.
module Sync =

    open System.IO
    open System.Text.RegularExpressions
    open Cdd.Core.Spot

    /// Ein Projekt im Code: Name (Dateiname ohne Endung) + referenzierte Projektnamen.
    type CodeProject =
        { Name       : string
          References : string list }

    /// Liest alle *.fsproj/*.csproj unter srcDir (rekursiv) und extrahiert ProjectReferences.
    let scanProjects (srcDir: string) : CodeProject list =
        if not (Directory.Exists srcDir) then []
        else
            [| "*.fsproj"; "*.csproj" |]
            |> Array.collect (fun pat -> Directory.GetFiles(srcDir, pat, SearchOption.AllDirectories))
            |> Array.sort
            |> Array.map (fun f ->
                let refs =
                    Regex.Matches(File.ReadAllText f, "ProjectReference\\s+Include=\"([^\"]+)\"")
                    |> Seq.map (fun m ->
                        Path.GetFileNameWithoutExtension(m.Groups.[1].Value.Replace('\\', '/')))
                    |> List.ofSeq
                { Name = Path.GetFileNameWithoutExtension f; References = refs })
            |> List.ofArray

    /// Konventionelle Code-Wurzeln eines Repos — Werkzeuge sind Code wie alles andere.
    let projectRoots = [ "src"; "tools"; "apps" ]

    /// Scannt alle Projekt-Wurzeln unter root (src, tools, apps).
    let scanRepo (root: string) : CodeProject list =
        projectRoots
        |> List.collect (fun d -> scanProjects (Path.Combine(root, d)))

    type SyncResult =
        { Id     : EntityId
          Name   : string
          Status : Convergence
          Detail : string }

    /// Vergleicht Code-Projekte mit den Component-Knoten (Zuordnung über Name).
    /// Liefert den Report und die Knoten mit aktualisiertem Konvergenz-Status.
    let compare (projects: CodeProject list) (entries: SpotEntry list) : SyncResult list * SpotEntry list =
        let comps =
            entries
            |> List.choose (fun e ->
                match e.Payload with
                | ComponentNode c -> Some(e, c)
                | _ -> None)
        let byName = comps |> List.map (fun (e, c) -> c.Name, (e, c)) |> Map.ofList
        let idByName = comps |> List.map (fun (e, c) -> c.Name, e.Id) |> Map.ofList

        let results =
            [ for p in projects do
                match Map.tryFind p.Name byName with
                | None ->
                    yield { Id = EntityId("comp-" + p.Name.ToLowerInvariant().Replace(".", "-"))
                            Name = p.Name; Status = Orphaned
                            Detail = "Code-Projekt ohne Modell-Komponente" }
                | Some(e, c) ->
                    let expected =
                        p.References
                        |> List.choose (fun r -> Map.tryFind r idByName)
                        |> List.map idValue
                        |> Set.ofList
                    let actual = c.DependsOn |> List.map idValue |> Set.ofList
                    if expected = actual then
                        yield { Id = e.Id; Name = p.Name; Status = Aligned; Detail = "synchron" }
                    else
                        yield { Id = e.Id; Name = p.Name; Status = Diverged
                                Detail = sprintf "Modell: [%s] · Code: [%s]"
                                             (String.concat ", " actual) (String.concat ", " expected) }
              for e, c in comps do
                if projects |> List.forall (fun p -> p.Name <> c.Name) then
                    yield { Id = e.Id; Name = c.Name; Status = Pending
                            Detail = "Modell-Komponente ohne Code" } ]

        let statusById =
            results
            |> List.filter (fun r -> entries |> List.exists (fun e -> e.Id = r.Id))
            |> List.map (fun r -> r.Id, r.Status)
            |> Map.ofList
        let updated =
            entries
            |> List.map (fun e ->
                match e.Payload, Map.tryFind e.Id statusById with
                | ComponentNode _, Some s -> { e with Convergence = s }
                | _ -> e)
        results, updated

    /// Welche Test-Knoten tragen einen Marker im Test-Quellcode?
    /// Erkennung über Marker-Präsenz (kein Testlauf, keine Pass/Fail-Prüfung):
    ///   F#/C#:  [<Trait("spot", "<test-knoten-id>")>]
    ///   beliebig: Kommentar  [spot: <test-knoten-id>]
    let scanTestMarkers (testDir: string) : Set<string> =
        if not (Directory.Exists testDir) then Set.empty
        else
            let exts = set [ ".fs"; ".fsx"; ".cs"; ".mjs"; ".js" ]
            Directory.GetFiles(testDir, "*.*", SearchOption.AllDirectories)
            |> Array.filter (fun f ->
                let p = f.Replace('\\', '/')  // OS-native Separatoren normalisieren (Windows: \obj\)
                Set.contains (Path.GetExtension(f).ToLowerInvariant()) exts
                && not (p.Contains "/obj/") && not (p.Contains "/bin/"))
            |> Array.collect (fun f ->
                let text = File.ReadAllText f
                [| for m in Regex.Matches(text, "Trait\(\"spot\",\s*\"([^\"]+)\"\)") -> m.Groups.[1].Value
                   for m in Regex.Matches(text, "\[spot:\s*([a-zA-Z0-9_-]+)\]") -> m.Groups.[1].Value |])
            |> Set.ofArray

    /// Das Konvergenz-Orakel. Ein Test-Knoten wird `Aligned`, wenn seine Id als
    /// Marker im Testcode vorkommt (`covered`), sonst `Pending` — andere Knoten
    /// bleiben unberührt. Diese Funktion prüft NUR Marker-Präsenz; dass die Tests
    /// grün sind, sichert die CI-Suite (rot blockt den Merge) plus der reflexive
    /// Selbst-Test. Zusammen gilt: ein `Aligned`-Test-Knoten ⇒ grüner Test, nicht
    /// bloß Prozess-Exit-0.
    let SetzeSpecAligned (covered: Set<string>) (entry: SpotEntry) : SpotEntry =
        match entry.Payload with
        | TestNode _ ->
            let aligned = Set.contains (idValue entry.Id) covered
            { entry with Convergence = (if aligned then Aligned else Pending) }
        | _ -> entry

    /// Misst die Konvergenz der Test-Knoten über das Orakel `SetzeSpecAligned`.
    /// Liefert (Id, gespeichert, gemessen) für alle Abweichungen + aktualisierte Knoten.
    let syncTests (covered: Set<string>) (entries: SpotEntry list) =
        let updated = entries |> List.map (SetzeSpecAligned covered)
        let mismatches =
            List.zip entries updated
            |> List.choose (fun (o, n) ->
                match o.Payload with
                | TestNode _ when o.Convergence <> n.Convergence -> Some(o.Id, o.Convergence, n.Convergence)
                | _ -> None)
        mismatches, updated
