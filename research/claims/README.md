# Claim Ledger

Das öffentliche Claim Ledger lebt nicht als zweite Wahrheit in diesem Ordner.
Operative Signale und Claims gehören in den EIDOS-System-Twin. Nur eine explizit
sanitizierte Publikationsprojektion wird als versionierter
`.spot/claim-*.json`-`ResearchClaimNode` veröffentlicht und verweist über
`SourceRefs` auf öffentliche Knowledge-Knoten. `cdd export-context` erzeugt daraus
eine lesbare Projektion.

## Promotion eines Claims

```text
Unknown/Declared
  → Proposed
  → Experiment + Evidence
  → Verified oder Contested
  → Replikation
  → Ratified / OutcomeConfirmed
```

Kein Statusübergang erfolgt allein aufgrund einer LLM-Antwort. Für `Verified` und
`OutcomeConfirmed` erzwingt `cdd validate` mindestens eine benannte Knowledge-Quelle.
Ein Forschungsprotokoll muss zusätzlich erklären, welches Artefakt, welche Version
und welche Methode die Aussage tragen.

Aktuelle Hypothesen:

- `claim-essential-complexity-remains`
- `claim-gates-bound-autonomy`
- `claim-spot-traceability`
- `claim-cause-bound-loop-guards-reduce-waste`
