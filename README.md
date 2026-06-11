# cong-driven-development (CDD)

[![CI](https://github.com/Koschnag/cong-driven-development/actions/workflows/ci.yml/badge.svg)](https://github.com/Koschnag/cong-driven-development/actions/workflows/ci.yml)
[![Release](https://img.shields.io/github/v/release/Koschnag/cong-driven-development)](https://github.com/Koschnag/cong-driven-development/releases/latest)
[![Live-Demo](https://img.shields.io/badge/Demo-koschnag.github.io-4ea1ff)](https://koschnag.github.io/cong-driven-development/)
[![License: MPL-2.0](https://img.shields.io/badge/License-MPL--2.0-brightgreen.svg)](LICENSE)

> **Vision:** AI-natives Software-Entwicklungs-Framework, das Modell, Spezifikation,
> Test, Architektur, Infrastruktur und Wissensbasis in einem **Single Point of Truth**
> (SPOT) vereint. Mensch beschreibt was, AI-Agents konvergieren auf das wie.

## Was es sein soll

Eine IDE/Framework-Hybrid für **AI-native Softwareentwicklung**. Statt iterativ Code zu
tippen, beschreibst Du Intent, Constraints und Akzeptanz-Kriterien — AI-Agents liefern
Implementierung, Tests, Doku, Infrastruktur. Du monitorst, gibst Feedback, managst.

### Konzept-Vermischung (bewusst)

| Klassische Disziplin | Was CDD davon übernimmt |
|---|---|
| **Enterprise Architect / OMG UML** | Modellierungs-Layer (Klassen, Sequenzen, Komponenten) |
| **Visual Studio Class Designer** | Round-Trip zwischen Modell und Code |
| **Model-Driven Development** | Modell ist primär, Code ist Derivat |
| **Spec-Driven Development** | Maschinenlesbare Spezifikation als Vertrag |
| **Test-Driven Development** | Tests sind aus Spec abgeleitet, nicht handgeschrieben |
| **Mathematisch-philosophische Modellierung** | Typen + Axiome + Beweisbarkeit (F#/Lean-Pfad) |
| **AI Agents als Worker** | Implementations-, Test-, Doku-, Review-Agents kollaborativ |
| **RAG + Vector-DB + Knowledge-Base** | SPOT ist gleichzeitig Dokumentation + Embeddings + Code |
| **Business Analyst** | Domain-Modell als Sprache zwischen Fachseite und Technik |
| **DevOps / GitOps / Infrastruktur** | Infrastruktur ist Teil des SPOT, nicht separat |
| **Security** | Threat-Model, Risk-Tracking, MFA-Audit nativ im Modell |
| **Multidimensionale Darstellung** | UML 2D, Mermaid 2D, 3D-Graph-View, Time-Travel |

### Was CDD NICHT sein soll

- Kein weiteres Code-Generation-Tool, das einmal scaffolded und dann vergessen wird
- Kein UI-Builder
- Keine LLM-Wrapper-CLI
- Kein "Cursor-Klon"

CDD ist der **Layer über LLMs**, der das SPOT-Modell + Konvergenz-Protokoll + Agent-Choreographie
definiert.

## Architektur (Seed)

```
src/Cdd.Core/         — Domain: SPOT-Typen, Spec-Sprache, Convergence-Algebra
src/Cdd.Cli/          — `cdd` CLI: Modell-Validation, Agent-Trigger, SPOT-Sync
src/Cdd.Web/          — Cockpit: Web-GUI + REST-API über dem SPOT-Graphen
tests/Cdd.Tests/      — Spec→Test-Generation, Round-Trip-Tests
```

## Stack

- **F#** für Domain (typsicher, ADTs, Discriminated Unions für SPOT)
- **C#** für IO-Adapter (LLM-Clients, Git, FS) wenn nötig
- **.NET 10**
- **Lean 4** später für Beweise (wenn Theoreme entstehen)
- **MPL-2.0** Lizenz

Kein Python. Nie.

## Usage

```bash
dotnet build
dotnet run --project src/Cdd.Cli -- init           # SPOT-Store (.spot/) mit Seed-Knoten anlegen
dotnet run --project src/Cdd.Cli -- list           # Knoten + Konvergenz-Status
dotnet run --project src/Cdd.Cli -- validate       # Modell prüfen (Exit 1 bei Fehlern)
dotnet run --project src/Cdd.Cli -- derive-tests --write   # Tests aus Spec-Kriterien ableiten
dotnet run --project src/Cdd.Cli -- diff           # Drift-/Konvergenz-Report
```

Der SPOT-Graph liegt als ein JSON-File pro Knoten unter `.spot/` — git-freundlich,
diffbar, mergebar.

### Demo

**▶ Sofort im Browser testen:** https://koschnag.github.io/cong-driven-development/
— die Demo zeigt das **CDD-Selbstmodell**: dieses Repo modelliert sich selbst in
[`.spot/`](.spot/) (Ontologie, Prämissen, ADRs, Risiken, Specs), die CI validiert
es bei jedem PR. Änderungen bleiben im localStorage deines Browsers.

Weitere Wege (alle GitHub-nativ):
- **Codespaces:** Repo öffnen (devcontainer konfiguriert), `dotnet run --project src/Cdd.Web`
- **Container:** `docker run -p 8080:8080 -v $PWD/.spot-demo:/data ghcr.io/koschnag/cdd:latest`
- **Releases:** self-contained Binaries (CLI + Cockpit) für Linux/Windows/macOS

### Cockpit (Web-GUI)

```bash
dotnet run --project src/Cdd.Web -- --root . --urls http://localhost:5179
```

Knoten anlegen/editieren/löschen, **multidimensionale Sichten** auf denselben
SPOT: Abhängigkeits-Graph, **UML-Klassendiagramm der Ontologie**, Validierung,
Drift-Report — plus Test-Ableitung per Klick und Light/Dark-Theme
(Light im Visual-Studio-Look). Knotenarten: Spec, Test, Risk, Infra, Component,
Prämisse, Entscheidung (ADR), Knowledge-Quelle, Tool und **Begriff** —
die ubiquitäre Sprache des Projekts als Ontologie mit IsA/PartOf/RelatesTo-Beziehungen.

## Status

**v0.2.** Cockpit-Slice steht:

- ✅ SPOT-Modell als F#-Discriminated-Union (Spec, Test, Risk, Infra, Component,
  Premise, Decision/ADR, Knowledge, Tool, Term/Ontologie)
- ✅ Ubiquitäre Sprache: Begriffe mit Definition, Synonymen und UML-Beziehungen
  (Generalisierung, Komposition, Assoziation), als Klassendiagramm gerendert
- ✅ SPOT-Persistenz (JSON-pro-Entity unter `.spot/`, Id-Sanitization gegen Path-Traversal)
- ✅ CLI: `cdd init|list|validate|diff|derive-tests`
- ✅ Web-Cockpit: REST-API + GUI (Editor, Mermaid-Graph, Validierung, Drift)
- ✅ Validierung: Referenz-Integrität, Zyklenerkennung, Konvergenz-Hygiene
- ✅ Spec→Test-Ableitung (idempotent, ein Test pro Akzeptanzkriterium)

Roadmap als Nächstes:
1. `cdd export-context`: SPOT-Graph als LLM-Kontextpaket (die „Vorlage" für Agents)
2. Knowledge-Ingestion: PDFs/Links/Bücher als Knowledge-Knoten mit Takeaways
3. Erstes Agent-Interface (LLM-agnostic), Tool-Knoten als Capability-Registry
4. Round-Trip: Code → Modell und Modell → Code (echter `diff` gegen Code)
5. Multi-Agent-Choreographie

## Mitmachen / Entwicklung

Git-Strategie, CI/CD, Releases und Qualitäts-Gates sind in [docs/devops.md](docs/devops.md)
beschrieben.

## Lizenz

[MPL-2.0](LICENSE).
