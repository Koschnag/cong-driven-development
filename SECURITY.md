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

Relevanter Scope:

- unsichere Deserialisierung und Path Traversal über `.spot/`-Knoten;
- CDD-Web-/MCP-Adapter und der Umgang mit Provider-Credentials;
- Archive-, Link-, XML- und Ressourcenangriffe im CourseForge-Import;
- unbeabsichtigte Verarbeitung oder Veröffentlichung personenbezogener Kursdaten;
- Umgehung von Evidence-, Capability- oder Promotion-Gates.

Bitte keine echten Moodle-Archive oder privaten Logs an eine Meldung hängen.
Eine minimale synthetische Reproduktion ist bevorzugt; falls vertrauliche
Evidenz zwingend nötig ist, stimmen wir einen geschützten Übertragungsweg ab.
