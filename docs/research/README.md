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
Abnahme. Siehe [PROTOCOL.md](PROTOCOL.md), [CDD_MAPPING.md](CDD_MAPPING.md)
und [CASE_STUDY_LOG.md](CASE_STUDY_LOG.md).

Die Showcase-Seite bleibt read-only: `main` steht für geprüfte Wahrheit,
`live-wip` dokumentiert Kontinuität, aber keine Abnahme. Nicht exportierte oder
nicht gemessene Werte bleiben `unknown`.

## Gebundener Riftward-Protokollstand

CDD beschreibt die öffentliche, sanitizierte Analysegrenze. Der operative,
vorregistrierte Messvertrag liegt dagegen im Riftward-Produktrepository. Die
aktuelle, öffentliche Quellenbindung ist Riftward-Commit
[`d7d5f949758a3a38ca4238ceadfbbd83965eb71d`](https://github.com/Koschnag/ai-fantasy-rts-rpg/tree/d7d5f949758a3a38ca4238ceadfbbd83965eb71d),
Quellbaum `3ce6338f6524b9349af716755c91d01d77cd3b93`, Protokoll
`riftward-research-observability` 2.0.1 und Bundle-SHA-256
`58b93d5a7ce8b0c1b182030d36eab9f156ff1aa8f2c2d246be54bcd53f3bf1de`.
Diese Werte bezeichnen nur den veröffentlichten Protokollquellstand, keine
Runtime- oder Ergebnisdaten.

Direkte Quellen: [Protokoll](https://github.com/Koschnag/ai-fantasy-rts-rpg/blob/d7d5f949758a3a38ca4238ceadfbbd83965eb71d/docs/research/PROTOCOL.md),
[Datenwörterbuch](https://github.com/Koschnag/ai-fantasy-rts-rpg/blob/d7d5f949758a3a38ca4238ceadfbbd83965eb71d/docs/research/OBSERVABILITY_DATA_DICTIONARY.md),
[Metriken](https://github.com/Koschnag/ai-fantasy-rts-rpg/blob/d7d5f949758a3a38ca4238ceadfbbd83965eb71d/docs/research/METRICS.md),
[Reproduktion](https://github.com/Koschnag/ai-fantasy-rts-rpg/blob/d7d5f949758a3a38ca4238ceadfbbd83965eb71d/docs/research/REPRODUCIBILITY.md) und
[Privacy/Publikation](https://github.com/Koschnag/ai-fantasy-rts-rpg/blob/d7d5f949758a3a38ca4238ceadfbbd83965eb71d/docs/research/PRIVACY_AND_PUBLICATION.md).
Noch existiert daraus kein öffentliches `prospective-observed` Ergebnis. Die
CDD-v1-Producer-Kompatibilität bleibt bis zu einem sanitisierten, exakt
roundtrippenden Riftward-Fixture offen; bis dahin bleiben alle Laufmetriken
`unknown` und Raw-Export bleibt publikationsgesperrt.
