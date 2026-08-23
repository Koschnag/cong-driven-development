# EvoSDLC-Bench v0

Deterministischer Construct Test für den EIDOS-v0-Assurance-Kernel.

```bash
dotnet run -c Release --project src/Cdd.Cli -- \
  eidos benchmark --out bench/eidos/results
```

Die zehn Fälle prüfen eine saubere Promotion und neun Fail-closed-Pfade. Die
lineare Baseline ist absichtlich schwach und prüft nur ein Unit-Test-Ergebnis.
Der Benchmark zeigt daher Mechanism Coverage und Reproduzierbarkeit, nicht
externe Validität oder allgemeine Überlegenheit.

Versionierte Ergebnisse:

- `results/eidos-benchmark.json` — maschinenlesbar
- `results/eidos-benchmark.md` — lesbarer Report
