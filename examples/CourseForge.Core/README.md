# CourseForge.Core

CourseForge ist das generische CDD/EIDOS-Referenzprojekt: Ein Moodle-Kursexport
wird zu einem datensparsamen Course IR und anschließend zu einem deterministischen,
fachlich noch zu ratifizierenden Lernspielplan.

Der aktuelle Vertical Slice kann:

- einen **bereits extrahierten** Moodle-Backup-Ordner lesen;
- `moodle_backup.xml` und `sections/section_*/section.xml` normalisieren;
- Dateianzahl, Gesamtgröße und verlinkte Dateien fail-closed begrenzen;
- Nutzer-, Bewertungs-, Log- und Binärdaten ignorieren;
- pro Abschnitt die Phasen Explore, Guided Practice, Independent Practice und
  Transfer Check erzeugen;
- Bug-/Feature-Signale in `ProposalOnly`-Vorschläge triagieren und über eine
  explizite Antikorruptionsschicht in risikotypisierte EIDOS Change Intents überführen;
- sensible Signale verwerfen und Security-Signale getrennt eskalieren.

Noch nicht implementiert:

- das sichere Entpacken von `.mbz`;
- semantische Extraktion von Lernzielen und Aufgaben;
- eine konkrete Game-Runtime;
- CourseForge-spezifische Candidate-Erzeugung und Sandbox-Ausführung;
- automatische oder produktive Promotion.

`NeedsAuthoring=true` ist absichtlich ein hartes Ehrlichkeitsmerkmal: Aus einem
Abschnittstitel folgt keine fachlich korrekte Lernmechanik.

Die Fixture unter `fixtures/minimal-moodle/` ist vollständig synthetisch.
