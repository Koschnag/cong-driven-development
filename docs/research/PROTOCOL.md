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

## Öffentlicher Export

Künftig können pro Arbeitseinheit redigierte Metadaten exportiert werden:
öffentliche Task-ID, Commit-/Tree-Referenz, Evidenzklasse, Gate-Ergebnis,
Zeitfenster, Ergebnisstatus und bekannte Einschränkungen. Rohlogs,
Observability-Daten, private Routen/Hosts, Credentials, Prompts, Sessions,
Personenbezug und unveröffentlichte Datensätze bleiben privat.

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
9. [Ralph loop](https://github.com/anthropics/claude-code/blob/main/plugins/ralph-wiggum/commands/ralph-loop.md) dokumentiert wiederholte frische Iterationen mit Zustand in Dateien/Git; eine Schleife ist keine unabhängige Abnahme.

Riftward kombiniert diese bekannten Muster mit expliziten Zuständen für
geprüftes `main`, Kontinuitäts-`live-wip`, unabhängige Assurance, begrenzte
Recovery und menschliche Promotion. Das sind zu prüfende Designentscheidungen,
kein belegter Neuheits- oder Überlegenheitsanspruch.
