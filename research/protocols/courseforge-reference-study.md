# Protokoll: CourseForge als CDD/EIDOS-Referenzprojekt

Status: Entwurf für Präregistrierung, noch keine Wirkungsstudie.

## Ziel

CourseForge prüft zwei voneinander getrennte Schleifen:

1. **Content Loop:** Moodle-Metadaten → Course IR → authoring-gegateter Spielplan.
2. **Evolution Loop:** Nutzerfeedback → Signal → ProposalOnly Change Intent →
   isolierter Candidate → Assurance → menschliche Promotion.

Automatische Inhaltsprojektion und Softwareevolution dürfen nicht als dieselbe
Autonomiestufe behandelt werden.

## Baselines

| ID | Verfahren |
|---|---|
| B0 | konventionelle manuelle Entwicklung |
| B1 | LLM-Agent ohne CDD-Gates |
| B2 | CDD-Kernel mit typisierten Specs und technischen Gates |
| B3 | EIDOS-Kandidat mit Signal-, Evidence- und Promotion-Protokoll |

## Messgrößen

### Software Engineering

- Lead Time vom reproduzierbaren Signal zum geprüften Candidate
- Anteil akzeptierter Candidates
- escaped defects und Rollbacks
- Reviewer-Zeit
- Trace-Coverage: Intent → Spec → Code → Test → Evidence → Release
- Reproduzierbarkeit auf einem frischen Checkout

### Lernen

- Leistung bei neuen Transferaufgaben ohne Hilfen
- benötigte Hilfestufen
- Bearbeitungszeit und Fehlertypen
- verzögerter Retentionstest
- selbstberichtete kognitive Belastung

Spielzeit, XP oder Klickrate gelten nicht als Kompetenznachweis.

## Sicherheits- und Datenschutzprotokoll

- Nur synthetische Fixtures liegen im öffentlichen Repository.
- Der Adapter liest eine Metadaten-Allowlist; `users.xml`, Bewertungen, Logs und
  Binärinhalte werden nicht in den Course IR übernommen.
- `.mbz`-Extraktion erfolgt später nur isoliert mit Schutz gegen Zip-Bombs,
  Path Traversal, Links, Dateianzahl und Gesamtgröße.
- Öffentliche Feedback-Formulare erheben weder Konto noch E-Mail und warnen vor
  persönlichen Daten.
- Security-/Privacy-Signale gehen in Private Vulnerability Reporting.
- Originale Hochschulunterlagen benötigen Nutzungsrechte; öffentliche Versuche
  verwenden neu erstellte generische Aufgaben.

## Threats to Validity

- Ein einzelnes Kursformat oder Modul generalisiert nicht auf andere Fächer.
- Autor und Implementierer sind derzeit dieselbe Person.
- Agenten- und Modellversionen verändern sich schneller als die Methode.
- Generator und Validator können korrelierte Fehler besitzen.
- Verbesserte Spielleistung ist nicht automatisch Prüfungstransfer.

Alle Ergebnisse müssen mit Commit, Toolversion, Modell/Provider, Seeds,
Testartefakten und bekannten Abweichungen veröffentlicht werden.
