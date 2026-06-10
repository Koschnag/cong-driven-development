# Security Policy

## Unterstützte Versionen

Nur das jeweils neueste Release erhält Security-Fixes.

## Schwachstelle melden

Bitte **kein öffentliches Issue** für Sicherheitslücken. Stattdessen:

1. **GitHub Private Vulnerability Reporting** (bevorzugt):
   *Security → Report a vulnerability* in diesem Repo
2. Antwort innerhalb von 7 Tagen; Fix-Ziel für bestätigte Lücken: 30 Tage

Bitte beilegen: betroffene Version/Commit, Reproduktion, Impact-Einschätzung.

## Scope

CDD ist eine lokale CLI ohne Netzwerk-Listener. Relevant sind v. a.:
unsichere Deserialisierung der `.spot/`-JSON-Dateien, Path-Traversal über
Entity-Ids, und künftig der Umgang mit LLM-API-Credentials.
