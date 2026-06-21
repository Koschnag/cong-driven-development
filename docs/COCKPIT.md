# Cong OS — das Cockpit

> Eine souveräne, **chat-primäre** Kommandozentrale über einem getypten Modell.
> Eine Eingabe treibt einen Agenten über mehrere *Wahrheiten*; jedes Ergebnis ist eine
> Projektion desselben **Single Point of Truth** (SPOT). F#/C#/.NET, kein Python,
> MPL-2.0, mesh-only, nichts ruft nach Hause.

Cong OS ist die GUI von [CDD](../README.md): ein Browser-bedienbares Cockpit, das im
eigenen Rechenzentrum läuft (eine `dotnet`-Binary, eine URL) und denselben SPOT-Graphen
bedient, den die CLI und der MCP-Server schon kennen. Es ersetzt keine zweite Wahrheit
durch eine GUI — es macht den einen Graphen *sichtbar, navigierbar und treibbar*.

## Ein Axiom: der Faden ist die Wahrheit

Es gibt genau **einen Gesprächsfaden**. Alles andere — das Modell, der Plan, die
Entwicklung, die Infrastruktur, die Produktion, die Doku — ist eine **Projektion**, die
*neben* den Faden gerufen wird, nie eine konkurrierende Top-Level-Fläche.

Drei feste Regionen, jedes Mal gleich (Asperger-freundlich: vorhersagbare Struktur;
ADHD-freundlich: das Arbeitsgedächtnis liegt sichtbar außerhalb des Kopfes):

- **Schiene** (links) — externalisiertes Arbeitsgedächtnis: die *eine* nächste Aktion
  (JETZT) + Pins. Plus die EA-Toolbox am Diagramm.
- **Mitte** — eine **Split-Fläche**: das Architektur-Diagramm (links) und der Faden
  (rechts), beide sichtbar. Der Faden kann per `⌘0` das Diagramm einklappen und den
  Schirm füllen (Hyperfokus) — der Split ist Default, nicht Zwang.
- **Bühne** (rechts) — die *eine* herbeigerufene Projektion (Inspector, Modell-Cube,
  Drift, Doku, @-Gedächtnis …). Standard: zu. `Esc` schließt.

Eine **Omnibox** (`⌘K`) ist die einzige Tür. Deterministisches Routing, keine
LLM-Klassifikation: das erste Zeichen entscheidet — `@` = Gedächtnis-Suche,
`pin ` = anheften, bekannte Knoten-Id = navigieren, sonst = Auftrag an den Faden.

## Die vier Wahrheiten

Cong OS ist als Personal-OS über **vier Wahrheiten** gedacht. Ehrlich getrennt nach
*gebaut* und *Fahrplan* (kein Marketing über die Realität):

