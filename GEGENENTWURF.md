# Mit Lucke, eine Schraube weiter

*Eine Position zu Loop Engineering, Konvergenz-gegen-Spec und der unmechanisierbaren Grenze.*
*Cong Chanh Vinzenz Nguyen.*

---

Ich teile Luckes Prämisse. Voll-Automation ist Hype. Rice ist unentscheidbar, METR misst rund 5,3 Stunden autonome Aufgabendauer bei 50 % Erfolg, 34–62 % der LLM-generierten Tests sind ungültig, SWE-bench ist saturiert. „Coding is solved" ist eine Marketing-Phrase ohne Träger. Ich bin nicht hier, um das zu bestreiten. Ich bin hier, um eine Schraube weiterzudrehen.

Die konstruktive Frage ist nicht „klappt Automation?", sondern: Wo genau liegt die unmechanisierbare Grenze — und lässt sie sich typisieren? Meine Antwort vorweg, so scharf wie ich sie halten kann: Die Grenze ist das Setzen der Invariante. Jemand wählt die gewünschte Eigenschaft. Sie wird nicht entschieden, sie wird gesetzt — und diese Setzung liegt außerhalb des Entscheidungsproblems. Das ist nicht Penrose-Lucas; Rice trifft Mensch und Maschine symmetrisch. Es ist operational, nicht metaphysisch, und deckt sich mit Kambhampatis LLM-Modulo: der externe Verifizierer ist die Quelle der Garantie.

Operational heißt: Akzeptanzkriterium verschieben. Heute heißt es — ein Mensch liest einen Diff und glaubt. Ich ersetze das durch — eine Maschine entscheidet Konvergenz gegen eine externalisierte, getypte Referenz. Entscheidend ist die Architektur. Generator und Orakel sind getrennt: die generierende Engine kann per Tool-Allowlist keinen Code schreiben; das Orakel setzt einen Spec-Knoten nur dann auf „Aligned", wenn ein echter dotnet-test-Lauf grün ist — geprüft wird Test-PASS, nicht bloßer Prozess-Exit-0. Das ist der Unterschied zu Kiro oder GitHub Spec Kit: dort erzeugt dasselbe System Spec, Code und Test — die Referenz ist nicht unabhängig vom Prüfling. Bei mir ist sie es.

Zwei öffentliche Belege, beide CI-grün auf GitHub-Hardware, reproduzierbar.

**Koschnag/runenruf** — ein Spiel, der Existenzbeweis: der Loop kann gegen ein Orakel terminieren, das Exit-0 nicht glaubt. Die Suite läuft vollständig grün — 46/46, der README nennt den Reproduktions-Befehl —, darunter eine FsCheck-Property (`spec-siegel-lager-nichtnegativ`) über jeden Seed und jede Befehlsfolge; deterministische Headless-Sim. Ehrlich: das ist die freundlichste Domäne — geschlossen, deterministisch. „Trifft die Spec die Welt" ist dort fast leer, weil die Spec die Welt ist.

**Koschnag/ledger-casestudy** — ein doppisches Hauptbuch, das Gegenstück: echtes IO (Journal auf Platte plus Replay, `spec-replay-treue`), ein veränderliches Requirement (Gebühren kamen nach der ersten Spec), F#/.NET 9, int64-Cent statt Float, also exakte Erhaltung. 5/5 grün. Hier ist „trifft das Modell die Welt" nicht leer: der naive erste Entwurf zieht die Gebühr beim Sender ab und schreibt sie nirgends gut — Wert verschwindet. Die Invariante `spec-werterhaltung` — Gesamtsumme aller Konten konstant — fängt das ab: „Falsifiable, after 4 tests." Fix ist Doppik, die Gebühr ist Gegenbuchung. Geld verschwindet nicht. Der naive Entwurf behauptet es, die Invariante verwirft ihn — ohne dass ein Diff gelesen wurde.

Das ist der Kern: Review ist Konvergenz gegen die Spec, nicht der gelesene Diff. Der naive Gebühren-Entwurf wird rot, mit minimalem Gegenbeispiel, ohne Diff-Lektüre — `./demo-gate-at-failure.sh` im Ledger-Repo führt genau das reproduzierbar vor: korrektes Modell grün, naives Modell injiziert, Orakel rot, revert. Und es komprimiert: in diesen zwei Fällen deckt eine 1-Zeilen-Invariante über den FsCheck-Generator einen unbeschränkten Eingaberaum — gegenüber 41 LoC Ledger-Domain und 125 LoC Runenruf-Sim. Eine Beispieltabelle für dieselbe Garantie ginge gegen unendlich Zeilen, die sie nie abdeckt. Du reviewst eine Zeile, keinen 41-bis-125-Zeilen-Diff. „Spec ist nur Code im Trenchcoat" hält hier nicht: eine Beispieltabelle leistet, was die Invariante leistet, in diesen Fällen nachweislich nicht.

