# Review-Hub

> Das Repository liefert Evidenz und offene Fragen. Die Forschungsentscheidung bleibt
> beim Review – nicht beim Generator.

## Schnellster Einstieg

1. **[Research Studio](docs/research/)**: briefing-orientierte Übersicht über Claims,
   Quellen, Risiken, Prämissen, Grenzen, Medien und Teilprojekte.
2. **[Research Track](research/README.md)**: Forschungsfragen, Methodik, Claims,
   Benchmark-Protokolle und Publikationsplan.
3. **[Paper](docs/paper-terminierungs-orakel.pdf)** und
   **[Whitepaper](docs/whitepaper-konvergenz.md)**: Langfassungen des
   bisherigen Strangs zum Terminierungsorakel.
4. **[CDD Studio](docs/ide/)**: Drill-down in den versionierten SPOT-Snapshot.

## Was maschinell nachprüfbar ist

```bash
dotnet build -c Release
dotnet test tests/Cdd.Tests -c Release
dotnet run -c Release --project src/Cdd.Cli -- validate
dotnet run -c Release --project src/Cdd.Cli -- sync-tests
dotnet run -c Release --project src/Cdd.Cli -- sync-docs --check
bash scripts/check-public-data.sh
```

Aktuelle Knoten-, Test- und Konvergenzzahlen werden aus dem SPOT erzeugt und deshalb
nicht in diesem Dokument handgepflegt. CI-Status und reproduzierbare Snapshots stehen
unter [GitHub Actions](https://github.com/Koschnag/cong-driven-development/actions).

## Was bewusst offen bleibt

- Externe Validität über die vorhandenen Prototypen und synthetischen Szenarien hinaus.
- Unabhängigkeit und Güte der Orakel bei realen, korrelierten Fehlern.
- Kosten und Nutzen der SPOT-Pflege im Langzeitbetrieb.
- Normative Wahl der Invarianten und Verantwortung für Promotion.
- Peer Review, Replikationen und belastbare Vergleiche gegen starke Baselines.
- Wirkungsnachweis des CourseForge-Referenzprojekts mit datenschutzkonformen Studien.

## Feedback-Regel

Feedback darf einen prüfbaren Change Intent oder Issue-Entwurf erzeugen. Es darf weder
Code automatisch promoten noch Claims stillschweigend von `Proposed` zu `Verified`
heben. Bitte bei jedem Einwand angeben, welche Beobachtung ihn bestätigen oder
widerlegen würde.

Vor einer öffentlichen Veröffentlichung gilt zusätzlich die
[Publikationsrichtlinie](PUBLICATION_POLICY.md). Private Betriebs-, Identitäts-, Memory-
und Infrastrukturdaten bleiben außerhalb dieses Showcase-Repositories.
