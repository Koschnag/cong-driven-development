namespace Cdd.Core

/// Modell → Code: generiert Implementierungs-Vorgaben aus dem SPOT.
/// Erste Stufe: xUnit-Test-Skelette für noch nicht abgedeckte Test-Knoten —
/// mit fertigem Trait("spot", …)-Marker, damit sync-tests die Umsetzung misst.
module Generate =

    open System.Text
    open Cdd.Core.Spot

    /// Macht aus einem Test-Knoten-Namen einen gültigen F#-Backtick-Bezeichner.
    let private safeName (name: string) =
        name.Replace("`", "'").Replace("\r", " ").Replace("\n", " ")

    /// F#-Stringliteral escapen.
    let private esc (s: string) = s.Replace("\\", "\\\\").Replace("\"", "\\\"")

    /// Test-Skelette für alle Test-Knoten, die noch keinen Marker im
    /// Test-Code haben (covered = Ergebnis von Sync.scanTestMarkers).
    /// Gruppiert nach Spec, Kriterium als Given/When/Then-Kommentar.
    let testSkeletons (covered: Set<string>) (entries: SpotEntry list) : string =
        let specs =
            entries
            |> List.choose (fun e -> match e.Payload with SpecNode s -> Some(e.Id, s) | _ -> None)
            |> Map.ofList
        let uncovered =
            entries
            |> List.choose (fun e ->
                match e.Payload with
                | TestNode t when not (Set.contains (idValue e.Id) covered) -> Some(e, t)
                | _ -> None)

        let sb = StringBuilder()
        let line (t: string) = sb.AppendLine(t) |> ignore
        line "// Generiert mit `cdd derive-code` — Test-Skelette aus dem SPOT-Modell."
        line "// Implementieren, in die Test-Suite übernehmen, committen:"
        line "// `cdd sync-tests --write` misst die Knoten danach als Aligned."
        line "module DerivedTests"
        line ""
        line "open Xunit"
        line ""
        if List.isEmpty uncovered then
            line "// Alle Test-Knoten sind bereits durch markierte Tests abgedeckt. 🎉"
        else
            for e, t in uncovered |> List.sortBy (fun (e, _) -> idValue e.Id) do
                let spec = Map.tryFind t.SpecRef specs
                match spec with
                | Some s ->
                    line (sprintf "// Spec: %s — %s" s.Title s.Intent)
                    // Kriterium über den Index im Knoten-Namen finden ist fragil —
                    // stattdessen alle Kriterien als Kontext anbieten:
                    for c in s.Criteria do
                        line (sprintf "//   GIVEN %s WHEN %s THEN %s" c.Given c.When c.Then)
                | None -> ()
                line (sprintf "[<Fact; Trait(\"spot\", \"%s\")>]" (esc (idValue e.Id)))
                line (sprintf "let ``%s`` () =" (safeName t.Name))
                line "    // Arrange"
                line "    // Act"
                line "    // Assert"
                line "    failwith \"TODO: aus dem Akzeptanzkriterium implementieren\""
                line ""
        sb.ToString()
