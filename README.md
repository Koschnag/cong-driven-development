# cong-driven-development (CDD)

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

## Status

**v0.1.** Erster vertikaler Slice steht:

- ✅ SPOT-Modell als F#-Discriminated-Union (Spec, Test, Risk, Infra, Component)
- ✅ SPOT-Persistenz (JSON-pro-Entity unter `.spot/`)
- ✅ CLI: `cdd init|list|validate|diff|derive-tests`
- ✅ Validierung: Referenz-Integrität, Zyklenerkennung, Konvergenz-Hygiene
- ✅ Spec→Test-Ableitung (idempotent, ein Test pro Akzeptanzkriterium)

Roadmap als Nächstes:
1. Erstes Agent-Interface (LLM-agnostic)
2. Round-Trip: Code → Modell und Modell → Code (echter `diff` gegen Code statt Status-Spiegel)
3. Multi-Agent-Choreographie

## Lizenz

[MPL-2.0](LICENSE).

## Bezug zum Cong-Universum

- **Eigenständiges Repo** (nicht Teil von `cong-portfolio`)
- **Erlebnis-/Stack-Vorgänger:** `cong-portfolio/13_dc/model/dc-model.fsx` (F#-SoT mit Drift-Reconciler) ist ein Proof-of-Concept des SPOT-Patterns
- **Zukünftige Integration:** CDD könnte 13_dc und 14_platform als ersten Real-World-Konsumenten haben
