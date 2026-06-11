namespace Cdd.Core

/// Persistenz des SPOT-Graphen: ein JSON-File pro Knoten unter <root>/.spot/.
/// Ein File pro Entity hält Diffs klein und Merges git-freundlich.
module Store =

    open System.IO
    open Cdd.Core.Spot

    /// Verzeichnis, in dem die SPOT-Knoten liegen.
    let spotDir (root: string) = Path.Combine(root, ".spot")

    let private idPattern =
        System.Text.RegularExpressions.Regex("^[a-zA-Z0-9][a-zA-Z0-9_-]*$")

    /// Ids werden Dateinamen — nur [a-zA-Z0-9_-], kein Path-Traversal möglich.
    let isValidId (id: EntityId) = idPattern.IsMatch(idValue id)

    let private pathFor root (id: EntityId) =
        Path.Combine(spotDir root, idValue id + ".json")

    /// Existiert bereits ein SPOT-Store unter root?
    let exists (root: string) = Directory.Exists(spotDir root)

    /// Schreibt einen Knoten (legt das .spot-Verzeichnis bei Bedarf an).
    /// Wirft bei ungültiger Id (Dateinamen-Sicherheit).
    let save (root: string) (entry: SpotEntry) =
        if not (isValidId entry.Id) then
            invalidArg "entry" (sprintf "Ungültige Entity-Id '%s' (erlaubt: a-zA-Z0-9_-)" (idValue entry.Id))
        Directory.CreateDirectory(spotDir root) |> ignore
        File.WriteAllText(pathFor root entry.Id, Json.serialize entry)

    /// Löscht einen Knoten; false, wenn er nicht existiert.
    let delete (root: string) (id: EntityId) =
        if not (isValidId id) then false
        else
            let p = pathFor root id
            if File.Exists p then File.Delete p; true
            else false

    /// Lädt alle Knoten, deterministisch nach Dateinamen sortiert.
    let load (root: string) : SpotEntry list =
        let dir = spotDir root
        if not (Directory.Exists dir) then []
        else
            Directory.GetFiles(dir, "*.json")
            |> Array.sort
            |> Array.map (fun f ->
                try
                    let entry = Json.deserialize<SpotEntry> (File.ReadAllText f)
                    if obj.ReferenceEquals(entry, null) then
                        raise (IOException(sprintf "'%s' enthält keinen SPOT-Knoten" (Path.GetFileName f)))
                    entry
                with
                | :? IOException -> reraise ()
                | ex ->
                    raise (IOException(sprintf "'%s' ist kein gültiger SPOT-Knoten: %s"
                                           (Path.GetFileName f) ex.Message)))
            |> Array.toList
