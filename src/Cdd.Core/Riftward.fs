namespace Cdd.Core

/// Longitudinal Riftward evidence: the sanitized publication boundary between
/// durable autopilot runs and public research claims.
///
/// Riftward is a longitudinal case study, not a proof of product effect. This
/// module projects finished runs into records that carry only declared
/// provenance and aggregate counters. Session ids, dispatch modes, scope paths,
/// instructions, findings, digests, commit hashes and free-text summaries never
/// leave the local run state. Aggregates are deterministic so that any published
/// number can be reproduced from the persisted run states alone.
module Riftward =

    /// Declared provenance of one worker profile. This is an operator claim
    /// about provider, model and harness — not proof of the actual weights,
    /// system prompt or router behind an endpoint.
    type RunProvenance =
        { Provider : string
          Model : string
          Harness : string }

    /// Comparison configuration: declared provenance per separated role plus a
    /// caller-owned digest of the fixed task, gates, tools, budgets and versions.
    type RunConfiguration =
        { Scout : RunProvenance
          Builder : RunProvenance
          Critic : RunProvenance
          Reviewer : RunProvenance
          EvaluationProtocolDigest : string }

    /// Sanitized per-run record. Every field is either an identifier that the
    /// mission already publishes or an aggregate counter from the evaluation
    /// and metrics of the durable run state.
    type RunRecord =
        { RunId : string
          MissionId : string
          Configuration : RunConfiguration
          Status : Autopilot.RunStatus
          Evaluation : Autopilot.Evaluation
          InputTokens : int64
          OutputTokens : int64 }

    let private blank (value: string) = System.String.IsNullOrWhiteSpace value

    let private provenanceErrors role (provenance: RunProvenance) =
        [ if blank provenance.Provider then yield sprintf "Riftward %s provider is required." role
          if blank provenance.Model then yield sprintf "Riftward %s model is required." role
          if blank provenance.Harness then yield sprintf "Riftward %s harness is required." role ]

    let private configurationErrors (configuration: RunConfiguration) =
        [ if blank configuration.EvaluationProtocolDigest then
              yield "Riftward configuration requires an evaluation protocol digest."
          yield! provenanceErrors "Scout" configuration.Scout
          yield! provenanceErrors "Builder" configuration.Builder
          yield! provenanceErrors "Critic" configuration.Critic
          yield! provenanceErrors "Reviewer" configuration.Reviewer ]

    let private evaluationErrors runId status (evaluation: Autopilot.Evaluation) =
        [ if evaluation.CompletedSlices < 0 then yield sprintf "Riftward record %s has negative CompletedSlices." runId
          if evaluation.TotalSlices < 1 then yield sprintf "Riftward record %s requires at least one attempted slice." runId
          if evaluation.AgentTurns < 0 then yield sprintf "Riftward record %s has negative AgentTurns." runId
          if evaluation.ToolCalls < 0 then yield sprintf "Riftward record %s has negative ToolCalls." runId
          if evaluation.PrematureStops < 0 then yield sprintf "Riftward record %s has negative PrematureStops." runId
          if evaluation.SameSessionResumes < 0 then yield sprintf "Riftward record %s has negative SameSessionResumes." runId
          if evaluation.FreshStarts < 0 then yield sprintf "Riftward record %s has negative FreshStarts." runId
          if evaluation.RepairCycles < 0 then yield sprintf "Riftward record %s has negative RepairCycles." runId
          if evaluation.GateRuns < 0 then yield sprintf "Riftward record %s has negative GateRuns." runId
          if evaluation.GateFailures < 0 then yield sprintf "Riftward record %s has negative GateFailures." runId
          if evaluation.HumanInterventions < 0 then yield sprintf "Riftward record %s has negative HumanInterventions." runId
          if evaluation.DurationMilliseconds < 0L then yield sprintf "Riftward record %s has negative duration." runId
          if evaluation.CompletedSlices > evaluation.TotalSlices then
              yield sprintf "Riftward record %s completes more slices than it attempted." runId
          if evaluation.GateFailures > evaluation.GateRuns then
              yield sprintf "Riftward record %s has more gate failures than gate runs." runId
          if evaluation.FullSolve <> (status = Autopilot.Completed) then
              yield sprintf "Riftward record %s has a status/evaluation contradiction." runId
          if evaluation.FullSolve && evaluation.CompletedSlices <> evaluation.TotalSlices then
              yield sprintf "Riftward record %s claims a full solve without completing every slice." runId ]

    let private validateRecord (record: RunRecord) =
        [ if blank record.RunId then yield "Riftward records require a non-empty RunId."
          if blank record.MissionId then
              yield sprintf "Riftward record %s requires a non-empty MissionId." record.RunId
          for error in configurationErrors record.Configuration do
              yield sprintf "Riftward record %s: %s" record.RunId error
          if record.Status = Autopilot.Running then
              yield sprintf "Riftward record %s is not terminal." record.RunId
          yield! evaluationErrors record.RunId record.Status record.Evaluation
          if record.InputTokens < 0L then yield sprintf "Riftward record %s has negative input tokens." record.RunId
          if record.OutputTokens < 0L then yield sprintf "Riftward record %s has negative output tokens." record.RunId ]

    /// Project a durable run into its publishable research record. Only
    /// terminal runs are accepted: a running snapshot has no stable outcome,
    /// and long uptime is explicitly not an autonomy result.
    let observeRun evaluationProtocolDigest (run: Autopilot.RunState) : Result<RunRecord, string list> =
        let workerErrors =
            [ for role in [ Autopilot.Scout; Autopilot.Builder; Autopilot.Critic; Autopilot.Reviewer ] do
                let profiles = run.Plan.Workers |> List.filter (fun item -> item.Role = role)
                match profiles with
                | [ _ ] -> ()
                | [] -> yield sprintf "Run %s declares no %A worker." run.RunId role
                | _ -> yield sprintf "Run %s declares more than one %A worker." run.RunId role ]
        if blank evaluationProtocolDigest then
            Error [ "Riftward records require a non-empty evaluation protocol digest." ]
        elif run.Status = Autopilot.Running then
            Error [ sprintf "Run %s is still running; riftward records require a terminal state." run.RunId ]
        elif not workerErrors.IsEmpty then Error workerErrors
        else
            let provenance role =
                run.Plan.Workers
                |> List.find (fun item -> item.Role = role)
                |> fun item -> { Provider = item.Provider; Model = item.Model; Harness = item.Harness }
            let record =
                { RunId = run.RunId
                  MissionId = run.Plan.MissionId
                  Configuration =
                    { Scout = provenance Autopilot.Scout
                      Builder = provenance Autopilot.Builder
                      Critic = provenance Autopilot.Critic
                      Reviewer = provenance Autopilot.Reviewer
                      EvaluationProtocolDigest = evaluationProtocolDigest }
                  Status = run.Status
                  Evaluation = Autopilot.evaluate run
                  InputTokens = run.Metrics.InputTokens
                  OutputTokens = run.Metrics.OutputTokens }
            match validateRecord record with
            | [] -> Ok record
            | errors -> Error errors

    /// Deterministic aggregate over unique runs that share one mission and one
    /// declared comparison configuration. Sums and medians are integer-only.
    type BaselineAggregate =
        { Configuration : RunConfiguration
          MissionId : string
          RunIds : string list
          Runs : int
          Missions : int
          FullSolves : int
          BlockedRuns : int
          AttemptedSlices : int
          CompletedSlices : int
          PrematureStops : int
          SameSessionResumes : int
          FreshStarts : int
          RepairCycles : int
          GateRuns : int
          GateFailures : int
          HumanInterventions : int
          MedianDurationMilliseconds : int64
          InputTokens : int64
          OutputTokens : int64 }

    /// How far an aggregate may be quoted in research claims. Repetition is a
    /// necessary precondition for comparison, never evidence of causality,
    /// product effect or long-term autonomy.
    type RepetitionFitness =
        | Anecdotal
        | Repeated

    let private validateAggregate (aggregate: BaselineAggregate) =
        let duplicateRunIds =
            aggregate.RunIds
            |> List.countBy id
            |> List.choose (fun (runId, count) -> if count > 1 then Some runId else None)
        let nonNegativeIntFields =
            [ "Runs", aggregate.Runs
              "Missions", aggregate.Missions
              "FullSolves", aggregate.FullSolves
              "BlockedRuns", aggregate.BlockedRuns
              "AttemptedSlices", aggregate.AttemptedSlices
              "CompletedSlices", aggregate.CompletedSlices
              "PrematureStops", aggregate.PrematureStops
              "SameSessionResumes", aggregate.SameSessionResumes
              "FreshStarts", aggregate.FreshStarts
              "RepairCycles", aggregate.RepairCycles
              "GateRuns", aggregate.GateRuns
              "GateFailures", aggregate.GateFailures
              "HumanInterventions", aggregate.HumanInterventions ]
        [ if blank aggregate.MissionId then yield "Riftward aggregate requires a non-empty MissionId."
          yield! configurationErrors aggregate.Configuration
          for runId in aggregate.RunIds do
              if blank runId then yield "Riftward aggregate RunIds cannot contain blank values."
          for runId in duplicateRunIds do
              yield sprintf "Riftward aggregate contains duplicate RunId %s." runId
          if aggregate.RunIds <> List.sort aggregate.RunIds then
              yield "Riftward aggregate RunIds must be canonically sorted."
          for field, value in nonNegativeIntFields do
              if value < 0 then yield sprintf "Riftward aggregate has negative %s." field
          if aggregate.Runs < 1 then yield "Riftward aggregate requires at least one run."
          if aggregate.Runs <> aggregate.RunIds.Length then
              yield "Riftward aggregate Runs must equal the number of RunIds."
          if aggregate.Missions <> 1 then
              yield "A Riftward mission baseline must represent exactly one mission."
          if aggregate.FullSolves > aggregate.Runs then
              yield "Riftward aggregate FullSolves cannot exceed Runs."
          if aggregate.BlockedRuns > aggregate.Runs then
              yield "Riftward aggregate BlockedRuns cannot exceed Runs."
          if bigint aggregate.FullSolves + bigint aggregate.BlockedRuns <> bigint aggregate.Runs then
              yield "Riftward aggregate terminal outcomes must equal Runs."
          if aggregate.AttemptedSlices < aggregate.Runs then
              yield "Riftward aggregate must contain at least one attempted slice per run."
          if aggregate.CompletedSlices > aggregate.AttemptedSlices then
              yield "Riftward aggregate CompletedSlices cannot exceed AttemptedSlices."
          if aggregate.FullSolves > aggregate.CompletedSlices then
              yield "Riftward aggregate FullSolves cannot exceed CompletedSlices."
          if aggregate.GateFailures > aggregate.GateRuns then
              yield "Riftward aggregate GateFailures cannot exceed GateRuns."
          if aggregate.MedianDurationMilliseconds < 0L then
              yield "Riftward aggregate has negative median duration."
          if aggregate.InputTokens < 0L then yield "Riftward aggregate has negative input tokens."
          if aggregate.OutputTokens < 0L then yield "Riftward aggregate has negative output tokens." ]

    /// Classify only a valid aggregate against an explicit, caller-owned
    /// minimum. Invalid or forged aggregate projections fail closed.
    let classify minimumRepetitions (aggregate: BaselineAggregate) : Result<RepetitionFitness, string list> =
        let errors =
            [ if minimumRepetitions < 1 then yield "The repetition minimum must be positive."
              yield! validateAggregate aggregate ]
        if not errors.IsEmpty then Error errors
        elif aggregate.RunIds.Length >= minimumRepetitions then Ok Repeated
        else Ok Anecdotal

    /// A comparison CDD admits for research reporting. It carries both
    /// validated aggregates unchanged; admitting a contrast derives no
    /// ranking, causality, product effect or long-term autonomy from it.
    type BaselineComparison =
        { MissionId : string
          EvaluationProtocolDigest : string
          MinimumRepetitions : int
          Left : BaselineAggregate
          Right : BaselineAggregate }

    /// Admit exactly one two-configuration contrast. Both sides must be valid
    /// aggregates of one mission under one evaluation protocol, both must reach
    /// the explicitly named repetition minimum, and the declared configurations
    /// must genuinely differ. Anecdotal baselines stay incomparable.
    let compareBaselines
        (minimumRepetitions: int)
        (left: BaselineAggregate)
        (right: BaselineAggregate)
        : Result<BaselineComparison, string list> =
        let structuralErrors =
            [ if minimumRepetitions < 1 then yield "The repetition minimum must be positive."
              yield! validateAggregate left
              yield! validateAggregate right ]
        if not structuralErrors.IsEmpty then Error structuralErrors
        else
            let anecdotal side aggregate =
                match classify minimumRepetitions aggregate with
                | Error errors -> Some errors
                | Ok Anecdotal ->
                    Some [ sprintf "Riftward %s baseline is anecdotal below the named repetition minimum and may not be compared." side ]
                | Ok Repeated -> None
            let failures =
                [ match anecdotal "left" left with Some errors -> yield! errors | None -> ()
                  match anecdotal "right" right with Some errors -> yield! errors | None -> ()
                  if left.MissionId <> right.MissionId then
                      yield sprintf "Riftward baselines compare one mission; observed %s and %s." left.MissionId right.MissionId
                  if left.Configuration.EvaluationProtocolDigest <> right.Configuration.EvaluationProtocolDigest then
                      yield "Riftward baselines must share one evaluation protocol digest."
                  if left.Configuration = right.Configuration then
                      yield "A Riftward comparison requires two distinct declared configurations."
                  let overlappingRunIds =
                      Set.intersect (Set.ofList left.RunIds) (Set.ofList right.RunIds)
                      |> Set.toList
                  for runId in overlappingRunIds do
                      yield sprintf "Riftward comparison reuses RunId %s across both configurations." runId ]
            if not failures.IsEmpty then Error failures
            else
                Ok
                    { MissionId = left.MissionId
                      EvaluationProtocolDigest = left.Configuration.EvaluationProtocolDigest
                      MinimumRepetitions = minimumRepetitions
                      Left = left
                      Right = right }

    let private median (values: int64 list) =
        let sorted = List.sort values
        let middle = sorted.Length / 2
        if sorted.Length % 2 = 1 then sorted.[middle]
        else
            let lower = sorted.[middle - 1]
            let upper = sorted.[middle]
            lower + (upper - lower) / 2L

    let private sumInt missionId field (select: 'item -> int) (values: 'item list) =
        let total = values |> List.fold (fun sum value -> sum + bigint (select value)) 0I
        if total > bigint System.Int32.MaxValue then
            Error(sprintf "Riftward aggregate %s overflows %s." missionId field)
        else Ok(int total)

    let private sumInt64 missionId field (select: 'item -> int64) (values: 'item list) =
        let total = values |> List.fold (fun sum value -> sum + bigint (select value)) 0I
        if total > bigint System.Int64.MaxValue then
            Error(sprintf "Riftward aggregate %s overflows %s." missionId field)
        else Ok(int64 total)

    let private aggregateGroup ((configuration, missionId), runs: RunRecord list) =
        let evaluations : Autopilot.Evaluation list = runs |> List.map (fun record -> record.Evaluation)
        let intResults =
            let fields : (string * (Autopilot.Evaluation -> int)) list =
                [ "AttemptedSlices", (fun item -> item.TotalSlices)
                  "CompletedSlices", (fun item -> item.CompletedSlices)
                  "PrematureStops", (fun item -> item.PrematureStops)
                  "SameSessionResumes", (fun item -> item.SameSessionResumes)
                  "FreshStarts", (fun item -> item.FreshStarts)
                  "RepairCycles", (fun item -> item.RepairCycles)
                  "GateRuns", (fun item -> item.GateRuns)
                  "GateFailures", (fun item -> item.GateFailures)
                  "HumanInterventions", (fun item -> item.HumanInterventions) ]
            fields
            |> List.map (fun (field, select) -> field, sumInt missionId field select evaluations)
        let int64Results =
            let fields : (string * (RunRecord -> int64)) list =
                [ "InputTokens", (fun record -> record.InputTokens)
                  "OutputTokens", (fun record -> record.OutputTokens) ]
            fields
            |> List.map (fun (field, select) -> field, sumInt64 missionId field select runs)
        let errors =
            [ for _, result in intResults do
                  match result with Error error -> yield error | Ok _ -> ()
              for _, result in int64Results do
                  match result with Error error -> yield error | Ok _ -> () ]
        if not errors.IsEmpty then Error errors
        else
            let ints =
                intResults
                |> List.map (fun (field, result) -> field, Result.defaultValue 0 result)
                |> Map.ofList
            let int64s =
                int64Results
                |> List.map (fun (field, result) -> field, Result.defaultValue 0L result)
                |> Map.ofList
            Ok
                { Configuration = configuration
                  MissionId = missionId
                  RunIds = runs |> List.map (fun record -> record.RunId) |> List.sort
                  Runs = runs.Length
                  Missions = 1
                  FullSolves = evaluations |> List.filter (fun item -> item.FullSolve) |> List.length
                  BlockedRuns = runs |> List.filter (fun record -> record.Status = Autopilot.Blocked) |> List.length
                  AttemptedSlices = ints.["AttemptedSlices"]
                  CompletedSlices = ints.["CompletedSlices"]
                  PrematureStops = ints.["PrematureStops"]
                  SameSessionResumes = ints.["SameSessionResumes"]
                  FreshStarts = ints.["FreshStarts"]
                  RepairCycles = ints.["RepairCycles"]
                  GateRuns = ints.["GateRuns"]
                  GateFailures = ints.["GateFailures"]
                  HumanInterventions = ints.["HumanInterventions"]
                  MedianDurationMilliseconds = evaluations |> List.map (fun item -> item.DurationMilliseconds) |> median
                  InputTokens = int64s.["InputTokens"]
                  OutputTokens = int64s.["OutputTokens"] }

    /// Aggregate records into mission/protocol baselines. Identical duplicate
    /// records are idempotent; contradictory records for one RunId fail closed.
    let aggregate (records: RunRecord list) : Result<BaselineAggregate list, string list> =
        let recordErrors =
            records
            |> List.collect validateRecord
        let duplicateErrors, uniqueRecords =
            records
            |> List.groupBy (fun record -> record.RunId)
            |> List.fold (fun (errors, accepted) (runId, duplicates) ->
                match List.distinct duplicates with
                | [ record ] -> errors, record :: accepted
                | _ ->
                    sprintf "RunId %s has contradictory Riftward records." runId :: errors, accepted)
                ([], [])
        let errors = recordErrors @ duplicateErrors |> List.sort
        if not errors.IsEmpty then Error errors
        else
            let groupResults =
                uniqueRecords
                |> List.groupBy (fun record -> record.Configuration, record.MissionId)
                |> List.map aggregateGroup
            let groupErrors =
                groupResults
                |> List.collect (function Error errors -> errors | Ok _ -> [])
                |> List.sort
            if not groupErrors.IsEmpty then Error groupErrors
            else
                groupResults
                |> List.choose (function Ok aggregate -> Some aggregate | Error _ -> None)
                |> List.sortBy (fun item ->
                    item.Configuration.EvaluationProtocolDigest, item.MissionId,
                    item.Configuration.Scout, item.Configuration.Builder,
                    item.Configuration.Critic, item.Configuration.Reviewer)
                |> Ok
