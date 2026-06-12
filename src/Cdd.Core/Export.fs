namespace Cdd.Core

/// Export des SPOT-Graphen als LLM-Kontextpaket — zugleich die lebende
/// Dokumentation des Projekts. Der Graph ist die Quelle, dieses Markdown Derivat.
module Export =

    open System.Text
    open Cdd.Core.Spot

    let private convLabel = function
        | Aligned -> "Aligned" | Pending -> "Pending"
        | Diverged -> "Diverged" | Orphaned -> "Orphaned"

    let private levelLabel = function
        | Low -> "Low" | Medium -> "Medium" | High -> "High" | Critical -> "Critical"

    let toMarkdown (entries: SpotEntry list) : string =
        let sb = StringBuilder()
        let line (s: string) = sb.AppendLine(s) |> ignore
        let blank () = line ""
        let pick f = entries |> List.choose (fun e -> f e.Payload |> Option.map (fun p -> e, p))

        line "# SPOT-Kontext"
        blank ()
        line (sprintf "Generiert aus %d Knoten (`cdd export-context`). Der SPOT-Graph ist die \
                       Quelle — dieses Dokument ist Derivat und ersetzt handgepflegte Doku."
                (List.length entries))
        blank ()
        let counts =
            [ Aligned; Pending; Diverged; Orphaned ]
            |> List.map (fun c ->
                sprintf "%s %d" (convLabel c) (entries |> List.filter (fun e -> e.Convergence = c) |> List.length))
            |> String.concat " · "
        line (sprintf "**Konvergenz:** %s" counts)
        blank ()

        let terms = pick (function TermNode t -> Some t | _ -> None)
        if not terms.IsEmpty then
            line "## Ubiquitäre Sprache (Ontologie)"
            blank ()
            line "Diese Begriffe sind verbindlich — in Code, Antworten und allen Artefakten:"
            blank ()
            for e, t in terms do
                let syn = if t.Synonyms.IsEmpty then "" else sprintf " *(auch: %s)*" (String.concat ", " t.Synonyms)
                line (sprintf "- **%s**%s — %s" t.Name syn t.Definition)
                for rel in t.Relations do
                    let kind, target =
                        match rel with
                        | IsA i -> "ist ein", i
                        | PartOf i -> "Teil von", i
                        | RelatesTo i -> "bezieht sich auf", i
                    line (sprintf "  - %s `%s`" kind (idValue target))
            blank ()

        let invariants = pick (function InvariantNode i -> Some i | _ -> None)
        if not invariants.IsEmpty then
            line "## Invarianten (Governance — werden bei jeder Validierung erzwungen)"
            blank ()
            for _, i in invariants do
                let rule =
                    match i.Rule with
                    | SpecsNeedTests -> "jede Spec braucht mindestens einen Test"
                    | CriticalRisksNeedMitigation -> "kritische Risiken brauchen eine Mitigation"
                    | TermsNeedDefinition -> "jeder Begriff braucht eine Definition"
                    | IdPrefix(k, p) -> sprintf "Ids der Art '%s' beginnen mit '%s'" k p
                line (sprintf "- **%s** — %s" i.Description rule)
            blank ()

        let premises = pick (function PremiseNode p -> Some p | _ -> None)
        if not premises.IsEmpty then
            line "## Prämissen (nicht verhandelbar)"
            blank ()
            for _, p in premises do
                line (sprintf "- **%s** — %s" p.Statement p.Rationale)
            blank ()

        let decisions = pick (function DecisionNode d -> Some d | _ -> None)
        if not decisions.IsEmpty then
            line "## Entscheidungen (ADRs)"
            blank ()
            for e, d in decisions do
                line (sprintf "### %s (`%s`)" d.Title (idValue e.Id))
                line (sprintf "- **Kontext:** %s" d.Context)
                line (sprintf "- **Entscheidung:** %s" d.Choice)
                line (sprintf "- **Konsequenzen:** %s" d.Consequences)
                match d.Supersedes with
                | Some old -> line (sprintf "- **Ersetzt:** `%s`" (idValue old))
                | None -> ()
                blank ()

        let specs = pick (function SpecNode s -> Some s | _ -> None)
        if not specs.IsEmpty then
            line "## Spezifikationen"
            blank ()
            for e, s in specs do
                line (sprintf "### %s (`%s`, %s)" s.Title (idValue e.Id) (convLabel e.Convergence))
                line (sprintf "**Intent:** %s" s.Intent)
                blank ()
                for c in s.Criteria do
                    line (sprintf "- GIVEN %s WHEN %s THEN %s" c.Given c.When c.Then)
                blank ()

        let risks = pick (function RiskNode r -> Some r | _ -> None)
        if not risks.IsEmpty then
            line "## Risiken"
            blank ()
            for _, r in risks do
                let mit = r.Mitigation |> Option.map (sprintf " — Mitigation: %s") |> Option.defaultValue ""
                line (sprintf "- **%s** (Likelihood %s, Impact %s)%s"
                        r.Statement (levelLabel r.Likelihood) (levelLabel r.Impact) mit)
            blank ()

        let comps = pick (function ComponentNode c -> Some c | _ -> None)
        if not comps.IsEmpty then
            line "## Komponenten"
            blank ()
            for e, c in comps do
                let deps =
                    if c.DependsOn.IsEmpty then ""
                    else sprintf " → hängt ab von %s" (c.DependsOn |> List.map (idValue >> sprintf "`%s`") |> String.concat ", ")
                line (sprintf "- **%s** (`%s`)%s" c.Name (idValue e.Id) deps)
            blank ()

        let knowledge = pick (function KnowledgeNode k -> Some k | _ -> None)
        if not knowledge.IsEmpty then
            line "## Wissensquellen"
            blank ()
            for _, k in knowledge do
                line (sprintf "- **%s** (%s, %s)" k.Title k.MediaType k.Source)
                for t in k.Takeaways do line (sprintf "  - %s" t)
            blank ()

        let tools = pick (function ToolNode t -> Some t | _ -> None)
        if not tools.IsEmpty then
            line "## Tools (Agent-Capabilities)"
            blank ()
            for _, t in tools do
                let ep = t.Endpoint |> Option.map (sprintf " — %s") |> Option.defaultValue ""
                line (sprintf "- **%s** — %s%s" t.Name t.Purpose ep)
            blank ()

        let pending = entries |> List.filter (fun e -> e.Convergence <> Aligned)
        if not pending.IsEmpty then
            line "## Offene Arbeit (nicht Aligned)"
            blank ()
            for e in pending do
                line (sprintf "- `%s` (%s, %s)" (idValue e.Id) (kindOf e) (convLabel e.Convergence))
            blank ()

        sb.ToString()
