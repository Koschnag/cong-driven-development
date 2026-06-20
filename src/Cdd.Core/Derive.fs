namespace Cdd.Core

/// Spec → Test-Ableitung. Tests sind ein Derivat der Spezifikation, nicht handgeschrieben.
module Derive =

    open Cdd.Core.Spot

    /// Stabile Id eines abgeleiteten Tests: <spec-id>-test-<n> (1-basiert).
    let derivedTestId (specId: EntityId) (index: int) =
        EntityId(sprintf "%s-test-%d" (idValue specId) (index + 1))

    /// Erzeugt für jede Spec einen Test pro Akzeptanzkriterium.
    /// Bereits vorhandene (gleiche Id) werden nicht doppelt erzeugt.
    let deriveTests (entries: SpotEntry list) : SpotEntry list =
        let known = entries |> List.map (fun e -> e.Id) |> Set.ofList

        [ for e in entries do
            match e.Payload with
            | SpecNode s ->
                for i, c in List.indexed s.Criteria do
                    let id = derivedTestId e.Id i
                    if not (Set.contains id known) then
                        yield
                            { Id = id
                              Convergence = Pending
                              Payload =
                                TestNode
                                    { SpecRef = e.Id
                                      Name = sprintf "%s — when %s then %s" s.Title c.When c.Then
                                      Derived = true } }
            | _ -> () ]
