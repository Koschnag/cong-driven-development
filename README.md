# cong-driven-development (CDD)

[![CI](https://github.com/Koschnag/cong-driven-development/actions/workflows/ci.yml/badge.svg)](https://github.com/Koschnag/cong-driven-development/actions/workflows/ci.yml)
[![Release](https://img.shields.io/github/v/release/Koschnag/cong-driven-development)](https://github.com/Koschnag/cong-driven-development/releases/latest)
[![Live-Demo](https://img.shields.io/badge/Demo-koschnag.github.io-4ea1ff)](https://koschnag.github.io/cong-driven-development/)
[![Open in Codespaces](https://img.shields.io/badge/Codespaces-im%20Browser-24292e?logo=github)](https://codespaces.new/Koschnag/cong-driven-development)
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
src/Cdd.Mcp/          — MCP-Server: SPOT als Werkzeugkasten für KI-Clients (C#-IO-Adapter)
tests/Cdd.Tests/      — Spec→Test-Generation, Round-Trip-Tests
```

## Stack

- **F#** für Domain (typsicher, ADTs, Discriminated Unions für SPOT)
- **C#** für IO-Adapter (LLM-Clients, Git, FS) wenn nötig
- **.NET 9**
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
dotnet run --project src/Cdd.Cli -- export-context --out kontext.md  # SPOT als LLM-Vorlage + Doku
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

### MCP-Server (KI-Integration)

Im Repo liegt eine `.mcp.json` — wer den Checkout mit Claude Code öffnet, bekommt
den CDD-Server automatisch angeboten (Zustimmung genügt). Manuell, z. B. für ein
anderes Arbeitsverzeichnis:

```bash
claude mcp add cdd -- dotnet run --project src/Cdd.Mcp -- --root .
```

Danach kann z. B. Claude Code den SPOT direkt bearbeiten: `spot_list`, `spot_get`,
`spot_upsert`, `spot_delete`, `spot_validate`, `spot_export_context`,
`spot_derive_tests`, `spot_sync_code` — jede Mutation antwortet mit dem
Validierungs-Stand inklusive Invarianten.

### Cockpit „Cong OS" (Web-GUI)

```bash
dotnet run --project src/Cdd.Web -- --root . --urls http://localhost:5179
```

Das Cockpit ist **chat-primär**: ein Gesprächsfaden treibt einen Agenten über den
SPOT, jede Sicht ist eine Projektion desselben Modells. → Volle Beschreibung in
**[docs/COCKPIT.md](docs/COCKPIT.md)**. Kurz:

- **Split-Mitte** — Architektur-Diagramm + Faden zugleich; `⌘0` gibt dem Faden Vollbild.
- **Konvergenz-Loop** — der „▶ Loop bis Konvergenz"-Knopf treibt die (experimentelle)
  `cdd-mapper`-Loop; das Gate ist als `markerAligned && testprojekte>0 && alleTestsGruen`
  **entworfen** — kein „Agent sagt fertig". Implementiert prüft `SetzeSpecAligned`
  Marker-Präsenz, Greenness via CI (→ [GEGENENTWURF.md](GEGENENTWURF.md)).
- **Formal-Sicht** — derselbe SPOT als „code behind" in Typentheorie / Prädikatenlogik /
  Kategorien (KaTeX), jede Linse mit ehrlichem Caveat.
- **@-Gedächtnis** — `cong.db`-Volltextsuche (FTS5), serverseitig nur `sensitive=0`.
- **Souveräne Engine-Kette** — Claude Code primär, Mistral-EU + lokales Ollama über einen
  echten agentischen Tool-Loop gegen die SPOT-Tools.
- **EA-Toolbox + Symbol-System** — UML/SysML-Glyphen, Konvergenz am Rand; Knoten/Relationen
  per Klick. Multidimensionale Sichten (Graph, UML-Ontologie, OLAP-Cube), Light/Dark.

Knotenarten: Spec, Test, Risk, Infra, Component, Prämisse, Entscheidung (ADR),
Knowledge-Quelle, Tool und **Begriff** — die ubiquitäre Sprache als Ontologie mit
IsA/PartOf/RelatesTo-Beziehungen.

## Status

<!-- spot:status -->
**66 Knoten im Selbstmodell** · 4 aktive Invarianten · 17/20 abgeleitete Tests automatisiert

### Kann es (Specs, gemessen Aligned)

- ✅ **Agent-Interface** — Prosa-Eingaben werden durch eine KI in validierte Modelländerungen übersetzt
- ✅ **Chat-primaere Cockpit-Shell** — Das Cockpit ist chat-primaer: eine Omnibar als einzige Tuer, eine Menueleiste, die Rail mit Flaechen, der Faden und eine Statuszeile.
- ✅ **Doku-Konvergenz** — Der README-Status wird aus dem Selbstmodell generiert — Doku-Drift ist ein CI-Fehler
- ✅ **Fehlerliste & Widerspruchs-Erkennung** — Inkonsistenzen, Widersprüche und Regelverstöße sind eine klickbare Liste wie in Visual Studio
- ✅ **Formale code-behind-Sicht** — Dasselbe SPOT-Modell ist als formale Notation (Typen/Logik/Kategorien, KaTeX) darstellbar.
- ✅ **Getypte Diagramm-Flaeche mit Toolbox** — Die Split-Mitte zeigt den getypten SPOT-Graphen als Cytoscape-Diagramm mit mehreren Sichten und der EA-Toolbox.
- ✅ **Governance by Invariance** — Regeln sind Modell-Knoten und werden bei jeder Validierung (lokal + CI) erzwungen
- ✅ **LLM-Kontextexport** — Der SPOT-Graph wird zur Vorlage, aus der ein Agent den Rest baut
- ✅ **MCP-Server** — Jeder MCP-Client (Claude Code, Claude Desktop, …) kann den SPOT direkt lesen, validieren und mutieren
- ✅ **Modell → Code (derive-code)** — Aus unabgedeckten Test-Knoten entstehen implementierbare Test-Skelette mit fertigem Mess-Marker
- ✅ **Modell-Validierung** — Der SPOT-Graph ist jederzeit strukturell konsistent
- ✅ **Round-Trip: Code → Modell** — Komponenten-Konvergenz wird aus den echten Projekt-Referenzen abgeleitet, nicht behauptet
- ✅ **Spec→Test-Ableitung** — Tests sind Derivat der Spezifikation, nicht handgeschrieben
- ✅ **Test-Konvergenz messen** — Abgeleitete Test-Knoten werden gegen echte automatisierte Tests gemessen statt behauptet

### In Arbeit / geplant (Pending)

- 🔜 **Gate-Selbsthärtung** — Das Konvergenz-Orakel wird auf das eigene Modell angewendet: ein Test-Knoten gilt nur als Aligned, wenn ein echter Test-Marker existiert, nicht durch bloße Behauptung

Prämissen, Entscheidungen (ADRs) und geltende Invarianten: [docs/decisions.md](docs/decisions.md)

*Diese Sektion wird aus dem SPOT-Selbstmodell generiert (`cdd sync-docs`) — Hand-Edits werden überschrieben.*
<!-- /spot:status -->

## Mitmachen / Entwicklung

Git-Strategie, CI/CD, Releases und Qualitäts-Gates sind in [docs/devops.md](docs/devops.md)
beschrieben.

## Lizenz

[MPL-2.0](LICENSE).
