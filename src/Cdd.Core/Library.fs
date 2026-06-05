namespace Cdd.Core

/// SPOT — Single Point of Truth. Seed-Typen, werden iterativ erweitert.
module Spot =

    type EntityId = EntityId of string

    /// Konvergenz-Status eines Modell-Knotens gegen die aktuelle Implementierung.
    type Convergence =
        | Diverged       // Implementierung weicht vom Modell ab
        | Aligned        // Modell und Code sind synchron
        | Pending        // Modell existiert, Code noch nicht
        | Orphaned       // Code existiert, kein Modell

    /// Minimaler SPOT-Eintrag. Wird zu Discriminated-Union erweitert
    /// sobald Entity-Typen klar sind (Spec, Test, Risk, Infra, ...).
    type SpotEntry = {
        Id          : EntityId
        Kind        : string
        Description : string
        Convergence : Convergence
    }
