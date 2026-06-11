# AGENTS.md — Anleitung für Coding-Agents

Kontext für AI-Agents (GitHub Copilot, Claude Code, etc.), die in diesem Repo arbeiten.

## Projekt

F#-Monorepo (.NET 9): `Cdd.Core` (Domain), `Cdd.Cli` (CLI `cdd`), `Cdd.Web`
(Cockpit: REST-API + statisches Frontend, kein Node-Toolchain), `Cdd.Tests` (xUnit).
Kernkonzept: SPOT-Graph (Single Point of Truth) als Discriminated Union,
persistiert als ein JSON-File pro Knoten unter `.spot/`.

## Build & Test

```bash
dotnet build -c Release -warnaserror   # muss warnungsfrei sein
dotnet test                            # alle Tests müssen grün sein
```

Smoke-Test wie im CI: `cdd init/list/validate/derive-tests --write/diff`
in einem Temp-Verzeichnis ausführen.

## Regeln

- **F# für Domain-Logik** (`Cdd.Core`): DUs, Records, Module — keine Klassenhierarchien.
- **Kein Python**, keine Python-Tooling-Abhängigkeiten.
- Neue Compile-Files in der `.fsproj` registrieren (F# ist reihenfolgesensitiv:
  Abhängigkeiten zuerst).
- JSON-Serialisierung ausschließlich über `Cdd.Core.Json` (FSharp.SystemTextJson).
- Jede Logik-Änderung braucht Tests in `tests/Cdd.Tests/Tests.fs`.
- Commits: Conventional Commits (`feat:`, `fix:`, `chore:`, …), PRs gegen `main`,
  Squash-Merge. Details: `docs/devops.md`.

## Was Agents NICHT tun sollen

- Keine neuen NuGet-Pakete ohne triftigen Grund (Minimal-Dependencies-Prinzip)
- Release-Tags (`v*`) nur nach explizitem Maintainer-Auftrag

## Das Selbstmodell (`.spot/` im Repo-Root)

CDD modelliert sich selbst: `.spot/` enthält das gepflegte Selbstmodell
(Ontologie, Prämissen, ADRs, Risiken, Specs des Projekts) — es ist Demo-Projekt
und Dogfood zugleich. Die CI validiert es bei jedem PR (`cdd validate`).
Regeln: Änderungen daran bewusst vornehmen, danach lokal `cdd validate` laufen
lassen; neue Features sollten zuerst als Spec-Knoten (Pending) dort erscheinen.
Regenerierbar via `dotnet fsi scripts/self-model.fsx` (überschreibt die
generierten Knoten).
