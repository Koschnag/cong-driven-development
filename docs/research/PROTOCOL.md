# Forschungsprotokoll: kontinuierliche agentische Softwareentwicklung

## Geltungsbereich

Riftward wird hier als dokumentierte Systemhypothese und longitudinale
Fallstudie beschrieben. Es gibt keinen Neuheits- oder Überlegenheitsanspruch.
Die Einheit der Beobachtung ist eine begrenzte Änderung vom menschlichen Ziel
über Implementierung und unabhängige Prüfung bis zu akzeptiert, abgelehnt oder
`unknown`.

```text
Menschliches Ziel/Risiko -> Planner -> Builder -> unabhängige Assurance
                                      -> Gates/Recovery -> Promotion -> main
                                                          \-> live-wip (Kontinuität, keine Abnahme)
```

* Der Planner zerlegt Ziel, Risiken und prüfbare Akzeptanzsignale.
* Der Builder erstellt einen isolierten Kandidaten und die zugehörigen Tests.
* Die unabhängige Assurance prüft Kandidat, Diff und Evidenz mit eigener Sicht.
* Gates begrenzen Scope, Daten und Promotion; Recovery macht unterbrochene
  Arbeit nachvollziehbar, ohne ungeprüfte Zustände als Wahrheit auszugeben.
* Ein Promoter veröffentlicht nur nach den vereinbarten Gates in `main`.

`main` ist die geprüfte Wahrheit. `live-wip` ist ein Kontinuitäts- und
Arbeitsstand; er ist weder automatisch produktiv noch akzeptiert. 24/7
Verfügbarkeit bedeutet nicht, dass ein Agent aktiv arbeitet, produktiv ist,
akzeptierte Arbeit erzeugt oder etwas veröffentlicht hat.

## Evidenzklassen

| Klasse | Bedeutung | Öffentliche Darstellung |
| --- | --- | --- |
| `retrospective-derived` | aus vorhandenen Receipts/Commits nachträglich abgeleitet | Methode, Quelle und Grenzen nennen |
| `prospective-observed` | vorab definierte Beobachtung mit künftigem Export | Messmethode und Zeitraum nennen |
| `synthetic-test-only` | nur mit künstlichen Fixtures reproduziert | nicht als Produktionsergebnis ausgeben |
| `unknown` | nicht gemessen, nicht exportiert oder nicht entscheidbar | `unknown`, nie erfundene Null oder Schätzung |

Git, Receipts und kuratierter Retrieval-Kontext können Verlauf bewahren; sie
ersetzen weder unabhängige Prüfung noch menschliche Entscheidung. Ziel,
Risikoakzeptanz und Veröffentlichung bleiben beim Menschen.

## Protokollregister und Autorität

