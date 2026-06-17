namespace Cdd.Core

/// Modell-Historie: weil jeder SPOT-Knoten ein git-diffbares JSON-File unter .spot/ ist,
/// IST `git log` über .spot/ die Historie des Modells — Zeitreise gratis, ohne eigenen Store.
/// Read-only und defensiv: kein git / kein Repo → [] bzw. "" statt Exception (wie /api/infra/status).
module History =

    open System.Diagnostics
    open System.Text.RegularExpressions

    /// Ein Commit, der das Modell (mit-)veränderte.
    type Commit =
        { Hash: string; Short: string; Author: string; Date: string; Subject: string }

    let private idOk = Regex("^[a-zA-Z0-9][a-zA-Z0-9_-]*$")
    let private hashOk = Regex("^[0-9a-fA-F]{4,40}$")

    // Trennzeichen, die in Commit-Nachrichten nicht vorkommen → kein Quoting-Chaos.
    let private US = ""   // zwischen Feldern
    let private RS = ""   // zwischen Commits
    let private fmt = sprintf "--pretty=format:%%H%s%%h%s%%an%s%%ad%s%%s%s" US US US US RS

    /// `git -C <root> <args>`; stdout bei Erfolg, sonst "" (nie throw).
    let private git (root: string) (args: string list) : string =
        try
            let psi = ProcessStartInfo("git")
            psi.ArgumentList.Add("-C"); psi.ArgumentList.Add(root)
            args |> List.iter psi.ArgumentList.Add
            psi.RedirectStandardOutput <- true
            psi.RedirectStandardError <- true
            psi.UseShellExecute <- false
            use p = new Process()
            p.StartInfo <- psi
            if not (p.Start()) then ""
            else
                let out = p.StandardOutput.ReadToEnd()
                p.WaitForExit()
                if p.ExitCode = 0 then out else ""
        with _ -> ""

    let private parse (raw: string) : Commit list =
        if System.String.IsNullOrWhiteSpace raw then []
        else
            raw.Split([| RS |], System.StringSplitOptions.RemoveEmptyEntries)
            |> Array.choose (fun rc ->
                let f = rc.Trim().Split([| US |], System.StringSplitOptions.None)
                if f.Length >= 5 then
                    Some { Hash = f.[0]; Short = f.[1]; Author = f.[2]; Date = f.[3]; Subject = f.[4] }
                else None)
            |> Array.toList

    /// Commits, die irgendeinen .spot/-Knoten berührten — die Modell-Timeline.
    let model (root: string) (limit: int) : Commit list =
        let n = max 1 (min limit 500)
        git root [ "log"; sprintf "-%d" n; "--date=short"; fmt; "--"; ".spot" ] |> parse

    /// Commits, die EINEN Knoten berührten (Knoten-Lebenslauf).
    let node (root: string) (id: string) (limit: int) : Commit list =
        if not (idOk.IsMatch id) then []
        else
            let n = max 1 (min limit 200)
            git root [ "log"; sprintf "-%d" n; "--date=short"; "--follow"; fmt; "--"; ".spot/" + id + ".json" ] |> parse

    /// Der rohe Knoten-JSON-Stand zu einem Commit (Zeitreise). Sanitisiert gegen Path-/Ref-Injection.
    let nodeAt (root: string) (id: string) (hash: string) : string =
        if not (idOk.IsMatch id) || not (hashOk.IsMatch hash) then ""
        else git root [ "show"; hash + ":.spot/" + id + ".json" ]
