namespace CourseForge.Core

/// Deterministische Projektion des Course-IR in einen Spielplan.
/// Inhaltliche Aufgaben bleiben authoring-pflichtig, bis ein fachliches Gate sie ratifiziert.
module GamePlanBuilder =

    let private phaseSlug = function
        | Explore -> "explore"
        | GuidedPractice -> "guided"
        | IndependentPractice -> "independent"
        | TransferCheck -> "transfer"

    let private phaseTitle = function
        | Explore -> "entdecken"
        | GuidedPractice -> "geführt anwenden"
        | IndependentPractice -> "selbstständig anwenden"
        | TransferCheck -> "übertragen"

    let create (imported: CourseImport) : GamePlan =
        let phases = [ Explore; GuidedPractice; IndependentPractice; TransferCheck ]
        let missions =
            [ for section in imported.Course.Sections do
                for phase in phases do
                    yield
                        { Id = sprintf "%s-%s" section.Id (phaseSlug phase)
                          Title = sprintf "%s: %s" section.Title (phaseTitle phase)
                          SourceSectionId = section.Id
                          Phase = phase
                          NeedsAuthoring = true } ]
        { CourseId = imported.Course.Id
          SourceFingerprint = imported.SourceFingerprint
          Missions = missions }