Diese Datei ist die öffentliche CDD-Einordnung. Sie darf den operativen
Riftward-Messvertrag weder ersetzen noch still erweitern. Die öffentliche
Quelle der Registry ist ausschließlich der unveränderliche Riftward-Commit
[`d7d5f949758a3a38ca4238ceadfbbd83965eb71d`](https://github.com/Koschnag/ai-fantasy-rts-rpg/tree/d7d5f949758a3a38ca4238ceadfbbd83965eb71d).
Commit und Tree in dieser Tabelle identifizieren nur diesen veröffentlichten
Quellstand; sie sind keine Runtime- oder Exportdaten:

| Feld | Gebundener Wert |
| --- | --- |
| Protokoll-ID / Version | `riftward-research-observability` / `2.0.1` |
| Öffentlicher Quellbaum | `3ce6338f6524b9349af716755c91d01d77cd3b93` |
| Protokollbundle SHA-256 | `58b93d5a7ce8b0c1b182030d36eab9f156ff1aa8f2c2d246be54bcd53f3bf1de` |
| Operative Quelle | [PROTOCOL.md](https://github.com/Koschnag/ai-fantasy-rts-rpg/blob/d7d5f949758a3a38ca4238ceadfbbd83965eb71d/docs/research/PROTOCOL.md) |
| Datenvertrag | [OBSERVABILITY_DATA_DICTIONARY.md](https://github.com/Koschnag/ai-fantasy-rts-rpg/blob/d7d5f949758a3a38ca4238ceadfbbd83965eb71d/docs/research/OBSERVABILITY_DATA_DICTIONARY.md) |
| Auswertung | [METRICS.md](https://github.com/Koschnag/ai-fantasy-rts-rpg/blob/d7d5f949758a3a38ca4238ceadfbbd83965eb71d/docs/research/METRICS.md) |
| Reproduktion / Privacy | [REPRODUCIBILITY.md](https://github.com/Koschnag/ai-fantasy-rts-rpg/blob/d7d5f949758a3a38ca4238ceadfbbd83965eb71d/docs/research/REPRODUCIBILITY.md) / [PRIVACY_AND_PUBLICATION.md](https://github.com/Koschnag/ai-fantasy-rts-rpg/blob/d7d5f949758a3a38ca4238ceadfbbd83965eb71d/docs/research/PRIVACY_AND_PUBLICATION.md) |

Der CDD-Vertrag `riftward-observatory-v1` bleibt ein öffentlicher,
sanitisierter Analyse- und Publikations-Gate-Draft. Er gilt nicht als
Producer-Kompatibilitätsbeleg. Ein solches Gate schließt erst ein von der
operativen Seite erzeugtes, sanitisiertes Fixture mit exaktem Roundtrip. Zum
Zeitpunkt dieser Registry-Aktualisierung sind prospektive Ereignisse,
Autonomiedauer, menschliche Minuten, Tokens und Kosten weiterhin `unknown`.

## Sichtbarer nächster Gate

Das nächste Gate ist ein von Riftward bereitgestelltes, sanitisiertes Fixture,
das in CDD exakt validiert und roundtrippt. Bis dahin bleibt jeder Raw-Export
publikationsgesperrt. Runtime-Commit-/Tree-IDs, Rohlogs, Observability-Daten,
private Routen/Hosts, Credentials, Prompts, Sessions, Personenbezug und
unveröffentlichte Datensätze bleiben lokal. Es gibt keinen T-053-Export und
keine öffentliche `prospective-observed` Messung.

## Related work

Die Literatur belegt bekannte Bausteine, nicht die Riftward-Hypothese als
Ganzes:

1. [SWE-agent (2024)](https://arxiv.org/abs/2405.15793) untersucht Agent-Computer-Interfaces für Repository-Änderungen und Tests.
2. [OpenHands (2024)](https://arxiv.org/abs/2407.16741) beschreibt eine offene Plattform für allgemeine Softwareentwicklungsagenten und Sandbox-Ausführung.
3. [MetaGPT (2023)](https://arxiv.org/abs/2308.00352) nutzt SOPs und arbeitsteilige Rollen mit Zwischenartefakten.
4. [ChatDev (2023)](https://arxiv.org/abs/2307.07924) modelliert spezialisierte Rollen und Kommunikationsketten für Entwicklungsaufgaben.
5. [SWE-bench (2023/2024)](https://arxiv.org/abs/2310.06770) prüft reale Issue-zu-Patch-Aufgaben; es ist kein longitudinaler SDLC-Nachweis. Das [offizielle Repository](https://github.com/SWE-bench/SWE-bench) dokumentiert Harness und Datensatz.
6. [SWE-bench Verified (2024)](https://openai.com/index/introducing-swe-bench-verified/) zeigt, dass auch die Auswahl und menschliche Prüfung von Evaluationsaufgaben relevant ist.
7. [MemGPT (2023)](https://arxiv.org/abs/2310.08560) untersucht hierarchisches Gedächtnis über Sitzungen, nicht automatisch Provenienz oder Promotion.
8. [RepoCoder (2023)](https://arxiv.org/abs/2303.12570) verbindet iterative Repository-Retrieval- und Generationsschritte.
9. [Ralph loop](https://github.com/anthropics/claude-code/blob/main/plugins/ralph-wiggum/README.md) dokumentiert wiederholte frische Iterationen mit Zustand in Dateien/Git; eine Schleife ist keine unabhängige Abnahme.
10. [TheAgentCompany (2024/2025)](https://arxiv.org/abs/2412.14161) evaluiert digitale Arbeit in einer simulierten Softwarefirma; es ist ein kontrollierter Aufgabenbenchmark, keine über Wochen gewachsene Produktgeschichte.
11. [SWE-Milestone (2026)](https://arxiv.org/abs/2603.13428) ist der engste Vergleich für kontinuierliche Software-Evolution: abhängige Milestones werden in einer fortlaufenden Sequenz bearbeitet und Fehlerakkumulation wird messbar.
12. [SlopCodeBench (2026)](https://arxiv.org/abs/2603.24755) misst strukturelle Erosion und Redundanz über wiederholte Erweiterungen. Das motiviert Riftwards Architekturtrends zusätzlich zu grünen Tests.
13. [GameDevBench (2026)](https://arxiv.org/abs/2602.11103) prüft multimodale Agenten an abgegrenzten Game-Development-Aufgaben; visuelles Feedback hilft, ersetzt aber keine longitudinale Produktkohärenz.
14. [OpenGame (2026)](https://arxiv.org/abs/2604.18394) kombiniert einen spezialisierten Game-Coding-Stack mit Build-, visueller Usability- und Intent-Evaluation für erzeugte Webspiele.
15. [GameEngineBench (2026)](https://arxiv.org/abs/2607.03525) untersucht abgegrenzte C++-Änderungen in realen Unreal-Projekten und ist damit ein relevanter Gegenpol zu Riftwards eigenem Runtimepfad.
16. [SWE-EVO (2025)](https://arxiv.org/abs/2512.18470) untersucht langfristige Software-Evolution über aufeinander aufbauende Aufgaben; es ist ein Vergleich für Sequenz- und Wartbarkeitsfragen, kein Nachweis für Riftward.
17. [SWE-CI (2026)](https://arxiv.org/abs/2603.03823) betrachtet CI-gebundene Software-Evolution über historische Änderungsfolgen; es ist ein Vergleich für Evaluationsdesign, keine operative Riftward-Evidenz.
18. [GameXpert-Bench (2026)](https://arxiv.org/abs/2608.21833) evaluiert Game-Development-Aufgaben mit interaktiven Produktkriterien; es ist ein Benchmarkvergleich, keine longitudinale Fallstudie.

Riftward kombiniert diese bekannten Muster mit expliziten Zuständen für
geprüftes `main`, Kontinuitäts-`live-wip`, unabhängige Assurance, begrenzte
Recovery und menschliche Promotion. Das sind zu prüfende Designentscheidungen,
kein belegter Neuheits- oder Überlegenheitsanspruch.

Der vorsichtige mögliche Beitrag liegt deshalb nicht in „KI schreibt ein
Spiel“, sondern in einer naturalistischen, öffentlich nachvollziehbaren
Langzeit-Fallstudie: dieselbe wachsende Codebasis, reale Betriebsunterbrechungen,
Governance- und Recovery-Grenzen sowie prospektiv gemessene Kosten,
Interventionen und Defekte. Ob diese Kombination besser konvergiert als
einfachere Agentenschleifen, ist eine offene Hypothese und muss durch die
vorregistrierten Metriken und spätere isolierte Ablationen entschieden werden.
