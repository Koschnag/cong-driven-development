namespace Cdd.Core

/// EIDOS v0: a small, deterministic trusted kernel for evidence-governed
/// change compilation. The kernel deliberately stops at an isolated ZT2
/// sandbox. It does not grant production authority to a generator.
module Eidos =

    open System
    open System.IO
    open System.Security.Cryptography
    open System.Text
    open Cdd.Core.Spot

    // ── Content addressing ─────────────────────────────────────────────

    let sha256 (text: string) =
        text
        |> Encoding.UTF8.GetBytes
        |> SHA256.HashData
        |> Convert.ToHexString
        |> fun value -> value.ToLowerInvariant()

    let private digest value = value |> Json.serialize |> sha256
    let private shortHash (value: string) = value.Substring(0, min 16 value.Length)
    let private contentId prefix value = sprintf "%s-%s" prefix (digest value |> shortHash)

    // ── Epistemic intake and system twin ───────────────────────────────

    type EpistemicStatus =
        | Observed
        | Declared
        | Inferred
        | Proposed
        | Ratified
        | Verified
        | Contested
        | Deprecated
        | OutcomeConfirmed
        | Unknown

    type Signal =
        { Id : string
          Source : string
          CapturedAt : DateTimeOffset
          Scope : string
          Content : string
          ContentHash : string }

    type Provenance =
        { SourceSignalIds : string list
          Actor : string
          Method : string
          ToolVersion : string
          RecordedAt : DateTimeOffset }

    type Claim =
        { Id : string
          Subject : string
          Predicate : string
          Object : string
          Scope : string
          Status : EpistemicStatus
          Confidence : decimal option
          Provenance : Provenance }

    type ProjectedClaim =
        { Claim : Claim
          EffectiveStatus : EpistemicStatus }

    type ClaimConflict =
        { Subject : string
          Predicate : string
          Scope : string
          ClaimIds : string list
          Values : string list }

    type SystemTwin =
        { Version : string
          CreatedAt : DateTimeOffset
          Signals : Signal list
          Claims : ProjectedClaim list
          UnknownScopes : string list
          Conflicts : ClaimConflict list
          Findings : string list
          Digest : string }

    let createSignal
        (source: string)
        (scope: string)
        (capturedAt: DateTimeOffset)
        (content: string)
        : Signal =
        let contentHash = sha256 content
        let basis = source, scope, capturedAt.ToUniversalTime(), contentHash
        { Id = contentId "signal" basis
          Source = source
          CapturedAt = capturedAt
          Scope = scope
          Content = content
          ContentHash = contentHash }

    let createClaim
        (status: EpistemicStatus)
        (subject: string)
        (predicate: string)
        (value: string)
        (scope: string)
        (confidence: decimal option)
        (provenance: Provenance)
        : Claim =
        let basis =
            status, subject, predicate, value, scope, confidence,
            provenance.SourceSignalIds, provenance.Actor, provenance.Method,
            provenance.ToolVersion, provenance.RecordedAt.ToUniversalTime()
        { Id = contentId "claim" basis
          Subject = subject
          Predicate = predicate
          Object = value
          Scope = scope
          Status = status
          Confidence = confidence
          Provenance = provenance }

    let private isActiveStatus = function
        | Deprecated -> false
        | _ -> true

    /// Project raw signals and claims without collapsing contradiction or missing
    /// provenance. The original claim status remains in Claim.Status; the
    /// effective status records what the current projection can justify.
    let projectTwin
        (version: string)
        (createdAt: DateTimeOffset)
        (expectedScopes: string list)
        (signals: Signal list)
        (claims: Claim list)
        : SystemTwin =
        let signals = signals |> List.sortBy (fun (signal: Signal) -> signal.Id)
        let signalIds = signals |> List.map (fun signal -> signal.Id) |> Set.ofList
        let claims = claims |> List.sortBy (fun (claim: Claim) -> claim.Id)

        let missingProvenance =
            claims
            |> List.choose (fun (claim: Claim) ->
                let missing =
                    claim.Provenance.SourceSignalIds
                    |> List.filter (fun id -> not (Set.contains id signalIds))
                if missing.IsEmpty then None
                else
                    Some(
                        claim.Id,
                        sprintf "Claim %s references missing signals: %s"
                            claim.Id (String.concat ", " missing)))

        let conflicts =
            claims
            |> List.filter (fun (claim: Claim) -> isActiveStatus claim.Status)
            |> List.groupBy (fun (claim: Claim) -> claim.Subject, claim.Predicate, claim.Scope)
            |> List.choose (fun ((subject, predicate, scope), grouped) ->
                let values = grouped |> List.map (fun (claim: Claim) -> claim.Object) |> List.distinct |> List.sort
                if values.Length <= 1 then None
                else
                    Some
                        { Subject = subject
                          Predicate = predicate
                          Scope = scope
                          ClaimIds = grouped |> List.map (fun (claim: Claim) -> claim.Id) |> List.sort
                          Values = values })
            |> List.sortBy (fun conflict -> conflict.Subject, conflict.Predicate, conflict.Scope)

        let contestedIds =
            conflicts
            |> List.collect (fun conflict -> conflict.ClaimIds)
            |> Set.ofList
        let unknownIds = missingProvenance |> List.map fst |> Set.ofList

        let projected =
            claims
            |> List.map (fun (claim: Claim) ->
                let effective =
                    if Set.contains claim.Id unknownIds then Unknown
                    elif Set.contains claim.Id contestedIds then Contested
                    else claim.Status
                { Claim = claim; EffectiveStatus = effective })

        let knownScopes =
            projected
            |> List.filter (fun projectedClaim ->
                projectedClaim.EffectiveStatus <> Unknown
                && projectedClaim.EffectiveStatus <> Deprecated)
            |> List.map (fun projectedClaim -> projectedClaim.Claim.Scope)
            |> Set.ofList

        let unknownScopes =
            expectedScopes
            |> List.distinct
            |> List.filter (fun scope -> not (Set.contains scope knownScopes))
            |> List.sort

        let findings =
            [ yield! missingProvenance |> List.map snd
              for conflict in conflicts do
                  yield
                      sprintf "Contested %s/%s in %s: %s"
                          conflict.Subject conflict.Predicate conflict.Scope
                          (String.concat " <> " conflict.Values)
              for scope in unknownScopes do
                  yield sprintf "Unknown scope: %s" scope ]

        let basis =
            version, createdAt.ToUniversalTime(), signals, projected,
            unknownScopes, conflicts, findings
        { Version = version
          CreatedAt = createdAt
          Signals = signals
          Claims = projected
          UnknownScopes = unknownScopes
          Conflicts = conflicts
          Findings = findings
          Digest = digest basis }

    // ── Risk, doctrine and mission dispatch ────────────────────────────

    type HazardDimension =
        | FunctionalCorrectness
        | DataIntegrity
        | SecurityPrivacy
        | Availability
        | ExternalContracts
        | RegulatoryImpact
        | PerformanceDeterminism
        | Irreversibility
        | BlastRadius
        | EpistemicUncertainty

    type RiskRating =
        { Dimension : HazardDimension
          Level : Level
          Rationale : string }

    type HazardAssessment =
        { Ratings : RiskRating list
          Highest : Level }

    type AssuranceKind =
        | ProvenanceCheck
        | UnitTests
        | ContractCompatibility
        | SecurityScan
        | SandboxIsolation
        | RecoveryCheck
        | ReplayCheck

    type AssuranceObligation =
        { Id : string
          Kind : AssuranceKind
          Description : string
          Required : bool
          IndependentFromGenerator : bool
          MaxAgeMinutes : int }

    type TrustZone =
        | ZT0
        | ZT1
        | ZT2
        | ZT3
        | ZT4
        | ZT5
        | ZT6

    type MissionBudget =
        { MaxDurationSeconds : int
          MaxCandidates : int
          MaxArtifactBytes : int64 }

    type Doctrine =
        { Id : string
          Version : string
          MaxAutonomy : TrustZone
          MaxAcceptedRisk : Level
          AllowedCapabilities : string list
          MaxBudget : MissionBudget
          EvidenceMaxAgeMinutes : int
          RequireValidatorSeparation : bool
          ForbidProductionCredentials : bool
          EscalationAuthority : string }

    type ChangeIntent =
        { Id : string
          Title : string
          DesiredOutcome : string
          Scope : string list
          Constraints : string list
          SuccessCriteria : string list
          RequestedAt : DateTimeOffset
          Hazard : HazardAssessment }

    type MissionOrder =
        { Id : string
          Situation : string
          Intent : ChangeIntent
          Scope : string list
          Unit : string list
          Constraints : string list
          Obligations : AssuranceObligation list
          Reporting : string list
          Success : string list
          Abort : string list
          Budget : MissionBudget
          Authority : string
          TrustZone : TrustZone
          DoctrineId : string
          DoctrineVersion : string
          TwinDigest : string
          IssuedAt : DateTimeOffset }

    type ControlDecision =
        | Continue
        | Abort of reasons: string list
        | Escalate of authority: string * reasons: string list

    let private levelRank = function
        | Low -> 0
        | Medium -> 1
        | High -> 2
        | Critical -> 3

    let private zoneRank = function
        | ZT0 -> 0
        | ZT1 -> 1
        | ZT2 -> 2
        | ZT3 -> 3
        | ZT4 -> 4
        | ZT5 -> 5
        | ZT6 -> 6

    let hazard (ratings: RiskRating list) : HazardAssessment =
        let ratings = ratings |> List.sortBy (fun (rating: RiskRating) -> rating.Dimension)
        let highest =
            ratings
            |> List.map (fun (rating: RiskRating) -> rating.Level)
            |> List.sortByDescending levelRank
            |> List.tryHead
            |> Option.defaultValue Low
        { Ratings = ratings; Highest = highest }

    let createIntent
        (title: string)
        (desiredOutcome: string)
        (scope: string list)
        (constraints: string list)
        (success: string list)
        (requestedAt: DateTimeOffset)
        (hazardAssessment: HazardAssessment)
        : ChangeIntent =
        let basis =
            title, desiredOutcome, List.sort scope, List.sort constraints,
            List.sort success, requestedAt.ToUniversalTime(), hazardAssessment
        { Id = contentId "intent" basis
          Title = title
          DesiredOutcome = desiredOutcome
          Scope = scope |> List.distinct |> List.sort
          Constraints = constraints |> List.distinct |> List.sort
          SuccessCriteria = success |> List.distinct |> List.sort
          RequestedAt = requestedAt
          Hazard = hazardAssessment }

    let private obligation
        (kind: AssuranceKind)
        (description: string)
        (maxAge: int)
        : AssuranceObligation =
        { Id = sprintf "obligation-%A" kind |> fun value -> value.ToLowerInvariant()
          Kind = kind
          Description = description
          Required = true
          IndependentFromGenerator = true
          MaxAgeMinutes = maxAge }

    let deriveObligations
        (doctrine: Doctrine)
        (intent: ChangeIntent)
        (twin: SystemTwin)
        (trustZone: TrustZone)
        : AssuranceObligation list =
        let hasAtLeast dimension minimum =
            intent.Hazard.Ratings
            |> List.exists (fun (rating: RiskRating) ->
                rating.Dimension = dimension
                && levelRank rating.Level >= levelRank minimum)

        [ obligation ProvenanceCheck "Bind claims, sources, policy and artifact identity." doctrine.EvidenceMaxAgeMinutes
          obligation UnitTests "Run deterministic behavioral checks." doctrine.EvidenceMaxAgeMinutes
          if hasAtLeast ExternalContracts Medium
             || hasAtLeast FunctionalCorrectness High then
              obligation ContractCompatibility "Prove the previous public contract remains valid." doctrine.EvidenceMaxAgeMinutes
          if hasAtLeast SecurityPrivacy Medium then
              obligation SecurityScan "Scan the candidate for declared security hazards." doctrine.EvidenceMaxAgeMinutes
          if zoneRank trustZone >= zoneRank ZT2 then
              obligation SandboxIsolation "Prove deployment remains inside the credential-free sandbox." doctrine.EvidenceMaxAgeMinutes
              obligation RecoveryCheck "Prove the baseline is unchanged and recovery is available." doctrine.EvidenceMaxAgeMinutes
          if not twin.UnknownScopes.IsEmpty
             || not twin.Conflicts.IsEmpty
             || hasAtLeast EpistemicUncertainty Medium then
              obligation ProvenanceCheck "Resolve or preserve uncertainty and contradiction explicitly." doctrine.EvidenceMaxAgeMinutes
          obligation ReplayCheck "Recompute content hashes and the ledger chain." doctrine.EvidenceMaxAgeMinutes ]
        |> List.distinctBy (fun item -> item.Kind)
        |> List.sortBy (fun item -> item.Kind)

    let defaultDoctrine : Doctrine =
        { Id = "doctrine-eidos-zt2"
          Version = "1.0.0"
          MaxAutonomy = ZT2
          MaxAcceptedRisk = High
          AllowedCapabilities =
            [ "artifact.write"
              "gate.execute"
              "ledger.append"
              "sandbox.deploy" ]
          MaxBudget =
            { MaxDurationSeconds = 120
              MaxCandidates = 3
              MaxArtifactBytes = 1_048_576L }
          EvidenceMaxAgeMinutes = 30
          RequireValidatorSeparation = true
          ForbidProductionCredentials = true
          EscalationAuthority = "maintainer" }

    let dispatch
        (doctrine: Doctrine)
        (trustZone: TrustZone)
        (requestedCapabilities: string list)
        (budget: MissionBudget)
        (intent: ChangeIntent)
        (twin: SystemTwin)
        (issuedAt: DateTimeOffset)
        : Result<MissionOrder, string list> =
        let missingCapabilities =
            requestedCapabilities
            |> List.distinct
            |> List.filter (fun capability ->
                not (List.contains capability doctrine.AllowedCapabilities))

        let unknownScope =
            intent.Scope
            |> List.filter (fun scope -> List.contains scope twin.UnknownScopes)

        let errors =
            [ if zoneRank trustZone > zoneRank doctrine.MaxAutonomy then
                  yield sprintf "Trust zone %A exceeds doctrine maximum %A." trustZone doctrine.MaxAutonomy
              if levelRank intent.Hazard.Highest > levelRank doctrine.MaxAcceptedRisk then
                  yield
                      sprintf "Risk %A exceeds doctrine maximum %A."
                          intent.Hazard.Highest doctrine.MaxAcceptedRisk
              if budget.MaxDurationSeconds <= 0
                 || budget.MaxDurationSeconds > doctrine.MaxBudget.MaxDurationSeconds then
                  yield "Requested duration exceeds the doctrine budget."
              if budget.MaxCandidates <= 0
                 || budget.MaxCandidates > doctrine.MaxBudget.MaxCandidates then
                  yield "Requested candidate count exceeds the doctrine budget."
              if budget.MaxArtifactBytes <= 0L
                 || budget.MaxArtifactBytes > doctrine.MaxBudget.MaxArtifactBytes then
                  yield "Requested artifact size exceeds the doctrine budget."
              if not missingCapabilities.IsEmpty then
                  yield
                      sprintf "Capabilities are not allowlisted: %s"
                          (String.concat ", " missingCapabilities)
              if not unknownScope.IsEmpty then
                  yield
                      sprintf "Intent touches unknown scope: %s"
                          (String.concat ", " unknownScope) ]

        if not errors.IsEmpty then Error errors
        else
            let capabilities = requestedCapabilities |> List.distinct |> List.sort
            let obligations = deriveObligations doctrine intent twin trustZone
            let basis =
                intent.Id, twin.Digest, doctrine.Id, doctrine.Version, trustZone,
                capabilities, budget, obligations, issuedAt.ToUniversalTime()
            Ok
                { Id = contentId "mission" basis
                  Situation = sprintf "Change request against twin %s." twin.Version
                  Intent = intent
                  Scope = intent.Scope
                  Unit = capabilities
                  Constraints = intent.Constraints
                  Obligations = obligations
                  Reporting =
                    [ "Report each gate with content-addressed evidence."
                      "Report promotion or rejection with concrete reasons." ]
                  Success = intent.SuccessCriteria
                  Abort =
                    [ "Budget exceeded."
                      "Capability or policy violation."
                      "Missing, stale, correlated, or failed evidence." ]
                  Budget = budget
                  Authority = doctrine.EscalationAuthority
                  TrustZone = trustZone
                  DoctrineId = doctrine.Id
                  DoctrineVersion = doctrine.Version
                  TwinDigest = twin.Digest
                  IssuedAt = issuedAt }

    let controlCheck
        (doctrine: Doctrine)
        (mission: MissionOrder)
        (elapsedSeconds: int)
        (activeCapabilities: string list)
        (policyViolations: string list)
        : ControlDecision =
        let missing =
            mission.Unit
            |> List.filter (fun capability -> not (List.contains capability activeCapabilities))
        let abortReasons =
            [ if elapsedSeconds > mission.Budget.MaxDurationSeconds then
                  yield "Mission duration budget exceeded."
              if not missing.IsEmpty then
                  yield sprintf "Missing active capabilities: %s" (String.concat ", " missing) ]
        if not abortReasons.IsEmpty then Abort abortReasons
        elif not policyViolations.IsEmpty then
            Escalate(doctrine.EscalationAuthority, policyViolations)
        else Continue

    // ── Semantic change compilation and ledger ─────────────────────────

    type ArtifactOperation =
        | Add
        | Modify
        | Delete

    type ArtifactChange =
        { Path : string
          Operation : ArtifactOperation
          Content : string
          ContentHash : string }

    type SemanticDelta =
        { Summary : string
          BeforeVersion : string
          AfterVersion : string
          AddedFields : string list
          RemovedFields : string list
          ChangedContracts : string list
          BackwardCompatible : bool }

    type CandidateProposal =
        { Name : string
          Delta : SemanticDelta
          Artifacts : ArtifactChange list
          Assumptions : string list
          DeploymentPlan : string list
          RecoveryPlan : string list }

    type RejectedAlternative =
        { Name : string
          Reasons : string list }

    type Candidate =
        { Id : string
          Name : string
          IntentId : string
          TwinDigest : string
          DoctrineId : string
          PolicyVersion : string
          GeneratorId : string
          SemanticDelta : SemanticDelta
          ArtifactChanges : ArtifactChange list
          AssuranceObligations : AssuranceObligation list
          Assumptions : string list
          RejectedAlternatives : RejectedAlternative list
          DeploymentPlan : string list
          RecoveryPlan : string list
          ArtifactHash : string }

    type LedgerEvent =
        { Sequence : int
          Kind : string
          At : DateTimeOffset
          MissionId : string
          Detail : string
          PreviousHash : string
          Hash : string }

    type Compilation =
        { Candidates : Candidate list
          Rejected : RejectedAlternative list
          Ledger : LedgerEvent list
          LedgerHash : string }

    let artifact
        (operation: ArtifactOperation)
        (path: string)
        (content: string)
        : ArtifactChange =
        { Path = path
          Operation = operation
          Content = content
          ContentHash = sha256 content }

    let private validRelativePath (path: string) =
        not (String.IsNullOrWhiteSpace path)
        && not (Path.IsPathRooted path)
        && path.Split([| '/'; '\\' |], StringSplitOptions.RemoveEmptyEntries)
           |> Array.forall (fun segment -> segment <> "." && segment <> "..")

    let private artifactDigest (artifacts: ArtifactChange list) =
        artifacts
        |> List.sortBy (fun (artifact: ArtifactChange) -> artifact.Path)
        |> List.map (fun (artifact: ArtifactChange) ->
            artifact.Path, artifact.Operation, artifact.ContentHash)
        |> digest

    let appendEvent
        (missionId: string)
        (at: DateTimeOffset)
        (kind: string)
        (detail: string)
        (events: LedgerEvent list)
        : LedgerEvent list =
        let sequence = events.Length + 1
        let previousHash =
            events |> List.tryLast |> Option.map (fun (event: LedgerEvent) -> event.Hash) |> Option.defaultValue ""
        let hash = digest (sequence, kind, at.ToUniversalTime(), missionId, detail, previousHash)
        events @
        [ { Sequence = sequence
            Kind = kind
            At = at
            MissionId = missionId
            Detail = detail
            PreviousHash = previousHash
            Hash = hash } ]

    let verifyLedger (events: LedgerEvent list) =
        let rec loop (previous: string) (expected: int) (remaining: LedgerEvent list) =
            match remaining with
            | [] -> true
            | event :: tail ->
                let expectedHash =
                    digest(
                        event.Sequence,
                        event.Kind,
                        event.At.ToUniversalTime(),
                        event.MissionId,
                        event.Detail,
                        previous)
                event.Sequence = expected
                && event.PreviousHash = previous
                && event.Hash = expectedHash
                && loop event.Hash (expected + 1) tail
        loop "" 1 events

    let compile
        (generatorId: string)
        (mission: MissionOrder)
        (proposals: CandidateProposal list)
        : Compilation =
        let requiresCompatibility =
            mission.Constraints
            |> List.exists (fun constraintText ->
                constraintText.Contains("backward", StringComparison.OrdinalIgnoreCase)
                || constraintText.Contains("abwärts", StringComparison.OrdinalIgnoreCase))

        let assess (proposal: CandidateProposal) =
            let invalidPaths =
                proposal.Artifacts
                |> List.filter (fun (artifact: ArtifactChange) -> not (validRelativePath artifact.Path))
                |> List.map (fun (artifact: ArtifactChange) -> artifact.Path)
            let duplicatePaths =
                proposal.Artifacts
                |> List.countBy (fun (artifact: ArtifactChange) -> artifact.Path)
                |> List.choose (fun (path, count) -> if count > 1 then Some path else None)
            let oversized =
                proposal.Artifacts
                |> List.sumBy (fun (artifact: ArtifactChange) -> int64 (Encoding.UTF8.GetByteCount artifact.Content))
                |> fun size -> size > mission.Budget.MaxArtifactBytes
            let reasons =
                [ if requiresCompatibility && not proposal.Delta.BackwardCompatible then
                      yield "The proposal violates the backward-compatibility constraint."
                  if not invalidPaths.IsEmpty then
                      yield sprintf "Unsafe artifact paths: %s" (String.concat ", " invalidPaths)
                  if not duplicatePaths.IsEmpty then
                      yield sprintf "Duplicate artifact paths: %s" (String.concat ", " duplicatePaths)
                  if oversized then
                      yield "The proposal exceeds the artifact budget."
                  for artifact in proposal.Artifacts do
                      if sha256 artifact.Content <> artifact.ContentHash then
                          yield sprintf "Artifact content hash mismatch: %s" artifact.Path ]
            proposal, reasons

        let assessed = proposals |> List.sortBy (fun proposal -> proposal.Name) |> List.map assess
        let rejected =
            assessed
            |> List.choose (fun (proposal, reasons) ->
                if reasons.IsEmpty then None
                else Some { Name = proposal.Name; Reasons = reasons })

        let accepted = assessed |> List.filter (fun (_, reasons) -> reasons.IsEmpty) |> List.map fst
        let accepted =
            if accepted.Length > mission.Budget.MaxCandidates then
                accepted |> List.truncate mission.Budget.MaxCandidates
            else accepted

        let truncated =
            assessed
            |> List.filter (fun (proposal, reasons) ->
                reasons.IsEmpty
                && not (accepted |> List.exists (fun item -> item.Name = proposal.Name)))
            |> List.map (fun (proposal, _) ->
                { Name = proposal.Name
                  Reasons = [ "Candidate budget exhausted." ] })

        let rejected = (rejected @ truncated) |> List.sortBy (fun item -> item.Name)

        let candidates =
            accepted
            |> List.map (fun (proposal: CandidateProposal) ->
                let artifacts = proposal.Artifacts |> List.sortBy (fun (artifact: ArtifactChange) -> artifact.Path)
                let artifactHash = artifactDigest artifacts
                let basis =
                    proposal.Name, mission.Intent.Id, mission.TwinDigest,
                    mission.DoctrineId, mission.DoctrineVersion, generatorId,
                    proposal.Delta, artifactHash, mission.Obligations
                { Id = contentId "candidate" basis
                  Name = proposal.Name
                  IntentId = mission.Intent.Id
                  TwinDigest = mission.TwinDigest
                  DoctrineId = mission.DoctrineId
                  PolicyVersion = mission.DoctrineVersion
                  GeneratorId = generatorId
                  SemanticDelta = proposal.Delta
                  ArtifactChanges = artifacts
                  AssuranceObligations = mission.Obligations
                  Assumptions = proposal.Assumptions |> List.distinct |> List.sort
                  RejectedAlternatives = rejected
                  DeploymentPlan = proposal.DeploymentPlan
                  RecoveryPlan = proposal.RecoveryPlan
                  ArtifactHash = artifactHash })

        let mutable ledger : LedgerEvent list = []
        ledger <-
            appendEvent mission.Id mission.IssuedAt "CompilationStarted"
                (sprintf "%d proposals" proposals.Length) ledger
        for rejectedAlternative in rejected do
            ledger <-
                appendEvent mission.Id mission.IssuedAt "AlternativeRejected"
                    (sprintf "%s: %s" rejectedAlternative.Name
                        (String.concat " " rejectedAlternative.Reasons)) ledger
        for candidate in candidates do
            ledger <-
                appendEvent mission.Id mission.IssuedAt "CandidateCompiled"
                    (sprintf "%s %s" candidate.Id candidate.ArtifactHash) ledger
        let ledgerHash =
            ledger |> List.tryLast |> Option.map (fun event -> event.Hash) |> Option.defaultValue ""
        { Candidates = candidates
          Rejected = rejected
          Ledger = ledger
          LedgerHash = ledgerHash }

    // ── Evidence and fail-closed promotion ─────────────────────────────

    type EvidenceResult =
        | Passed
        | Failed
        | Skipped

    type EvidenceRecord =
        { ObligationId : string
          Result : EvidenceResult
          ValidatorId : string
          ToolVersion : string
          Environment : string
          ExecutedAt : DateTimeOffset
          ArtifactHash : string
          PolicyVersion : string
          Details : string
          DetailsHash : string }

    type EvidencePack =
        { Id : string
          CandidateId : string
          ArtifactHash : string
          PolicyVersion : string
          Environment : string
          CreatedAt : DateTimeOffset
          Records : EvidenceRecord list
          Digest : string }

    type PromotionStatus =
        | Promoted
        | Rejected

    type PromotionDecision =
        { Status : PromotionStatus
          CandidateId : string
          EvidencePackId : string
          Target : TrustZone
          DecidedAt : DateTimeOffset
          Reasons : string list }

    let evidence
        (obligation: AssuranceObligation)
        (result: EvidenceResult)
        (validator: string)
        (toolVersion: string)
        (environment: string)
        (executedAt: DateTimeOffset)
        (artifactHash: string)
        (policyVersion: string)
        (details: string)
        : EvidenceRecord =
        { ObligationId = obligation.Id
          Result = result
          ValidatorId = validator
          ToolVersion = toolVersion
          Environment = environment
          ExecutedAt = executedAt
          ArtifactHash = artifactHash
          PolicyVersion = policyVersion
          Details = details
          DetailsHash = sha256 details }

    let createEvidencePack
        (candidate: Candidate)
        (environment: string)
        (createdAt: DateTimeOffset)
        (records: EvidenceRecord list)
        : EvidencePack =
        let records =
            records
            |> List.sortBy (fun (record: EvidenceRecord) -> record.ObligationId, record.ValidatorId)
        let basis =
            candidate.Id, candidate.ArtifactHash, candidate.PolicyVersion,
            environment, createdAt.ToUniversalTime(), records
        let packDigest = digest basis
        { Id = sprintf "evidence-%s" (shortHash packDigest)
          CandidateId = candidate.Id
          ArtifactHash = candidate.ArtifactHash
          PolicyVersion = candidate.PolicyVersion
          Environment = environment
          CreatedAt = createdAt
          Records = records
          Digest = packDigest }

    let verifyEvidencePack (pack: EvidencePack) =
        let basis =
            pack.CandidateId, pack.ArtifactHash, pack.PolicyVersion,
            pack.Environment, pack.CreatedAt.ToUniversalTime(), pack.Records
        pack.Digest = digest basis
        && pack.Id = sprintf "evidence-%s" (digest basis |> shortHash)
        && (pack.Records
            |> List.forall (fun (record: EvidenceRecord) -> record.DetailsHash = sha256 record.Details))

    let evaluatePromotion
        (doctrine: Doctrine)
        (target: TrustZone)
        (now: DateTimeOffset)
        (candidate: Candidate)
        (pack: EvidencePack)
        : PromotionDecision =
        let reasons = ResizeArray<string>()
        if not (verifyEvidencePack pack) then
            reasons.Add "Evidence pack integrity check failed."
        if pack.CandidateId <> candidate.Id then
            reasons.Add "Evidence pack belongs to another candidate."
        if pack.ArtifactHash <> candidate.ArtifactHash then
            reasons.Add "Evidence pack is bound to another artifact."
        if pack.PolicyVersion <> candidate.PolicyVersion then
            reasons.Add "Evidence pack is bound to another policy version."
        if zoneRank target > zoneRank doctrine.MaxAutonomy then
            reasons.Add "Promotion target exceeds doctrine autonomy."
        if target = ZT2 && not (pack.Environment.StartsWith("zt2:", StringComparison.Ordinal)) then
            reasons.Add "ZT2 evidence was not produced in a named ZT2 environment."

        for obligation in candidate.AssuranceObligations |> List.filter (fun (item: AssuranceObligation) -> item.Required) do
            let matches =
                pack.Records
                |> List.filter (fun (record: EvidenceRecord) -> record.ObligationId = obligation.Id)
            match matches with
            | [] -> reasons.Add(sprintf "Missing evidence for %s." obligation.Id)
            | [ record ] ->
                if record.Result <> Passed then
                    reasons.Add(sprintf "Evidence for %s is %A." obligation.Id record.Result)
                if record.ArtifactHash <> candidate.ArtifactHash then
                    reasons.Add(sprintf "Evidence for %s has another artifact hash." obligation.Id)
                if record.PolicyVersion <> candidate.PolicyVersion then
                    reasons.Add(sprintf "Evidence for %s has another policy version." obligation.Id)
                if record.Environment <> pack.Environment then
                    reasons.Add(sprintf "Evidence for %s has another environment." obligation.Id)
                let age = now - record.ExecutedAt
                if age < TimeSpan.Zero || age.TotalMinutes > float obligation.MaxAgeMinutes then
                    reasons.Add(sprintf "Evidence for %s is stale or future-dated." obligation.Id)
                if doctrine.RequireValidatorSeparation
                   && obligation.IndependentFromGenerator
                   && record.ValidatorId = candidate.GeneratorId then
                    reasons.Add(sprintf "Evidence for %s is generator-correlated." obligation.Id)
            | _ -> reasons.Add(sprintf "Ambiguous duplicate evidence for %s." obligation.Id)

        let distinctReasons = reasons |> Seq.distinct |> Seq.sort |> Seq.toList
        { Status = if distinctReasons.IsEmpty then Promoted else Rejected
          CandidateId = candidate.Id
          EvidencePackId = pack.Id
          Target = target
          DecidedAt = now
          Reasons = distinctReasons }

    // ── OpsLab: deterministic ZT2 demonstrator ─────────────────────────

    type OpsLabFault =
        | NoFault
        | FailedGate
        | FailedUnitGate
        | StaleEvidence
        | CorrelatedValidator
        | MissingEvidence
        | ArtifactMismatch
        | PolicyMismatch
        | TamperedPack
        | BudgetExceeded

    type OpsLabStatus =
        | RunPromoted
        | RunRejected

    type OpsLabMetrics =
        { MechanicalHumanTouches : int
          CandidateCount : int
          RejectedAlternativeCount : int
          RequiredObligations : int
          PassedObligations : int
          FailedObligations : int
          ReplayVerified : bool }

    type OpsLabRun =
        { SchemaVersion : int
          RunId : string
          StartedAt : DateTimeOffset
          CompletedAt : DateTimeOffset
          Fault : OpsLabFault
          Doctrine : Doctrine
          Twin : SystemTwin
          Mission : MissionOrder
          Compilation : Compilation
          Candidate : Candidate
          EvidencePack : EvidencePack
          Promotion : PromotionDecision
          Ledger : LedgerEvent list
          BaselineHash : string
          Status : OpsLabStatus
          Metrics : OpsLabMetrics }

    type ReplayResult =
        { RunId : string
          Verified : bool
          Checks : (string * bool) list
          Reasons : string list }

    let private baselineArtifacts =
        [ artifact Add "schema.json" """{
  "version": 1,
  "required": ["reportId", "title", "summary"],
  "properties": {
    "reportId": { "type": "string" },
    "title": { "type": "string" },
    "summary": { "type": "string" }
  }
}"""
          artifact Add "example-report.json" """{
  "reportId": "R-001",
  "title": "Baseline report",
  "summary": "Synthetic OpsLab fixture"
}""" ]

    let private candidateArtifacts =
        [ artifact Modify "schema.json" """{
  "version": 2,
  "required": ["reportId", "title", "summary"],
  "properties": {
    "reportId": { "type": "string" },
    "title": { "type": "string" },
    "summary": { "type": "string" },
    "ownerTeam": { "type": "string", "optional": true }
  }
}"""
          artifact Modify "example-report.json" """{
  "reportId": "R-001",
  "title": "Baseline report",
  "summary": "Synthetic OpsLab fixture",
  "ownerTeam": "operations"
}"""
          artifact Add "index.html" """<!doctype html>
<html lang="en">
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>OpsLab Reports v2</title>
<style>
body{font:16px system-ui;max-width:44rem;margin:auto;padding:2rem;background:#101722;color:#eef}
label{display:grid;gap:.35rem;margin:1rem 0}input,textarea{font:inherit;padding:.7rem}
button{font:inherit;padding:.7rem 1rem;background:#65d5a5;border:0;border-radius:.4rem}
#result{white-space:pre-wrap;background:#192638;padding:1rem;border-radius:.5rem}
</style>
<h1>OpsLab Reports <small>v2</small></h1>
<p>Offline synthetic ZT2 application. The new owner-team field is optional.</p>
<form id="report-form">
  <label>Report ID <input name="reportId" required value="R-002"></label>
  <label>Title <input name="title" required value="Sandbox report"></label>
  <label>Summary <textarea name="summary" required>Evidence-governed change.</textarea></label>
  <label>Owner team <input name="ownerTeam" placeholder="optional"></label>
  <button>Validate report</button>
</form>
<pre id="result" aria-live="polite">Ready.</pre>
<script src="app.js"></script>
</html>"""
          artifact Add "app.js" """const form = document.querySelector("#report-form");
const result = document.querySelector("#result");
form.addEventListener("submit", event => {
  event.preventDefault();
  const report = Object.fromEntries(new FormData(form));
  const missing = ["reportId", "title", "summary"].filter(key => !report[key]?.trim());
  result.textContent = missing.length
    ? `Rejected: missing ${missing.join(", ")}`
    : `Accepted v2\n${JSON.stringify(report, null, 2)}`;
});"""
          artifact Add "deployment.json" """{
  "zone": "ZT2",
  "network": false,
  "credentialsMounted": false,
  "productionAuthority": false
}""" ]

    let private scenario (now: DateTimeOffset) : SystemTwin * MissionOrder * Compilation * Candidate =
        let signal =
            createSignal "opslab-fixture" "report-contract" now
                "Consumers need an optional ownerTeam field; v1 reports must remain valid."
        let provenance : Provenance =
            { SourceSignalIds = [ signal.Id ]
              Actor = "opslab-owner"
              Method = "declared synthetic requirement"
              ToolVersion = "fixture-1"
              RecordedAt = now }
        let claim =
            createClaim Ratified "report-schema" "requires-change" "add ownerTeam"
                "report-contract" (Some 1.0M) provenance
        let twin =
            projectTwin "opslab-v1" now [ "report-contract" ] [ signal ] [ claim ]
        let assessment =
            hazard
                [ { Dimension = FunctionalCorrectness
                    Level = High
                    Rationale = "Existing reports must continue to validate." }
                  { Dimension = ExternalContracts
                    Level = High
                    Rationale = "The versioned report schema is a public contract." }
                  { Dimension = SecurityPrivacy
                    Level = Low
                    Rationale = "The fixture contains synthetic data only." }
                  { Dimension = BlastRadius
                    Level = Low
                    Rationale = "Deployment is restricted to an isolated ZT2 directory." }
                  { Dimension = EpistemicUncertainty
                    Level = Low
                    Rationale = "The fixture has a complete, ratified synthetic requirement." } ]
        let intent =
            createIntent
                "Add optional owner-team metadata"
                "Version-two reports can carry ownerTeam while every valid v1 report remains valid."
                [ "report-contract" ]
                [ "Preserve backward compatibility."
                  "No network or production credentials."
                  "Deploy to ZT2 only." ]
                [ "The v1 fixture validates against v2."
                  "The ownerTeam field remains optional."
                  "All evidence is replayable." ]
                now assessment
        let capabilities =
            [ "artifact.write"; "gate.execute"; "ledger.append"; "sandbox.deploy" ]
        let mission =
            dispatch defaultDoctrine ZT2 capabilities
                { MaxDurationSeconds = 60
                  MaxCandidates = 2
                  MaxArtifactBytes = 131_072L }
                intent twin now
            |> function
                | Ok mission -> mission
                | Error reasons -> failwith (String.concat " " reasons)

        let safeProposal : CandidateProposal =
            { Name = "optional-owner-team"
              Delta =
                { Summary = "Add ownerTeam as an optional report field."
                  BeforeVersion = "1"
                  AfterVersion = "2"
                  AddedFields = [ "ownerTeam" ]
                  RemovedFields = []
                  ChangedContracts = [ "report-schema" ]
                  BackwardCompatible = true }
              Artifacts = candidateArtifacts
              Assumptions = [ "The v1 fixture represents the minimum supported contract." ]
              DeploymentPlan = [ "Materialize in staging."; "Promote to the isolated ZT2 sandbox." ]
              RecoveryPlan = [ "Delete the sandbox projection."; "Keep the baseline content-addressed." ] }
        let breakingProposal =
            { safeProposal with
                Name = "required-owner-team"
                Delta =
                    { safeProposal.Delta with
                        Summary = "Add ownerTeam as a required report field."
                        BackwardCompatible = false }
                Assumptions = [ "All consumers can migrate atomically." ] }
        let compilation =
            compile "eidos-change-compiler-v0" mission [ safeProposal; breakingProposal ]
        let candidate =
            compilation.Candidates
            |> List.tryHead
            |> Option.defaultWith (fun () -> failwith "OpsLab produced no admissible candidate.")
        twin, mission, compilation, candidate

    let private baselineHash () = artifactDigest baselineArtifacts

    let private evaluateScenario
        (now: DateTimeOffset)
        (fault: OpsLabFault)
        : SystemTwin * MissionOrder * Compilation * Candidate * EvidencePack * PromotionDecision =
        let twin, mission, compilation, candidate = scenario now
        let environment = "zt2:opslab-static"

        let makeRecord (obligation: AssuranceObligation) : EvidenceRecord =
            let validator =
                match obligation.Kind with
                | ProvenanceCheck -> "opslab-provenance-oracle-v1"
                | UnitTests -> "opslab-behavior-oracle-v1"
                | ContractCompatibility -> "opslab-contract-oracle-v1"
                | SecurityScan -> "opslab-security-oracle-v1"
                | SandboxIsolation -> "opslab-isolation-oracle-v1"
                | RecoveryCheck -> "opslab-recovery-oracle-v1"
                | ReplayCheck -> "opslab-replay-oracle-v1"
            let details =
                match obligation.Kind with
                | ProvenanceCheck ->
                    sprintf "signal=%s claim=%s twin=%s"
                        twin.Signals.Head.Id twin.Claims.Head.Claim.Id twin.Digest
                | UnitTests -> "Required v1 fields and optional v2 ownerTeam validated."
                | ContractCompatibility -> "The v1 fixture remains valid under schema v2."
                | SecurityScan -> "Static synthetic artifacts contain no secret material."
                | SandboxIsolation -> "network=false credentialsMounted=false productionAuthority=false"
                | RecoveryCheck -> sprintf "baseline=%s unchanged=true" (baselineHash ())
                | ReplayCheck -> sprintf "candidate-artifact=%s recomputed=true" candidate.ArtifactHash
            let result =
                if fault = FailedGate && obligation.Kind = ContractCompatibility then Failed
                elif fault = FailedUnitGate && obligation.Kind = UnitTests then Failed
                else Passed
            let validator =
                if fault = CorrelatedValidator && obligation.Kind = UnitTests then
                    candidate.GeneratorId
                else validator
            let executedAt =
                if fault = StaleEvidence && obligation.Kind = UnitTests then
                    now.AddHours(-2.0)
                else now
            let artifactHash =
                if fault = ArtifactMismatch && obligation.Kind = UnitTests then
                    sha256 "another-artifact"
                else candidate.ArtifactHash
            let policyVersion =
                if fault = PolicyMismatch && obligation.Kind = UnitTests then "0.0.0"
                else candidate.PolicyVersion
            evidence obligation result validator "1.0.0" environment executedAt
                artifactHash policyVersion details

        let records =
            candidate.AssuranceObligations
            |> List.filter (fun obligation ->
                not (fault = MissingEvidence && obligation.Kind = RecoveryCheck))
            |> List.map makeRecord

        let cleanPack = createEvidencePack candidate environment now records
        let pack =
            if fault = TamperedPack then
                { cleanPack with Digest = String.replicate 64 "0" }
            else cleanPack

        let decision =
            if fault = BudgetExceeded then
                { Status = Rejected
                  CandidateId = candidate.Id
                  EvidencePackId = pack.Id
                  Target = ZT2
                  DecidedAt = now
                  Reasons = [ "Mission duration budget exceeded." ] }
            else
                evaluatePromotion defaultDoctrine ZT2 now candidate pack
        twin, mission, compilation, candidate, pack, decision

    let private writeArtifacts (root: string) (artifacts: ArtifactChange list) =
        for artifact in artifacts do
            let target = Path.GetFullPath(Path.Combine(root, artifact.Path))
            let boundary = Path.GetFullPath(root) + string Path.DirectorySeparatorChar
            if not (target.StartsWith(boundary, StringComparison.Ordinal)) then
                invalidArg "artifacts" (sprintf "Artifact escapes sandbox: %s" artifact.Path)
            let parent = Path.GetDirectoryName target
            if not (String.IsNullOrWhiteSpace parent) then
                Directory.CreateDirectory(parent) |> ignore
            match artifact.Operation with
            | Add | Modify -> File.WriteAllText(target, artifact.Content)
            | Delete -> if File.Exists target then File.Delete target

    let private directoryDigest (root: string) =
        if not (Directory.Exists root) then ""
        else
            Directory.GetFiles(root, "*", SearchOption.AllDirectories)
            |> Array.sort
            |> Array.map (fun path ->
                Path.GetRelativePath(root, path).Replace('\\', '/'),
                File.ReadAllText(path) |> sha256)
            |> Array.toList
            |> digest

    let private artifactDirectoryMatches (root: string) (artifacts: ArtifactChange list) =
        artifacts
        |> List.forall (fun (artifact: ArtifactChange) ->
            let target = Path.Combine(root, artifact.Path)
            match artifact.Operation with
            | Add | Modify -> File.Exists target && sha256 (File.ReadAllText target) = artifact.ContentHash
            | Delete -> not (File.Exists target))

    let runOpsLab
        (outputRoot: string)
        (now: DateTimeOffset)
        (fault: OpsLabFault)
        : OpsLabRun =
        let twin, mission, compilation, candidate, pack, decision =
            evaluateScenario now fault
        let runBasis = mission.Id, now.ToUniversalTime(), fault
        let runId = contentId "run" runBasis
        let runsRoot = Path.Combine(Path.GetFullPath outputRoot, ".eidos", "runs")
        let runRoot = Path.Combine(runsRoot, runId)
        if Directory.Exists runRoot then
            invalidOp (sprintf "Run already exists: %s" runId)
        Directory.CreateDirectory(runRoot) |> ignore

        let baselineRoot = Path.Combine(runRoot, "baseline")
        let stagingRoot = Path.Combine(runRoot, "staging")
        let sandboxRoot = Path.Combine(runRoot, "sandbox")
        Directory.CreateDirectory(baselineRoot) |> ignore
        Directory.CreateDirectory(stagingRoot) |> ignore
        writeArtifacts baselineRoot baselineArtifacts
        writeArtifacts stagingRoot candidate.ArtifactChanges
        let before = directoryDigest baselineRoot

        let completedAt = now.AddSeconds(1.0)
        let mutable ledger = compilation.Ledger
        ledger <-
            appendEvent mission.Id now "MissionDispatched"
                (sprintf "%s %A" mission.Id mission.TrustZone) ledger
        for record in pack.Records do
            ledger <-
                appendEvent mission.Id completedAt "GateResult"
                    (sprintf "%s %A %s" record.ObligationId record.Result record.ValidatorId) ledger
        ledger <-
            appendEvent mission.Id completedAt "PromotionDecision"
                (sprintf "%A: %s" decision.Status (String.concat " " decision.Reasons)) ledger

        if decision.Status = Promoted then
            Directory.CreateDirectory(sandboxRoot) |> ignore
            writeArtifacts sandboxRoot candidate.ArtifactChanges

        let replayChecks =
            [ "ledger", verifyLedger ledger
              "evidence-pack", verifyEvidencePack pack
              "staging-artifacts", artifactDirectoryMatches stagingRoot candidate.ArtifactChanges
              "baseline-unchanged", before = directoryDigest baselineRoot
              "sandbox-policy",
                  if decision.Status = Promoted then
                      artifactDirectoryMatches sandboxRoot candidate.ArtifactChanges
                  else not (Directory.Exists sandboxRoot) ]
        let replayVerified = replayChecks |> List.forall snd
        let passed =
            pack.Records |> List.filter (fun (record: EvidenceRecord) -> record.Result = Passed) |> List.length
        let failed =
            pack.Records |> List.filter (fun (record: EvidenceRecord) -> record.Result <> Passed) |> List.length
        let metrics =
            { MechanicalHumanTouches = 0
              CandidateCount = compilation.Candidates.Length
              RejectedAlternativeCount = compilation.Rejected.Length
              RequiredObligations =
                  candidate.AssuranceObligations
                  |> List.filter (fun (obligation: AssuranceObligation) -> obligation.Required)
                  |> List.length
              PassedObligations = passed
              FailedObligations = failed
              ReplayVerified = replayVerified }
        let run : OpsLabRun =
            { SchemaVersion = 1
              RunId = runId
              StartedAt = now
              CompletedAt = completedAt
              Fault = fault
              Doctrine = defaultDoctrine
              Twin = twin
              Mission = mission
              Compilation = compilation
              Candidate = candidate
              EvidencePack = pack
              Promotion = decision
              Ledger = ledger
              BaselineHash = before
              Status = if decision.Status = Promoted then RunPromoted else RunRejected
              Metrics = metrics }

        File.WriteAllText(Path.Combine(runRoot, "run.json"), Json.serialize run)
        Directory.CreateDirectory(Path.Combine(runRoot, "ledger")) |> ignore
        for event in ledger do
            let name = sprintf "%04d-%s.json" event.Sequence (shortHash event.Hash)
            File.WriteAllText(Path.Combine(runRoot, "ledger", name), Json.serialize event)
        File.WriteAllText(
            Path.Combine(runRoot, "SUMMARY.md"),
            String.concat "\n"
                [ "# EIDOS OpsLab run"
                  ""
                  sprintf "- Run: `%s`" run.RunId
                  sprintf "- Status: **%A**" run.Status
                  sprintf "- Candidate: `%s`" run.Candidate.Id
                  sprintf "- Evidence pack: `%s`" run.EvidencePack.Id
                  sprintf "- Replay verified: **%b**" run.Metrics.ReplayVerified
                  sprintf "- Human mechanical touches: **%d**" run.Metrics.MechanicalHumanTouches
                  ""
                  "## Promotion reasons"
                  ""
                  if run.Promotion.Reasons.IsEmpty then "- All required evidence passed."
                  else yield! run.Promotion.Reasons |> List.map (sprintf "- %s")
                  ""
                  "This run is synthetic and restricted to a local ZT2 directory. It contains no production authority." ])
        run

    let replayOpsLab (runRoot: string) : ReplayResult =
        let runPath = Path.Combine(runRoot, "run.json")
        if not (File.Exists runPath) then
            { RunId = Path.GetFileName runRoot
              Verified = false
              Checks = []
              Reasons = [ "run.json is missing." ] }
        else
            let run = Json.deserialize<OpsLabRun> (File.ReadAllText runPath)
            let stagingRoot = Path.Combine(runRoot, "staging")
            let baselineRoot = Path.Combine(runRoot, "baseline")
            let sandboxRoot = Path.Combine(runRoot, "sandbox")
            let checks =
                [ "run-id", run.RunId = Path.GetFileName runRoot
                  "candidate-artifact-hash",
                      run.Candidate.ArtifactHash =
                          artifactDigest run.Candidate.ArtifactChanges
                  "evidence-pack", verifyEvidencePack run.EvidencePack
                  "ledger", verifyLedger run.Ledger
                  "ledger-files",
                      Directory.Exists(Path.Combine(runRoot, "ledger"))
                      && Directory.GetFiles(Path.Combine(runRoot, "ledger"), "*.json").Length
                         = run.Ledger.Length
                  "staging-artifacts",
                      artifactDirectoryMatches stagingRoot run.Candidate.ArtifactChanges
                  "baseline-unchanged", directoryDigest baselineRoot = run.BaselineHash
                  "sandbox-policy",
                      if run.Promotion.Status = Promoted then
                          artifactDirectoryMatches sandboxRoot run.Candidate.ArtifactChanges
                      else not (Directory.Exists sandboxRoot) ]
            let reasons =
                checks
                |> List.choose (fun (name, passed) ->
                    if passed then None else Some(sprintf "Replay check failed: %s." name))
            { RunId = run.RunId
              Verified = reasons.IsEmpty
              Checks = checks
              Reasons = reasons }

    let parseFault (value: string) : OpsLabFault option =
        match (if isNull value then "" else value.Trim().ToLowerInvariant()) with
        | "" | "none" | "no-fault" -> Some NoFault
        | "failed-gate" -> Some FailedGate
        | "failed-unit-gate" -> Some FailedUnitGate
        | "stale-evidence" -> Some StaleEvidence
        | "correlated-validator" -> Some CorrelatedValidator
        | "missing-evidence" -> Some MissingEvidence
        | "artifact-mismatch" -> Some ArtifactMismatch
        | "policy-mismatch" -> Some PolicyMismatch
        | "tampered-pack" -> Some TamperedPack
        | "budget-exceeded" -> Some BudgetExceeded
        | _ -> None

    // ── Falsifiable feature benchmark ──────────────────────────────────

    type BenchmarkExpectation =
        | ExpectPromotion
        | ExpectRejection

    type BenchmarkCaseResult =
        { Id : string
          Fault : OpsLabFault
          Expected : BenchmarkExpectation
          EidosDecision : PromotionStatus
          BaselineDecision : PromotionStatus
          EidosCorrect : bool
          BaselineCorrect : bool
          Rationale : string }

    type BenchmarkScore =
        { Correct : int
          Total : int
          Accuracy : decimal
          UnsafeApprovals : int }

    type BenchmarkReport =
        { SchemaVersion : int
          Benchmark : string
          ReferenceTime : DateTimeOffset
          Cases : BenchmarkCaseResult list
          Eidos : BenchmarkScore
          LinearBaseline : BenchmarkScore
          ScopeNote : string }

    let private expectedStatus = function
        | ExpectPromotion -> Promoted
        | ExpectRejection -> Rejected

    /// Deliberately weak comparator: a fixed linear chain that trusts one green
    /// unit-test result and ignores binding, freshness, independence, budgets,
    /// and the remaining obligations. It is a feature baseline, not a model
    /// leaderboard.
    let private linearBaseline (candidate: Candidate) (pack: EvidencePack) =
        pack.Records
        |> List.tryFind (fun record ->
            candidate.AssuranceObligations
            |> List.exists (fun obligation ->
                obligation.Kind = UnitTests
                && obligation.Id = record.ObligationId))
        |> function
            | Some record when record.Result = Passed -> Promoted
            | _ -> Rejected

    let runBenchmark () : BenchmarkReport =
        let referenceTime = DateTimeOffset(2026, 7, 24, 12, 0, 0, TimeSpan.Zero)
        let cases =
            [ "clean", NoFault, ExpectPromotion,
                "All required, fresh, independent evidence is bound to the candidate."
              "red-contract", FailedGate, ExpectRejection,
                "A required compatibility gate is red."
              "red-unit", FailedUnitGate, ExpectRejection,
                "The unit-test gate is red."
              "stale", StaleEvidence, ExpectRejection,
                "A required record exceeds its freshness window."
              "correlated", CorrelatedValidator, ExpectRejection,
                "The generator validates its own candidate."
              "missing", MissingEvidence, ExpectRejection,
                "A required recovery record is absent."
              "artifact-binding", ArtifactMismatch, ExpectRejection,
                "A record is bound to another artifact."
              "policy-binding", PolicyMismatch, ExpectRejection,
                "A record is bound to another policy version."
              "tampered-pack", TamperedPack, ExpectRejection,
                "The evidence-pack digest was modified."
              "budget", BudgetExceeded, ExpectRejection,
                "The mission exceeded its hard duration budget." ]
            |> List.map (fun (id, fault, expected, rationale) ->
                let _, _, _, candidate, pack, eidosDecision =
                    evaluateScenario referenceTime fault
                let baselineDecision = linearBaseline candidate pack
                let expectedDecision = expectedStatus expected
                { Id = id
                  Fault = fault
                  Expected = expected
                  EidosDecision = eidosDecision.Status
                  BaselineDecision = baselineDecision
                  EidosCorrect = eidosDecision.Status = expectedDecision
                  BaselineCorrect = baselineDecision = expectedDecision
                  Rationale = rationale })

        let score selector decisionSelector =
            let correct = cases |> List.filter selector |> List.length
            let total = cases.Length
            let unsafeApprovals =
                cases
                |> List.filter (fun item ->
                    item.Expected = ExpectRejection
                    && decisionSelector item = Promoted)
                |> List.length
            correct, total, unsafeApprovals

        let eidosCorrect, total, eidosUnsafe =
            score (fun item -> item.EidosCorrect) (fun item -> item.EidosDecision)
        let baselineCorrect, _, baselineUnsafe =
            score (fun item -> item.BaselineCorrect) (fun item -> item.BaselineDecision)
        let makeScore correct unsafe =
            { Correct = correct
              Total = total
              Accuracy = decimal correct / decimal total
              UnsafeApprovals = unsafe }
        { SchemaVersion = 1
          Benchmark = "EvoSDLC-Bench v0 — OpsLab assurance fault injection"
          ReferenceTime = referenceTime
          Cases = cases
          Eidos = makeScore eidosCorrect eidosUnsafe
          LinearBaseline = makeScore baselineCorrect baselineUnsafe
          ScopeNote =
            "Hand-authored construct test for the implemented assurance mechanisms. "
            + "It demonstrates behavior and reproducibility, not external validity or general superiority." }

    let benchmarkMarkdown (report: BenchmarkReport) =
        let rows =
            report.Cases
            |> List.map (fun (item: BenchmarkCaseResult) ->
                sprintf "| `%s` | %A | %A | %A | %A |"
                    item.Id item.Expected item.EidosDecision item.BaselineDecision item.Fault)
        String.concat "\n"
            [ "# EIDOS engineering benchmark"
              ""
              sprintf "**%s** · fixed reference time `%O`" report.Benchmark report.ReferenceTime
              ""
              "| Case | Expected | EIDOS | Linear baseline | Fault |"
              "|---|---|---|---|---|"
              yield! rows
              ""
              sprintf "- EIDOS: **%d/%d**, unsafe approvals **%d**"
                  report.Eidos.Correct report.Eidos.Total report.Eidos.UnsafeApprovals
              sprintf "- Linear baseline: **%d/%d**, unsafe approvals **%d**"
                  report.LinearBaseline.Correct report.LinearBaseline.Total
                  report.LinearBaseline.UnsafeApprovals
              ""
              sprintf "> %s" report.ScopeNote ]
