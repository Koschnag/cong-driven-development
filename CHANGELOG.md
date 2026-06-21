# Changelog

Alle nennenswerten Änderungen an diesem Projekt. Format angelehnt an
[Keep a Changelog](https://keepachangelog.com/), Versionierung nach [SemVer](https://semver.org/).

## [0.6.0] — 2026-06-21

### Added
- **Zweiter maschineller Beweis: Nichtnegativität.** `proofs/Werterhaltung.lean`
  beweist jetzt sorry-frei sowohl `werterhaltung` als auch `nichtnegativ` (kein Konto
  wird je negativ, über jede Buchungsfolge). `#print axioms` für beide: nur `propext`,
  `Quot.sound`. Verifiziert auf Lean 4.31.0. Die Drei-Schichten-Kette
  Typen→Property→Beweis ist damit für zwei Invarianten geschlossen.
- ledger: In-Suite-Diskriminierungstest (der naive Gebühren-Entwurf verliert beweisbar
  Wert, der korrekte erhält ihn) + geweitete Generatoren → 6/6.
- Website: öffentliche Landing, teilbarer Blog, OG-Vorschaubild; Homelab-Deployment
  (`docker-compose.yml` + Caddy-Reverse-Proxy mit Basic-Auth, `docs/homelab.md`).

### Fixed
- `derive-tests` korrigiert veraltete Test-Namen bei umsortierten Kriterien statt sie
  blind zu überspringen (+ Regressionstest) → CDD 37/37.
- `Cdd.Core.Sync.SetzeSpecAligned`: das Orakel ist jetzt eine echte, benannte API; die
  Doku beschreibt es korrekt (Marker-Präsenz; Greenness via CI-Suite + reflexiver
  Selbst-Test, nicht durch die Funktion).
- `scanTestMarkers`: Pfad-Separatoren normalisiert (Windows `\obj\`/`\bin\` wurde nicht
  gefiltert).
- CLI: unbekanntes Argument exitet mit 1 statt still mit 0.
- Zahlen und Commit-Anker über Paper, Whitepaper, Decks, Landing konsistent;
  A-Marken an stabile Refs gepinnt.

## [0.5.x] — 2026-06

Konsistenz- und Korrektheits-Patches: ehrliche Orakel-Formulierung (kein Overclaim
„nur bei grünem Test"), Homelab-Härtung (bcrypt-Hash via `env_file`, Ports 80+443),
Landing für Entwickler-Zielgruppe.

## [0.4.0] — 2026-06-18

Erstes vollständiges Set: Paper (arXiv-Stil, A/B-Verifizierbarkeitstabelle, Lean-Listing),
Whitepaper, Onboarding-Deck, Talk; self-contained Binaries + Container-Image.

## [0.1.0 – 0.3.0] — 2026-06

SPOT-Kern (F#-DU-Graph), CLI (`init/validate/derive-tests/diff/sync-*`), MCP-Server,
Web-Cockpit, reflexives Selbst-Modell, erster Lean-Beweis (Werterhaltung).

---

## Bekannte Grenzen (bewusst offen, dokumentiert)

- **N = 2 Fallstudien**, beide klein — Anwendbarkeit, nicht Skalierung.
- **Zwei** Lean-bewiesene Invarianten (Werterhaltung, Nichtnegativität); Isolation und
  Replay-Treue sind nur FsCheck-geprüft (Roadmap).
- Das **Web-Cockpit inkl. Chat-Loop ist experimentell**; erprobter Kern ist CLI + MCP +
  Konvergenz-Orakel. Das Cockpit hat **keine eingebaute Authentifizierung** — im Homelab
  hinter Reverse-Proxy/SSO betreiben.
- `Sync.compare` behandelt **doppelte Component-Namen** nicht (eine von ihnen wird still
  übergangen) — Minor, tritt nur bei manuell duplizierten Namen auf; Validierungs-Regel ist Roadmap.
- `derive-tests` nutzt **positionsbasierte IDs**; Namen werden bei Änderung korrigiert, die
  IDs bleiben positionsgebunden (bewusst, gegen Self-Modell-Ripple).
- **Preprint, nicht peer-reviewed.**

[0.6.0]: https://github.com/Koschnag/cong-driven-development/releases/tag/v0.6.0
[0.4.0]: https://github.com/Koschnag/cong-driven-development/releases/tag/v0.4.0
