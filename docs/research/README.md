# Research Studio

Das Research Studio ist eine **read-only Projektion** des öffentlichen, versionierten
SPOT-Snapshots in `docs/ide/_demo/spot.json`. Claims, Quellen, Risiken, Prämissen,
Entscheidungen und Konvergenzstatus werden beim Laden abgeleitet; sie werden nicht als
zweiter Forschungsstand in HTML dupliziert.

Kuratiert sind nur die Erklärung der Teilprojekte, das Systemdiagramm und die
Briefing-Narrative. Feedback wird nicht automatisch gespeichert oder ausgeführt: Das
Formular öffnet einen vorbefüllten GitHub-Issue, den die Nutzerin oder der Nutzer vor
dem Absenden prüft. Ein Issue besitzt keine Promotion-Autorität.

Medien unter `docs/media/` und das browsernative Deck `docs/research/briefing.html` sind
ebenfalls Projektionen. Bei Änderungen am Kernmodell sind Snapshot, Präsentation und
Video vor einem Release neu zu prüfen.

## Projektion aktualisieren

```bash
dotnet build -c Release -warnaserror
bash scripts/sync-public-spot.sh
bash scripts/sync-public-spot.sh --check
```

Das Skript erzeugt `spot.json`, den nach Konvergenz gruppierten `diff.json` und
das Markdown-Kontextpaket deterministisch aus `.spot/`. Die CI scheitert, wenn
die öffentliche Projektion hinter dem versionierten SPOT zurückbleibt.

## Riftward als öffentliche Fallstudie

Die begleitende Dokumentation beschreibt eine vorsichtige Systemhypothese und
longitudinale Fallstudie zu kontinuierlicher agentischer Softwareentwicklung.
Sie ist kein Beweis von Neuheit, allgemeiner Überlegenheit oder autonomer
Abnahme. Siehe [PROTOCOL.md](PROTOCOL.md), [CCD_MAPPING.md](CCD_MAPPING.md)
und [CASE_STUDY_LOG.md](CASE_STUDY_LOG.md).

Die Showcase-Seite bleibt read-only: `main` steht für geprüfte Wahrheit,
`live-wip` dokumentiert Kontinuität, aber keine Abnahme. Nicht exportierte oder
nicht gemessene Werte bleiben `unknown`.
