namespace CourseForge.Core

/// Generische, datensparsame Repräsentation eines importierten Kurses.
type CourseSection =
    { Id       : string
      Title    : string
      Position : int }

type Course =
    { Id        : string
      Title     : string
      ShortName : string
      Sections  : CourseSection list }

/// Grenzen für das Lesen eines bereits sicher extrahierten Moodle-Backup-Ordners.
type ImportLimits =
    { MaxFiles      : int
      MaxTotalBytes : int64 }

type ImportFinding =
    | SensitiveDataExcluded
    | NonMetadataFilesIgnored of count: int
    | SectionWithoutName of sectionId: string

type ImportError =
    | FolderNotFound
    | MoodleManifestMissing
    | FileLimitExceeded of actual: int * maximum: int
    | SizeLimitExceeded of actual: int64 * maximum: int64
    | LinkedFileRejected
    | InvalidMetadata of fileName: string * reason: string

type CourseImport =
    { Course            : Course
      SourceFingerprint : string
      Findings          : ImportFinding list }

type MissionPhase =
    | Explore
    | GuidedPractice
    | IndependentPractice
    | TransferCheck

/// Ein authoring-fähiger Spielplan. Der Kern erfindet keine fachliche Richtigkeit:
/// NeedsAuthoring bleibt true, bis ein fachliches Orakel die Mission ratifiziert.
type GameMission =
    { Id              : string
      Title           : string
      SourceSectionId : string
      Phase           : MissionPhase
      NeedsAuthoring  : bool }

type GamePlan =
    { CourseId          : string
      SourceFingerprint : string
      Missions          : GameMission list }

type FeedbackKind =
    | BugReport
    | FeatureRequest
    | PedagogyFeedback
    | ImportCompatibility
    | SecurityOrPrivacy

/// Öffentlicher Feedback-Input ohne Nutzerkonto, E-Mail, Telemetrie oder Rohanhänge.
type FeedbackSignal =
    { Id                   : string
      Kind                 : FeedbackKind
      Summary              : string
      Reproduction         : string option
      BuildVersion         : string
      ContainsPersonalData : bool }

type AssuranceObligation =
    | Reproduce
    | UnitTests
    | ImportSafety
    | AccessibilityReview
    | PedagogyReview
    | SecurityReview
    | HumanPromotion

/// Öffentliche, rein vorschlagende Zwischenrepräsentation. Erst der explizite
/// EIDOS-Adapter kompiliert sie in einen operativen Cdd.Core.Eidos.ChangeIntent.
type FeedbackChangeProposal =
    { SignalId    : string
      Kind        : FeedbackKind
      Problem     : string
      ProposalOnly: bool
      Obligations : AssuranceObligation list }

type TriageResult =
    | Candidate of FeedbackChangeProposal
    | RejectedSensitive
    | EscalatedSecurity

module Defaults =
    let importLimits =
        { MaxFiles = 10_000
          MaxTotalBytes = 512L * 1024L * 1024L }
