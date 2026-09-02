namespace Cdd.Core

/// Deterministic, provider-neutral controller for durable agentic SDLC runs.
///
/// Models and harnesses execute typed actions and return observations. They do
/// not own the run state and cannot declare a mission promoted. The controller
/// persists progress, bounds recovery and repair, and fails closed when gates,
/// role separation or checkpoints are missing.
module Autopilot =

    open System
    open System.IO

    type LifecycleStage =
        | Discover
        | Specify
        | Design
        | Implement
        | Verify
        | Release
        | Operate
        | Learn

    type AgentRole =
        | Scout
        | Builder
        | Critic
        | Reviewer

    type WorkerProfile =
        { Id : string
          Role : AgentRole
          Provider : string
          Model : string
          Harness : string
          ReadOnly : bool
          Capabilities : string list }

    type GateDefinition =
        { Id : string
          Name : string
          Program : string
          Arguments : string list
          ValidatorId : string
          TimeoutSeconds : int }

    type WorkSlice =
        { Id : string
          Stage : LifecycleStage
          Title : string
          Objective : string
          Scope : string list
          AcceptanceCriteria : string list
          RequiredGateIds : string list }

    type RecoveryPolicy =
        { MaxSameSessionResumes : int
          MaxFreshStarts : int
          MaxRepairCycles : int }

    type RunPlan =
        { MissionId : string
          Objective : string
          Slices : WorkSlice list
          Workers : WorkerProfile list
          Gates : GateDefinition list
          Recovery : RecoveryPolicy }

    type SlicePhase =
        | Scouting
        | Building
        | Gating
        | Critiquing
        | Repairing
        | Reviewing
        | Checkpointing
        | SliceComplete

    type RunStatus =
        | Running
        | Completed
        | Blocked

    type DispatchMode =
        | FreshSession
        | ResumeSession of sessionId: string

    type ContextSlice =
        { MissionId : string
          MissionObjective : string
          SliceId : string
          LifecycleStage : LifecycleStage
          Objective : string
          Scope : string list
          AcceptanceCriteria : string list
          ContextDigest : string option
          CandidateDigest : string option
          OpenFindings : string list }

    type AgentDispatch =
        { Worker : WorkerProfile
          Mode : DispatchMode
          Context : ContextSlice
          Instruction : string
          ExpectedTerminalMarkers : string list }

    type GateExecution =
        { Gate : GateDefinition
          SliceId : string
          CandidateDigest : string }

    type CheckpointRequest =
        { RunId : string
          MissionId : string
          SliceId : string
          CandidateDigest : string
          RequiredPassedGateIds : string list }

    type Evaluation =
        { FullSolve : bool
          CompletedSlices : int
          TotalSlices : int
          AgentTurns : int
          ToolCalls : int
          PrematureStops : int
          SameSessionResumes : int
          FreshStarts : int
          RepairCycles : int
          GateRuns : int
          GateFailures : int
          HumanInterventions : int
          DurationMilliseconds : int64 }

    type ControllerAction =
        | DispatchAgent of AgentDispatch
        | ExecuteGate of GateExecution
        | CreateCheckpoint of CheckpointRequest
        | MissionComplete of Evaluation
        | Escalate of reasons: string list

    type AgentTerminal =
        | WorkCompleted
        | ChangesRequested
        | CannotProceed

    type AgentTelemetry =
        { DurationMilliseconds : int64
          ToolCalls : int
          InputTokens : int64
          OutputTokens : int64 }

    type AgentTurnObservation =
        { WorkerId : string
          Role : AgentRole
          SessionId : string
          DispatchMode : DispatchMode
          TerminalMarker : AgentTerminal option
          Summary : string
          SubjectDigest : string option
          OutputDigest : string option
          Findings : string list
          Telemetry : AgentTelemetry }

    type GateObservation =
        { GateId : string
          ValidatorId : string
          CandidateDigest : string
          Passed : bool
          ExitCode : int
          EvidenceDigest : string
          DurationMilliseconds : int64
          Detail : string }

    type CheckpointObservation =
        { SliceId : string
          CandidateDigest : string
          Succeeded : bool
          CommitHash : string
          CleanWorktree : bool
          Detail : string }

    type RunObservation =
        | AgentTurnObserved of AgentTurnObservation
        | GateObserved of GateObservation
        | CheckpointObserved of CheckpointObservation
        | HumanInterventionObserved of reason: string

    type RecoveryState =
        { Role : AgentRole
          SessionId : string
          SameSessionResumes : int
          FreshStarts : int }

    type SliceExecution =
        { SliceId : string
          Phase : SlicePhase
          ContextDigest : string option
          CandidateDigest : string option
          PassedGateIds : string list
          OpenFindings : string list
          RepairCycles : int
          Recovery : RecoveryState option
          CheckpointCommit : string option }

    type RunMetrics =
        { AgentTurns : int
          ToolCalls : int
          PrematureStops : int
          SameSessionResumes : int
          FreshStarts : int
          RepairCycles : int
          GateRuns : int
          GateFailures : int
          HumanInterventions : int
          DurationMilliseconds : int64
          InputTokens : int64
          OutputTokens : int64 }

    type LedgerEntry =
        { Sequence : int
          At : DateTimeOffset
          Observation : RunObservation
          PreviousHash : string
          Hash : string }

    type RunState =
        { SchemaVersion : string
          RunId : string
          Plan : RunPlan
          Status : RunStatus
          ActiveSliceIndex : int
          SliceExecutions : SliceExecution list
          BlockReasons : string list
          Metrics : RunMetrics
          Ledger : LedgerEntry list
          StartedAtUtc : DateTimeOffset
          UpdatedAtUtc : DateTimeOffset
          FinishedAtUtc : DateTimeOffset option }

    let private blank (value: string) = String.IsNullOrWhiteSpace value

    let private duplicates values =
        values
        |> List.countBy id
        |> List.choose (fun (value, count) -> if count > 1 then Some value else None)

    /// Validate the whole execution contract before any worker receives work.
    let validatePlan (plan: RunPlan) : string list =
        let worker role = plan.Workers |> List.filter (fun item -> item.Role = role)
        let workerIds = plan.Workers |> List.map (fun item -> item.Id)
        let gateIds = plan.Gates |> List.map (fun item -> item.Id)
        let sliceIds = plan.Slices |> List.map (fun item -> item.Id)
        let builders = worker Builder |> List.map (fun item -> item.Id) |> Set.ofList
        [ if blank plan.MissionId then yield "MissionId is required."
          if blank plan.Objective then yield "Mission objective is required."
          if plan.Slices.IsEmpty then yield "At least one bounded work slice is required."
          for duplicate in duplicates sliceIds do
              yield sprintf "Duplicate slice id: %s" duplicate
          for duplicate in duplicates workerIds do
              yield sprintf "Duplicate worker id: %s" duplicate
          for duplicate in duplicates gateIds do
              yield sprintf "Duplicate gate id: %s" duplicate
          for role in [ Scout; Builder; Critic; Reviewer ] do
              if (worker role).Length <> 1 then
                  yield sprintf "Exactly one %A worker is required." role
          for profile in worker Critic @ worker Reviewer do
              if not profile.ReadOnly then
                  yield sprintf "%A worker %s must be read-only." profile.Role profile.Id
          match worker Builder, worker Critic, worker Reviewer with
          | [ builder ], [ critic ], [ reviewer ] ->
              if builder.Id = critic.Id || builder.Id = reviewer.Id || critic.Id = reviewer.Id then
                  yield "Builder, critic and reviewer identities must be distinct."
          | _ -> ()
          for profile in plan.Workers do
              if blank profile.Id || blank profile.Provider || blank profile.Model || blank profile.Harness then
                  yield sprintf "Worker %A has incomplete identity or harness metadata." profile.Role
          for gate in plan.Gates do
              if blank gate.Id || blank gate.Name || blank gate.Program || blank gate.ValidatorId then
                  yield sprintf "Gate '%s' is incomplete." gate.Id
              if gate.TimeoutSeconds <= 0 then
                  yield sprintf "Gate %s must have a positive timeout." gate.Id
              if gate.TimeoutSeconds > 86_400 then
                  yield sprintf "Gate %s timeout exceeds the 24-hour controller limit." gate.Id
              if Set.contains gate.ValidatorId builders then
                  yield sprintf "Gate %s validator must be independent from the builder." gate.Id
          for slice in plan.Slices do
              if blank slice.Id || blank slice.Title || blank slice.Objective then
                  yield sprintf "Slice '%s' requires id, title and objective." slice.Id
              if slice.Scope.IsEmpty then
                  yield sprintf "Slice %s requires an explicit scope." slice.Id
              if slice.AcceptanceCriteria.IsEmpty then
                  yield sprintf "Slice %s requires acceptance criteria." slice.Id
              if slice.RequiredGateIds.IsEmpty then
                  yield sprintf "Slice %s requires at least one deterministic gate." slice.Id
              for gateId in slice.RequiredGateIds |> List.distinct do
                  if not (List.contains gateId gateIds) then
                      yield sprintf "Slice %s references unknown gate %s." slice.Id gateId
          if plan.Recovery.MaxSameSessionResumes < 0 then
              yield "MaxSameSessionResumes cannot be negative."
          if plan.Recovery.MaxFreshStarts < 1 then
              yield "MaxFreshStarts must include at least the initial turn."
          if plan.Recovery.MaxRepairCycles < 0 then
              yield "MaxRepairCycles cannot be negative." ]

    let private emptyMetrics : RunMetrics =
        { AgentTurns = 0
          ToolCalls = 0
          PrematureStops = 0
          SameSessionResumes = 0
          FreshStarts = 0
          RepairCycles = 0
          GateRuns = 0
          GateFailures = 0
          HumanInterventions = 0
          DurationMilliseconds = 0L
          InputTokens = 0L
          OutputTokens = 0L }

    let private initialSlice sliceId : SliceExecution =
        { SliceId = sliceId
          Phase = Scouting
          ContextDigest = None
          CandidateDigest = None
          PassedGateIds = []
          OpenFindings = []
          RepairCycles = 0
          Recovery = None
          CheckpointCommit = None }

    /// Create an in-memory run. Persistence is an adapter concern below.
    let create (at: DateTimeOffset) (plan: RunPlan) : Result<RunState, string list> =
        match validatePlan plan with
        | errors when not errors.IsEmpty -> Error errors
        | _ ->
            let hash = Eidos.sha256 (Json.serialize (plan, at.ToUniversalTime()))
            let runId = sprintf "run-%s-%s" (at.ToUniversalTime().ToString("yyyyMMddTHHmmssZ")) (hash.Substring(0, 10))
            Ok
                { SchemaVersion = "cdd.autopilot/v1"
                  RunId = runId
                  Plan = plan
                  Status = Running
                  ActiveSliceIndex = 0
                  SliceExecutions = plan.Slices |> List.map (fun slice -> initialSlice slice.Id)
                  BlockReasons = []
                  Metrics = emptyMetrics
                  Ledger = []
                  StartedAtUtc = at
                  UpdatedAtUtc = at
                  FinishedAtUtc = None }

    let private activeSlice (run: RunState) = run.Plan.Slices.[run.ActiveSliceIndex]
    let private activeExecution (run: RunState) = run.SliceExecutions.[run.ActiveSliceIndex]
    let private roleForPhase = function
        | Scouting -> Some Scout
        | Building | Repairing -> Some Builder
        | Critiquing -> Some Critic
        | Reviewing -> Some Reviewer
        | _ -> None

    let private workerFor role run =
        run.Plan.Workers |> List.find (fun worker -> worker.Role = role)

    let private dispatchInstruction phase =
        match phase with
        | Scouting ->
            "Inspect only the declared scope. Return a content-addressed context digest and concrete implementation hazards."
        | Building ->
            "Implement only this slice. Preserve user changes, satisfy the acceptance criteria, and return the candidate digest."
        | Repairing ->
            "Repair only the recorded findings. Do not weaken gates or acceptance criteria. Return a new candidate digest."
        | Critiquing ->
            "Review the candidate read-only for correctness, scope and missing tests. Return concrete findings or completion."
        | Reviewing ->
            "Independently review the candidate and passed evidence read-only. Do not edit or promote it."
        | _ -> ""

    let private evaluation (run: RunState) =
        { FullSolve = run.Status = Completed
          CompletedSlices = run.SliceExecutions |> List.filter (fun item -> item.Phase = SliceComplete) |> List.length
          TotalSlices = run.Plan.Slices.Length
          AgentTurns = run.Metrics.AgentTurns
          ToolCalls = run.Metrics.ToolCalls
          PrematureStops = run.Metrics.PrematureStops
          SameSessionResumes = run.Metrics.SameSessionResumes
          FreshStarts = run.Metrics.FreshStarts
          RepairCycles = run.Metrics.RepairCycles
          GateRuns = run.Metrics.GateRuns
          GateFailures = run.Metrics.GateFailures
          HumanInterventions = run.Metrics.HumanInterventions
          DurationMilliseconds = run.Metrics.DurationMilliseconds }

    let evaluate = evaluation

    /// Pure decision function: the same state always yields the same next action.
    let nextAction (run: RunState) : ControllerAction =
        match run.Status with
        | Completed -> MissionComplete(evaluation run)
        | Blocked -> Escalate run.BlockReasons
        | Running ->
            let slice = activeSlice run
            let execution = activeExecution run
            match roleForPhase execution.Phase with
            | Some role ->
                let profile = workerFor role run
                let mode =
                    match execution.Recovery with
                    | Some recovery when recovery.Role = role
                                         && recovery.SameSessionResumes < run.Plan.Recovery.MaxSameSessionResumes ->
                        ResumeSession recovery.SessionId
                    | _ -> FreshSession
                DispatchAgent
                    { Worker = profile
                      Mode = mode
                      Context =
                        { MissionId = run.Plan.MissionId
                          MissionObjective = run.Plan.Objective
                          SliceId = slice.Id
                          LifecycleStage = slice.Stage
                          Objective = slice.Objective
                          Scope = slice.Scope
                          AcceptanceCriteria = slice.AcceptanceCriteria
                          ContextDigest = execution.ContextDigest
                          CandidateDigest = execution.CandidateDigest
                          OpenFindings = execution.OpenFindings }
                      Instruction = dispatchInstruction execution.Phase
                      ExpectedTerminalMarkers = [ "WorkCompleted"; "ChangesRequested"; "CannotProceed" ] }
            | None ->
                match execution.Phase with
                | Gating ->
                    let gateId =
                        slice.RequiredGateIds
                        |> List.find (fun gateId -> not (List.contains gateId execution.PassedGateIds))
                    let gate = run.Plan.Gates |> List.find (fun item -> item.Id = gateId)
                    ExecuteGate
                        { Gate = gate
                          SliceId = slice.Id
                          CandidateDigest = execution.CandidateDigest.Value }
                | Checkpointing ->
                    CreateCheckpoint
                        { RunId = run.RunId
                          MissionId = run.Plan.MissionId
                          SliceId = slice.Id
                          CandidateDigest = execution.CandidateDigest.Value
                          RequiredPassedGateIds = execution.PassedGateIds |> List.sort }
                | SliceComplete -> Escalate [ "Internal controller error: active slice is already complete." ]
                | _ -> Escalate [ "Internal controller error: no action for phase." ]

    let private updateActive (execution: SliceExecution) (run: RunState) =
        { run with
            SliceExecutions =
                run.SliceExecutions
                |> List.mapi (fun index item -> if index = run.ActiveSliceIndex then execution else item) }

    let private block (at: DateTimeOffset) (reasons: string list) (run: RunState) =
        { run with
            Status = Blocked
            BlockReasons = reasons
            UpdatedAtUtc = at
            FinishedAtUtc = Some at }

    let private nextRepair (at: DateTimeOffset) (findings: string list) (execution: SliceExecution) (run: RunState) =
        let repairs = execution.RepairCycles + 1
        let metrics = { run.Metrics with RepairCycles = run.Metrics.RepairCycles + 1 }
        if repairs > run.Plan.Recovery.MaxRepairCycles then
            block at [ sprintf "Repair budget exhausted for slice %s." execution.SliceId ] { run with Metrics = metrics }
        else
            { execution with
                Phase = Repairing
                PassedGateIds = []
                OpenFindings = findings
                RepairCycles = repairs
                Recovery = None }
            |> fun updated -> updateActive updated { run with Metrics = metrics }

    let private appendLedger (at: DateTimeOffset) (observation: RunObservation) (run: RunState) =
        let sequence = run.Ledger.Length + 1
        let previous = run.Ledger |> List.tryLast |> Option.map (fun item -> item.Hash) |> Option.defaultValue ""
        let hash = Eidos.sha256 (Json.serialize (sequence, at.ToUniversalTime(), observation, previous))
        { run with
            Ledger = run.Ledger @ [ { Sequence = sequence; At = at; Observation = observation; PreviousHash = previous; Hash = hash } ]
            UpdatedAtUtc = at }

    let verifyLedger (entries: LedgerEntry list) =
        let rec verify previous expected remaining =
            match remaining with
            | [] -> true
            | entry :: tail ->
                let hash = Eidos.sha256 (Json.serialize (entry.Sequence, entry.At.ToUniversalTime(), entry.Observation, previous))
                entry.Sequence = expected
                && entry.PreviousHash = previous
                && entry.Hash = hash
                && verify entry.Hash (expected + 1) tail
        verify "" 1 entries

    let private telemetryValid (telemetry: AgentTelemetry) =
        telemetry.DurationMilliseconds >= 0L
        && telemetry.ToolCalls >= 0
        && telemetry.InputTokens >= 0L
        && telemetry.OutputTokens >= 0L

    let private recordInterrupted
        (at: DateTimeOffset)
        (turn: AgentTurnObservation)
        (execution: SliceExecution)
        (run: RunState) =
        let priorFreshStarts = execution.Recovery |> Option.map (fun item -> item.FreshStarts) |> Option.defaultValue 0
        let priorResumes = execution.Recovery |> Option.map (fun item -> item.SameSessionResumes) |> Option.defaultValue 0
        let freshStarts, resumes =
            match turn.DispatchMode with
            | FreshSession -> priorFreshStarts + 1, 0
            | ResumeSession expected when expected = turn.SessionId -> priorFreshStarts, priorResumes + 1
            | ResumeSession _ -> priorFreshStarts, run.Plan.Recovery.MaxSameSessionResumes
        let recovery =
            { Role = turn.Role
              SessionId = turn.SessionId
              SameSessionResumes = resumes
              FreshStarts = freshStarts }
        let metrics =
            { run.Metrics with
                PrematureStops = run.Metrics.PrematureStops + 1
                SameSessionResumes = run.Metrics.SameSessionResumes
                FreshStarts = run.Metrics.FreshStarts }
        let updated = updateActive { execution with Recovery = Some recovery } { run with Metrics = metrics }
        if resumes >= run.Plan.Recovery.MaxSameSessionResumes
           && freshStarts >= run.Plan.Recovery.MaxFreshStarts then
            block at [ sprintf "Agent recovery budget exhausted for %A on slice %s." turn.Role execution.SliceId ] updated
        else updated

    let private applyAgent
        (at: DateTimeOffset)
        (turn: AgentTurnObservation)
        (run: RunState)
        : Result<RunState, string list> =
        let execution = activeExecution run
        let expectedRole = roleForPhase execution.Phase
        let expectedWorker = expectedRole |> Option.map (fun role -> workerFor role run)
        let expectedMode =
            match nextAction run with
            | DispatchAgent dispatch -> Some dispatch.Mode
            | _ -> None
        let errors =
            [ if expectedRole <> Some turn.Role then
                  yield sprintf "Expected %A, observed %A." expectedRole turn.Role
              match expectedWorker with
              | Some worker when worker.Id <> turn.WorkerId ->
                  yield sprintf "Expected worker %s, observed %s." worker.Id turn.WorkerId
              | _ -> ()
              if blank turn.SessionId then yield "Agent session id is required."
              if not (telemetryValid turn.Telemetry) then yield "Agent telemetry cannot be negative."
              if expectedMode <> Some turn.DispatchMode then
                  yield sprintf "Expected dispatch mode %A, observed %A." expectedMode turn.DispatchMode
              match turn.DispatchMode, execution.Recovery with
              | ResumeSession sessionId, Some recovery when sessionId <> recovery.SessionId ->
                  yield "Resume observation does not match the recoverable session."
              | ResumeSession _, None -> yield "No session is available to resume."
              | _ -> ()
              if (turn.Role = Critic || turn.Role = Reviewer)
                 && turn.SubjectDigest <> execution.CandidateDigest then
                  yield "Review observation targets a stale or unknown candidate digest." ]
        if not errors.IsEmpty then Error errors
        else
            let metrics =
                { run.Metrics with
                    AgentTurns = run.Metrics.AgentTurns + 1
                    ToolCalls = run.Metrics.ToolCalls + turn.Telemetry.ToolCalls
                    DurationMilliseconds = run.Metrics.DurationMilliseconds + turn.Telemetry.DurationMilliseconds
                    InputTokens = run.Metrics.InputTokens + turn.Telemetry.InputTokens
                    OutputTokens = run.Metrics.OutputTokens + turn.Telemetry.OutputTokens
                    SameSessionResumes =
                        run.Metrics.SameSessionResumes
                        + (match turn.DispatchMode with ResumeSession _ -> 1 | FreshSession -> 0)
                    FreshStarts =
                        run.Metrics.FreshStarts
                        + (match turn.DispatchMode with FreshSession -> 1 | ResumeSession _ -> 0) }
            let run = { run with Metrics = metrics }
            match turn.TerminalMarker with
            | None -> Ok(recordInterrupted at turn execution run)
            | Some CannotProceed ->
                let reason = if blank turn.Summary then sprintf "%A worker cannot proceed." turn.Role else turn.Summary
                Ok(block at [ reason ] run)
            | Some ChangesRequested when turn.Role = Critic || turn.Role = Reviewer ->
                if turn.Findings.IsEmpty then Error [ "ChangesRequested requires concrete findings." ]
                else Ok(nextRepair at turn.Findings execution run)
            | Some ChangesRequested -> Error [ sprintf "%A cannot issue ChangesRequested in phase %A." turn.Role execution.Phase ]
            | Some WorkCompleted ->
                match execution.Phase with
                | Scouting ->
                    match turn.OutputDigest with
                    | Some digest when not (blank digest) ->
                        Ok(updateActive { execution with Phase = Building; ContextDigest = Some digest; Recovery = None } run)
                    | _ -> Error [ "Scout completion requires a context digest." ]
                | Building | Repairing ->
                    match turn.OutputDigest with
                    | Some digest when not (blank digest) ->
                        Ok(updateActive
                            { execution with
                                Phase = Gating
                                CandidateDigest = Some digest
                                PassedGateIds = []
                                OpenFindings = []
                                Recovery = None } run)
                    | _ -> Error [ "Builder completion requires a candidate digest." ]
                | Critiquing ->
                    if turn.Findings.IsEmpty then
                        Ok(updateActive { execution with Phase = Reviewing; Recovery = None } run)
                    else Ok(nextRepair at turn.Findings execution run)
                | Reviewing ->
                    if turn.Findings.IsEmpty then
                        Ok(updateActive { execution with Phase = Checkpointing; Recovery = None } run)
                    else Ok(nextRepair at turn.Findings execution run)
                | phase -> Error [ sprintf "Agent completion is invalid in phase %A." phase ]

    let private applyGate
        (at: DateTimeOffset)
        (observation: GateObservation)
        (run: RunState)
        : Result<RunState, string list> =
        let slice = activeSlice run
        let execution = activeExecution run
        let expected = nextAction run
        match expected with
        | ExecuteGate request ->
            let errors =
                [ if observation.GateId <> request.Gate.Id then
                      yield sprintf "Expected gate %s, observed %s." request.Gate.Id observation.GateId
                  if observation.ValidatorId <> request.Gate.ValidatorId then
                      yield sprintf "Gate %s used unexpected validator %s." observation.GateId observation.ValidatorId
                  if observation.CandidateDigest <> request.CandidateDigest then
                      yield "Gate observation targets a stale candidate digest."
                  if blank observation.EvidenceDigest then yield "Gate evidence digest is required."
                  if observation.DurationMilliseconds < 0L then yield "Gate duration cannot be negative."
                  if observation.Passed && observation.ExitCode <> 0 then
                      yield "A passing gate must have exit code zero." ]
            if not errors.IsEmpty then Error errors
            else
                let metrics =
                    { run.Metrics with
                        GateRuns = run.Metrics.GateRuns + 1
                        GateFailures = run.Metrics.GateFailures + (if observation.Passed then 0 else 1)
                        DurationMilliseconds = run.Metrics.DurationMilliseconds + observation.DurationMilliseconds }
                let run = { run with Metrics = metrics }
                if observation.Passed then
                    let passed = observation.GateId :: execution.PassedGateIds |> List.distinct
                    let phase =
                        if slice.RequiredGateIds |> List.forall (fun gateId -> List.contains gateId passed)
                        then Critiquing
                        else Gating
                    Ok(updateActive { execution with PassedGateIds = passed; Phase = phase } run)
                else
                    let detail = if blank observation.Detail then sprintf "Gate %s failed." observation.GateId else observation.Detail
                    Ok(nextRepair at [ detail ] execution run)
        | _ -> Error [ sprintf "Gate observation is invalid in phase %A." execution.Phase ]

    let private applyCheckpoint
        (at: DateTimeOffset)
        (observation: CheckpointObservation)
        (run: RunState)
        : Result<RunState, string list> =
        let execution = activeExecution run
        match nextAction run with
        | CreateCheckpoint request ->
            let errors =
                [ if observation.SliceId <> request.SliceId then yield "Checkpoint targets the wrong slice."
                  if observation.CandidateDigest <> request.CandidateDigest then yield "Checkpoint targets a stale candidate."
                  if observation.Succeeded && blank observation.CommitHash then yield "Successful checkpoint requires a commit hash."
                  if observation.Succeeded && not observation.CleanWorktree then yield "Successful checkpoint requires a clean worktree." ]
            if not errors.IsEmpty then Error errors
            elif not observation.Succeeded then
                let reason = if blank observation.Detail then "Checkpoint failed." else observation.Detail
                Ok(block at [ reason ] run)
            else
                let finished = { execution with Phase = SliceComplete; CheckpointCommit = Some observation.CommitHash }
                let run = updateActive finished run
                if run.ActiveSliceIndex + 1 = run.Plan.Slices.Length then
                    Ok { run with Status = Completed; FinishedAtUtc = Some at; UpdatedAtUtc = at }
                else
                    Ok { run with ActiveSliceIndex = run.ActiveSliceIndex + 1; UpdatedAtUtc = at }
        | _ -> Error [ sprintf "Checkpoint observation is invalid in phase %A." execution.Phase ]

    /// Apply one harness observation after validating it against the expected
    /// controller action. Invalid or stale observations never mutate the run.
    let applyObservation (at: DateTimeOffset) (observation: RunObservation) (run: RunState) =
        if run.Status <> Running then Error [ sprintf "Run %s is already %A." run.RunId run.Status ]
        else
            let result =
                match observation with
                | AgentTurnObserved turn -> applyAgent at turn run
                | GateObserved gate -> applyGate at gate run
                | CheckpointObserved checkpoint -> applyCheckpoint at checkpoint run
                | HumanInterventionObserved reason ->
                    if blank reason then Error [ "Human intervention requires a reason." ]
                    else
                        Ok
                            { run with
                                Metrics =
                                    { run.Metrics with
                                        HumanInterventions = run.Metrics.HumanInterventions + 1 } }
            result |> Result.map (appendLedger at observation)

    let private runtimeRoot workspaceRoot =
        Path.Combine(Path.GetFullPath workspaceRoot, ".ai", "runtime", "runs")

    let runDirectory workspaceRoot runId = Path.Combine(runtimeRoot workspaceRoot, runId)

    let private statePath runDirectory = Path.Combine(runDirectory, "state.json")

    let private statusText = function
        | Running -> "running"
        | Completed -> "completed"
        | Blocked -> "blocked"

    /// Persist state atomically and maintain the small `.ai` run manifest used
    /// by Studio. The full state remains provider-neutral and replayable.
    let save (runDirectory: string) (run: RunState) =
        Directory.CreateDirectory runDirectory |> ignore
        let stateFile = statePath runDirectory
        let temporary = stateFile + ".tmp-" + Guid.NewGuid().ToString("N")
        File.WriteAllText(temporary, Json.serialize run)
        File.Move(temporary, stateFile, true)
        let manifest =
            {| runId = run.RunId
               missionId = run.Plan.MissionId
               status = statusText run.Status
               activeSlice = (if run.Status = Completed then "" else (activeSlice run).Id)
               phase = (if run.Status = Completed then "completed" else sprintf "%A" (activeExecution run).Phase)
               startedAtUtc = run.StartedAtUtc.ToUniversalTime().ToString("O")
               finishedAtUtc = run.FinishedAtUtc |> Option.map (fun value -> value.ToUniversalTime().ToString("O")) |}
        File.WriteAllText(Path.Combine(runDirectory, "run.json"), Json.serialize manifest)
        if run.Status <> Running then
            File.WriteAllText(Path.Combine(runDirectory, "summary.json"), Json.serialize (evaluation run))

    let load runDirectory =
        let path = statePath (Path.GetFullPath runDirectory)
        if not (File.Exists path) then Error [ sprintf "Autopilot state not found: %s" path ]
        else
            try
                let state = Json.deserialize<RunState> (File.ReadAllText path)
                if state.SchemaVersion <> "cdd.autopilot/v1" then
                    Error [ sprintf "Unsupported autopilot schema: %s" state.SchemaVersion ]
                elif not (verifyLedger state.Ledger) then Error [ "Autopilot ledger verification failed." ]
                else Ok state
            with ex -> Error [ sprintf "Autopilot state is invalid: %s" ex.Message ]

    let initialize workspaceRoot at plan =
        create at plan
        |> Result.bind (fun run ->
            let directory = runDirectory workspaceRoot run.RunId
            if Directory.Exists directory then Error [ sprintf "Run directory already exists: %s" directory ]
            else
                save directory run
                Ok(directory, run))

    let record runDirectory at observation =
        load runDirectory
        |> Result.bind (applyObservation at observation)
        |> Result.map (fun run -> save runDirectory run; run)
