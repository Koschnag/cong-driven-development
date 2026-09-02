# Cong-Driven Development (CDD)

### Evidence-gated software evolution — vom Forschungsclaim bis zum überprüften Release

[![CI](https://github.com/Koschnag/cong-driven-development/actions/workflows/ci.yml/badge.svg)](https://github.com/Koschnag/cong-driven-development/actions/workflows/ci.yml)
[![Release](https://img.shields.io/github/v/release/Koschnag/cong-driven-development)](https://github.com/Koschnag/cong-driven-development/releases/latest)
[![Live-Demo](https://img.shields.io/badge/Demo-koschnag.github.io-4ea1ff)](https://koschnag.github.io/cong-driven-development/)
[![Open in Codespaces](https://img.shields.io/badge/Codespaces-im%20Browser-24292e?logo=github)](https://codespaces.new/Koschnag/cong-driven-development)
[![License: MPL-2.0](https://img.shields.io/badge/License-MPL--2.0-brightgreen.svg)](LICENSE)

> **Vision:** Wenn KI Implementierung beschleunigt, werden Intent, Spezifikation,
> Modellierung, Verifikation und Governance zum Engpass. CDD macht diese Arbeit in
> einem typisierten **Single Point of Truth (SPOT)** explizit und überprüfbar.

## Öffentliches Forschungsprogramm

CDD ist nicht nur ein Tool-Repository. Es veröffentlicht regelmäßig einen
reproduzierbaren Forschungsstand aus vier miteinander verbundenen Artefakten:

| Ergebnis | Hier zu finden | Aktueller Stand |
|---|---|---|
| **Paper & Research Track** | [`research/`](research/) · [Paper](docs/paper-terminierungs-orakel.pdf) | Preprint und offene Hypothesen |
| **Framework / Engine** | [`src/Cdd.Core/`](src/Cdd.Core/) · [`src/Cdd.Cli/`](src/Cdd.Cli/) | ausführbarer F#-Kernel |
| **Referenzprojekt CourseForge** | [`examples/CourseForge.Core/`](examples/CourseForge.Core/) | sicherer Metadata→Course-IR→Game-Plan Vertical Slice |
| **CDD Studio** | [öffentliche IDE-Demo](https://koschnag.github.io/cong-driven-development/ide/) · [`src/Cdd.Web/`](src/Cdd.Web/) | experimentelle Projektion desselben SPOT |
| **Open Control Plane** | [`docs/open-control-plane-landscape.md`](docs/open-control-plane-landscape.md) · `/workspace.html` | reale Workspace-Projektion und Tool-/Forschungslandkarte (Pre-Alpha) |
| **Full-Agentic SDLC** | [`docs/full-agentic-sdlc.md`](docs/full-agentic-sdlc.md) · `Cdd.Core.Autopilot` | persistenter Controller + CLI-Harness + Studio-Projektion |
| **Loop Engineering** | [`docs/loop-engineering.md`](docs/loop-engineering.md) · [`research/protocols/loop-engineering-v1.md`](research/protocols/loop-engineering-v1.md) | getestete Guard-Foundation + vorregistrierte Ralph-Baseline |

Der Claim-Ledger ist selbst Teil des SPOT: `Observed`, `Proposed`, `Verified`,
`Contested` und `Unknown` bleiben unterscheidbar. Eine technisch implementierte
Funktion ist dadurch nicht automatisch eine wissenschaftlich bestätigte Aussage.

```text
Signal → Claim / Change Intent → Candidate → unabhängige Assurance
       → Evidence Pack → menschliches Promotion Gate → Outcome
```

Das generische Referenzprojekt **CourseForge** untersucht zwei Schleifen:

- Moodle-Metadaten → datensparsamer Course IR → fachlich zu ratifizierender Lernspielplan;
- Bug/Feature-Feedback → `ProposalOnly` Change Intent → Tests/Evidenz → Freigabe.

Keine echte Kursdatei, kein Nutzerprofil und keine private Infrastruktur gehört in
dieses öffentliche Repo. Verbindlich: [Public Research & Data Policy](PUBLICATION_POLICY.md).

Der öffentliche Hintergrundimpuls ist
[„Software Engineering im KI-Zeitalter: Gegenthese zum Hype“](https://www.linkedin.com/pulse/software-engineering-im-ki-zeitalter-gegenthese-zum-hype-nguyen-imnof/):
*Embrace the Boring. Resist the Hype. Learn the Fundamentals. Go for Abstraction.*
Der Essay liefert Hypothesen — ihre Gültigkeit muss die Forschung erst zeigen.
Der Vergleich mit Anthropic, OpenAI, formalen Methoden, FOSS-Werkzeugen und
offenen SDLC-Standards steht in der
**[Open-Control-Plane-Landschaft](docs/open-control-plane-landscape.md)**.

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

## EIDOS v0.8 alpha: der erste ausführbare Evolutions-Kernel

CDD enthält jetzt einen kleinen, deterministischen **EIDOS-ZT2-Kernel**:
epistemisch typisierte Claims, einen Read-only System Twin, Doctrine und
Mission Orders, semantische Candidate-Kompilierung, unabhängige Evidence Packs,
fail-closed Promotion sowie einen vollständig replaybaren OpsLab-Lauf.

Eine Agentic-SDLC-Chain ist darin kein festes Betriebssystem, sondern ein
lageabhängig kompilierter Einsatzplan:

```text
Signal → Lagebild → Change Intent → Mission Order → Candidate
       → unabhängige Assurance → Evidence → Sandbox → Outcome
```

Der Scope endet absichtlich bei **ZT2**: kein Netzwerk, keine produktiven
Credentials und keine Produktionsautorität. EIDOS Studio ist als
mobile-first PWA auf Desktop und Smartphone nutzbar; die CLI-Releases laufen
auf Linux, Windows und macOS. Architektur, Forschungsstand und ehrliche Grenzen
stehen in **[docs/eidos.md](docs/eidos.md)** und
**[docs/eidos-research-report.md](docs/eidos-research-report.md)**.

Der ausführbare **CDD Autopilot** ergänzt EIDOS um eine persistente,
providerneutrale SDLC-Kette: begrenzte Work Slices, getrennte
Scout/Builder/Critic/Reviewer-Rollen, Resume/Fresh-Start-Recovery,
deterministische Gates, unabhängige Review und saubere Git-Checkpoints. Details
und ein vollständiger Plan stehen in
**[docs/full-agentic-sdlc.md](docs/full-agentic-sdlc.md)**.

Die langlebige Ralph-artige Iteration wird in
**[docs/loop-engineering.md](docs/loop-engineering.md)** konkretisiert: genau ein
Mutator, kleine Candidate-gebundene Schritte, read-only Advisory-Lanes,
unabhängige Review, getrennte Produkt-/Infrastruktur-/Protokollbudgets und
modellfreie Circuit Breaker. Die Effizienzwirkung ist ein öffentlicher
`Proposed`-Claim, kein bereits bestätigtes Forschungsergebnis.
Implementiert ist derzeit die reine, serialisierbare Loop-Guard-
Entscheidungsfoundation; ihre Einbindung in `RunState`, Scheduler, CLI und
Publisher sowie die vollständige lokale Durchsetzung aller Betriebsbudgets
sind offen.

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

Der CDD-Vertrauenskern bleibt bewusst **Python-frei**: Domain, Persistenz,
Promotion und das Monorepo verwenden F#/.NET. Polyglotte Werkzeuge – etwa ein
Blender-Python-Adapter oder ein Rust-Validator – dürfen außerhalb des Kerns
über offene, typisierte Ports angebunden werden. Damit bleibt die
Vertrauenskette klein, ohne die Toolchain an eine Sprache zu fesseln.

## Usage

```bash
dotnet build
dotnet run --project src/Cdd.Cli -- init           # SPOT-Store (.spot/) mit Seed-Knoten anlegen
dotnet run --project src/Cdd.Cli -- list           # Knoten + Konvergenz-Status
dotnet run --project src/Cdd.Cli -- validate       # Modell prüfen (Exit 1 bei Fehlern)
dotnet run --project src/Cdd.Cli -- derive-tests --write   # Tests aus Spec-Kriterien ableiten
dotnet run --project src/Cdd.Cli -- diff           # Drift-/Konvergenz-Report
dotnet run --project src/Cdd.Cli -- export-context --out kontext.md  # SPOT als LLM-Vorlage + Doku
dotnet run --project src/Cdd.Cli -- eidos run      # vollständiger synthetischer ZT2-Lauf
dotnet run --project src/Cdd.Cli -- eidos replay <run-ordner>
dotnet run --project src/Cdd.Cli -- eidos benchmark --out bench/eidos/results
dotnet run --project src/Cdd.Cli -- autopilot init examples/autopilot/full-sdlc-plan.json --workspace /pfad/zum/projekt
dotnet run --project src/Cdd.Cli -- autopilot next /pfad/zum/projekt/.ai/runtime/runs/<run-id>
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
- **EIDOS Studio:** Web-Cockpit starten und `/eidos.html` öffnen; als PWA auf
  Android, iOS und Desktop installierbar. Bug-/Feature-Entwürfe bleiben lokal,
  bis sie bewusst exportiert werden.

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

Die read-only Control-Plane-Projektion ist fail-closed und wird explizit für
beliebig viele lokale Workspaces aktiviert:

```bash
CDD_ENABLE_WORKSPACES=true dotnet run --project src/Cdd.Web -- \
  --root . --workspace ../referenzprojekt --urls http://localhost:5179
```

Danach zeigt `/workspace.html` Git-, `.spot`- und `.ai`-Beobachtungen, ohne
lokale Hostpfade auszugeben. Ein Running-Status stammt aus dem Run-Manifest und
ist ohne separaten Heartbeat ausdrücklich kein Prozess-Liveness-Nachweis.

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
- **@-Gedächtnis** — optionale sanitisierte Knowledge-Store-Projektion; default-deny und
  serverseitig auf nicht-sensitive Einträge begrenzt.
- **Souveräne Engine-Kette** — Claude Code primär, Mistral-EU + lokales Ollama über einen
  echten agentischen Tool-Loop gegen die SPOT-Tools.
- **EA-Toolbox + Symbol-System** — UML/SysML-Glyphen, Konvergenz am Rand; Knoten/Relationen
  per Klick. Multidimensionale Sichten (Graph, UML-Ontologie, OLAP-Cube), Light/Dark.

Knotenarten: Spec, Test, Risk, Infra, Component, Prämisse, Entscheidung (ADR),
Knowledge-Quelle, Tool und **Begriff** — die ubiquitäre Sprache als Ontologie mit
IsA/PartOf/RelatesTo-Beziehungen.

## Status

<!-- spot:status -->
**217 Knoten im Selbstmodell** · 4 aktive Invarianten · 67/73 abgeleitete Tests automatisiert

### Kann es (Specs, gemessen Aligned)

- ✅ **Agent-Interface** — Prosa-Eingaben werden durch eine KI in validierte Modelländerungen übersetzt
- ✅ **Chat-primaere Cockpit-Shell** — Das Cockpit ist chat-primaer: eine Omnibar als einzige Tuer, eine Menueleiste, die Rail mit Flaechen, der Faden und eine Statuszeile.
- ✅ **Course IR zu authoring-gegatem Spielplan** — Ein importierter Kurs erzeugt reproduzierbare Lernmissions-Skelette, ohne fachliche Richtigkeit vorzutaueschen.
- ✅ **Datensparsamer Moodle-Folder-Import** — Ein generischer Course-IR-Adapter liest nur benoetigte Kursmetadaten und schliesst sensible Moodle-Daten aus.
- ✅ **Doctrine und Mission Orders** — Jede Agentenausführung erhält einen typisierten Auftrag mit Rechten, Budget, Obligations, Reporting und Abbruchbedingungen
- ✅ **Doku-Konvergenz** — Der README-Status wird aus dem Selbstmodell generiert — Doku-Drift ist ein CI-Fehler
- ✅ **Epistemisch typisierte Claims** — Beobachtung, Aussage, Ableitung, Vorschlag, Ratifikation und Verifikation bleiben unterscheidbar und provenienzbehaftet
- ✅ **Evidence Packs und Promotion** — Promotion ist eine reproduzierbare Policy-Entscheidung über Evidence statt eine Selbstbestätigung des Generators
- ✅ **Fail-closed public runtime boundary** — Eine öffentliche CDD-Auslieferung darf ohne Betreiberfreigabe weder mutieren noch Memory- oder Runtime-Daten lesen
- ✅ **Feedback zu kontrolliertem EIDOS Change Intent** — Bug- und Feature-Signale duerfen nur pruefbare Vorschlaege erzeugen, die ein expliziter Adapter in risikotypisierte EIDOS Change Intents ohne Promotion-Autoritaet kompiliert.
- ✅ **Fehlerliste & Widerspruchs-Erkennung** — Inkonsistenzen, Widersprüche und Regelverstöße sind eine klickbare Liste wie in Visual Studio
- ✅ **Formale code-behind-Sicht** — Dasselbe SPOT-Modell ist als formale Notation (Typen/Logik/Kategorien, KaTeX) darstellbar.
- ✅ **Getypte Diagramm-Flaeche mit Toolbox** — Die Split-Mitte zeigt den getypten SPOT-Graphen als Cytoscape-Diagramm mit mehreren Sichten und der EA-Toolbox.
- ✅ **Governance by Invariance** — Regeln sind Modell-Knoten und werden bei jeder Validierung (lokal + CI) erzwungen
- ✅ **Kandidat- und ursachengebundene Loop Guards** — CDD stellt eine deterministisch getestete, serialisierbare Loop-Guard-Entscheidungsfoundation bereit; ihre Scheduler-, RunState-, CLI- und Publisher-Integration bleibt offen
- ✅ **LLM-Kontextexport** — Der SPOT-Graph wird zur Vorlage, aus der ein Agent den Rest baut
- ✅ **MCP-Server** — Jeder MCP-Client (Claude Code, Claude Desktop, …) kann den SPOT direkt lesen, validieren und mutieren
- ✅ **Modell → Code (derive-code)** — Aus unabgedeckten Test-Knoten entstehen implementierbare Test-Skelette mit fertigem Mess-Marker
- ✅ **Modell-Validierung** — Der SPOT-Graph ist jederzeit strukturell konsistent
- ✅ **Offene Workspace-Control-Plane-Projektion** — CDD Studio projiziert reale Projekte, Missionen, Runs und Evidenz über ein offenes read-only Adaptermodell, ohne Hostpfade oder Anbieter als Domänenwahrheit offenzulegen
- ✅ **Persistente Full-Agentic-SDLC-Kette** — CDD führt lange Software-Missionen providerneutral, resumierbar und evidenzgesteuert über kleine Work Slices statt über einen unkontrollierten Modell-Loop
- ✅ **Round-Trip: Code → Modell** — Komponenten-Konvergenz wird aus den echten Projekt-Referenzen abgeleitet, nicht behauptet
- ✅ **Sanitisierte longitudinale Riftward-Baseline** — Terminierte Autopilot-Runs werden zu sanitisierten, deterministischen Baselines je Mission und explizit versioniertem Evaluationsprotokoll aggregiert, ohne Sessions, Scopes, Prompts oder Artefakte preiszugeben
- ✅ **Sanitisiertes öffentliches Research-Claim-Ledger** — Operative EIDOS-Claims werden nur als schmale, quellengebundene und öffentlich geprüfte Forschungsprojektion im SPOT veröffentlicht.
- ✅ **Semantic Change Compiler** — Intent, Twin, Policies und Evidenz erzeugen vergleichbare Candidates statt einer unprüfbaren Einzelantwort
- ✅ **Spec→Test-Ableitung** — Tests sind Derivat der Spezifikation, nicht handgeschrieben
- ✅ **Test-Konvergenz messen** — Abgeleitete Test-Knoten werden gegen echte automatisierte Tests gemessen statt behauptet
- ✅ **Zero-Touch-Sandbox im OpsLab** — Ein klar definierter Change wird autonom bis zu einer isolierten, vollständig replaybaren Sandbox durchgeführt

### In Arbeit / geplant (Pending)

- 🔜 **Gate-Selbsthärtung** — Das Konvergenz-Orakel wird auf das eigene Modell angewendet: ein Test-Knoten gilt nur als Aligned, wenn ein echter Test-Marker existiert, nicht durch bloße Behauptung
- 🔜 **Reproduzierbare Research Snapshots** — Regelmaessige Forschungsstaende pinnen Code, Claims, Protokolle, Checksummen und Build-Evidenz auf denselben Commit.
- 🔜 **Repräsentative Evidence Fitness** — CDD verhindert Promotion durch grüne, aber am eigentlichen Claim vorbeimessende Proxy-Evidence
- 🔜 **Risikoadaptives Assurance-Portfolio** — CDD wählt komplementäre offene Nachweisverfahren nach Risiko und Systemform, statt einen Formalismus oder das erzeugende Modell zum universellen Orakel zu machen
- 🔜 **SPOT-projiziertes Research Studio** — Eine Review-Oberfläche zeigt Forschungsstand, Lücken, Grenzen, Medien und Teilprojekte ohne zweite Wahrheit oder automatische Promotion
- 🔜 **Semantische Foundation für Slice-Leases** — CDD stellt einen getesteten fail-closed Entscheidungskern und eine typisierte äußere Vertragsnaht für zeitlich begrenzte Slice-Ownership bereit; Scheduling, atomare Registry und reale Worktree-Isolation bleiben vor parallelem Dispatch erforderlich
- 🔜 **Typisierte Committed-Bytes-Portabilitäts-Evidence** — CDD klassifiziert Portabilitätsnachweise über die tatsächlich versionierten Candidate-Bytes an einer fail-closed Action/Observation-Naht; reale Adapterplanung und persistente Ausführung bleiben getrennte nächste Schritte
- 🔜 **Zulässige Vergleiche sanitierter Riftward-Baselines** — CDD gibt zwei deklarierte Konfigurationen nur dann als vergleichbar frei, wenn beide Seiten valide Aggregate derselben Mission und desselben Evaluationsprotokolls oberhalb des benannten Wiederholungsminimums sind und einen echten Kontrast bilden; Rangfolge, Kausalität oder Produktwirkung leitet der Kern daraus nicht ab

Prämissen, Entscheidungen (ADRs) und geltende Invarianten: [docs/decisions.md](docs/decisions.md)

*Diese Sektion wird aus dem SPOT-Selbstmodell generiert (`cdd sync-docs`) — Hand-Edits werden überschrieben.*
<!-- /spot:status -->

## Mitmachen / Entwicklung

Git-Strategie, CI/CD, Releases und Qualitäts-Gates sind in [docs/devops.md](docs/devops.md)
beschrieben.

## Lizenz

[MPL-2.0](LICENSE).
