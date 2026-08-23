namespace CourseForge.Core

/// Kontrollierte Feedback-zu-Intent-Schleife. Sie erzeugt niemals direkt Code,
/// Commits oder Releases; öffentliches Feedback besitzt nur Proposal-Autorität.
module Evolution =

    let triage (signal: FeedbackSignal) : TriageResult =
        if signal.ContainsPersonalData then
            RejectedSensitive
        elif signal.Kind = SecurityOrPrivacy then
            EscalatedSecurity
        else
            let kindObligations =
                match signal.Kind with
                | BugReport -> [ Reproduce; UnitTests ]
                | FeatureRequest -> [ UnitTests; AccessibilityReview ]
                | PedagogyFeedback -> [ PedagogyReview; AccessibilityReview ]
                | ImportCompatibility -> [ ImportSafety; UnitTests ]
                | SecurityOrPrivacy -> [ SecurityReview ]
            Candidate
                { SignalId = signal.Id
                  Kind = signal.Kind
                  Problem = signal.Summary.Trim()
                  ProposalOnly = true
                  Obligations = kindObligations @ [ HumanPromotion ] }
