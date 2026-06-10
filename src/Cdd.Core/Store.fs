namespace Cdd.Core

/// Persistenz des SPOT-Graphen: ein JSON-File pro Knoten unter <root>/.spot/.
/// Ein File pro Entity hält Diffs klein und Merges git-freundlich.
module Store =

    open System.IO
    open Cdd.Core.Spot

    /// Verzeichnis, in dem die SPOT-Knoten liegen.
    let spotDir (root: string) = Path.Combine(root, ".spot")

    let private pathFor root (id: EntityId) =
        Path.Combine(spotDir root, idValue id + ".json")

    /// Existiert bereits ein SPOT-Store unter root?
    let exists (root: string) = Directory.Exists(spotDir root)

    /// Schreibt einen Knoten (legt das .spot-Verzeichnis bei Bedarf an).
    let save (root: string) (entry: SpotEntry) =
        Directory.CreateDirectory(spotDir root) |> ignore
        File.WriteAllText(pathFor root entry.Id, Json.serialize entry)

    /// Lädt alle Knoten, deterministisch nach Dateinamen sortiert.
    let load (root: string) : SpotEntry list =
        let dir = spotDir root
        if not (Directory.Exists dir) then []
        else
            Directory.GetFiles(dir, "*.json")
            |> Array.sort
            |> Array.map (fun f -> Json.deserialize<SpotEntry> (File.ReadAllText f))
            |> Array.toList