| # | Wahrheit | Quelle | Status |
|---|----------|--------|--------|
| 1 | **Struktur** | SPOT (`.spot/`, getypter F#-DU-Graph) | **gebaut** — gelesen/geschrieben/validiert/getrieben |
| 2 | **Inhalt** | `cong.db` Volltext (FTS5) | **gebaut** — `@`-Route, nur `sensitive=0` (s. u.) |
| 3 | **Infra-Soll** | `dc-model` | Fahrplan |
| 4 | **Infra-Ist** | Live-DC (Komodo via MCP) | Fahrplan (heute Adopt-Stub) |

### Wahrheit #2: @-Gedächtnis — Souveränität by construction

`@begriff` durchsucht das persönliche Data-Warehouse (`cong.db`, 84k Nachrichten) per
FTS5 und rendert Treffer-Karten. Die **klinischen/sensiblen Daten verlassen die Maschine
nie**: das Cockpit liest eine *sanitisierte* `cong-memory.db`, die ausschließlich
`sensitive=0`-Zeilen enthält — die `sensitive`-Spalte existiert dort nicht einmal.
Defense-in-depth: zeigt `CDD_MEMORY_DB` doch je auf eine DB mit `sensitive`-Spalte, filtert
die Abfrage hart `m.sensitive=0 AND c.sensitive=0`. Das ist nicht verhandelbar.

## Der Konvergenz-Loop — der Kern

> Der Loop terminiert auf **Konvergenz-gegen-Spec**, mechanisch geprüft —
> nicht auf „der Agent sagt fertig".

„Man promptet nicht mehr, man baut den Loop, der nachpromptet, bis ein Ziel erreicht
ist." Die ganze Aussage steckt im **Abbruchkriterium**. Cong OS treibt dafür eine
Konvergenz-Loop (`cdd-mapper`, experimentell, sichtbar als Faden-Turn). Das Gate ist als
Konjunktion **entworfen** — Marker-Abdeckung *und* ein echter grüner Testlauf:

```
GateBestanden  =  markerAligned  &&  testprojekte > 0  &&  alleTestsGruen   (Cockpit-Design)
```

`Exit 0` reicht dafür nicht: ein Lauf gilt nur grün, wenn die Ausgabe bestandene Tests
ausweist und weder „Failed" noch „No test" enthält. **Implementiert** ist heute das Orakel
`SetzeSpecAligned` (`Cdd.Core.Sync`): es setzt einen Test-Knoten `Aligned` bei Marker-Präsenz
im Testcode — nur `Pending → Aligned`, nie von Hand, der Ausführer darf den Status nicht
anfassen. Dass diese Marker zu grünen Tests gehören, sichert die grüne CI-Suite plus der
reflexive Selbst-Test; den `alleTestsGruen`-Teil im Cockpit selbst zu erzwingen, ist Roadmap.
Der Prompt verbietet ausdrücklich, Spec oder Tests zu ändern, „nur um das Gate zu täuschen".

→ Vertiefung: [GEGENENTWURF.md](../GEGENENTWURF.md).

### Beweis auf hartem Terrain (Capstone)

Das Verfahren wurde end-to-end an [`runenruf`](https://github.com/Koschnag/runenruf)
gezeigt — einer RTS-RPG-Engine in F#/C# (kein To-do-CRUD). Ein Feature wurde **durch
das Tool** entwickelt, nicht von Hand:

1. **Modell** — eine Pending-Spec `spec-kampfkraft` (Kampfkraft = Leben + Schaden·5) wurde
   in den Runenruf-SPOT gelegt; das Cockpit lief dabei als IDE *auf* dem Runenruf-Repo
   (`--root runenruf`).
2. **Loop** — `cdd-mapper --go` trieb den Ausführer (`claude -p`), der `Voelker.kampfkraft`
   + `Voelker.istStaerker` + zwei Tests implementierte.
3. **Gate** — gemessen, nicht behauptet: `dotnet test` **29/29 grün** (unabhängig
   nachgemessen). Bemerkenswert: der Ausführer *durfte sich nicht selbst fertig erklären* —
   er weigerte sich ausdrücklich, den Convergence-Status von Hand zu setzen. Die 29/29 wurden
   unabhängig (CI) gemessen; das Orakel promotete die Spec auf `Aligned`, weil die zugehörigen
   Test-Marker im Code vorlagen — die Messung selbst leistet die CI, nicht das Orakel.
4. **Ergebnis** — `spec-kampfkraft` + beide Test-Knoten `Aligned`, `cdd validate` ohne
   Befund, das konvergierte Feature im Cockpit mit seiner Formal-Notation
   `⟦spec-kampfkraft⟧ := C₁ × C₂` sichtbar. (runenruf `cdd-mapper/auto` @ `78cd3a6`.)

Konvergenz wird *gemessen*, nicht *erklärt* — auf einem Spiel-Engine-Spec genauso wie auf
einem Ticket.

## Formal-Sicht — das Modell als „code behind"

Der SPOT ist *wirklich* ein getypter F#-DU-Graph. Darum lässt er sich ehrlich in
formaler Notation rendern (KaTeX, lokal gevendort). Drei Linsen, jede mit ihrem
load-bearing Caveat — getrennt nach Beweis / Analogie / Motivation:

- **Typen** (Curry-Howard) — Spec = Typ, Test = Bewohner, `Aligned = Γ ⊢_𝒪 t : ⟦S⟧`.
  Das `𝒪`-Subskript ist ehrlich: Bewohnbarkeit entscheidet ein **Orakel** (`dotnet test`
  ∧ Marker), *kein* syntaktischer Typechecker. Die These „review = typechecker grün" als
  sichtbare Notation. λ-Kalkül ist als eigene Sicht **verworfen** (dekorativ) — nur
  Fußnote.
- **Logik** — die Governance-Invarianten als prädikatenlogische Sätze über der endlichen
  Struktur 𝔐; `cdd validate` ist `𝔐 ⊨ Φ`, eine Verletzung *ist* der falsifizierende
  Zeuge. Endliche Domäne ⇒ entscheidbar ⇒ Gödel/Tarski gelten hier **nicht** (und werden
  nicht angeführt).
- **Kategorien** — die **freie** Kategorie auf `DependsOn` und die Preorder auf `IsA`,
  *per Konstruktion* benannt. Niemals „der SPOT-Graph *ist* eine Kategorie".

Jeder geklickte Knoten zeigt seine Notation auch inline im Inspector.

## Symbol-System & Toolbox

Eine kohärente UML/SysML-Glyph-Grammatik statt Unicode-Sammelsurium: äußere **Form** =
ontologische Familie (Classifier/Constraint/Artifact/Derivation), innere **Marke** =
formaler Operator (`⊢ ⊨ λ ∈ ◇ …`), **Konvergenz** nur am Rand (Halo/dim/Doppelrand/
gestrichelt — auch in Graustufen und bei 16 px unterscheidbar). Inline-SVG, kein
Build-Step. Dieselben Glyphen in Diagramm, Toolbox und Schiene.

Die **EA-Toolbox** legt die 11 Knotenarten (in 4 Familien) und die 5 echt
kantentragenden Relationen an — One-Click erzeugt einen validen `Pending`-Stub
(deterministisch, kein LLM), Relationen werden „bewaffnet" und Quelle→Ziel geklickt.

## Souveräne Engine-Kette

Drei Engines hinter *einem* Vertrag (`IEngine`), frei wählbar:

1. **Claude Code** (primär) — headless `claude --print`, mit dem SPOT-MCP-Server verbunden.
2. **Mistral-EU** (Backup) und **Ollama/Qwen** (lokal, offline-souverän) — über einen
   echten **agentischen Tool-Loop**: die Engine ruft die In-Process-SPOT-Tools
   (`spot_list/get/validate/upsert/delete/derive_tests/export_context`), bekommt die
   Ergebnisse zurückgefüttert und loopt bis fertig. So *treiben* auch die Fallback-Engines
   den SPOT, statt nur über ihn zu chatten.

Jeder Engine-Run bekommt einen **Kontext-Slice** statt eines Full-Dumps: der axiomatische
Kern (Ontologie · Invarianten · Prämissen) + ein kompakter Index *immer*, volle Details
*nur* für die im Auftrag genannten Knoten. Token-Ökonomie ohne Kontextverlust.

## Self-Host

```bash
# Build
dotnet build -c Release

# Cockpit starten (eine URL, ein Modell-Root)
dotnet src/Cdd.Web/bin/Release/net9.0/Cdd.Web.dll \
  --root /pfad/zum/repo --urls http://0.0.0.0:5179

# optional: @-Gedächtnis aktivieren
export CDD_MEMORY_DB=/pfad/zur/sanitisierten/cong-memory.db
```

Zugriff gestuft: Operator-Tier = volle Macht nur über das eigene Mesh; ein optionaler
Public-Edge nur read+chat hinter SSO+MFA. Kein SaaS, kein Telemetrie-Heimruf.

## Bedienung an einem Ort

| Taste | Wirkung |
|-------|---------|
| `⌘K` | Omnibox (die eine Tür) |
| `⌘.` | Das Nächste tun (JETZT) |
| `⌘0` | Faden-Vollbild (Diagramm ein/aus) |
| `⌘J` | Fehlerliste / Ausgabe (Bottom-Dock) |
| `1`–`8` | Bühnen-Flächen |
| `@…` | Gedächtnis-Suche · `pin …` anheften |
| `Esc` | Bühne / bewaffnete Relation schließen |
