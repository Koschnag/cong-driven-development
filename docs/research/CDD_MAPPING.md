# CDD-Mapping

| CDD-/EIDOS-Schritt | Riftward-Beobachtung | Evidenzgrenze |
| --- | --- | --- |
| Ziel und Signal | Menschliches Ziel, Risiko und Akzeptanzsignal | Ziel ist keine Erfolgsmessung |
| Missionsordnung | Planner erzeugt begrenzten Auftrag | Prospektive Ausführung muss separat belegt werden |
| Kandidat | Builder arbeitet isoliert; `live-wip` hält Kontinuität | WIP ist keine Abnahme |
| Assurance | unabhängiger Reviewer prüft Diff, Tests und Herkunft | Reviewer-Receipt erforderlich |
| Recovery | unterbrochene Arbeit wird gebunden fortgesetzt oder verworfen | kein stilles Überschreiben |
| Promotion | Promoter überführt nur gated Arbeit nach `main` | Mensch behält Freigabeautorität |
| Ergebnis | `accepted`, `rejected` oder `unknown` | keine Werte aus Run-Anzahl ableiten |

Diese Zuordnung ist eine öffentliche Erklärung, keine Behauptung, dass jede
Zeile bereits prospektiv gemessen wurde.

## Vertrags-Crosswalk

| Ebene | Vertrag | Rolle | Aktueller Nachweis |
| --- | --- | --- | --- |
| Riftward Produkt | `riftward-research-observability` 2.0.0 | autoritatives, vorregistriertes Event-, Metrik- und Reproduktionsprotokoll | Commit [`a8da858`](https://github.com/Koschnag/ai-fantasy-rts-rpg/tree/a8da858d9a25892a4671104c57f5edfe3c789a39), Bundle `a127ab37de6752a6defd8b9ebcb04c37cba0e3343863b5c10f53a9d109e20a65` |
| Private Ops-Grenze | künftiger sanitizierender Producer | Rohcollection und explizite Public-Ableitung; keine CDD-Promotion | Producer-Fixture `unknown` |
| CDD öffentlich | `riftward-observatory-v1` | Analysemodell und Publikationsgate für bereits sanitizierte Daten | Draft implementiert; Producer-Kompatibilität offen |
| CDD Research Studio | read-only SPOT-/Dokuprojektion | erklärt Claims, Quellen und Lücken | keine Runtime- oder Steuerungsautorität |

Die Felder der beiden Schemata dürfen erst nach einem expliziten
Transformvertrag als kompatibel gelten. Insbesondere ersetzt CDD-Missingness
mit `Observed`/`Estimated`/`Unavailable`/`NotApplicable` nicht Riftwards
literal `unknown`; eine Public-Projektion muss diese Semantik nachweislich und
verlustfrei abbilden. Bis zum Golden-Producer-Fixture werden keine Ereignisse
automatisch importiert oder veröffentlicht.
