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
- **GPL-3 oder MPL-2** Lizenz (TBD)

Kein Python. Nie.

## Status

**Seed.** Solution kompiliert, sonst leer. Vision steht, Implementierung beginnt.

Inkrementelle Roadmap:
1. SPOT-Modell als F#-Discriminated-Union (Entities, Specs, Tests, Risks, Infra)
2. SPOT-Persistenz (SQLite oder JSON-pro-Entity)
3. CLI: `cdd validate`, `cdd diff`, `cdd derive-tests`
4. Erster Agent-Interface (LLM-agnostic)
5. Round-Trip: Code → Modell und Modell → Code
6. Multi-Agent-Choreographie

## Lizenz

TBD.

## Bezug zum Cong-Universum

- **Eigenständiges Repo** (nicht Teil von `cong-portfolio`)
- **Erlebnis-/Stack-Vorgänger:** `cong-portfolio/13_dc/model/dc-model.fsx` (F#-SoT mit Drift-Reconciler) ist ein Proof-of-Concept des SPOT-Patterns
- **Zukünftige Integration:** CDD könnte 13_dc und 14_platform als ersten Real-World-Konsumenten haben
