namespace CourseForge.Core

open System
open Cdd.Core
open Cdd.Core.Spot

/// Explizite Antikorruptionsschicht zwischen öffentlichem CourseForge-Feedback
/// und dem operativen EIDOS-Kernel. Triage allein besitzt keine Ausführungs- oder
/// Promotion-Autorität.
module EidosAdapter =

    let private rating dimension level rationale : Eidos.RiskRating =
        { Dimension = dimension
          Level = level
          Rationale = rationale }

    let private hazardFor (proposal: FeedbackChangeProposal) =
        let specialized =
            match proposal.Kind with
            | BugReport ->
                [ rating Eidos.FunctionalCorrectness Medium
                    "A reported defect can change observable behavior." ]
            | FeatureRequest ->
                [ rating Eidos.BlastRadius Medium
                    "A feature can affect more than its originating interaction." ]
            | PedagogyFeedback ->
                [ rating Eidos.EpistemicUncertainty High
                    "Pedagogical effectiveness requires an independent domain and outcome review." ]
            | ImportCompatibility ->
                [ rating Eidos.ExternalContracts High
                    "An LMS import change touches an external and potentially untrusted format." ]
            | SecurityOrPrivacy ->
                [ rating Eidos.SecurityPrivacy Critical
                    "Security and privacy reports must never enter the autonomous candidate path." ]
        Eidos.hazard (
            rating Eidos.Irreversibility Low
                "The proposal has no promotion authority and must remain reversible."
            :: specialized)

    let toChangeIntent
        (requestedAt: DateTimeOffset)
        (proposal: FeedbackChangeProposal)
        : Eidos.ChangeIntent =
        if not proposal.ProposalOnly then
            invalidArg (nameof proposal) "CourseForge feedback must remain proposal-only."

        let constraints =
            [ "proposal-only"
              "no production authority"
              "human promotion required"
              yield!
                proposal.Obligations
                |> List.map (fun obligation -> sprintf "assurance:%A" obligation) ]

        Eidos.createIntent
            (sprintf "CourseForge feedback %s" proposal.SignalId)
            proposal.Problem
            [ "examples/CourseForge.Core"; "public-feedback" ]
            constraints
            [ "the reported need is reproduced or falsified"
              "all derived assurance obligations have independent evidence"
              "promotion remains a separate EIDOS policy decision" ]
            requestedAt
            (hazardFor proposal)
