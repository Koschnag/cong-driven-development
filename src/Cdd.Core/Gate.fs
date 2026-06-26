namespace Cdd.Core

/// Das harte Konvergenz-Gate (spec-gate-selbst-hart).
///
/// `Sync.SetzeSpecAligned` setzt einen Test-Knoten `Aligned` schon bei bloßer
/// Marker-Präsenz im Quellcode — ein `failwith`-TODO-Skelett (von
/// `Generate.testSkeletons` erzeugt) trägt aber den Marker und läuft trotzdem ROT.
/// Dieses Modul schließt die Lücke: `Aligned` nur, wenn der Marker abdeckt UND ein
/// echter `dotnet test`-Lauf grün ist. Greenness wird damit IN-Loop messbar, nicht
/// nur out-of-loop in der CI. Das Orakel falsifiziert (rot blockt) — es verifiziert
/// keine Korrektheit (Rice/Dijkstra): grün heißt „die geschriebenen Tests passen".
module Gate =

    open System.Text.RegularExpressions
    open Cdd.Core.Spot

    /// Ergebnis eines Testlaufs, geparst aus dem TRX-Logger (VSTest --logger trx).
    type TrxResult =
        { Passed  : int
          Failed  : int
          Skipped : int }

    /// Grün = mindestens ein bestandener Test und kein fehlgeschlagener.
    /// `Passed = 0` ist NICHT grün — ein leerer/„No test"-Lauf erschleicht kein Aligned.
    let istGruen (r: TrxResult) : bool = r.Passed > 0 && r.Failed = 0

    /// Parst die Counters aus einer TRX-Datei:
    /// `<ResultSummary><Counters total=".." passed=".." failed=".." skipped=".." /></ResultSummary>`.
    /// (`passed="` matcht nicht in `passedButRunAborted="`, daher attribut-genau.)
    let parseTrx (trxXml: string) : TrxResult =
        let attr name =
            let m = Regex.Match(trxXml, name + "=\"(\\d+)\"")
            if m.Success then int m.Groups.[1].Value else 0
        { Passed = attr "passed"; Failed = attr "failed"; Skipped = attr "skipped" }

    /// Das Orakel mit Zähnen: promoviert einen Test-Knoten genau dann auf `Aligned`,
    /// wenn ein Marker ihn abdeckt UND der Testlauf grün ist; sonst `Pending`.
    /// Andere Knotenarten bleiben unberührt. Gegenstück zu `Sync.SetzeSpecAligned`,
    /// aber mit der grünen Vorbedingung — kein `Aligned` durch bloße Marker-Präsenz.
    let setzeAlignedWennGruen (trx: TrxResult) (covered: Set<string>) (entry: SpotEntry) : SpotEntry =
        match entry.Payload with
        | TestNode _ ->
            let aligned = Set.contains (idValue entry.Id) covered && istGruen trx
            { entry with Convergence = (if aligned then Aligned else Pending) }
        | _ -> entry

    /// Ist der Gesamt-Gate-Zustand grün? Echter grüner Lauf UND keine
    /// Validierungsfehler (strukturelle Invarianten via `Validate`).
    let gateGruen (trx: TrxResult) (entries: SpotEntry list) : bool =
        istGruen trx && (entries |> Validate.validate |> Validate.errors |> List.isEmpty)
