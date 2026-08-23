# EIDOS engineering benchmark

**EvoSDLC-Bench v0 — OpsLab assurance fault injection** · fixed reference time `24.07.2026 12:00:00 +00:00`

| Case | Expected | EIDOS | Linear baseline | Fault |
|---|---|---|---|---|
| `clean` | ExpectPromotion | Promoted | Promoted | NoFault |
| `red-contract` | ExpectRejection | Rejected | Promoted | FailedGate |
| `red-unit` | ExpectRejection | Rejected | Rejected | FailedUnitGate |
| `stale` | ExpectRejection | Rejected | Promoted | StaleEvidence |
| `correlated` | ExpectRejection | Rejected | Promoted | CorrelatedValidator |
| `missing` | ExpectRejection | Rejected | Promoted | MissingEvidence |
| `artifact-binding` | ExpectRejection | Rejected | Promoted | ArtifactMismatch |
| `policy-binding` | ExpectRejection | Rejected | Promoted | PolicyMismatch |
| `tampered-pack` | ExpectRejection | Rejected | Promoted | TamperedPack |
| `budget` | ExpectRejection | Rejected | Promoted | BudgetExceeded |

- EIDOS: **10/10**, unsafe approvals **0**
- Linear baseline: **2/10**, unsafe approvals **8**

> Hand-authored construct test for the implemented assurance mechanisms. It demonstrates behavior and reproducibility, not external validity or general superiority.