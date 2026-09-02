# Case-study log

## CS-000 — Protokollbaseline (2026-08-31)

* Status: `retrospective-derived` für die Methodik; aktuelle Produktions- und
  Erfolgszahlen: `unknown`.
* Beobachtungsobjekt: Planner → Builder → unabhängige Assurance → Gates und
  Recovery → menschlich autorisierte Promotion.
* Öffentliche Aussage: dokumentierte Systemhypothese und longitudinale
  Fallstudie, kein Neuheits- oder Überlegenheitsbeweis.
* Nicht aus dieser Baseline ableitbar: aktive Laufzeit, Produktivität,
  Akzeptanz, Verfügbarkeit, Erfolgsquote oder autonome Veröffentlichung.

Künftige Einträge verwenden vorab definierte Exporte und kennzeichnen jede
Beobachtung als `prospective-observed`, `synthetic-test-only` oder `unknown`.
Private Rohbeobachtungen werden nicht in dieses Log kopiert.

## CS-001 — Abgrenzung zum Stand der Forschung (2026-09-01)

* Status: `retrospective-derived` Literaturabgleich; keine Riftward-
  Produktionsmessung.
* Engste Vergleichsklassen: kontinuierliche Milestone-Sequenzen
  (SWE-Milestone), Qualitätsabbau bei iterativer Erweiterung (SlopCodeBench),
  simulierte digitale Softwarearbeit (TheAgentCompany) sowie abgegrenzte
  Game-Development- und Runtime-Aufgaben (GameDevBench, OpenGame,
  GameEngineBench).
* Verbleibende Riftward-Hypothese: Eine naturalistische, über längere Zeit
  weiterentwickelte Produktlinie mit öffentlicher WIP-/Promotion-Provenienz,
  unabhängiger Assurance und prospektiver Betriebsinstrumentierung kann andere
  Erkenntnisse liefern als kontrollierte Einzel- oder Benchmarksequenzen.
* Aussagegrenze: Das ist Forschungsdesign, kein Neuheitsbeweis. Ohne
  prospective-observed Exporte bleiben Autonomiedauer, Effizienz,
  menschlicher Aufwand und Qualitätsentwicklung `unknown`.

## CS-002 — Operatives Protokoll vor Messbeginn eingefroren (2026-09-02)

* Status: `retrospective-derived` Registry-Eintrag; kein beobachteter
  Autopilotlauf und kein Forschungsergebnis.
* Gebundene Quelle: Riftward-Commit
  [`a8da858d9a25892a4671104c57f5edfe3c789a39`](https://github.com/Koschnag/ai-fantasy-rts-rpg/tree/a8da858d9a25892a4671104c57f5edfe3c789a39),
  Protokoll `riftward-research-observability` 2.0.0, Bundle-SHA-256
  `a127ab37de6752a6defd8b9ebcb04c37cba0e3343863b5c10f53a9d109e20a65`.
* Beobachtung: Abschnitt 0 ist als unveränderlicher Git-Baum veröffentlicht;
  Collector, Producer-Kompatibilität und erster prospektiver Echtlauf werden
  dadurch nicht als vorhanden behauptet.
* Offene Gate-Grenze: CDD `PublicObservationSnapshotV1` bleibt ein
  Publikations-Gate-Draft, bis ein sanitisiertes operatives Fixture exakt
  parst und roundtrippt. Alle nicht gemessenen Betriebswerte bleiben
  `unknown`.