Die Grenzen, ungeschönt: zwei kleine Fallstudien, ein Spiel und ein Hauptbuch. Kein Issue-Tracker, keine unbeschränkte Autonomie — die ist nicht da, und ich behaupte nicht, dass sie es ist. Der Property-Layer (FsCheck) ist real in beiden Repos. Lean ist für die Werterhaltung *bewiesen* — `proofs/Werterhaltung.lean` im Ledger-Repo, sorry-frei (`#print axioms` nennt nur `propext`, `Quot.sound`), CI-verifiziert auf GitHub-Hardware; für weitere Invarianten Roadmap. Damit ist für diesen Invarianten die Kette Typen → Property → Beweis geschlossen: dieselbe Erhaltung wird getypt, über Zufallsfolgen geprüft und für *jede* Folge bewiesen. Der Mensch bleibt am Modell-Gate, fundamental, aber operational: jemand muss die Invariante setzen, und diese Setzung liegt außerhalb des Entscheidungsproblems. Nicht „Mensch kann, was Maschine nicht kann" — Rice trifft beide.

Den intellektuellen Kern habe nicht ich erfunden. Lahiri nennt ihn eine Grand Challenge — *„Intent Formalization: A Grand Challenge for Reliable Coding in the Age of AI Agents"*, arXiv:2603.17150 (2026). Storey nennt das Fehlen externalisierter Begründung „Intent Debt" — sinngemäß die Abwesenheit externalisierten Rationales, das Entwickler und Agenten brauchen, um sicher mit Code zu arbeiten (*„From Technical Debt to Cognitive and Intent Debt: Rethinking Software Health in the Age of AI"*, arXiv:2603.22106, 2026). Die getypte Spec ist genau dieses externalisierte Rationale. Ich bin nicht der Denker der Idee. Ich bin, soweit mir bekannt und verteidigbar, der erste, der sie als durchgängig getypte, lauffähige Kette mit architektonisch getrenntem Generator und Orakel und property-verifiziertem Gate implementiert hat — für einen Kern-Invarianten bis zum sorry-freien Lean-Beweis durchgezogen.

Lucke hat die Diagnose. Ich liefere die Schraube danach. Sie kann die Invariante nicht wählen — alles danach kann sie prüfen, wenn man sie lässt, aber nicht schreiben.

---

## Belege (reproduzierbar, CI-grün auf GitHub-Hardware)

- **github.com/Koschnag/runenruf** — Fallstudie 1 (Spiel, Existenzbeweis). `git clone … && dotnet test tests/Runenruf.Tests` → 46/46 grün, inkl. FsCheck-Property `spec-siegel-lager-nichtnegativ` über jeden Seed/jede Befehlsfolge.
- **github.com/Koschnag/ledger-casestudy** — Fallstudie 2 (Hauptbuch, schlägt zurück). `dotnet test tests/Ledger.Tests` → 5/5; `./demo-gate-at-failure.sh` zeigt das Orakel beim Verwerfen eines falschen Modells (`Falsifiable, after N tests`); `lean proofs/Werterhaltung.lean` beweist die Werterhaltung sorry-frei (CI-Job `lean-proof`).
- **github.com/Koschnag/cong-driven-development** — die Methode/IDE (v0.4.0), Orakel `SetzeSpecAligned` (Aligned nur bei echtem grünem Test).

## Quellen

- Shuvendu K. Lahiri, *Intent Formalization: A Grand Challenge for Reliable Coding in the Age of AI Agents*, arXiv:2603.17150 (2026).
- Margaret-Anne Storey, *From Technical Debt to Cognitive and Intent Debt: Rethinking Software Health in the Age of AI*, arXiv:2603.22106 (2026). Intent Debt (Volltext): „the absence of externalized rationale that developers and AI agents need to work safely with code."
- S. Kambhampati u. a., *LLMs Can't Plan, But Can Help Planning in LLM-Modulo Frameworks*, arXiv:2402.01817 (2024).
- METR, *Measuring AI Ability to Complete Long Tasks* (Time-Horizon, ~5,3 h @ 50 %, Jan 2026).

*Bewiesen: Werterhaltung in Lean 4, sorry-frei (`proofs/Werterhaltung.lean`). Roadmap, nicht behauptet: Lean-Beweise weiterer Invarianten; Fallstudien mit Nebenläufigkeit. Der Uniqueness-Anspruch ist auf „soweit bekannt und verteidigbar der erste" begrenzt — keine Prioritätsrecherche.*
