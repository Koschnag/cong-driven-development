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

    /// Prämisse: gesetzte Annahme, von der KI und Mensch ableiten dürfen.
    type Premise =
        { Statement : string
          Rationale : string }

    /// Architektur-/Projekt-Entscheidung (ADR-Stil), optional eine ältere ersetzend.
    type Decision =
        { Title        : string
          Context      : string
          Choice       : string
          Consequences : string
          Supersedes   : EntityId option }

    /// Wissensquelle (Buch, PDF, Link, Blog), aus der Agents lernen und ableiten.
    type Knowledge =
        { Title     : string
          Source    : string        // URL, Pfad oder ISBN
          MediaType : string        // book | pdf | link | blog
          Takeaways : string list }

    /// Tool, mit dem Agents angereichert werden (Fähigkeits-Erweiterung).
    type Tool =
        { Name     : string
          Purpose  : string
          Endpoint : string option }

    /// Die Nutzlast eines SPOT-Knotens. Erweiterbar, sobald neue Knotenarten entstehen.
    type Payload =
        | SpecNode      of Spec
        | TestNode      of Test
        | RiskNode      of Risk
        | InfraNode     of Infra
        | ComponentNode of Component
        | PremiseNode   of Premise
        | DecisionNode  of Decision
        | KnowledgeNode of Knowledge
        | ToolNode      of Tool

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
        | PremiseNode _   -> "premise"
        | DecisionNode _  -> "decision"
        | KnowledgeNode _ -> "knowledge"
        | ToolNode _      -> "tool"
