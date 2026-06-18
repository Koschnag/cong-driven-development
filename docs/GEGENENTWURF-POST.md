# Gegenentwurf-Post (Entwurf) — Antwort auf Luckes „My job is to write loops"

**Status:** ENTWURF, nicht gepostet. Veröffentlichung nur auf Congs explizites Go.
**Link-Urteil des Review-Panels:** *safe to link — MIT FIXES* (siehe unten). Empfehlung:
**Teaser-with-demo** (Repo verlinken), aber erst nach den Fixes + einer aufgezeichneten
asciinema (Dry-Run zeigt den Prompt, `--go` zeigt rotes-Gate→grünes-Gate). Ton: kein „du",
geschlechtsneutral.

---

# Loop Engineering, zu Ende gedacht: der Loop muss gegen ein Orakel terminieren — nicht gegen „Agent sagt fertig"

Cherny hat recht: man promptet nicht mehr, man baut den Loop, der nachpromptet, bis ein Ziel erreicht ist. Luckes Vorbehalte sind ebenfalls richtig — und sie zielen alle auf denselben Punkt: das Abbruchkriterium. „Bis ein verifizierbares Ziel erreicht ist" ist die ganze Aussage. „Agent meldet fertig" ist kein Kriterium.

These: Der Loop terminiert auf Konvergenz-gegen-Spec, mechanisch geprüft. Typechecker grün, abgeleitete Tests grün, Modell und Code Aligned. Sonst loopt er weiter — oder bricht am Limit ab. Diese eine Änderung beantwortet jeden Einwand.

Gezeigt wird, nicht behauptet. Drei Repos, klickbar.

## Das Gate hat Zähne (das ist der Kern)

Das Abbruchkriterium ist reine Logik, ohne IO testbar:

    GateBestanden = markerAligned && testprojekte > 0 && alleTestsGruen

Exit 0 reicht nicht. Ein Lauf gilt nur als grün, wenn die Ausgabe bestandene Tests ausweist und weder „Failed" noch „No test" enthält. Die Tests dazu schließen genau die Löcher, durch die ein Agent ein Gate täuscht: keine Testprojekte ⇒ nie grün. Exit 0 mit „No test" ⇒ nicht grün. Marker Aligned ohne echte Tests ⇒ nicht grün. Reviewt wird eine Wahrheitstabelle, nicht ein Diff.

## Der Loop bricht wirklich ab

