namespace Cdd.Core

/// Drift-Report: Solange es noch keinen Code-Reflektor gibt, ist "diff" der
/// Konvergenz-Spiegel des Modells — was ist aligned, was driftet, was fehlt.
module Diff =

    open Cdd.Core.Spot

    type DriftReport =
        { Aligned  : SpotEntry list
          Pending  : SpotEntry list
          Diverged : SpotEntry list
          Orphaned : SpotEntry list }

    let report (entries: SpotEntry list) : DriftReport =
        let by c = entries |> List.filter (fun e -> e.Convergence = c)
        { Aligned  = by Aligned
          Pending  = by Pending
          Diverged = by Diverged
          Orphaned = by Orphaned }

    /// True, wenn alle Knoten Aligned sind (kein Drift).
    let isConverged (entries: SpotEntry list) =
        entries |> List.forall (fun e -> e.Convergence = Aligned)
