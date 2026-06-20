# Prämissen & Entscheidungen

*Generiert aus dem SPOT-Selbstmodell (`cdd sync-docs`) — Hand-Edits werden überschrieben.*

## Prämissen (nicht verhandelbar)

### Cloud-first: nichts muss lokal laufen.
*Thin Clients als Terminals; GitHub (Pages, Codespaces, GHCR, Releases) trägt alles* · `premise-cloud-first`

### Kein Python — nie.
*Ein Stack (.NET/F#), keine Toolchain-Fragmentierung; Typsicherheit durchgängig* · `premise-kein-python`

### Typsicherheit vor Flexibilität.
*Illegale SPOT-Zustände sollen nicht repräsentierbar sein — das Typsystem ist das Schema* · `premise-typsicherheit`

## Entscheidungen (ADRs)

### F# für die Domain · `adr-001-fsharp`
- **Kontext:** Das SPOT-Modell braucht Summen-Typen, Pattern-Matching und Unveränderlichkeit
- **Entscheidung:** F# mit Discriminated Unions als Modellsprache; C# nur für IO-Adapter
- **Konsequenzen:** Kleinere Community, dafür beweisbar korrektere Modelle und Lean-4-Anschlussfähigkeit

### Ein JSON-File pro Knoten · `adr-002-json-store`
- **Kontext:** Der SPOT muss git-diffbar, mergebar und ohne Server nutzbar sein
- **Entscheidung:** Persistenz als .spot/<id>.json via FSharp.SystemTextJson
- **Konsequenzen:** Kein Query-Layer; bei Wachstum später SQLite/Index möglich, Format bleibt Austauschformat

### GitHub-native Infrastruktur · `adr-003-github-only`
- **Kontext:** Eigene Domains/Server erzeugen Pflegekosten und private Abhängigkeiten
- **Entscheidung:** Pages für die Demo, Actions für CI/CD, GHCR für Container, Releases für Binaries
- **Konsequenzen:** Demo-Modus braucht localStorage statt Backend; volle Version via Codespaces/Container

### Lizenz MPL-2.0 · `adr-004-mpl2`
- **Kontext:** Offenheit gewünscht, aber Datei-Copyleft statt viralem Projekt-Copyleft
- **Entscheidung:** MPL-2.0
- **Konsequenzen:** Kommerzielle Nutzung möglich, Änderungen an CDD-Dateien bleiben offen

## Geltende Invarianten (Governance)

- 🛡️ **Jeder Begriff der ubiquitären Sprache ist definiert** — jeder Begriff braucht eine Definition · `inv-begriffe-definiert`
- 🛡️ **Kritische Risiken brauchen eine Mitigation** — kritische Risiken brauchen eine Mitigation · `inv-kritische-risiken`
- 🛡️ **Jede Spec hat mindestens einen Test** — jede Spec braucht mindestens einen Test · `inv-specs-getestet`
- 🛡️ **Begriffe heißen term-*** — Ids der Art 'term' beginnen mit 'term-' · `inv-term-praefix`

