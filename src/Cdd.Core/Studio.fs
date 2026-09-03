namespace Cdd.Core

/// Open control-plane contracts for CDD Studio.
///
/// The module deliberately contains no filesystem, network or vendor SDK code.
/// Adapters observe external systems and submit typed observations; the core
/// projects them into one workspace view and selects assurance techniques from
/// mission characteristics. This keeps GitHub, an editor, an LLM or a workflow
/// engine replaceable without changing the CDD domain.
module Studio =

    open System

    type IntegrationStandard =
        | Cli
        | Rest
        | Mcp
        | Lsp
        | Glsp
        | Oslc
        | CloudEvents
        | CDEvents
        | Otlp
        | Oci
        | InToto

    type AdapterDirection =
        | Observe
        | Execute
        | Bidirectional

    type AdapterContract =
        { Id : string
          Name : string
          Standard : IntegrationStandard
          Direction : AdapterDirection
          Purpose : string
          Authority : string
          License : string }

    /// Stable open seams. Products may implement any subset, but no vendor is
    /// allowed to become the authoritative domain model.
    let openContracts : AdapterContract list =
        [ { Id = "contract-cli"; Name = "Command Line Interface"; Standard = Cli
            Direction = Bidirectional
            Purpose = "Smallest portable execution and automation boundary."
            Authority = "The invoked tool owns its result; CDD records provenance."
            License = "protocol / implementation-neutral" }
          { Id = "contract-rest"; Name = "REST / OpenAPI"; Standard = Rest
            Direction = Bidirectional
            Purpose = "Language-neutral service and automation boundary for non-agent clients."
            Authority = "CDD validates domain commands; transport does not imply permission."
            License = "open specifications" }
          { Id = "contract-mcp"; Name = "Model Context Protocol"; Standard = Mcp
            Direction = Bidirectional
            Purpose = "Model-neutral discovery and invocation of agent tools and resources."
            Authority = "CDD Doctrine restricts capabilities; MCP does not grant authority."
            License = "open specification" }
          { Id = "contract-lsp"; Name = "Language Server Protocol"; Standard = Lsp
            Direction = Bidirectional
            Purpose = "Decouple textual language intelligence from editors."
            Authority = "Language server owns language semantics."
            License = "open specification" }
          { Id = "contract-glsp"; Name = "Graphical Language Server Platform"; Standard = Glsp
            Direction = Bidirectional
            Purpose = "Decouple typed diagram editing from the Studio shell."
            Authority = "Graphical language server validates model operations."
            License = "EPL-2.0" }
          { Id = "contract-oslc"; Name = "Open Services for Lifecycle Collaboration"; Standard = Oslc
            Direction = Bidirectional
            Purpose = "Link requirements, architecture, quality and change resources across lifecycle tools."
            Authority = "The source lifecycle system remains authoritative for its resource."
            License = "OASIS open standard" }
          { Id = "contract-cdevents"; Name = "CDEvents over CloudEvents"; Standard = CDEvents
            Direction = Observe
            Purpose = "Normalize CI/CD and operations events without binding the event source."
            Authority = "Event source owns occurrence; CDD owns interpretation and policy."
            License = "Apache-2.0 specification" }
          { Id = "contract-otlp"; Name = "OpenTelemetry Protocol"; Standard = Otlp
            Direction = Observe
            Purpose = "Vendor-neutral traces, metrics and logs for actual-state evidence."
            Authority = "Telemetry is observation, never automatic product truth."
            License = "Apache-2.0" }
          { Id = "contract-oci"; Name = "OCI artifacts and in-toto attestations"; Standard = Oci
            Direction = Bidirectional
            Purpose = "Portable content-addressed artifacts and supply-chain evidence."
            Authority = "Digest and attestation policy control promotion."
            License = "open specifications" } ]

    type AssuranceTechnique =
        | TypeChecking
        | StaticAnalysis
        | ExampleTesting
        | PropertyTesting
        | RelationalModelFinding
        | TemporalModelChecking
        | ContractVerification
        | DeductiveProof
        | PolicyEvaluation
        | SupplyChainProvenance
        | RuntimeVerification
        | Observability
        | HumanRatification

    type AssuranceTool =
        { Id : string
          Name : string
          Technique : AssuranceTechnique
          License : string
          Interface : string
          Purpose : string
          Limitation : string }

    /// FOSS-first reference portfolio. These are candidates behind contracts,
    /// not hard dependencies of the kernel.
    let openAssurancePortfolio : AssuranceTool list =
        [ { Id = "assurance-fsharp"; Name = "F# compiler and type system"
            Technique = TypeChecking; License = "MIT"
            Interface = "CLI / compiler diagnostics"
            Purpose = "Make invalid domain states difficult or impossible to represent."
            Limitation = "Types prove only properties encoded in the type model." }
          { Id = "assurance-static"; Name = ".NET analyzers"
            Technique = StaticAnalysis; License = "FOSS ecosystem"
            Interface = "compiler / SARIF"
            Purpose = "Find local defects and policy violations cheaply on every change."
            Limitation = "Rules are partial approximations of program behavior." }
          { Id = "assurance-xunit"; Name = "xUnit"
            Technique = ExampleTesting; License = "Apache-2.0"
            Interface = "dotnet test / test reports"
            Purpose = "Verify executable examples and regression contracts."
            Limitation = "Examples cover selected executions, not the whole state space." }
          { Id = "assurance-fscheck"; Name = "FsCheck"
            Technique = PropertyTesting; License = "BSD-3-Clause"
            Interface = "dotnet test / counterexample"
            Purpose = "Search broad generated input spaces for invariant violations."
            Limitation = "Sampling finds counterexamples but is not a universal proof." }
          { Id = "assurance-alloy"; Name = "Alloy Analyzer"
            Technique = RelationalModelFinding; License = "MIT"
            Interface = "model / bounded counterexample"
            Purpose = "Explore relationships, permissions and structural constraints."
            Limitation = "Analysis is exhaustive only within the declared finite scope." }
          { Id = "assurance-tla"; Name = "TLA+ / TLC"
            Technique = TemporalModelChecking; License = "MIT"
            Interface = "specification / state trace"
            Purpose = "Check safety and liveness of concurrent, durable and distributed workflows."
            Limitation = "A correct abstract model does not prove its implementation conforms." }
          { Id = "assurance-dafny"; Name = "Dafny"
            Technique = ContractVerification; License = "MIT"
            Interface = "CLI / proof obligations"
            Purpose = "Verify functional contracts, frames and termination with SMT automation."
            Limitation = "Specifications and solver assumptions remain part of the trust boundary." }
          { Id = "assurance-lean"; Name = "Lean 4"
            Technique = DeductiveProof; License = "Apache-2.0"
            Interface = "CLI / kernel-checked proof term"
            Purpose = "Prove a small number of load-bearing invariants with a small trusted kernel."
            Limitation = "Proof construction and model-to-runtime correspondence are expensive." }
          { Id = "assurance-opa"; Name = "Open Policy Agent"
            Technique = PolicyEvaluation; License = "Apache-2.0"
            Interface = "CLI / REST / decision log"
            Purpose = "Keep promotion, capability and compliance policy separate from executors."
            Limitation = "A policy engine cannot establish that the policy itself is normatively right." }
          { Id = "assurance-slsa"; Name = "SLSA / in-toto"
            Technique = SupplyChainProvenance; License = "open specifications"
            Interface = "attestation / digest"
            Purpose = "Bind artifacts to builders, inputs and reproducible provenance."
            Limitation = "Provenance establishes origin and process, not product correctness." }
          { Id = "assurance-runtime"; Name = "Runtime and chaos harness"
            Technique = RuntimeVerification; License = "adapter-defined"
            Interface = "CLI / evidence record"
            Purpose = "Test failure, recovery and resource behavior in representative environments."
            Limitation = "A finite experiment cannot cover every production condition." }
          { Id = "assurance-otel"; Name = "OpenTelemetry"
            Technique = Observability; License = "Apache-2.0"
            Interface = "OTLP"
            Purpose = "Correlate actual runtime traces, metrics and logs across replaceable backends."
            Limitation = "Telemetry quality depends on instrumentation and sampling." }
          { Id = "assurance-human"; Name = "Named human authority"
            Technique = HumanRatification; License = "not applicable"
            Interface = "signed decision / review evidence"
            Purpose = "Ratify intent, ethics, aesthetics and irreversible business choices."
            Limitation = "Human judgement is fallible and must retain rationale and accountability." } ]

    type MissionProfile =
        { ConcurrentOrDistributed : bool
          RelationshipHeavy : bool
          HighIntegrity : bool
          SecuritySensitive : bool
          ProductionChange : bool
          RuntimeBehavior : bool
          CreativeOrNormative : bool }

    type AssuranceRecommendation =
        { Tool : AssuranceTool
          Required : bool
          Rationale : string }

    let private tool id =
        openAssurancePortfolio |> List.find (fun candidate -> candidate.Id = id)

    /// Risk/shape-adaptive assurance selection. It selects complementary
    /// mechanisms; no single formalism is presented as universal.
    let recommendAssurance (profile: MissionProfile) : AssuranceRecommendation list =
        [ "assurance-fsharp", true, "Typed domain boundaries are the cheapest permanent gate."
          "assurance-static", true, "Static checks run on every candidate."
          "assurance-xunit", true, "Executable acceptance examples anchor expected behavior."
          "assurance-fscheck", true, "Properties search beyond hand-picked examples."
          if profile.RelationshipHeavy then
              "assurance-alloy", profile.HighIntegrity,
                  "Relational structure and permissions benefit from bounded counterexamples."
          if profile.ConcurrentOrDistributed then
              "assurance-tla", profile.HighIntegrity || profile.ProductionChange,
                  "Temporal behavior needs explicit state-transition and liveness analysis."
          if profile.HighIntegrity then
              "assurance-dafny", false,
                  "SMT-backed contracts can verify critical algorithms with moderate automation."
              "assurance-lean", false,
                  "Use kernel-checked proofs for a few load-bearing invariants."
          if profile.SecuritySensitive || profile.ProductionChange then
              "assurance-opa", true,
                  "Capabilities and promotion require an executor-independent policy decision."
              "assurance-slsa", true,
                  "Promoted artifacts require portable provenance and digest binding."
          if profile.RuntimeBehavior || profile.ProductionChange then
              "assurance-runtime", true,
                  "Runtime and recovery claims require representative execution evidence."
              "assurance-otel", profile.ProductionChange,
                  "Actual-state evidence should remain backend-neutral and correlated."
          if profile.CreativeOrNormative || profile.ProductionChange then
              "assurance-human", true,
                  "Normative, aesthetic and irreversible decisions retain named authority." ]
        |> List.map (fun (id, required, rationale) ->
            { Tool = tool id; Required = required; Rationale = rationale })
        |> List.distinctBy (fun recommendation -> recommendation.Tool.Id)

    type GitObservation =
        { Available : bool
          Branch : string
          Commit : string
          CommitTitle : string
          CommittedAt : string
          Remote : string
          DirtyFiles : int
          Ahead : int
          Behind : int }

    type WorkItemObservation =
        { Id : string
          Title : string
          Status : string
          Objective : string
          RequiredGates : string list }

    type RunObservation =
        { Id : string
          Status : string
          StartedAt : string
          FinishedAt : string option
          HasSummary : bool }

    /// Sanitized Studio projection of a durable Autopilot run. Scope paths,
    /// prompts and raw agent output deliberately remain in the local run state.
    type AgenticRunObservation =
        { Id : string
          MissionId : string
          Objective : string
          Status : string
          ActiveSliceId : string
          ActiveSliceTitle : string
          LifecycleStage : string
          Phase : string
          NextAction : string
          CurrentRole : string option
          Provider : string option
          Model : string option
          Harness : string option
          Evaluation : Autopilot.Evaluation
          BlockReasons : string list
          UpdatedAt : DateTimeOffset }

    type WorkspaceObservation =
        { Id : string
          Name : string
          Git : GitObservation
          WorkItems : WorkItemObservation list
          Runs : RunObservation list
          AgenticRuns : AgenticRunObservation list
          SpotNodes : int
          Sources : string list
          ObservedAt : DateTimeOffset }

    type WorkspaceState =
        | Ready
        | Attention
        | Blocked
        | Unknown

    type WorkItemCounts =
        { Draft : int
          Ready : int
          Running : int
          Review : int
          Accepted : int
          Blocked : int
          Other : int }

    type RunCounts =
        { Total : int
          Running : int
          Succeeded : int
          Failed : int
          WithSummary : int }

    type AgenticRunCounts =
        { Total : int
          Running : int
          Completed : int
          Blocked : int
          FullSolves : int
          PrematureStops : int
          HumanInterventions : int }

    type WorkspaceSnapshot =
        { Id : string
          Name : string
          State : WorkspaceState
          StateReasons : string list
          Git : GitObservation
          WorkItems : WorkItemCounts
          ActiveMission : WorkItemObservation option
          LatestRun : RunObservation option
          Runs : RunCounts
          ActiveAgenticRun : AgenticRunObservation option
          AgenticRuns : AgenticRunCounts
          SpotNodes : int
          Sources : string list
          ObservedAt : DateTimeOffset }

    let private status (value: string) =
        if String.IsNullOrWhiteSpace value then "unknown"
        else value.Trim().ToLowerInvariant()

    let private countWorkItems (items: WorkItemObservation list) =
        let count expected = items |> List.filter (fun item -> status item.Status = expected) |> List.length
        let known = [ "draft"; "ready"; "running"; "review"; "accepted"; "done"; "blocked" ]
        { Draft = count "draft"
          Ready = count "ready"
          Running = count "running"
          Review = count "review"
          Accepted = count "accepted" + count "done"
          Blocked = count "blocked"
          Other = items |> List.filter (fun item -> not (List.contains (status item.Status) known)) |> List.length }

    let private countRuns (runs: RunObservation list) =
        let count states =
            runs |> List.filter (fun run -> List.contains (status run.Status) states) |> List.length
        { Total = runs.Length
          Running = count [ "running"; "started"; "in_progress" ]
          Succeeded = count [ "succeeded"; "success"; "accepted"; "completed" ]
          Failed = count [ "failed"; "rejected"; "cancelled"; "aborted" ]
          WithSummary = runs |> List.filter (fun run -> run.HasSummary) |> List.length }

    let private countAgenticRuns (runs: AgenticRunObservation list) =
        let count expected = runs |> List.filter (fun run -> status run.Status = expected) |> List.length
        { Total = runs.Length
          Running = count "running"
          Completed = count "completed"
          Blocked = count "blocked"
          FullSolves = runs |> List.filter (fun run -> run.Evaluation.FullSolve) |> List.length
          PrematureStops = runs |> List.sumBy (fun run -> run.Evaluation.PrematureStops)
          HumanInterventions = runs |> List.sumBy (fun run -> run.Evaluation.HumanInterventions) }

    let projectAgenticRun (run: Autopilot.RunState) : AgenticRunObservation =
        let slice = run.Plan.Slices.[run.ActiveSliceIndex]
        let execution = run.SliceExecutions.[run.ActiveSliceIndex]
        let action = Autopilot.nextAction run
        let nextAction, worker =
            match action with
            | Autopilot.DispatchAgent dispatch -> "DispatchAgent", Some dispatch.Worker
            | Autopilot.ExecuteGate _ -> "ExecuteGate", None
            | Autopilot.CreateCheckpoint _ -> "CreateCheckpoint", None
            | Autopilot.DecideSliceLease _ -> "DecideSliceLease", None
            | Autopilot.EvaluateCommittedBytesPortability _ -> "EvaluateCommittedBytesPortability", None
            | Autopilot.MissionComplete _ -> "MissionComplete", None
            | Autopilot.Escalate _ -> "Escalate", None
        { Id = run.RunId
          MissionId = run.Plan.MissionId
          Objective = run.Plan.Objective
          Status = sprintf "%A" run.Status
          ActiveSliceId = slice.Id
          ActiveSliceTitle = slice.Title
          LifecycleStage = sprintf "%A" slice.Stage
          Phase = sprintf "%A" execution.Phase
          NextAction = nextAction
          CurrentRole = worker |> Option.map (fun item -> sprintf "%A" item.Role)
          Provider = worker |> Option.map (fun item -> item.Provider)
          Model = worker |> Option.map (fun item -> item.Model)
          Harness = worker |> Option.map (fun item -> item.Harness)
          Evaluation = Autopilot.evaluate run
          BlockReasons = run.BlockReasons
          UpdatedAt = run.UpdatedAtUtc }

    let private activeMission (items: WorkItemObservation list) =
        let rank (item: WorkItemObservation) =
            match status item.Status with
            | "running" -> 0
            | "review" -> 1
            | "ready" -> 2
            | "blocked" -> 3
            | "draft" -> 4
            | _ -> 5
        items |> List.sortBy (fun item -> rank item, item.Id) |> List.tryHead

    let projectWorkspace (observation: WorkspaceObservation) : WorkspaceSnapshot =
        let workItems = countWorkItems observation.WorkItems
        let runs = countRuns observation.Runs
        let agenticRuns = countAgenticRuns observation.AgenticRuns
        let mission = activeMission observation.WorkItems
        let reasons =
            [ if not observation.Git.Available then "Git state is unavailable."
              if observation.Git.DirtyFiles > 0 then
                  sprintf "%d uncommitted path(s) are present." observation.Git.DirtyFiles
              if observation.Git.Behind > 0 then
                  sprintf "Workspace is %d commit(s) behind its upstream." observation.Git.Behind
              if workItems.Blocked > 0 then
                  sprintf "%d work item(s) are blocked." workItems.Blocked
              if runs.Failed > 0 && runs.Succeeded = 0 then
                  "No successful evidence run is available."
              if agenticRuns.Blocked > 0 then
                  sprintf "%d agentic run(s) are blocked." agenticRuns.Blocked ]
        let state =
            if workItems.Blocked > 0 || agenticRuns.Blocked > 0 then Blocked
            elif not observation.Git.Available then Unknown
            elif not reasons.IsEmpty then Attention
            else Ready
        { Id = observation.Id
          Name = observation.Name
          State = state
          StateReasons = reasons
          Git = observation.Git
          WorkItems = workItems
          ActiveMission = mission
          LatestRun = observation.Runs |> List.sortByDescending (fun run -> run.StartedAt, run.Id) |> List.tryHead
          Runs = runs
          ActiveAgenticRun =
            observation.AgenticRuns
            |> List.sortBy (fun run -> (if status run.Status = "running" then 0 else 1), -run.UpdatedAt.UtcTicks)
            |> List.tryHead
          AgenticRuns = agenticRuns
          SpotNodes = observation.SpotNodes
          Sources = observation.Sources |> List.distinct |> List.sort
          ObservedAt = observation.ObservedAt }
