namespace Cdd.Core

/// SPOT — Single Point of Truth.
/// Das Modell ist primär; Code, Tests, Infra und Risiken sind Knoten desselben Graphen.
module Spot =

    /// Stabile, menschenlesbare Kennung eines SPOT-Knotens (z. B. "spec-login").
    type EntityId = EntityId of string

    /// Konvergenz-Status eines Modell-Knotens gegen die aktuelle Implementierung.
    type Convergence =
        | Diverged   // Implementierung weicht vom Modell ab
        | Aligned    // Modell und Code sind synchron
        | Pending    // Modell existiert, Code noch nicht
        | Orphaned   // Code existiert, kein Modell

    /// Gemeinsame Ordinalskala für Wahrscheinlichkeit/Impact/Priorität.
    type Level =
        | Low
        | Medium
        | High
        | Critical

    /// Akzeptanzkriterium in Given/When/Then-Form — der Vertrag, den eine Spec verspricht.
    type Criterion =
        { Given : string
          When  : string
          Then  : string }

    /// Maschinenlesbare Spezifikation: das "Was" als Vertrag.
    type Spec =
        { Title    : string
          Intent   : string
          Criteria : Criterion list }

    /// Test-Knoten. Aus einer Spec abgeleitete Tests tragen Derived = true.
    type Test =
        { SpecRef : EntityId   // welche Spec dieser Test abdeckt
          Name    : string
          Derived : bool }

    /// Risiko-Knoten mit Wahrscheinlichkeit, Impact und optionaler Mitigation.
    type Risk =
        { Statement  : string
          Likelihood : Level
          Impact     : Level
          Mitigation : string option }

    /// Infrastruktur-Knoten: Ressource bei einem Provider, frei konfigurierbar.
    type Infra =
        { Resource : string
          Provider : string
          Config   : Map<string, string> }

    /// Komponente mit gerichteten Abhängigkeiten auf andere SPOT-Knoten.
    type Component =
        { Name      : string
          DependsOn : EntityId list }

    /// Die Nutzlast eines SPOT-Knotens. Erweiterbar, sobald neue Knotenarten entstehen.
    type Payload =
        | SpecNode      of Spec
        | TestNode      of Test
        | RiskNode      of Risk
        | InfraNode     of Infra
        | ComponentNode of Component

    /// Ein Knoten im SPOT-Graph: Identität + Nutzlast + Konvergenzstatus.
    type SpotEntry =
        { Id          : EntityId
          Payload     : Payload
          Convergence : Convergence }

    /// Roher String einer EntityId.
    let idValue (EntityId s) = s

    /// Kurzes Art-Label eines Knotens (für Listen/Reports).
    let kindOf (entry: SpotEntry) =
        match entry.Payload with
        | SpecNode _      -> "spec"
        | TestNode _      -> "test"
        | RiskNode _      -> "risk"
        | InfraNode _     -> "infra"
        | ComponentNode _ -> "component"
