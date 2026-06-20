# Review-Hub — Das Terminierungs-Orakel

> Ein Ort. Klick dich durch. Alles ist verlinkt, jeder Befehl ist copy-paste, jede offene
> Frage ist markiert. Am Ende steht deine Entscheidung, nicht meine.

**Status:** review-fertig als **Preprint + Forschungsprototyp**. *Nicht* peer-reviewed.
Clean-Clone reproduziert alle Zahlen. Drei Repos + Paper + Whitepaper + Lean-Beweis, alles CI-grün.
Was bleibt: dein Urteil und das Posten.

---

## 1 · Lies das (in dieser Reihenfolge)

| | Artefakt | Was |
|---|---|---|
| 📄 | **[Paper (PDF)](docs/paper-terminierungs-orakel.pdf)** · [TeX](docs/paper-terminierungs-orakel.tex) | 16 S., das zitierfähige Stück. arXiv-Stil, A/B-Tabelle (§6.5), Lean-Listing, 12 Zitate. |
| 📄 | **[Whitepaper (PDF)](docs/whitepaper-konvergenz.pdf)** · [Markdown](docs/whitepaper-konvergenz.md) | Die Antwort auf den Loop-Engineering-Diskurs mit Tiefe, deutsch + engl. Abstract. |
| 💬 | **[GEGENENTWURF.md](GEGENENTWURF.md)** | Die kurze, postbare Position. |

Die **Antwort als LinkedIn-Kommentar** (Sie-Form, ~4 Absätze) liegt im Session-Verlauf —
nicht im Repo, weil sie *du* postest.

---

## 2 · Prüf die A-Marken selbst (maschinell verifiziert, ein Befehl je Repo)

Clean-Clone hat das gerade bestätigt — du bekommst exakt diese Zahlen:

```bash
# Methode/IDE — erwartet: 36/36
git clone https://github.com/Koschnag/cong-driven-development && cd cong-driven-development
dotnet test tests/Cdd.Tests

# Fallstudie A: Spiel (Existenzbeweis) — erwartet: 46/46
git clone https://github.com/Koschnag/runenruf && cd runenruf
dotnet test tests/Runenruf.Tests

# Fallstudie B: Hauptbuch (schlägt zurück) — erwartet: 5/5
git clone https://github.com/Koschnag/ledger-casestudy && cd ledger-casestudy
dotnet test tests/Ledger.Tests
lean proofs/Werterhaltung.lean      # sorry-frei: depends on axioms [propext, Quot.sound]
./demo-gate-at-failure.sh           # grün → Falsifiable (rot) → revert (grün)
```

| Repo | Anker-Commit | CI |
|---|---|---|
| [cong-driven-development](https://github.com/Koschnag/cong-driven-development) | `main` | [Actions](https://github.com/Koschnag/cong-driven-development/actions) |
| [runenruf](https://github.com/Koschnag/runenruf) | `56376a5` | [Actions](https://github.com/Koschnag/runenruf/actions) |
| [ledger-casestudy](https://github.com/Koschnag/ledger-casestudy) | `72b1284` | [Actions](https://github.com/Koschnag/ledger-casestudy/actions) |

---

## 3 · Der formale Kern (das Stärkste)

- 🔒 **[Lean-Beweis: Werterhaltung](https://github.com/Koschnag/ledger-casestudy/blob/main/proofs/Werterhaltung.lean)**
  — für *jede* Buchungsfolge bewiesen, sorry-frei, ohne Mathlib, Modell *mit* Ablehnungspfad
  (modellgleich zum F#-Code). CI-Job `lean-proof` grün.
- 🎯 **[Gate beim Scheitern](https://github.com/Koschnag/ledger-casestudy/blob/main/demo-gate-at-failure.sh)**
  — das Orakel verwirft ein falsches Modell reproduzierbar, ohne Diff-Lektüre.
- 🧱 **Drei Schichten an einem Invarianten:** Typen (`int64`) → FsCheck-Property → Lean-Beweis. *Nur*
  für die Werterhaltung geschlossen (die anderen Eigenschaften sind FsCheck-only — ehrlich so markiert).
- 📊 **A/B-Verifizierbarkeitstabelle** (Paper §6.5): jede tragende Behauptung als **A** (maschinell
  belegt) oder **B** (menschliche Setzung). Der Logik-gegen-LLM-Hebel.

---

## 4 · Ehrlich offen — deine Entscheidung (Klasse B)

Diese Punkte sind im Paper ausdrücklich markiert; sie blockieren einen Preprint *nicht*, aber sie
sind dein Urteil, nicht maschinell entscheidbar:

- **N = 2** Fallstudien (zeigt Anwendbarkeit, nicht Skalierung); beide klein.
- **Ein** Lean-bewiesener Invariant; weitere sind Roadmap.
- **Preprint, nicht peer-reviewed.**
- `rest_in_B`: „trifft die Spec die *Welt*" (nicht orakel-prüfbar), *wer* die Invariante setzt
  (normativ), Neuheit (Literatur-Aussage), Generalisierung über Domänen.
- **Zwei Entscheidungen:** (a) Preprint *jetzt* — oder vorher eine Schraube mehr (2. Lean-Invariant /
  härtere Fallstudie mit IO+Nebenläufigkeit)? (b) arXiv ja/nein?

---

## 5 · Dein Review-Pfad (zum Abhaken)

- [ ] **A-Marken nachfahren** — die vier Befehlsblöcke aus §2 (clone+test, lean, demo). Stimmen die Zahlen?
- [ ] **Paper lesen** — besonders Abstract, **A/B-Tabelle (§6.5)**, Grenzen (§6), Fazit (§8).
- [ ] **B-Marken verantworten** — trägst *du* jede menschliche Setzung? (Das ist die irreduzible Stelle.)
- [ ] **Entscheiden** — Preprint jetzt vs. eine Schraube mehr; arXiv ja/nein.
- [ ] **Außendarstellung** — Antwort posten (Sie-Form) + Whitepaper/Repos verlinken. *Dein Akt.*

---

*Warum dein Review die irreduzible Stelle ist: Diese Arbeit wurde KI-assistiert erzeugt. Dass sie
korrekt ist, dich trifft und die Claims tragen, kann das System nicht aus sich selbst zertifizieren
— derselbe Regress, den das Paper beschreibt. Du bist die Setzung. Die A-Marken sind re-runbar; die
B-Marken brauchen dich.*
