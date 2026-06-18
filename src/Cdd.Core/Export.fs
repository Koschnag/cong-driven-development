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

    let private ruleLabel = function
        | SpecsNeedTests -> "jede Spec braucht mindestens einen Test"
        | CriticalRisksNeedMitigation -> "kritische Risiken brauchen eine Mitigation"
        | TermsNeedDefinition -> "jeder Begriff braucht eine Definition"
        | IdPrefix(k, p) -> sprintf "Ids der Art '%s' beginnen mit '%s'" k p

    let private titleOf (e: SpotEntry) =
        match e.Payload with
        | SpecNode s -> s.Title
        | DecisionNode d -> d.Title
        | KnowledgeNode k -> k.Title
        | RiskNode r -> r.Statement
        | PremiseNode p -> p.Statement
        | InvariantNode i -> i.Description
        | InfraNode i -> i.Resource
        | ComponentNode c -> c.Name
        | TermNode t -> t.Name
        | ToolNode t -> t.Name
        | TestNode t -> t.Name

    /// Volle Details EINES Knotens (für im Auftrag genannte Knoten).
    let private detailOf (e: SpotEntry) : string =
        let id = idValue e.Id
        match e.Payload with
        | SpecNode s ->
            let crit = s.Criteria |> List.map (fun c -> sprintf "  - GIVEN %s WHEN %s THEN %s" c.Given c.When c.Then) |> String.concat "\n"
            sprintf "### %s (`%s`, %s)\n**Intent:** %s\n%s" s.Title id (convLabel e.Convergence) s.Intent crit
        | DecisionNode d ->
            sprintf "### %s (`%s`)\n- Kontext: %s\n- Entscheidung: %s\n- Konsequenzen: %s" d.Title id d.Context d.Choice d.Consequences
        | RiskNode r ->
            let mit = r.Mitigation |> Option.map (sprintf " — Mitigation: %s") |> Option.defaultValue ""
            sprintf "- **%s** (Likelihood %s, Impact %s)%s `%s`" r.Statement (levelLabel r.Likelihood) (levelLabel r.Impact) mit id
        | ComponentNode c ->
            let deps = if c.DependsOn.IsEmpty then "" else sprintf " → %s" (c.DependsOn |> List.map (idValue >> sprintf "`%s`") |> String.concat ", ")
            sprintf "- **%s** (`%s`, %s)%s" c.Name id (convLabel e.Convergence) deps
        | TermNode t -> sprintf "- **%s** — %s `%s`" t.Name t.Definition id
        | KnowledgeNode k ->
            let take = k.Takeaways |> List.map (sprintf "\n  - %s") |> String.concat ""
            sprintf "- **%s** (%s, %s) `%s`%s" k.Title k.MediaType k.Source id take
        | ToolNode t -> sprintf "- **%s** — %s `%s`" t.Name t.Purpose id
        | PremiseNode p -> sprintf "- **%s** — %s `%s`" p.Statement p.Rationale id
        | InvariantNode i -> sprintf "- **%s** — %s `%s`" i.Description (ruleLabel i.Rule) id
        | InfraNode inf -> sprintf "- **%s** @ %s `%s`" inf.Resource inf.Provider id
        | TestNode t -> sprintf "- Test **%s** covers `%s` (Derived=%b) `%s`" t.Name (idValue t.SpecRef) t.Derived id

    /// Kontext-SLICE für einen Engine-Run (Surface-Cut gegen den Full-Dump-Bug):
    /// axiomatischer Kern (Ontologie · Invarianten · Prämissen) + kompakter Index IMMER,
    /// volle Details NUR für Knoten, deren Id im Auftrag vorkommt. Schneidet die Token-Last
    /// drastisch, ohne den verbindlichen Kern oder die referenzierte Tiefe zu verlieren.
    let toContextSlice (prompt: string) (entries: SpotEntry list) : string =
        let sb = StringBuilder()
        let line (s: string) = sb.AppendLine(s) |> ignore
        let blank () = line ""
        let p = if isNull prompt then "" else prompt

        line "# SPOT-Kontext (Slice)"
        blank ()
        line (sprintf "%d Knoten im Modell. KONTEXT-SLICE: axiomatischer Kern + Index immer; volle \
                       Details nur für im Auftrag genannte Knoten. Voller Export: `cdd export-context`."
                (List.length entries))
        blank ()
        let counts =
            [ Aligned; Pending; Diverged; Orphaned ]
            |> List.map (fun c -> sprintf "%s %d" (convLabel c) (entries |> List.filter (fun e -> e.Convergence = c) |> List.length))
            |> String.concat " · "
        line (sprintf "**Konvergenz:** %s" counts)
        blank ()

        let terms = entries |> List.choose (fun e -> match e.Payload with TermNode t -> Some t | _ -> None)
        if not terms.IsEmpty then
            line "## Ubiquitäre Sprache (verbindlich)"
            blank ()
            for t in terms do
                let syn = if t.Synonyms.IsEmpty then "" else sprintf " *(auch: %s)*" (String.concat ", " t.Synonyms)
                line (sprintf "- **%s**%s — %s" t.Name syn t.Definition)
            blank ()

        let invs = entries |> List.choose (fun e -> match e.Payload with InvariantNode i -> Some i | _ -> None)
        if not invs.IsEmpty then
            line "## Invarianten (Governance — bei jeder Validierung erzwungen)"
            blank ()
            for i in invs do line (sprintf "- **%s** — %s" i.Description (ruleLabel i.Rule))
            blank ()

        let prems = entries |> List.choose (fun e -> match e.Payload with PremiseNode pr -> Some pr | _ -> None)
        if not prems.IsEmpty then
            line "## Prämissen (nicht verhandelbar)"
            blank ()
            for pr in prems do line (sprintf "- **%s** — %s" pr.Statement pr.Rationale)
            blank ()

        line "## Index (alle Knoten — eine Zeile je Knoten)"
        blank ()
        for e in entries do
            line (sprintf "- `%s` (%s, %s) — %s" (idValue e.Id) (kindOf e) (convLabel e.Convergence) (titleOf e))
        blank ()

        let referenced = entries |> List.filter (fun e -> p.Contains(idValue e.Id))
        if not referenced.IsEmpty then
            line "## Im Auftrag genannte Knoten (volle Details)"
            blank ()
            for e in referenced do line (detailOf e)
            blank ()

        let pending = entries |> List.filter (fun e -> e.Convergence <> Aligned)
        if not pending.IsEmpty then
            line "## Offene Arbeit (nicht Aligned)"
            blank ()
            for e in pending do line (sprintf "- `%s` (%s, %s)" (idValue e.Id) (kindOf e) (convLabel e.Convergence))
            blank ()

        sb.ToString()

    /// README-Status-Sektion — generiert aus dem Modell (cdd sync-docs).
    let statusMarkdown (entries: SpotEntry list) : string =
        let sb = StringBuilder()
        let line (t: string) = sb.AppendLine(t) |> ignore
        let specs = entries |> List.choose (fun e -> match e.Payload with SpecNode sp -> Some(e, sp) | _ -> None)
        let aligned = specs |> List.filter (fun (e, _) -> e.Convergence = Aligned)
        let pending = specs |> List.filter (fun (e, _) -> e.Convergence <> Aligned)
        let tests = entries |> List.choose (fun e -> match e.Payload with TestNode _ -> Some e | _ -> None)
        let testsAligned = tests |> List.filter (fun e -> e.Convergence = Aligned) |> List.length
        let invariants = entries |> List.filter (fun e -> kindOf e = "invariant") |> List.length
        line (sprintf "**%d Knoten im Selbstmodell** · %d aktive Invarianten · %d/%d abgeleitete Tests automatisiert"
                (List.length entries) invariants testsAligned (List.length tests))
        line ""
        line "### Kann es (Specs, gemessen Aligned)"
        line ""
        for _, sp in aligned |> List.sortBy (fun (_, sp) -> sp.Title) do
            line (sprintf "- ✅ **%s** — %s" sp.Title sp.Intent)
        if not pending.IsEmpty then
            line ""
            line "### In Arbeit / geplant (Pending)"
            line ""
            for _, sp in pending |> List.sortBy (fun (_, sp) -> sp.Title) do
                line (sprintf "- 🔜 **%s** — %s" sp.Title sp.Intent)
        line ""
        line "Prämissen, Entscheidungen (ADRs) und geltende Invarianten: [docs/decisions.md](docs/decisions.md)"
        line ""
        line "*Diese Sektion wird aus dem SPOT-Selbstmodell generiert (`cdd sync-docs`) — Hand-Edits werden überschrieben.*"
        sb.ToString()

    /// docs/decisions.md — Prämissen, Entscheidungen (ADRs) und geltende
    /// Invarianten als generiertes, versioniertes Doku-Artefakt.
    let decisionsMarkdown (entries: SpotEntry list) : string =
        let sb = StringBuilder()
        let line (t: string) = sb.AppendLine(t) |> ignore
        line "# Prämissen & Entscheidungen"
        line ""
        line "*Generiert aus dem SPOT-Selbstmodell (`cdd sync-docs`) — Hand-Edits werden überschrieben.*"
        line ""
        let premises = entries |> List.choose (fun e -> match e.Payload with PremiseNode p -> Some(e, p) | _ -> None)
        if not premises.IsEmpty then
            line "## Prämissen (nicht verhandelbar)"
            line ""
            for e, p in premises |> List.sortBy (fun (e, _) -> idValue e.Id) do
                line (sprintf "### %s" p.Statement)
                line (sprintf "*%s* · `%s`" p.Rationale (idValue e.Id))
                line ""
        let decisions = entries |> List.choose (fun e -> match e.Payload with DecisionNode d -> Some(e, d) | _ -> None)
        if not decisions.IsEmpty then
            line "## Entscheidungen (ADRs)"
            line ""
            for e, d in decisions |> List.sortBy (fun (e, _) -> idValue e.Id) do
                line (sprintf "### %s · `%s`" d.Title (idValue e.Id))
                line (sprintf "- **Kontext:** %s" d.Context)
                line (sprintf "- **Entscheidung:** %s" d.Choice)
                line (sprintf "- **Konsequenzen:** %s" d.Consequences)
                match d.Supersedes with
                | Some old -> line (sprintf "- **Ersetzt:** `%s`" (idValue old))
                | None -> ()
                line ""
        let invariants = entries |> List.choose (fun e -> match e.Payload with InvariantNode i -> Some(e, i) | _ -> None)
        if not invariants.IsEmpty then
            line "## Geltende Invarianten (Governance)"
            line ""
            for e, i in invariants |> List.sortBy (fun (e, _) -> idValue e.Id) do
                let rule =
                    match i.Rule with
                    | SpecsNeedTests -> "jede Spec braucht mindestens einen Test"
                    | CriticalRisksNeedMitigation -> "kritische Risiken brauchen eine Mitigation"
                    | TermsNeedDefinition -> "jeder Begriff braucht eine Definition"
                    | IdPrefix(k, pre) -> sprintf "Ids der Art '%s' beginnen mit '%s'" k pre
                line (sprintf "- 🛡️ **%s** — %s · `%s`" i.Description rule (idValue e.Id))
            line ""
        sb.ToString()
