namespace Cdd.Core

/// Public, additive scientific observations of autonomous software work.
/// This module is deliberately a publication model: it cannot dispatch work,
/// mutate a controller, or infer a success from activity or growth.
module Observatory =

    type ValueQuality =
        | Observed
        | Estimated
        | Unavailable
        | NotApplicable

    type EvidenceSource =
        | RiftwardRunRecord
        | PromotionReceipt
        | GateReceipt
        | OpenCodeUsageExport
        | OperatorDeclaration
        | DerivedAggregate

    type Measured<'value> =
        { Quality : ValueQuality
          Unit : string
          Value : 'value option
          Source : EvidenceSource option
          MissingReason : string option }

    type NonAcceptedDisposition =
        | Discarded
        | Superseded
        | Unresolved

    type CostStatus =
        | CostObserved
        | CostEstimated
        | CostUnavailable
        | CostNotApplicable

    type Cost =
        { Status : CostStatus
          Amount : decimal option
          Currency : string option
          Source : EvidenceSource option
          MissingReason : string option }

    [<RequireQualifiedAccess>]
    type ContentHashAlgorithm =
        | Sha256

    [<RequireQualifiedAccess>]
    type GitObjectAlgorithm =
        | Sha1
        | Sha256

    /// Closed authority set. Free-text labels cannot establish acceptance.
    type PromotionAuthority =
        | RequiredGateReceipt

    type SourceEventIdentity =
        { PublicEventId : string
          Sequence : int64
          HashAlgorithm : ContentHashAlgorithm
          EventHash : string }

    /// Untrusted, structural input to the verifier. Constructing this record is
    /// not evidence and cannot be placed in an accepted Episode directly.
    type PromotionEvidenceCandidate =
        { SourceEvent : SourceEventIdentity
          PublicPromotionId : string
          PublicTaskId : string
          PublicChangeSetId : string
          CandidateFingerprintAlgorithm : ContentHashAlgorithm
          PublicCandidateFingerprint : string
          GitObjectAlgorithm : GitObjectAlgorithm
          PublicCommitId : string
          PublicTreeId : string
          PromotedAtUtc : System.DateTimeOffset
          Authority : PromotionAuthority }

    type private VerifiedPromotionEvidenceData =
        { SourceEvent : SourceEventIdentity
          PublicPromotionId : string
          PublicTaskId : string
          PublicChangeSetId : string
          CandidateFingerprintAlgorithm : ContentHashAlgorithm
          PublicCandidateFingerprint : string
          GitObjectAlgorithm : GitObjectAlgorithm
          PublicCommitId : string
          PublicTreeId : string
          PromotedAtUtc : System.DateTimeOffset
          Authority : PromotionAuthority }

    /// Opaque proof token produced only by verifyPromotionEvidence.
    type VerifiedPromotionEvidence = private VerifiedPromotionEvidence of VerifiedPromotionEvidenceData

    type EpisodeOutcome =
        | Accepted of VerifiedPromotionEvidence
        | NotAccepted of NonAcceptedDisposition

    type AgentDeclaration =
        { Role : string
          Provider : string
          Model : string
          Harness : string }

    /// Provenance is either the complete declared multi-agent configuration or
    /// explicitly unavailable. It must never attribute a multi-role run to its builder alone.
    type AgentProvenance =
        | MultiAgentConfiguration of ConfigurationDigest : string * Roles : AgentDeclaration list
        | AgentProvenanceUnavailable of MissingReason : string

    type EpisodeMetrics =
        { DurationMilliseconds : Measured<int64>
          InputTokens : Measured<int64>
          OutputTokens : Measured<int64>
          RepairCycles : Measured<int>
          GateRuns : Measured<int>
          GateFailures : Measured<int>
          HumanInterventions : Measured<int> }

    /// A sanitized attempt. It intentionally has no prompt, host path, session
    /// id, raw log, private identifier, secret, or free-text evidence field.
    type Episode =
        { PublicTaskId : string
          PublicChangeSetId : string
          PublicAttemptId : string
          PublicEpochId : string
          Agent : AgentProvenance
          Outcome : EpisodeOutcome
          StartedAtUtc : Measured<System.DateTimeOffset>
          FinishedAtUtc : Measured<System.DateTimeOffset>
          Metrics : EpisodeMetrics
          Cost : Cost }

    type MetricCompleteness =
        { Observed : int
          Estimated : int
          Unavailable : int
          NotApplicable : int }

    type IntegerMetricAggregate =
        { ObservedTotal : int64
          EstimatedTotal : int64
          Completeness : MetricCompleteness }

    type CostAggregate =
        { Currency : string option
          ObservedTotal : decimal
          EstimatedTotal : decimal
          Completeness : MetricCompleteness }

    type Aggregate =
        { Episodes : int
          Accepted : int
          Discarded : int
          Superseded : int
          Unresolved : int
          DurationMilliseconds : IntegerMetricAggregate
          InputTokens : IntegerMetricAggregate
          OutputTokens : IntegerMetricAggregate
          RepairCycles : IntegerMetricAggregate
          GateRuns : IntegerMetricAggregate
          GateFailures : IntegerMetricAggregate
          HumanInterventions : IntegerMetricAggregate
          Cost : CostAggregate }

    type PublicRunObservationV1 =
        { SourceEvent : SourceEventIdentity
          PublicRunId : string
          PublicTaskId : string option
          TaskAttributionMissingReason : string option
          PublicChangeSetId : string option
          PublicAttemptId : string
          PublicEpochId : string
          Agent : AgentProvenance
          NonAcceptedDisposition : NonAcceptedDisposition option
          StartedAtUtc : Measured<System.DateTimeOffset>
          FinishedAtUtc : Measured<System.DateTimeOffset>
          Metrics : EpisodeMetrics
          Cost : Cost }

    type PublicPromotionObservationV1 =
        { PublicAttemptId : string
          Evidence : PromotionEvidenceCandidate }

    type PublicInterventionObservationV1 =
        { SourceEvent : SourceEventIdentity
          PublicRunId : string
          PublicTaskId : string option
          TaskAttributionMissingReason : string option
          OccurredAtUtc : System.DateTimeOffset
          Kind : string }

    type PublicTelemetryGapV1 =
        { SourceEvent : SourceEventIdentity
          Stream : string
          MissingFromUtc : System.DateTimeOffset
          MissingUntilUtc : System.DateTimeOffset
          Reason : string }

    type CoverageStatus =
        | Complete
        | Partial
        | UnavailableCoverage

    type PublicCoverageSourceV1 =
        { Source : EvidenceSource
          Status : CoverageStatus
          MissingReason : string option }

    type PublicObservationCoverageV1 =
        { WindowStartUtc : System.DateTimeOffset
          WindowEndUtc : System.DateTimeOffset
          Sources : PublicCoverageSourceV1 list }

    type PublicObservationIntegrityV1 =
        { PublicSnapshotId : string
          ManifestHashAlgorithm : ContentHashAlgorithm
          ManifestHash : string
          PreviousManifestHash : string option
          GeneratedAtUtc : System.DateTimeOffset }

    [<Literal>]
    let PublicObservationSnapshotV1Schema = "cdd.agentic-sdlc-observatory.public-observation-snapshot.v1"

    [<Literal>]
    let PublicObservationSnapshotV1PublicationGate = "draft-awaiting-ops-produced-fixture-roundtrip"

    /// Draft raw wire contract owned by CDD. It deliberately contains source
    /// observations, not derived Episodes, aggregates, detached totals, or ratios.
    type PublicObservationSnapshotV1 =
        { Schema : string
          RunObservations : PublicRunObservationV1 list
          PromotionObservations : PublicPromotionObservationV1 list
          InterventionObservations : PublicInterventionObservationV1 list
          TelemetryGaps : PublicTelemetryGapV1 list
          Coverage : PublicObservationCoverageV1
          Integrity : PublicObservationIntegrityV1 }

    let private blank (value: string) = System.String.IsNullOrWhiteSpace value

    let private lowerHex width (value: string) =
        not (isNull value)
        && value.Length = width
        && value |> Seq.forall (fun character -> (character >= '0' && character <= '9') || (character >= 'a' && character <= 'f'))

    let private validateContentHash field algorithm value =
        match algorithm with
        | ContentHashAlgorithm.Sha256 when lowerHex 64 value -> []
        | ContentHashAlgorithm.Sha256 -> [ sprintf "%s must be exactly 64 lower-hex characters for SHA-256." field ]

    let private validateGitObjectId field algorithm value =
        let width =
            match algorithm with
            | GitObjectAlgorithm.Sha1 -> 40
            | GitObjectAlgorithm.Sha256 -> 64
        if lowerHex width value then []
        else [ sprintf "%s must be exactly %d lower-hex characters for %A." field width algorithm ]

    let private validateUtc field (value: System.DateTimeOffset) =
        if value.Offset = System.TimeSpan.Zero then []
        else [ sprintf "%s must use UTC offset Z." field ]

    let private validateSourceEvent field (sourceEvent: SourceEventIdentity) =
        [ if blank sourceEvent.PublicEventId then yield sprintf "%s requires a non-blank public event id." field
          if sourceEvent.Sequence <= 0L then yield sprintf "%s sequence must be positive." field
          yield! validateContentHash (field + " event hash") sourceEvent.HashAlgorithm sourceEvent.EventHash ]

    /// Structural verifier and sole constructor of VerifiedPromotionEvidence.
    let verifyPromotionEvidence (candidate: PromotionEvidenceCandidate) : Result<VerifiedPromotionEvidence, string list> =
        let errors =
            [ yield! validateSourceEvent "Promotion source event" candidate.SourceEvent
              if blank candidate.PublicPromotionId then yield "Promotion evidence requires a public promotion id."
              if blank candidate.PublicTaskId then yield "Promotion evidence requires a public task id."
              if blank candidate.PublicChangeSetId then yield "Promotion evidence requires a public change-set id."
              yield! validateContentHash "Candidate fingerprint" candidate.CandidateFingerprintAlgorithm candidate.PublicCandidateFingerprint
              yield! validateGitObjectId "Commit id" candidate.GitObjectAlgorithm candidate.PublicCommitId
              yield! validateGitObjectId "Tree id" candidate.GitObjectAlgorithm candidate.PublicTreeId
              yield! validateUtc "Promotion timestamp" candidate.PromotedAtUtc ]
            |> List.sort
        if errors.IsEmpty then
            Ok
                (VerifiedPromotionEvidence
                    { SourceEvent = candidate.SourceEvent
                      PublicPromotionId = candidate.PublicPromotionId
                      PublicTaskId = candidate.PublicTaskId
                      PublicChangeSetId = candidate.PublicChangeSetId
                      CandidateFingerprintAlgorithm = candidate.CandidateFingerprintAlgorithm
                      PublicCandidateFingerprint = candidate.PublicCandidateFingerprint
                      GitObjectAlgorithm = candidate.GitObjectAlgorithm
                      PublicCommitId = candidate.PublicCommitId
                      PublicTreeId = candidate.PublicTreeId
                      PromotedAtUtc = candidate.PromotedAtUtc
                      Authority = candidate.Authority })
        else Error errors

    let private validateMeasured field validateValue (measured: Measured<'value>) =
        [ if blank measured.Unit then yield sprintf "%s requires a non-blank unit." field
          match measured.Quality, measured.Value, measured.Source, measured.MissingReason with
          | (Observed | Estimated), None, _, _ ->
              yield sprintf "%s is %A but has no value." field measured.Quality
          | (Observed | Estimated), _, None, _ ->
              yield sprintf "%s is %A but has no source." field measured.Quality
          | (Observed | Estimated), _, _, Some _ ->
              yield sprintf "%s is %A but has a missing reason." field measured.Quality
          | (Unavailable | NotApplicable), Some _, _, _ ->
              yield sprintf "%s is %A but carries a value." field measured.Quality
          | (Unavailable | NotApplicable), None, _, None ->
              yield sprintf "%s is %A but has no missing reason." field measured.Quality
          | _ -> ()
          match measured.MissingReason with
          | Some reason when blank reason -> yield sprintf "%s missing reason cannot be blank." field
          | _ -> ()
          match measured.Value with
          | Some value -> yield! validateValue value
          | None -> () ]

    let private nonNegative64 field value =
        if value < 0L then [ sprintf "%s cannot be negative." field ] else []

    let private nonNegative field value =
        if value < 0 then [ sprintf "%s cannot be negative." field ] else []

    let private validateCost (cost: Cost) =
        [ match cost.Status, cost.Amount, cost.Currency, cost.Source, cost.MissingReason with
          | (CostObserved | CostEstimated), None, _, _, _ ->
              yield sprintf "Cost is %A but has no amount." cost.Status
          | (CostObserved | CostEstimated), _, None, _, _ ->
              yield sprintf "Cost is %A but has no currency." cost.Status
          | (CostObserved | CostEstimated), _, _, None, _ ->
              yield sprintf "Cost is %A but has no source." cost.Status
          | (CostObserved | CostEstimated), _, _, _, Some _ ->
              yield sprintf "Cost is %A but has a missing reason." cost.Status
          | (CostUnavailable | CostNotApplicable), Some _, _, _, _ ->
              yield sprintf "Cost is %A but carries an amount." cost.Status
          | (CostUnavailable | CostNotApplicable), None, _, _, None ->
              yield sprintf "Cost is %A but has no missing reason." cost.Status
          | _ -> ()
          match cost.MissingReason with
          | Some reason when blank reason -> yield "Cost missing reason cannot be blank."
          | _ -> ()
          match cost.Amount with
          | Some amount when amount < 0M -> yield "Cost cannot be negative."
          | _ -> ()
          match cost.Currency with
          | Some currency when blank currency -> yield "Cost currency cannot be blank."
          | _ -> () ]

    let private validateAgent (provenance: AgentProvenance) =
        match provenance with
        | AgentProvenanceUnavailable reason when blank reason ->
            [ "Unavailable agent provenance requires a non-blank missing reason." ]
        | AgentProvenanceUnavailable _ -> []
        | MultiAgentConfiguration (digest, roles) ->
            [ if blank digest then yield "Multi-agent configuration requires a non-blank digest."
              if roles.Length <> 4 then yield "Multi-agent configuration requires exactly four declared roles."
              let expected = Set.ofList [ "scout"; "builder"; "critic"; "reviewer" ]
              let actual = roles |> List.map (fun role -> role.Role.Trim().ToLowerInvariant()) |> Set.ofList
              if actual <> expected then yield "Multi-agent configuration requires scout, builder, critic and reviewer roles exactly once."
              for role in roles do
                  if blank role.Role then yield "Agent role cannot be blank."
                  if blank role.Provider then yield "Agent provider cannot be blank."
                  if blank role.Model then yield "Agent model cannot be blank."
                  if blank role.Harness then yield "Agent harness cannot be blank." ]

    let private validateMetrics (metrics: EpisodeMetrics) =
        [ yield! validateMeasured "DurationMilliseconds" (nonNegative64 "DurationMilliseconds") metrics.DurationMilliseconds
          yield! validateMeasured "InputTokens" (nonNegative64 "InputTokens") metrics.InputTokens
          yield! validateMeasured "OutputTokens" (nonNegative64 "OutputTokens") metrics.OutputTokens
          yield! validateMeasured "RepairCycles" (nonNegative "RepairCycles") metrics.RepairCycles
          yield! validateMeasured "GateRuns" (nonNegative "GateRuns") metrics.GateRuns
          yield! validateMeasured "GateFailures" (nonNegative "GateFailures") metrics.GateFailures
          yield! validateMeasured "HumanInterventions" (nonNegative "HumanInterventions") metrics.HumanInterventions
          match metrics.GateFailures.Value, metrics.GateRuns.Value with
          | Some failures, Some runs when failures > runs -> yield "Gate failures cannot exceed gate runs."
          | _ -> () ]

    let validateEpisode (episode: Episode) =
        [ if blank episode.PublicTaskId then yield "Episodes require a public task id."
          if blank episode.PublicChangeSetId then yield "Episodes require a public change-set id."
          if blank episode.PublicAttemptId then yield "Episodes require a public attempt id."
          if blank episode.PublicEpochId then yield "Episodes require a public epoch id."
          yield! validateAgent episode.Agent
          yield! validateMeasured "StartedAtUtc" (fun _ -> []) episode.StartedAtUtc
          yield! validateMeasured "FinishedAtUtc" (fun _ -> []) episode.FinishedAtUtc
          match episode.StartedAtUtc.Value, episode.FinishedAtUtc.Value with
          | Some started, Some finished when finished < started -> yield "Episode finished before it started."
          | _ -> ()
          match episode.Outcome with
          | Accepted (VerifiedPromotionEvidence promotion) ->
              if promotion.PublicTaskId <> episode.PublicTaskId then
                  yield "Verified promotion task id does not match its episode."
              if promotion.PublicChangeSetId <> episode.PublicChangeSetId then
                  yield "Verified promotion change-set id does not match its episode."
              match episode.FinishedAtUtc.Quality, episode.FinishedAtUtc.Value with
              | Observed, Some finished when promotion.PromotedAtUtc < finished ->
                  yield "Promotion timestamp cannot precede the observed episode finish."
              | _ -> ()
          | NotAccepted _ -> ()
          yield! validateMetrics episode.Metrics
          yield! validateCost episode.Cost ]

    let private completeness (values: ValueQuality list) =
        { Observed = values |> List.filter (fun item -> item = Observed) |> List.length
          Estimated = values |> List.filter (fun item -> item = Estimated) |> List.length
          Unavailable = values |> List.filter (fun item -> item = Unavailable) |> List.length
          NotApplicable = values |> List.filter (fun item -> item = NotApplicable) |> List.length }

    let private aggregateInt64
        (select: Episode -> Measured<int64>)
        (episodes: Episode list)
        : Result<IntegerMetricAggregate, string> =
        let values = episodes |> List.map select
        let total quality =
            values
            |> List.choose (fun item -> if item.Quality = quality then item.Value else None)
            |> List.fold (fun sum value -> sum + bigint value) 0I
        let observed = total Observed
        let estimated = total Estimated
        if observed > bigint System.Int64.MaxValue || estimated > bigint System.Int64.MaxValue then
            Error "Observatory aggregate overflows an integer metric."
        else
            Ok
                { ObservedTotal = int64 observed
                  EstimatedTotal = int64 estimated
                  Completeness = values |> List.map (fun item -> item.Quality) |> completeness }

    let private aggregateInt
        (select: Episode -> Measured<int>)
        (episodes: Episode list)
        : Result<IntegerMetricAggregate, string> =
        aggregateInt64 (fun episode -> select episode |> fun item -> { Quality = item.Quality; Unit = item.Unit; Value = item.Value |> Option.map int64; Source = item.Source; MissingReason = item.MissingReason }) episodes

    let private aggregateCost (episodes: Episode list) : Result<CostAggregate, string> =
        let costs = episodes |> List.map (fun episode -> episode.Cost)
        let currencies =
            costs
            |> List.choose (fun item -> item.Currency)
            |> List.distinct
        if currencies.Length > 1 then Error "Observatory aggregate cannot combine multiple currencies."
        else
            let total status =
                costs
                |> List.choose (fun item -> if item.Status = status then item.Amount else None)
                |> List.sum
            let quality status =
                match status with
                | CostObserved -> Observed
                | CostEstimated -> Estimated
                | CostUnavailable -> Unavailable
                | CostNotApplicable -> NotApplicable
            Ok
                { Currency = currencies |> List.tryHead
                  ObservedTotal = total CostObserved
                  EstimatedTotal = total CostEstimated
                  Completeness = costs |> List.map (fun item -> quality item.Status) |> completeness }

    /// Aggregate every valid attempt, including discarded and failed work.
    /// Missing values stay missing and therefore cannot become zero productivity.
    let aggregate (episodes: Episode list) : Result<Aggregate, string list> =
        let duplicate field (select: 'item -> 'key) (items: 'item list) =
            items
            |> List.countBy select
            |> List.choose (fun (id, count) -> if count > 1 then Some(sprintf "Duplicate %s %A." field id) else None)
        let promotions : VerifiedPromotionEvidenceData list =
            episodes
            |> List.choose (fun episode ->
                match episode.Outcome with
                | Accepted (VerifiedPromotionEvidence promotion) -> Some promotion
                | NotAccepted _ -> None)
        let errors =
            [ yield! episodes |> List.collect validateEpisode
              yield! duplicate "public attempt id" (fun (item: Episode) -> item.PublicAttemptId) episodes
              yield! duplicate "authoritative source event id/sequence" (fun (item: VerifiedPromotionEvidenceData) -> item.SourceEvent.PublicEventId, item.SourceEvent.Sequence) promotions
              yield! duplicate "authoritative source event hash" (fun (item: VerifiedPromotionEvidenceData) -> item.SourceEvent.EventHash) promotions
              yield! duplicate "public promotion id" (fun (item: VerifiedPromotionEvidenceData) -> item.PublicPromotionId) promotions
              yield! duplicate "candidate/commit/tree binding" (fun (item: VerifiedPromotionEvidenceData) -> item.CandidateFingerprintAlgorithm, item.PublicCandidateFingerprint, item.GitObjectAlgorithm, item.PublicCommitId, item.PublicTreeId) promotions ]
            |> List.sort
        if not errors.IsEmpty then Error errors
        else
            let results =
                [ aggregateInt64 (fun item -> item.Metrics.DurationMilliseconds) episodes
                  aggregateInt64 (fun item -> item.Metrics.InputTokens) episodes
                  aggregateInt64 (fun item -> item.Metrics.OutputTokens) episodes
                  aggregateInt (fun item -> item.Metrics.RepairCycles) episodes
                  aggregateInt (fun item -> item.Metrics.GateRuns) episodes
                  aggregateInt (fun item -> item.Metrics.GateFailures) episodes ]
            match results, aggregateInt (fun item -> item.Metrics.HumanInterventions) episodes, aggregateCost episodes with
            | [ Ok duration; Ok input; Ok output; Ok repairs; Ok gates; Ok failures ], Ok interventions, Ok cost ->
                let accepted = episodes |> List.filter (fun item -> match item.Outcome with Accepted _ -> true | _ -> false) |> List.length
                let count (disposition: NonAcceptedDisposition) = episodes |> List.filter (fun item -> item.Outcome = NotAccepted disposition) |> List.length
                Ok
                    { Episodes = episodes.Length
                      Accepted = accepted
                      Discarded = count Discarded
                      Superseded = count Superseded
                      Unresolved = count Unresolved
                      DurationMilliseconds = duration
                      InputTokens = input
                      OutputTokens = output
                      RepairCycles = repairs
                      GateRuns = gates
                      GateFailures = failures
                      HumanInterventions = interventions
                      Cost = cost }
            | _ -> Error [ "Observatory aggregation failed unexpectedly." ]

    /// Adapt an already sanitized Riftward run record. A run record alone has
    /// no promotion authority, so this adapter always produces Unresolved.
    let fromRiftwardRun
        publicTaskId
        publicChangeSetId
        publicAttemptId
        publicEpochId
        (cost: Cost)
        (record: Riftward.RunRecord) =
        { PublicTaskId = publicTaskId
          PublicChangeSetId = publicChangeSetId
          PublicAttemptId = publicAttemptId
          PublicEpochId = publicEpochId
          Agent =
            MultiAgentConfiguration
                (record.Configuration.EvaluationProtocolDigest,
                 [ { Role = "scout"; Provider = record.Configuration.Scout.Provider; Model = record.Configuration.Scout.Model; Harness = record.Configuration.Scout.Harness }
                   { Role = "builder"; Provider = record.Configuration.Builder.Provider; Model = record.Configuration.Builder.Model; Harness = record.Configuration.Builder.Harness }
                   { Role = "critic"; Provider = record.Configuration.Critic.Provider; Model = record.Configuration.Critic.Model; Harness = record.Configuration.Critic.Harness }
                   { Role = "reviewer"; Provider = record.Configuration.Reviewer.Provider; Model = record.Configuration.Reviewer.Model; Harness = record.Configuration.Reviewer.Harness } ])
          Outcome = NotAccepted Unresolved
          StartedAtUtc = { Quality = Unavailable; Unit = "utc"; Value = None; Source = None; MissingReason = Some "not-published" }
          FinishedAtUtc = { Quality = Unavailable; Unit = "utc"; Value = None; Source = None; MissingReason = Some "not-published" }
          Metrics =
            { DurationMilliseconds = { Quality = Observed; Unit = "ms"; Value = Some record.Evaluation.DurationMilliseconds; Source = Some RiftwardRunRecord; MissingReason = None }
              InputTokens = { Quality = Observed; Unit = "tokens"; Value = Some record.InputTokens; Source = Some RiftwardRunRecord; MissingReason = None }
              OutputTokens = { Quality = Observed; Unit = "tokens"; Value = Some record.OutputTokens; Source = Some RiftwardRunRecord; MissingReason = None }
              RepairCycles = { Quality = Observed; Unit = "cycles"; Value = Some record.Evaluation.RepairCycles; Source = Some RiftwardRunRecord; MissingReason = None }
              GateRuns = { Quality = Observed; Unit = "runs"; Value = Some record.Evaluation.GateRuns; Source = Some RiftwardRunRecord; MissingReason = None }
              GateFailures = { Quality = Observed; Unit = "runs"; Value = Some record.Evaluation.GateFailures; Source = Some RiftwardRunRecord; MissingReason = None }
              HumanInterventions = { Quality = Observed; Unit = "interventions"; Value = Some record.Evaluation.HumanInterventions; Source = Some RiftwardRunRecord; MissingReason = None } }
          Cost = cost }

    let private validateTaskAttribution field (taskId: string option) (missingReason: string option) =
        [ match taskId, missingReason with
          | Some task, None when blank task -> yield sprintf "%s task id cannot be blank." field
          | Some _, None -> ()
          | None, Some reason when blank reason -> yield sprintf "%s task-attribution missing reason cannot be blank." field
          | None, Some _ -> ()
          | Some _, Some _ -> yield sprintf "%s cannot carry both a task id and a missing reason." field
          | None, None -> yield sprintf "%s requires either a task id or an explicit legacy-attribution missing reason." field ]

    let private validateRunObservation (run: PublicRunObservationV1) =
        [ yield! validateSourceEvent "Run source event" run.SourceEvent
          if blank run.PublicRunId then yield "Run observation requires a public run id."
          yield! validateTaskAttribution "Run observation" run.PublicTaskId run.TaskAttributionMissingReason
          match run.PublicTaskId, run.PublicChangeSetId with
          | Some _, None -> yield "Attributed run observation requires a public change-set id."
          | _, Some changeSet when blank changeSet -> yield "Run observation change-set id cannot be blank."
          | _ -> ()
          if blank run.PublicAttemptId then yield "Run observation requires a public attempt id."
          if blank run.PublicEpochId then yield "Run observation requires a public epoch id."
          yield! validateAgent run.Agent
          yield! validateMeasured "StartedAtUtc" (fun value -> validateUtc "Run start" value) run.StartedAtUtc
          yield! validateMeasured "FinishedAtUtc" (fun value -> validateUtc "Run finish" value) run.FinishedAtUtc
          match run.StartedAtUtc.Value, run.FinishedAtUtc.Value with
          | Some started, Some finished when finished < started -> yield "Run observation finished before it started."
          | _ -> ()
          yield! validateMetrics run.Metrics
          yield! validateCost run.Cost ]

    let private validatePromotionObservation (promotion: PublicPromotionObservationV1) =
        [ if blank promotion.PublicAttemptId then yield "Promotion observation requires a public attempt id."
          match verifyPromotionEvidence promotion.Evidence with
          | Ok _ -> ()
          | Error errors -> yield! errors ]

    let private validateInterventionObservation (intervention: PublicInterventionObservationV1) =
        [ yield! validateSourceEvent "Intervention source event" intervention.SourceEvent
          if blank intervention.PublicRunId then yield "Intervention observation requires a public run id."
          yield! validateTaskAttribution "Intervention observation" intervention.PublicTaskId intervention.TaskAttributionMissingReason
          yield! validateUtc "Intervention timestamp" intervention.OccurredAtUtc
          if blank intervention.Kind then yield "Intervention observation requires a non-blank kind." ]

    let private validateTelemetryGap (gap: PublicTelemetryGapV1) =
        [ yield! validateSourceEvent "Telemetry-gap source event" gap.SourceEvent
          if blank gap.Stream then yield "Telemetry gap requires a non-blank stream."
          yield! validateUtc "Telemetry gap start" gap.MissingFromUtc
          yield! validateUtc "Telemetry gap end" gap.MissingUntilUtc
          if gap.MissingUntilUtc < gap.MissingFromUtc then yield "Telemetry gap ends before it starts."
          if blank gap.Reason then yield "Telemetry gap requires a non-blank reason." ]

    let private validateCoverage (coverage: PublicObservationCoverageV1) =
        [ yield! validateUtc "Coverage window start" coverage.WindowStartUtc
          yield! validateUtc "Coverage window end" coverage.WindowEndUtc
          if coverage.WindowEndUtc < coverage.WindowStartUtc then yield "Coverage window ends before it starts."
          if obj.ReferenceEquals(coverage.Sources, null) then
              yield "Coverage sources cannot be null."
          else
              let duplicates = coverage.Sources |> List.countBy (fun item -> item.Source) |> List.filter (fun (_, count) -> count > 1)
              if not duplicates.IsEmpty then yield "Coverage cannot repeat a source."
              for source in coverage.Sources do
                  match source.Status, source.MissingReason with
                  | Complete, None -> ()
                  | Complete, Some _ -> yield "Complete coverage cannot carry a missing reason."
                  | (Partial | UnavailableCoverage), Some reason when not (blank reason) -> ()
                  | (Partial | UnavailableCoverage), _ -> yield "Partial or unavailable coverage requires a non-blank missing reason." ]

    let private validateIntegrity (integrity: PublicObservationIntegrityV1) =
        [ if blank integrity.PublicSnapshotId then yield "Snapshot integrity requires a public snapshot id."
          yield! validateContentHash "Manifest hash" integrity.ManifestHashAlgorithm integrity.ManifestHash
          match integrity.PreviousManifestHash with
          | Some hash -> yield! validateContentHash "Previous manifest hash" integrity.ManifestHashAlgorithm hash
          | None -> ()
          yield! validateUtc "Snapshot generation timestamp" integrity.GeneratedAtUtc ]

    let private duplicateSourceEvents (sourceEvents: SourceEventIdentity list) =
        let duplicateIds =
            sourceEvents
            |> List.countBy (fun item -> item.PublicEventId, item.Sequence)
            |> List.choose (fun (identity, count) -> if count > 1 then Some(sprintf "Duplicate authoritative source event id/sequence %A." identity) else None)
        let duplicateHashes =
            sourceEvents
            |> List.countBy (fun item -> item.EventHash)
            |> List.choose (fun (identity, count) -> if count > 1 then Some(sprintf "Duplicate authoritative source event hash %s." identity) else None)
        duplicateIds @ duplicateHashes

    let private duplicateValues field select items =
        items
        |> List.countBy select
        |> List.choose (fun (identity, count) -> if count > 1 then Some(sprintf "Duplicate %s %A." field identity) else None)

    let private validateSnapshotRelationships
        (runs: PublicRunObservationV1 list)
        (promotions: PublicPromotionObservationV1 list)
        (interventions: PublicInterventionObservationV1 list) =
        let runsByAttempt = runs |> List.groupBy (fun item -> item.PublicAttemptId) |> Map.ofList
        let runIds = runs |> List.map (fun item -> item.PublicRunId) |> Set.ofList
        [ yield! duplicateValues "public run id" (fun (item: PublicRunObservationV1) -> item.PublicRunId) runs
          yield! duplicateValues "public run attempt id" (fun (item: PublicRunObservationV1) -> item.PublicAttemptId) runs
          yield! duplicateValues "public promotion id" (fun (item: PublicPromotionObservationV1) -> item.Evidence.PublicPromotionId) promotions
          yield! duplicateValues "public promotion attempt id" (fun (item: PublicPromotionObservationV1) -> item.PublicAttemptId) promotions
          yield!
              duplicateValues
                  "candidate/commit/tree binding"
                  (fun (item: PublicPromotionObservationV1) ->
                      item.Evidence.CandidateFingerprintAlgorithm,
                      item.Evidence.PublicCandidateFingerprint,
                      item.Evidence.GitObjectAlgorithm,
                      item.Evidence.PublicCommitId,
                      item.Evidence.PublicTreeId)
                  promotions
          for promotion in promotions do
              match runsByAttempt |> Map.tryFind promotion.PublicAttemptId with
              | None -> yield sprintf "Promotion %s references unknown attempt %s." promotion.Evidence.PublicPromotionId promotion.PublicAttemptId
              | Some [ run ] ->
                  if run.NonAcceptedDisposition.IsSome then
                      yield sprintf "Run %s has both promotion and terminal non-acceptance." run.PublicRunId
                  match run.PublicTaskId, run.PublicChangeSetId with
                  | Some taskId, Some changeSetId ->
                      if promotion.Evidence.PublicTaskId <> taskId then
                          yield sprintf "Promotion %s task id does not match run %s." promotion.Evidence.PublicPromotionId run.PublicRunId
                      if promotion.Evidence.PublicChangeSetId <> changeSetId then
                          yield sprintf "Promotion %s change-set id does not match run %s." promotion.Evidence.PublicPromotionId run.PublicRunId
                      match run.FinishedAtUtc.Value with
                      | Some finished when promotion.Evidence.PromotedAtUtc < finished ->
                          yield sprintf "Promotion %s precedes run %s finish." promotion.Evidence.PublicPromotionId run.PublicRunId
                      | _ -> ()
                  | _ -> yield sprintf "Promotion %s cannot join run %s without task and change-set attribution." promotion.Evidence.PublicPromotionId run.PublicRunId
              | Some _ -> ()
          for intervention in interventions do
              if not (runIds.Contains intervention.PublicRunId) then
                  yield sprintf "Intervention references unknown run %s." intervention.PublicRunId ]

    /// Structural validation for the draft CDD-owned raw wire contract.
    let validatePublicObservationSnapshotV1 (snapshot: PublicObservationSnapshotV1) : Result<PublicObservationSnapshotV1, string list> =
        if obj.ReferenceEquals(snapshot, null) then Error [ "Public observation snapshot cannot be null." ]
        else
            let nullCollections =
                [ "RunObservations", obj.ReferenceEquals(snapshot.RunObservations, null)
                  "PromotionObservations", obj.ReferenceEquals(snapshot.PromotionObservations, null)
                  "InterventionObservations", obj.ReferenceEquals(snapshot.InterventionObservations, null)
                  "TelemetryGaps", obj.ReferenceEquals(snapshot.TelemetryGaps, null) ]
                |> List.choose (fun (field, isNullCollection) -> if isNullCollection then Some(sprintf "%s cannot be null." field) else None)
            if not nullCollections.IsEmpty then Error nullCollections
            else
                let sourceEvents =
                    [ yield! snapshot.RunObservations |> List.map (fun item -> item.SourceEvent)
                      yield! snapshot.PromotionObservations |> List.map (fun item -> item.Evidence.SourceEvent)
                      yield! snapshot.InterventionObservations |> List.map (fun item -> item.SourceEvent)
                      yield! snapshot.TelemetryGaps |> List.map (fun item -> item.SourceEvent) ]
                let errors =
                    [ if snapshot.Schema <> PublicObservationSnapshotV1Schema then
                          yield sprintf "Public observation snapshot requires schema %s." PublicObservationSnapshotV1Schema
                      yield! snapshot.RunObservations |> List.collect validateRunObservation
                      yield! snapshot.PromotionObservations |> List.collect validatePromotionObservation
                      yield! snapshot.InterventionObservations |> List.collect validateInterventionObservation
                      yield! snapshot.TelemetryGaps |> List.collect validateTelemetryGap
                      yield! validateCoverage snapshot.Coverage
                      yield! validateIntegrity snapshot.Integrity
                      if snapshot.Integrity.GeneratedAtUtc < snapshot.Coverage.WindowEndUtc then
                          yield "Snapshot generation timestamp precedes the coverage window end."
                      yield! duplicateSourceEvents sourceEvents
                      yield! validateSnapshotRelationships snapshot.RunObservations snapshot.PromotionObservations snapshot.InterventionObservations ]
                    |> List.sort
                if errors.IsEmpty then Ok snapshot else Error errors

    /// Exact serializer for the draft raw public snapshot contract.
    let serializePublicObservationSnapshotV1 (snapshot: PublicObservationSnapshotV1) =
        match validatePublicObservationSnapshotV1 snapshot with
        | Ok valid -> Ok (Json.serialize valid)
        | Error errors -> Error errors

    /// Exact parser for the draft raw public snapshot contract.
    let parsePublicObservationSnapshotV1 (json: string) : Result<PublicObservationSnapshotV1, string list> =
        try Json.deserialize<PublicObservationSnapshotV1> json |> validatePublicObservationSnapshotV1
        with ex -> Error [ sprintf "Public observation snapshot is not valid JSON for the CDD v1 contract: %s" ex.Message ]

    type EpisodeProjection =
        | JoinedEpisode of Episode
        | LegacyRunWithoutTaskAttribution of PublicRunId : string * MissingReason : string

    /// Derive analytical Episodes by joining raw run and promotion observations.
    /// Legacy runs without task attribution remain explicit and are never assigned a fabricated task.
    let deriveEpisodeProjections (snapshot: PublicObservationSnapshotV1) : Result<EpisodeProjection list, string list> =
        match validatePublicObservationSnapshotV1 snapshot with
        | Error errors -> Error errors
        | Ok valid ->
            let derive (run: PublicRunObservationV1) =
                let promotions = valid.PromotionObservations |> List.filter (fun item -> item.PublicAttemptId = run.PublicAttemptId)
                match run.PublicTaskId, run.PublicChangeSetId with
                | None, _ when promotions.IsEmpty ->
                    Ok (LegacyRunWithoutTaskAttribution (run.PublicRunId, run.TaskAttributionMissingReason |> Option.defaultValue "legacy-task-attribution-unavailable"))
                | None, _ ->
                    Error [ sprintf "Cannot join promotion for legacy run %s without task attribution." run.PublicRunId ]
                | Some _, None -> Error [ sprintf "Cannot derive episode for run %s without a change-set id." run.PublicRunId ]
                | Some taskId, Some changeSetId ->
                    let outcome =
                        match promotions with
                        | [] -> Ok (NotAccepted (run.NonAcceptedDisposition |> Option.defaultValue Unresolved))
                        | [ promotion ] ->
                            match run.NonAcceptedDisposition with
                            | Some _ -> Error [ sprintf "Run %s has both promotion and terminal non-acceptance." run.PublicRunId ]
                            | None -> verifyPromotionEvidence promotion.Evidence |> Result.map Accepted
                        | _ -> Error [ sprintf "Run %s has multiple promotion observations." run.PublicRunId ]
                    outcome
                    |> Result.bind (fun joinedOutcome ->
                        let episode =
                            { PublicTaskId = taskId
                              PublicChangeSetId = changeSetId
                              PublicAttemptId = run.PublicAttemptId
                              PublicEpochId = run.PublicEpochId
                              Agent = run.Agent
                              Outcome = joinedOutcome
                              StartedAtUtc = run.StartedAtUtc
                              FinishedAtUtc = run.FinishedAtUtc
                              Metrics = run.Metrics
                              Cost = run.Cost }
                        match validateEpisode episode with
                        | [] -> Ok (JoinedEpisode episode)
                        | errors -> Error errors)
            let derived = valid.RunObservations |> List.map derive
            let errors = derived |> List.collect (function Error items -> items | Ok _ -> [])
            let knownAttempts = valid.RunObservations |> List.map (fun item -> item.PublicAttemptId) |> Set.ofList
            let orphanErrors =
                valid.PromotionObservations
                |> List.choose (fun item -> if Set.contains item.PublicAttemptId knownAttempts then None else Some(sprintf "Promotion observation references unknown attempt %s." item.PublicAttemptId))
            let allErrors = (errors @ orphanErrors) |> List.sort
            if allErrors.IsEmpty then Ok (derived |> List.choose (function Ok item -> Some item | Error _ -> None))
            else Error allErrors
