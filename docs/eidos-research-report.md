# EIDOS v0.8 alpha — Forschungs- und Ergebnisbericht

Stand: 24. Juli 2026

## Kurzfassung

Der Ansatz ist technisch sinnvoll, wenn er als **evidenzgesteuerte,
risikobegrenzte Change Compilation** verstanden wird — nicht als Behauptung
vollautonomer Softwareentwicklung. Der erste falsifizierbare Kernel ist jetzt
implementiert und endet bewusst bei `ZT2`: einer lokalen Sandbox ohne Netzwerk,
Credentials oder Produktionsautorität.

Die Literatur stützt fünf Entwurfsentscheidungen:

1. Natürlichsprachlicher Intent muss in prüfbare Spezifikation überführt werden;
   genau diese Formalisierung bleibt ein zentraler Engpass.
2. Ein akzeptierender Verifier reicht nicht aus, wenn die Spezifikation selbst
   falsch oder unvollständig ist.
3. Generator, Validator und Promotion brauchen getrennte Verantwortlichkeiten.
4. Agentische Entscheidungen benötigen explizite, über den Lebenszyklus
   verfolgbare Provenienz.
5. Lernende Evaluatoren brauchen feste Anker und unabhängige Außenprüfungen,
   damit sie nicht auf spielbare Metriken kollabieren.

## Forschungsfrage

Kann ein kleiner, deterministischer Trusted Kernel bei einem abgegrenzten
Software-Change:

- Ursprung, Unsicherheit und Widerspruch sichtbar erhalten,
- aus Risiko passende Assurance-Pflichten ableiten,
- mehrere Kandidaten semantisch vergleichen,
- ungeeignete Kandidaten vor Ausführung verwerfen,
- Promotion ausschließlich an frische, unabhängige und artefaktgebundene
  Evidenz koppeln,
- und den gesamten Lauf reproduzierbar replayen?

## Methode

Die Untersuchung ist ein **Design-Science-Prototyp mit Construct Test**:

- Domäne: synthetische Report-/Submission-Anwendung.
- Change: Schema v1 → v2 mit optionalem Feld `ownerTeam`.
- harte Constraint: alle gültigen v1-Reports bleiben gültig.
- sichere Alternative: optionales Feld.
- verworfene Alternative: neues Pflichtfeld.
- Ziel: lokale `ZT2`-Sandbox.
- Vergleich: absichtlich schwache lineare Feature-Baseline, die nur einen
  grünen Unit-Test betrachtet.
- Fault Injection: rotes Contract-/Unit-Gate, veraltete Evidenz,
  Generator-Validator-Korrelation, fehlender Recovery-Nachweis, falsche
  Artefakt-/Policy-Bindung, manipuliertes Pack und Budgetüberschreitung.

Der Benchmark benutzt eine feste Referenzzeit und deterministische Fixtures.
JSON und Markdown werden unter `bench/eidos/results/` versioniert.

## Implementiertes Ergebnis

### Epistemischer Twin

`Signal`, `Claim`, `Provenance` und `EpistemicStatus` bleiben getrennt.
Fehlende Provenienz wird `Unknown`; widersprüchliche aktive Claims werden
`Contested`. Rohsignal und ursprünglicher Status werden nicht überschrieben.

### Doctrine und Mission Order

Risiko ist ein Vektor, keine einzelne Zahl. Die Doctrine begrenzt Trust Zone,
Capabilities, Laufzeit, Kandidatenzahl und Artefaktgröße. Aus Risiko,
Unsicherheit und Zielzone entstehen risikoadaptive
`AssuranceObligation`s. Ein unbekannter Scope oder eine nicht erlaubte
Capability verhindert den Dispatch.

### Semantic Change Compiler

Der Compiler erzeugt content-addressed Kandidaten, sortiert deterministisch,
verwirft unsichere Pfade, Hash-Abweichungen, Budgetverletzungen und
inkompatible Alternativen. Verworfene Alternativen bleiben als begründete
Evidenz erhalten.

### Evidence und Promotion

Evidence Records sind an Candidate-Artefakt, Policy, Umgebung, Tool,
Validator und Zeit gebunden. Promotion schlägt fail-closed fehl bei:

- fehlender, doppelter, roter oder übersprungener Evidenz,
- veralteter oder zukünftiger Evidenz,
- Generator-Validator-Korrelation,
- falscher Artefakt-, Policy- oder Umgebungsbindung,
- manipuliertem Evidence-Pack,
- Überschreitung der Doctrine-Autonomie.

### OpsLab und Studio

Ein sauberer Lauf kompiliert genau einen zulässigen Candidate, verwirft die
breaking Alternative, erzeugt alle Obligations und materialisiert nur die
isolierte Sandbox. Baseline, Ledger und Evidence Pack sind replaybar.
EIDOS Studio stellt denselben Kernel als responsive PWA bereit und kann
lokale, portable Bug-/Feature-Issue-Entwürfe erzeugen.

