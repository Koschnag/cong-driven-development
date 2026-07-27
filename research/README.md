# CDD Research Program

CDD untersucht eine Gegenthese zum gegenwärtigen KI-Hype:

> Wenn Codeerzeugung billiger wird, verschiebt sich der Engpass zu Intent,
> Spezifikation, Modellierung, Verifikation und Governance.

Das Repository ist zugleich Forschungsgegenstand und ausführbares Artefakt. Es
veröffentlicht keine privaten CC10-, Kurs-, Nutzer- oder Betriebsdaten. Öffentliche
Beispiele sind generisch und synthetisch.

## Forschungsobjekte

| Track | Artefakt | Wahrheitsquelle |
|---|---|---|
| Theorie | Paper, Begriffe, Forschungsfragen | `.spot/` + `research/` |
| Methode | CDD-Kernel und EIDOS-Zielarchitektur | `src/` + `.spot/` |
| Referenzprojekt | CourseForge: Course IR → Lernspielplan | `examples/CourseForge.Core/` |
| Studio | Projektionen vom Claim bis zum Code | `src/Cdd.Web/`, `docs/ide/` |
| Evidenz | Tests, Protokolle, Snapshots | `tests/`, `research/protocols/`, Releases |

## Was ist Claim, was ist Ergebnis?

EIDOS hält operative Rohsignale und `Eidos.Claim`s im System-Twin. Für die
öffentliche Forschung wird daraus nach Sanitization eine bewusst schmalere
`ResearchClaimNode`-Projektion im SPOT erzeugt. Dadurch gelangen private Signal-IDs,
Akteure, interne Scopes oder Betriebsdetails nicht automatisch ins öffentliche Repo.

Diese Forschungsprojektion ist epistemisch typisiert:

- `Observed`: eine datierte Beobachtung;
- `Declared`: Aussage einer benannten Quelle;
- `Inferred`: nachvollziehbare Ableitung;
- `Proposed`: zu prüfende Hypothese;
- `Ratified`: von einer benannten Authority akzeptiert;
- `Verified`: durch benannte Evidenz geprüft;
- `Contested`: widersprüchliche Evidenz;
- `Unknown`: Evidenz fehlt;
- `Deprecated`: nicht mehr verwendet;
- `OutcomeConfirmed`: erwartete Wirkung wurde nach der Änderung gemessen.

Der epistemische Status ist unabhängig von `Convergence`. Ein Research-Claim kann technisch
korrekt gespeichert (`Aligned`) und wissenschaftlich weiterhin nur `Proposed` sein.

## Leitende Forschungsfragen

1. Verbessert ein typisierter SPOT die Traceability von Intent über Code und Test
   bis zu Evidenz und Outcome?
2. Reduzieren unabhängige, risikoadaptive Gates Regressionen bei agentisch
   erzeugten Änderungen gegenüber einem ungegateten Agenten?
3. Welche Autonomiestufe ist bei welcher Evidenz und welchem Blast Radius
   vertretbar?
4. Kann ein generischer Course-IR-Adapter aus LMS-Metadaten reproduzierbare,
   fachlich ratifizierbare Lernspielartefakte erzeugen?
5. Verbessert der kontrollierte Übergang von Spielmechanik zu Transferaufgabe die
   Beherrschung neuer Aufgaben gegenüber konventioneller Übung?

## Offenheit und Grenzen

- Preprint und Forschungsprototyp, nicht peer-reviewed.
- Technische Machbarkeit ist kein Nachweis pädagogischer oder organisatorischer Wirkung.
- Lernwirkungsclaims benötigen freiwillige Studien, geeignete Baselines und ggf.
  Ethik-/Datenschutzfreigaben.
- Ein Agent darf Claims vorschlagen, aber weder wissenschaftliche Wahrheit noch
  Produktivfreigabe selbst ratifizieren.

Der öffentliche Hintergrundimpuls ist der Essay
[„Software Engineering im KI-Zeitalter: Gegenthese zum Hype“](https://www.linkedin.com/pulse/software-engineering-im-ki-zeitalter-gegenthese-zum-hype-nguyen-imnof/).
Er ist Quelle für Hypothesen, nicht Beleg ihrer Gültigkeit.