Der Runner (C#) findet eine Pending-Spec, baut den Prosa-Prompt, übergibt an `claude -p` headless, misst danach `validate` + echtes `dotnet test` je Projekt gegen das Gate — und loopt bis grün oder bis `--max-attempts`. Dry-Run ist Default und kostet nichts; scharf wird es erst mit `--go`. Die Token-Leitplanke ist nicht Disziplin, sondern ein modellierter Critical-Risk mit Mitigation (`risk-runner-amok`, `adr-004`) — als Knoten im Modell, einsehbar.

## Auf hartem Terrain

Damit die Allgemeinheit nicht behauptet bleibt: `runenruf`, eine RTS-RPG-Engine from scratch in F#/C# auf Silk.NET, 74 Modell-Knoten, deterministische Simulation. Kein To-do-CRUD. Ein Spiel-Engine-Spec trägt spec-driven durch dasselbe Gate — das Verfahren ist nicht domänengebunden.

## Punkt für Punkt

- **Saubere Spec** — das verlangte „sauber spezifizierte Ziel" ist die Eingangsbedingung, im Typsystem erzwungen: eine Spec ohne Given/When/Then-Kriterium ist ein Validierungs-Error, eine Spec ohne Test eine verletzte Invariante. Auf das Falsche loopen heißt: das Gate wird nie grün.
- **Großes Feature vs. kleines Ticket** — zugestanden. Der Runner nimmt bewusst eine Spec je Lauf, nicht den Issue-Tracker. Große Features zerfallen in modellierte Specs mit geprüften Abhängigkeiten. Kein Anspruch auf Gapless-Tracker-Magie.
- **Cognitive Debt** — der eigentliche Streitpunkt. Debt entsteht, wenn Code zur einzigen Wahrheit wird. Hier bleibt das getypte Modell die Wahrheit; Abweichung ist ein erstklassiger Befund (`Diverged`/`Orphaned`), Doku-Drift ein CI-Fehler. Der Mensch modelliert den Durchstich, die KI konvergiert auf das Modell — nie umgekehrt. Der Prompt verbietet ausdrücklich, Spec oder Tests zu ändern, „nur um das Gate zu täuschen".
- **Token-Ökonomie** — kein Brute-Force bis „fertig", sondern ein bounded Loop gegen ein billiges Orakel (Typechecker + Tests). Das Limit greift hart. Kein AI-Lab nötig.
- **Marketing vs. Realität** — darum sauber getrennt, was läuft und was Fahrplan ist.

## Ehrliche Grenze

Was läuft, ist per Klick belegbar: getyptes SPOT-Modell, Validierung mit vier erzwungenen Invarianten, das Gate mit Zähnen samt seinen Tests, der bounded Loop, der MCP-Server, das Cockpit. Cdd-Core baut grün, das Gate ist durch eigene Tests gegen die Schummel-Löcher abgesichert.

Was Fahrplan ist, wird als Fahrplan benannt — sonst fängt der Repo-Klick eine Lüge: Lean-Beweise gibt es keine (null .lean-Dateien). Property-based Tests gibt es keine (null FsCheck in allen Repos); alle Tests sind gewöhnliche Beispieltests. Der austauschbare Mistral-/Ollama-Anbieter ist spezifiziert, im Runner noch Pending — der Chat-Pfad existiert, der agentische Tool-Loop nicht. Das Programm-Dashboard zeigt Modell-Marker, keine test-verifizierten Quoten; beides wird nicht vermischt.

Souverän, self-host, bring-your-own-repo. F#/C#/.NET, kein Python, MPL-2.0. Kein SaaS. Nichts ruft nach Hause.

„Coding is solved" wird hier nicht behauptet. Behauptet wird: Konvergenz wird gemessen, nicht erklärt. Der Repo-Klick bestätigt den Satz.

---

## FIXES VOR DEM POSTEN (aus dem adversarialen Review — sonst fängt der Repo-Klick eine Überzeichnung)
1. **Install-Zeile**: `dotnet tool install -g cdd cdd-mapper` ist erst seit den PackAsTool-Commits
   (`cdd@d6b34a5`, `cdd-mapper@5e0d679`) *baubar* — aber **noch nicht auf nuget.org publiziert**.
   Entweder als „**Mit v0.1**: …" rahmen ODER auf „aus dem Source bauen + installieren" umformulieren.
   Nicht als Gegenwart stehen lassen.
2. **STATUS.md** ist veraltet (runenruf 69→74) und mischt Marker- vs. Test-Konvergenz —
   vor dem Verlinken neu generieren + Caption „Quoten = Marker-Konvergenz, NICHT test-verifiziert" *in die Datei*.
3. **Test-Zahlen** immer als `dotnet test`-Verdikt, nie als grep: Mapper **8/8**, cdd-core **33/33**,
   runenruf **26 gesamt / 25 spot-markiert**. Keine runde „58".
4. **cdd-programm-README**-Widerspruch glätten (Mapper IST gebaut; nur Chat-Steuerung + Anbieter-Naht Pending).
5. **„clonable starter"**: runenruf hat keine Root-`.sln` — Gate läuft pro Projekt; das dokumentieren,
   sonst stolpert ein Fremder über `dotnet build` im Root.
6. Optional stark: **asciinema** beilegen (Dry-Run → `--go`: rotes Gate → grünes Gate).
