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
* Der historische Registry-Stand wurde durch CS-003 ersetzt; er ist kein
  aktueller Mess- oder Vergleichsstand.
* Beobachtung: Abschnitt 0 ist als unveränderlicher Git-Baum veröffentlicht;
  Collector, Producer-Kompatibilität und erster prospektiver Echtlauf werden
  dadurch nicht als vorhanden behauptet.
* Offene Gate-Grenze: CDD `PublicObservationSnapshotV1` bleibt ein
  Publikations-Gate-Draft, bis ein sanitisiertes operatives Fixture exakt
  parst und roundtrippt. Alle nicht gemessenen Betriebswerte bleiben
  `unknown`.

## CS-003 — Öffentliche Quellenregistry T-053 korrigiert (2026-09-03)

* Status: `retrospective-derived` Registry-Korrektur; kein beobachteter
  Autopilotlauf, keine prospektive Behauptung und kein Forschungsergebnis.
* Gebundene öffentliche Quelle: Riftward-Commit
  [`d7d5f949758a3a38ca4238ceadfbbd83965eb71d`](https://github.com/Koschnag/ai-fantasy-rts-rpg/tree/d7d5f949758a3a38ca4238ceadfbbd83965eb71d),
  Quellbaum `3ce6338f6524b9349af716755c91d01d77cd3b93`, Protokoll
  `riftward-research-observability` 2.0.1, Bundle-SHA-256
  `58b93d5a7ce8b0c1b182030d36eab9f156ff1aa8f2c2d246be54bcd53f3bf1de`.
* Aussagegrenze: Commit und Tree identifizieren ausschließlich den
  veröffentlichten Protokollquellstand. Interne Runtime-Commit-/Tree-IDs und
  alle Laufmetriken werden nicht exportiert und bleiben `unknown`.
* Nächstes Gate: Ein extern bereitgestelltes, sanitisiertes Riftward-Fixture
  muss in CDD exakt validieren und roundtrippen. Bis dahin bleibt Raw-Export
  publikationsgesperrt.