## Reproduzierbares Messergebnis

| System | korrekt | unsichere Freigaben |
|---|---:|---:|
| EIDOS v0 Assurance Kernel | 10/10 | 0 |
| schwache lineare Feature-Baseline | 2/10 | 8 |

Das Ergebnis zeigt, dass die **implementierten Mechanismen in den dafür
konstruierten Fällen greifen**. Es beweist weder externe Validität noch eine
allgemeine Überlegenheit gegenüber realen Agentensystemen.

Reproduktion:

```bash
dotnet build -c Release -warnaserror
dotnet test -c Release --no-build
dotnet run -c Release --no-build --project src/Cdd.Cli -- eidos run --out /tmp/eidos
dotnet run -c Release --no-build --project src/Cdd.Cli -- eidos benchmark --out bench/eidos/results
```

## Forschungsbefund

### Was der Ansatz belastbar verbessert

- Er trennt Aussagen über die Welt von normativen Setzungen und maschinellen
  Ableitungen.
- Er macht die Promotion zu einer reproduzierbaren Policy-Entscheidung statt
  zur Selbstbestätigung eines Generators.
- Er kann Mechanismen einzeln falsifizieren, weil jede Gate-Verletzung einen
  benannten Fehlerpfad besitzt.
- Er erhält verworfene Alternativen und Unsicherheit als prüfbares Wissen.
- Er ist mit etablierten Provenienz- und Supply-Chain-Modellen anschlussfähig.

### Was noch nicht belegt ist

- Skalierung auf große Repositories und parallele Changes.
- Qualität bei mehrdeutigem, realem Stakeholder-Intent.
- echte Dekorrelation verschiedener Modelle, Organisationen oder Tools.
- kausale Outcome-Zuordnung nach einem Deployment.
- sicherer Strategy- oder Evaluator-Umbau.
- Vorteil gegenüber starken realen Agenten-Baselines.
- Produktivautonomie oberhalb `ZT2`.

## Nächste falsifizierbare Schritte

1. Evidence Pack auf W3C PROV und SLSA/in-toto abbilden und signieren.
2. ZT2-Ausführung in einen unabhängigen, kurzlebigen Sandbox-Runner verlagern.
3. Outcome-Ereignisse mit Interventions- und Confounder-Modell ergänzen.
4. Einen externen EvoSDLC-Benchmark preregistrieren und gegen mindestens eine
   starke lineare sowie eine agentische Baseline ausführen.
5. feste Anchor-Suites und unabhängige Audits für jede Strategy-Evolution
   erzwingen.

## Quellen und Einordnung

- [Intent Formalization: A Grand Challenge for Reliable Coding in the Age of AI Agents](https://arxiv.org/abs/2603.17150)
  beschreibt die Lücke zwischen natürlichem Intent und prüfbarer Spezifikation.
- [VeriAct: Beyond Verifiability](https://arxiv.org/abs/2604.00280)
  zeigt, warum verifier-akzeptierte Spezifikationen dennoch falsch oder
  unvollständig sein können.
- [LLMs Can't Plan, But Can Help Planning in LLM-Modulo Frameworks](https://arxiv.org/abs/2402.01817)
  motiviert die enge Kopplung generativer Modelle mit externen,
  modellbasierten Verifizierern.
- [Collaborative and AI-Supported Requirements Elicitation](https://arxiv.org/abs/2606.24060)
  liefert Evidenz dafür, Stakeholder-Kollaboration und AI-Synthese zu
  kombinieren statt normative Autorität zu automatisieren.
- [AI for Requirements Engineering: Industry adoption and Practitioner perspectives](https://arxiv.org/abs/2511.01324)
  berichtet deutlich stärkere Praxispräferenz für Human-AI Collaboration als
  für Vollautomatisierung.
- [Responsible Agentic AI Requires Explicit Provenance](https://arxiv.org/abs/2605.17169)
  begründet explizite Provenienz als Lebenszyklus- und Verantwortungsstruktur.
- [Who Grades the Grader?](https://arxiv.org/abs/2607.12790)
  motiviert feste Anker und unabhängige Audits bei evolvierenden Evaluatoren.
- [W3C PROV-O](https://www.w3.org/TR/prov-o/),
  [SLSA Provenance](https://slsa.dev/spec/v1.2/),
  [in-toto](https://in-toto.io/) und
  [NIST SP 800-218](https://csrc.nist.gov/pubs/sp/800/218/final)
  liefern anschlussfähige Modelle für Provenienz, Artefaktbindung,
  Lieferkettenintegrität und sichere Entwicklungspraktiken.

## Schlussfolgerung

Der Ansatz macht Sinn, solange Autonomie **pro Mission verdient**, Evidenz
unabhängig geprüft und Unwissen nicht als Sicherheit interpretiert wird.
EIDOS v0.8 alpha ist dafür ein funktionierender, falsifizierbarer Kern — noch
kein autonomes Produktionssystem.
