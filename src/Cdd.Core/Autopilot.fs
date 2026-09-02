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

    /// Stable identity of one ownership attempt for a bounded slice. Attempt
    /// numbers are monotonic per run, mission and slice; an expired attempt is retained
    /// as history and is never silently revived.
    type SliceLeaseIdentity =
        { RunId : string
          MissionId : string
          SliceId : string
          Attempt : int
          OwnerId : string
          WorktreeId : string }

    /// Request emitted by an execution adapter before it creates or assigns an
    /// isolated worktree. Paths are repository-relative scope identifiers, not
    /// host paths. The controller owns the semantic decision; atomic storage is
    /// deliberately an adapter responsibility.
    type SliceLeaseRequest =
        { Identity : SliceLeaseIdentity
          BaseDigest : string
          CandidateDigest : string option
          Scope : string list
          ExpiresAtUtc : DateTimeOffset }

    /// Persistable ownership fact. CandidateDigest advances explicitly when a
    /// builder produces a new candidate; heartbeat alone cannot change it.
    type SliceLease =
        { Identity : SliceLeaseIdentity
          BaseDigest : string
          CandidateDigest : string option
          Scope : string list
          AcquiredAtUtc : DateTimeOffset
          HeartbeatAtUtc : DateTimeOffset
          ExpiresAtUtc : DateTimeOffset }

    /// Every mutation, heartbeat or candidate binding must present the exact
    /// lease subject it observed. A mismatch is stale evidence, not recovery.
    type SliceLeaseSubject =
        { Identity : SliceLeaseIdentity
          BaseDigest : string
          CandidateDigest : string option
          Scope : string list }

    /// Pure lease transition requested at the outer controller boundary. The
    /// values contain no host paths or registry handles; IO adapters may carry
    /// them, but the CDD domain remains the decision authority.
    type SliceLeaseTransition =
        | AcquireLease of history: SliceLease list * request: SliceLeaseRequest
        | VerifyLease of current: SliceLease * subject: SliceLeaseSubject
        | HeartbeatLease of current: SliceLease * subject: SliceLeaseSubject * expiresAtUtc: DateTimeOffset
        | BindLeaseCandidate of current: SliceLease * subject: SliceLeaseSubject * candidateDigest: string

    /// An adapter observation must echo the exact transition and its claimed
    /// outcome. CDD recomputes the outcome before accepting the observation;
    /// an adapter cannot turn rejection into ownership by assertion.
    type SliceLeaseTransitionObservation =
        { Transition : SliceLeaseTransition
          Outcome : Result<SliceLease, string list> }

    type ControllerAction =
        | DispatchAgent of AgentDispatch
        | ExecuteGate of GateExecution
        | CreateCheckpoint of CheckpointRequest
        | DecideSliceLease of SliceLeaseTransition
        | MissionComplete of Evaluation
        | Escalate of reasons: string list

    type RunObservation =
        | AgentTurnObserved of AgentTurnObservation
        | GateObserved of GateObservation
        | CheckpointObserved of CheckpointObservation
        | SliceLeaseTransitionObserved of SliceLeaseTransitionObservation
        | HumanInterventionObserved of reason: string

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

    let private maximumLeaseDuration = TimeSpan.FromHours 24.0

    let private normalizeLeaseScope (value: string) =
        if blank value then Error "Lease scope entries cannot be blank."
        else
            let segments = value.Split('/', StringSplitOptions.None)
            let windowsDrive =
                value.Length >= 2 && Char.IsLetter value.[0] && value.[1] = ':'
            if value <> value.Trim() || value.Contains('\\') || value.EndsWith("/", StringComparison.Ordinal)
               || value |> Seq.exists Char.IsControl then
                Error(sprintf "Lease scope is not canonical: %s" value)
            elif value.StartsWith("/", StringComparison.Ordinal) || windowsDrive then
                Error(sprintf "Lease scope must be repository-relative: %s" value)
            elif segments |> Array.exists (fun segment -> segment = "" || segment = "." || segment = "..") then
                Error(sprintf "Lease scope is not canonical: %s" value)
            else Ok value

    let private normalizeLeaseScopes values =
        let normalized, errors =
            values
            |> List.fold (fun (accepted, rejected) value ->
                match normalizeLeaseScope value with
                | Ok scope -> scope :: accepted, rejected
                | Error error -> accepted, error :: rejected) ([], [])
        let scopes = normalized |> List.rev
        let duplicateErrors =
            duplicates scopes |> List.map (sprintf "Duplicate lease scope: %s")
        if values.IsEmpty then Error [ "A slice lease requires explicit scope." ]
        elif not errors.IsEmpty || not duplicateErrors.IsEmpty then
            Error(List.rev errors @ duplicateErrors)
        else Ok scopes

    let private scopesOverlap (left: string) (right: string) =
        left = right
        || left.StartsWith(right + "/", StringComparison.Ordinal)
        || right.StartsWith(left + "/", StringComparison.Ordinal)

    let private leaseIsLive (at: DateTimeOffset) (lease: SliceLease) = at < lease.ExpiresAtUtc

    let private validateLeaseIdentity (identity: SliceLeaseIdentity) =
        [ if blank identity.RunId then yield "Lease RunId is required."
          if blank identity.MissionId then yield "Lease MissionId is required."
          if blank identity.SliceId then yield "Lease SliceId is required."
          if identity.Attempt < 1 then yield "Lease attempt must be positive."
          if blank identity.OwnerId then yield "Lease owner is required."
          if blank identity.WorktreeId then yield "Lease worktree id is required." ]

    let private validateLeaseExpiry at expiresAt =
        [ if expiresAt <= at then yield "Lease expiry must be in the future."
          if expiresAt - at > maximumLeaseDuration then
              yield "Lease duration exceeds the 24-hour controller limit." ]

    let private validateLeaseRecord at index (lease: SliceLease) =
        [ for error in validateLeaseIdentity lease.Identity do
              yield sprintf "Lease history entry %d: %s" index error
          if blank lease.BaseDigest then
              yield sprintf "Lease history entry %d has no base digest." index
          match lease.CandidateDigest with
          | Some digest when blank digest ->
              yield sprintf "Lease history entry %d has a blank candidate digest." index
          | _ -> ()
          match normalizeLeaseScopes lease.Scope with
          | Error errors ->
              for error in errors do yield sprintf "Lease history entry %d: %s" index error
          | Ok scope when scope <> lease.Scope ->
              yield sprintf "Lease history entry %d has non-canonical scope." index
          | Ok _ -> ()
          if lease.HeartbeatAtUtc < lease.AcquiredAtUtc then
              yield sprintf "Lease history entry %d has a heartbeat before acquisition." index
          if lease.ExpiresAtUtc <= lease.HeartbeatAtUtc then
              yield sprintf "Lease history entry %d is not live after its heartbeat." index
          if lease.ExpiresAtUtc - lease.HeartbeatAtUtc > maximumLeaseDuration then
              yield sprintf "Lease history entry %d exceeds the 24-hour controller limit." index
          if at < lease.AcquiredAtUtc || at < lease.HeartbeatAtUtc then
              yield sprintf "Lease history entry %d is newer than the acquisition observation." index ]

    let private currentLeaseHistory at (history: SliceLease list) =
        let entryErrors =
            history
            |> List.mapi (validateLeaseRecord at)
            |> List.concat
        let identityErrors =
            history
            |> List.groupBy (fun lease ->
                lease.Identity.RunId, lease.Identity.MissionId,
                lease.Identity.SliceId, lease.Identity.Attempt)
            |> List.collect (fun ((runId, missionId, sliceId, attempt), entries) ->
                let identities = entries |> List.map (fun lease -> lease.Identity) |> List.distinct
                if identities.Length > 1 then
                    [ sprintf "Lease history has conflicting ownership for attempt %s/%s/%s/%d."
                        runId missionId sliceId attempt ]
                else [])
        let timelineErrors =
            history
            |> List.groupBy (fun lease -> lease.Identity)
            |> List.collect (fun (identity, entries) ->
                let immutableSubjects =
                    entries
                    |> List.map (fun lease -> lease.BaseDigest, lease.Scope, lease.AcquiredAtUtc)
                    |> List.distinct
                let sameHeartbeatConflicts =
                    entries
                    |> List.groupBy (fun lease -> lease.HeartbeatAtUtc)
                    |> List.exists (fun (_, versions) -> versions |> List.distinct |> List.length > 1)
                let transitionErrors =
                    entries
                    |> List.distinct
                    |> List.sortBy (fun lease -> lease.HeartbeatAtUtc)
                    |> List.pairwise
                    |> List.collect (fun (previous, next) ->
                        [ if next.CandidateDigest = previous.CandidateDigest then
                              if next.ExpiresAtUtc <= previous.ExpiresAtUtc then
                                  yield sprintf "Lease history does not monotonically extend expiry for %s/%s/%d."
                                      identity.RunId identity.SliceId identity.Attempt
                          else
                              match next.CandidateDigest with
                              | None ->
                                  yield sprintf "Lease history rolls back the candidate for %s/%s/%d."
                                      identity.RunId identity.SliceId identity.Attempt
                              | Some _ when next.ExpiresAtUtc <> previous.ExpiresAtUtc ->
                                  yield sprintf "Lease history changes candidate and expiry in one transition for %s/%s/%d."
                                      identity.RunId identity.SliceId identity.Attempt
                              | Some _ -> () ])
                [ if immutableSubjects.Length > 1 then
                      yield sprintf "Lease history drifts immutable subject for %s/%s/%d."
                          identity.RunId identity.SliceId identity.Attempt
                  if sameHeartbeatConflicts then
                      yield sprintf "Lease history has conflicting versions at one heartbeat for %s/%s/%d."
                          identity.RunId identity.SliceId identity.Attempt
                  yield! transitionErrors ])
        let current =
            history
            |> List.groupBy (fun lease -> lease.Identity)
            |> List.map (fun (_, entries) -> entries |> List.maxBy (fun lease -> lease.HeartbeatAtUtc))
        let attemptErrors =
            current
            |> List.groupBy (fun lease ->
                lease.Identity.RunId, lease.Identity.MissionId, lease.Identity.SliceId)
            |> List.collect (fun ((runId, missionId, sliceId), entries) ->
                let attempts = entries |> List.map (fun lease -> lease.Identity.Attempt) |> List.distinct |> List.sort
                let expected = if attempts.IsEmpty then [] else [ 1 .. List.last attempts ]
                if attempts <> expected then
                    [ sprintf "Lease history attempts are not contiguous for %s/%s/%s." runId missionId sliceId ]
                else [])
        let historyConflictErrors =
            current
            |> List.mapi (fun index left ->
                current
                |> List.skip (index + 1)
                |> List.collect (fun right ->
                    let ownershipIntervalsOverlap =
                        left.AcquiredAtUtc < right.ExpiresAtUtc
                        && right.AcquiredAtUtc < left.ExpiresAtUtc
                    [ if ownershipIntervalsOverlap
                         && left.Identity.RunId = right.Identity.RunId
                         && left.Identity.MissionId = right.Identity.MissionId
                         && left.Identity.SliceId = right.Identity.SliceId then
                          yield sprintf "Lease history has overlapping ownership for slice %s/%s/%s."
                              left.Identity.RunId left.Identity.MissionId left.Identity.SliceId
                      if ownershipIntervalsOverlap
                         && left.Identity.WorktreeId = right.Identity.WorktreeId then
                          yield sprintf "Lease history has overlapping ownership for worktree %s."
                              left.Identity.WorktreeId
                      if ownershipIntervalsOverlap
                         && left.Scope |> List.exists (fun a -> right.Scope |> List.exists (scopesOverlap a)) then
                          yield sprintf "Lease history has overlapping scope ownership for %s/%s/%d and %s/%s/%d."
                              left.Identity.RunId left.Identity.SliceId left.Identity.Attempt
                              right.Identity.RunId right.Identity.SliceId right.Identity.Attempt ]))
            |> List.concat
        let errors = entryErrors @ identityErrors @ timelineErrors @ attemptErrors @ historyConflictErrors
        if errors.IsEmpty then Ok current else Error errors

    /// Acquire a lease against the complete retained lease history. Conflicting
    /// live scope/worktree ownership and non-monotonic attempts fail closed.
    let acquireSliceLease
        (at: DateTimeOffset)
        (history: SliceLease list)
        (request: SliceLeaseRequest)
        : Result<SliceLease, string list> =
        let normalizedScope = normalizeLeaseScopes request.Scope
        let currentHistory = currentLeaseHistory at history
        let idempotentReplay =
            match normalizedScope, currentHistory with
            | Ok requestedScope, Ok current ->
                current
                |> List.tryFind (fun lease ->
                    lease.Identity = request.Identity
                    && lease.BaseDigest = request.BaseDigest
                    && lease.CandidateDigest = request.CandidateDigest
                    && lease.Scope = requestedScope
                    && lease.ExpiresAtUtc = request.ExpiresAtUtc
                    && leaseIsLive at lease)
            | _ -> None
        let priorAttempts =
            Result.defaultValue [] currentHistory
            |> List.filter (fun lease ->
                lease.Identity.RunId = request.Identity.RunId
                && lease.Identity.MissionId = request.Identity.MissionId
                && lease.Identity.SliceId = request.Identity.SliceId)
        let expectedAttempt =
            priorAttempts
            |> List.map (fun lease -> lease.Identity.Attempt)
            |> List.sortDescending
            |> List.tryHead
            |> Option.map ((+) 1)
            |> Option.defaultValue 1
        let live = Result.defaultValue [] currentHistory |> List.filter (leaseIsLive at)
        let conflictErrors =
            match normalizedScope with
            | Error _ -> []
            | Ok requestedScope ->
                live
                |> List.collect (fun lease ->
                    [ if lease.Identity.RunId = request.Identity.RunId
                         && lease.Identity.MissionId = request.Identity.MissionId
                         && lease.Identity.SliceId = request.Identity.SliceId then
                          yield sprintf "Slice %s/%s/%s already has a live lease."
                              request.Identity.RunId request.Identity.MissionId request.Identity.SliceId
                      if lease.Identity.WorktreeId = request.Identity.WorktreeId then
                          yield sprintf "Worktree %s already has a live lease." request.Identity.WorktreeId
                      if requestedScope
                         |> List.exists (fun requested -> lease.Scope |> List.exists (scopesOverlap requested)) then
                          yield sprintf "Lease scope conflicts with live attempt %s/%s/%d."
                              lease.Identity.RunId lease.Identity.SliceId lease.Identity.Attempt ])
        let errors =
            [ yield! validateLeaseIdentity request.Identity
              if blank request.BaseDigest then yield "Lease base digest is required."
              match request.CandidateDigest with
              | Some digest when blank digest -> yield "Lease candidate digest cannot be blank."
              | _ -> ()
              yield! validateLeaseExpiry at request.ExpiresAtUtc
              match normalizedScope with
              | Error scopeErrors -> yield! scopeErrors
              | Ok _ -> ()
              match currentHistory with
              | Error historyErrors -> yield! historyErrors
              | Ok _ -> ()
              if idempotentReplay.IsNone then
                  if request.Identity.Attempt <> expectedAttempt then
                      yield sprintf "Expected lease attempt %d, observed %d." expectedAttempt request.Identity.Attempt
                  yield! conflictErrors ]
        match idempotentReplay, errors with
        | Some lease, [] -> Ok lease
        | None, [] ->
            Ok
                { Identity = request.Identity
                  BaseDigest = request.BaseDigest
                  CandidateDigest = request.CandidateDigest
                  Scope = Result.defaultValue [] normalizedScope
                  AcquiredAtUtc = at
                  HeartbeatAtUtc = at
                  ExpiresAtUtc = request.ExpiresAtUtc }
        | _, errors -> Error errors

    let private validateLeaseSubject (subject: SliceLeaseSubject) (lease: SliceLease) =
        let normalizedScope = normalizeLeaseScopes subject.Scope
        [ if subject.Identity <> lease.Identity then yield "Lease identity is stale or conflicts with current ownership."
          if subject.BaseDigest <> lease.BaseDigest then yield "Lease base digest is stale."
          if subject.CandidateDigest <> lease.CandidateDigest then yield "Lease candidate digest is stale."
          match normalizedScope with
          | Error errors -> yield! errors
          | Ok scope when scope <> lease.Scope -> yield "Lease scope is stale or has drifted."
          | Ok _ -> () ]

    /// Validate current ownership without mutation. Expired leases never regain
    /// authority through a late observation.
    let verifySliceLease at subject lease =
        [ yield! validateLeaseRecord at 0 lease
          yield! validateLeaseSubject subject lease
          if not (leaseIsLive at lease) then yield "Slice lease is expired."
          if at < lease.HeartbeatAtUtc then yield "Lease observation predates the current heartbeat." ]

    /// Advance liveness only for the exact current subject. A heartbeat cannot
    /// rebind base, candidate, scope, owner, worktree or attempt.
    let heartbeatSliceLease at expiresAt subject lease =
        let errors =
            [ yield! verifySliceLease at subject lease
              if at <= lease.HeartbeatAtUtc then
                  yield "Heartbeat time must advance monotonically."
              yield! validateLeaseExpiry at expiresAt
              if expiresAt <= lease.ExpiresAtUtc then
                  yield "Heartbeat must extend the existing lease expiry." ]
        if errors.IsEmpty then
            Ok { lease with HeartbeatAtUtc = at; ExpiresAtUtc = expiresAt }
        else Error errors

    /// Bind a newly produced candidate to the exact live attempt. Callers must
    /// retain the previous lease as ledger evidence before persisting this one.
    let bindSliceLeaseCandidate at subject candidateDigest lease =
        let errors =
            [ yield! verifySliceLease at subject lease
              if at <= lease.HeartbeatAtUtc then
                  yield "Candidate binding time must advance monotonically."
              if blank candidateDigest then yield "Candidate digest is required."
              if lease.CandidateDigest = Some candidateDigest then
                  yield "Candidate digest must advance to a new value." ]
        if errors.IsEmpty then
            Ok { lease with CandidateDigest = Some candidateDigest; HeartbeatAtUtc = at }
        else Error errors

    /// Evaluate the typed outer-contract seam with the same pure lease rules
    /// used by direct domain callers. This does not persist a registry entry or
    /// create a worktree.
    let decideSliceLeaseTransition at transition =
        match transition with
        | AcquireLease(history, request) -> acquireSliceLease at history request
        | VerifyLease(current, subject) ->
            match verifySliceLease at subject current with
            | [] -> Ok current
            | errors -> Error errors
        | HeartbeatLease(current, subject, expiresAtUtc) ->
            heartbeatSliceLease at expiresAtUtc subject current
        | BindLeaseCandidate(current, subject, candidateDigest) ->
            bindSliceLeaseCandidate at subject candidateDigest current

    /// Accept an adapter claim only when it echoes the exact requested
    /// transition and exactly matches CDD's recomputed semantic outcome.
    let private validateSliceLeaseTransitionObservation at expectedTransition observation =
        if observation.Transition <> expectedTransition then
            Error [ "Slice lease observation does not match the requested transition." ]
        else
            let expectedOutcome = decideSliceLeaseTransition at expectedTransition
            if observation.Outcome <> expectedOutcome then
                Error [ "Slice lease observation outcome does not match the CDD decision." ]
            else
                expectedOutcome

    /// Validate the complete outer controller exchange, including both DU
    /// cases. A lease observation cannot answer another action, and another
    /// observation cannot answer a lease action.
    let validateSliceLeaseControllerExchange at expectedAction observation =
        match expectedAction, observation with
        | DecideSliceLease transition, SliceLeaseTransitionObserved leaseObservation ->
            validateSliceLeaseTransitionObservation at transition leaseObservation
        | DecideSliceLease _, _ ->
            Error [ "Run observation does not answer the expected slice lease action." ]
        | _, SliceLeaseTransitionObserved _ ->
            Error [ "Slice lease observation was not requested by the expected controller action." ]
        | _ ->
            Error [ "Controller exchange does not contain a slice lease action and observation." ]

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
                | SliceLeaseTransitionObserved _ ->
                    Error [ "Slice lease transitions are not scheduled by the current serial controller." ]
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
