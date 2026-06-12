namespace Cdd.Core

/// Validierung des SPOT-Graphen: strukturelle Integrität + Konvergenz-Hygiene.
module Validate =

    open Cdd.Core.Spot

    type Severity =
        | Error
        | Warning

    type Finding =
        { Severity : Severity
          EntityId : EntityId
          Message  : string }

    /// Zyklenerkennung über Component-Abhängigkeiten (DFS, gibt beteiligte Knoten zurück).
    let private cyclicComponents (entries: SpotEntry list) : Set<EntityId> =
        let deps =
            entries
            |> List.choose (fun e ->
                match e.Payload with
                | ComponentNode c -> Some(e.Id, c.DependsOn)
                | _ -> None)
            |> Map.ofList

        let mutable inCycle = Set.empty

        let rec visit (path: Set<EntityId>) (node: EntityId) =
            if Set.contains node path then
                inCycle <- Set.union inCycle path
            else
                match Map.tryFind node deps with
                | Some children ->
                    let path' = Set.add node path
                    for child in children do
                        visit path' child
                | None -> ()

        for KeyValue(node, _) in deps do
            visit Set.empty node
        inCycle

    /// Zyklen in der Begriffshierarchie (IsA/PartOf) — ein logischer Widerspruch.
    let private cyclicTerms (entries: SpotEntry list) : Set<EntityId> =
        let deps =
            entries
            |> List.choose (fun e ->
                match e.Payload with
                | TermNode t ->
                    let targets =
                        t.Relations
                        |> List.choose (function
                            | IsA target | PartOf target -> Some target
                            | RelatesTo _ -> None)
                    Some(e.Id, targets)
                | _ -> None)
            |> Map.ofList

        let mutable inCycle = Set.empty

        let rec visit (path: Set<EntityId>) (node: EntityId) =
            if Set.contains node path then
                inCycle <- Set.union inCycle path
            else
                match Map.tryFind node deps with
                | Some children ->
                    let path' = Set.add node path
                    for child in children do
                        visit path' child
                | None -> ()

        for KeyValue(node, _) in deps do
            visit Set.empty node
        inCycle

    /// Prüft den gesamten Graphen und liefert alle Befunde.
    let validate (entries: SpotEntry list) : Finding list =
        let ids = entries |> List.map (fun e -> e.Id) |> Set.ofList
        let specIds =
            entries
            |> List.choose (fun e ->
                match e.Payload with
                | SpecNode _ -> Some e.Id
                | _ -> None)
            |> Set.ofList
        let termIds =
            entries
            |> List.choose (fun e ->
                match e.Payload with
                | TermNode _ -> Some e.Id
                | _ -> None)
            |> Set.ofList
        let cyclic = cyclicComponents entries
        let termCycles = cyclicTerms entries
        let ambiguousNames =
            entries
            |> List.choose (fun e ->
                match e.Payload with
                | TermNode t -> Some (t.Name.Trim().ToLowerInvariant())
                | _ -> None)
            |> List.countBy id
            |> List.filter (fun (_, n) -> n > 1)
            |> List.map fst
            |> Set.ofList

        [ for e in entries do
            match e.Payload with
            | SpecNode s ->
                if List.isEmpty s.Criteria then
                    yield { Severity = Error; EntityId = e.Id
                            Message = "Spec hat keine Akzeptanzkriterien" }
            | TestNode t ->
                if not (Set.contains t.SpecRef specIds) then
                    yield { Severity = Error; EntityId = e.Id
                            Message = sprintf "Test referenziert unbekannte Spec '%s'" (idValue t.SpecRef) }
            | ComponentNode c ->
                for dep in c.DependsOn do
                    if dep = e.Id then
                        yield { Severity = Error; EntityId = e.Id
                                Message = "Component hängt von sich selbst ab" }
                    elif not (Set.contains dep ids) then
                        yield { Severity = Error; EntityId = e.Id
                                Message = sprintf "Abhängigkeit '%s' existiert nicht" (idValue dep) }
                if Set.contains e.Id cyclic then
                    yield { Severity = Error; EntityId = e.Id
                            Message = "Component ist Teil eines Abhängigkeits-Zyklus" }
            | RiskNode r ->
                if r.Impact = Critical && r.Mitigation.IsNone then
                    yield { Severity = Warning; EntityId = e.Id
                            Message = "Kritisches Risiko ohne Mitigation" }
            | DecisionNode d ->
                match d.Supersedes with
                | Some old when not (Set.contains old ids) ->
                    yield { Severity = Error; EntityId = e.Id
                            Message = sprintf "Supersedes verweist auf unbekannten Knoten '%s'" (idValue old) }
                | _ -> ()
            | KnowledgeNode k ->
                if k.Source.Trim() = "" then
                    yield { Severity = Warning; EntityId = e.Id
                            Message = "Knowledge-Quelle ohne Source (URL/Pfad/ISBN)" }
            | TermNode t ->
                if t.Definition.Trim() = "" then
                    yield { Severity = Warning; EntityId = e.Id
                            Message = "Begriff ohne Definition — ubiquitäre Sprache braucht Bedeutung" }
                if Set.contains e.Id termCycles then
                    yield { Severity = Error; EntityId = e.Id
                            Message = "Widerspruch: zyklische Begriffshierarchie (IsA/PartOf)" }
                if Set.contains (t.Name.Trim().ToLowerInvariant()) ambiguousNames then
                    yield { Severity = Warning; EntityId = e.Id
                            Message = sprintf "Mehrdeutigkeit: Begriff '%s' ist mehrfach definiert" t.Name }
                for rel in t.Relations do
                    let target = relationTarget rel
                    if target = e.Id then
                        yield { Severity = Error; EntityId = e.Id
                                Message = "Begriff bezieht sich auf sich selbst" }
                    elif not (Set.contains target termIds) then
                        yield { Severity = Error; EntityId = e.Id
                                Message = sprintf "Term-Beziehung zeigt auf '%s' — kein existierender Begriff" (idValue target) }
            | PremiseNode _ | ToolNode _ | InfraNode _ | InvariantNode _ -> ()

            match e.Convergence with
            | Orphaned ->
                yield { Severity = Warning; EntityId = e.Id
                        Message = "Orphaned: Code ohne Modell" }
            | Diverged ->
                yield { Severity = Warning; EntityId = e.Id
                        Message = "Diverged: Implementierung weicht vom Modell ab" }
            | Aligned | Pending -> ()

          // — Governance by Invariance: im Modell hinterlegte Regeln erzwingen —
          for inv in entries do
            match inv.Payload with
            | InvariantNode i ->
                let violation entityId detail =
                    { Severity = Error; EntityId = entityId
                      Message = sprintf "Invariante verletzt (%s): %s" i.Description detail }
                match i.Rule with
                | SpecsNeedTests ->
                    let testedSpecs =
                        entries
                        |> List.choose (fun x -> match x.Payload with TestNode t -> Some t.SpecRef | _ -> None)
                        |> Set.ofList
                    for x in entries do
                        match x.Payload with
                        | SpecNode _ when not (Set.contains x.Id testedSpecs) ->
                            yield violation x.Id "Spec ohne Test"
                        | _ -> ()
                | CriticalRisksNeedMitigation ->
                    for x in entries do
                        match x.Payload with
                        | RiskNode r when r.Impact = Critical && r.Mitigation.IsNone ->
                            yield violation x.Id "kritisches Risiko ohne Mitigation"
                        | _ -> ()
                | TermsNeedDefinition ->
                    for x in entries do
                        match x.Payload with
                        | TermNode t when t.Definition.Trim() = "" ->
                            yield violation x.Id "Begriff ohne Definition"
                        | _ -> ()
                | IdPrefix(kind, prefix) ->
                    for x in entries do
                        if kindOf x = kind && not ((idValue x.Id).StartsWith prefix) then
                            yield violation x.Id (sprintf "Id beginnt nicht mit '%s'" prefix)
            | _ -> () ]

    let errors findings   = findings |> List.filter (fun f -> f.Severity = Error)
    let warnings findings = findings |> List.filter (fun f -> f.Severity = Warning)
