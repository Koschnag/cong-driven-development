namespace Cdd.Core

/// Spec → Test-Ableitung. Tests sind ein Derivat der Spezifikation, nicht handgeschrieben.
module Derive =

    open Cdd.Core.Spot

    /// Stabile Id eines abgeleiteten Tests: <spec-id>-test-<n> (1-basiert).
    let derivedTestId (specId: EntityId) (index: int) =
        EntityId(sprintf "%s-test-%d" (idValue specId) (index + 1))

    /// Erzeugt für jede Spec einen Test pro Akzeptanzkriterium.
    /// Bereits vorhandene Test-Knoten werden am Namen abgeglichen: stimmt der Name noch,
    /// passiert nichts (idempotent); ist er veraltet — etwa weil ein Kriterium an dieser
    /// Position eingefügt/umsortiert wurde —, wird der Name korrigiert (Id und Status bleiben).
    /// Sonst wird der Test neu erzeugt.
    let deriveTests (entries: SpotEntry list) : SpotEntry list =
        let existing =
            entries
            |> List.choose (fun e ->
                match e.Payload with
                | TestNode t -> Some(idValue e.Id, (e, t))
                | _ -> None)
            |> Map.ofList

        [ for e in entries do
            match e.Payload with
            | SpecNode s ->
                for i, c in List.indexed s.Criteria do
                    let id = derivedTestId e.Id i
                    let name = sprintf "%s — when %s then %s" s.Title c.When c.Then
                    match Map.tryFind (idValue id) existing with
                    | Some(_, t) when t.Name = name -> ()                  // bereits korrekt — idempotent
                    | Some(oldE, t) ->                                     // veralteter Name → korrigieren
                        yield { oldE with Payload = TestNode { t with Name = name } }
                    | None ->                                              // neu
                        yield
                            { Id = id
                              Convergence = Pending
                              Payload =
                                TestNode
                                    { SpecRef = e.Id
                                      Name = name
                                      Derived = true } }
            | _ -> () ]
