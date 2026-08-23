# Research Snapshots

Produktreleases (`vX.Y.Z`) und Forschungsstände sind getrennt:

- `vX.Y.Z`: Framework-, Studio- und Referenzprojektversion;
- `research-YYYY.MM.N`: reproduzierbarer Forschungsstand.

Ein Research Snapshot enthält:

- Commit-ID und UTC-Zeitpunkt;
- SPOT-Kontextexport einschließlich Claim Ledger;
- Paper-/Protokollquellen;
- Test- und Validierungsstatus;
- SHA-256-Manifest aller enthaltenen Dateien;
- bekannte Grenzen.

Der monatliche Workflow erzeugt automatisch ein internes Actions-Artefakt. Ein
öffentlicher Research Release entsteht nur durch einen manuellen Lauf mit
`create_draft=true` und bleibt zunächst ein GitHub-Draft. Das menschliche
Promotion-Gate prüft Datenschutz, Claims, Zahlen, Lizenzen und Reproduzierbarkeit.
