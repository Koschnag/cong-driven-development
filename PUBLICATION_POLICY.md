# Public Research & Data Policy

Dieses Repository ist ein öffentliches Forschungs- und Portfolioartefakt. Es
enthält ausschließlich Material, das für weltweite Veröffentlichung geeignet ist.

## Erlaubt

- synthetische CourseForge-Fixtures und neu erstellte Aufgaben;
- öffentliche Quellen und paraphrasierte Takeaways;
- reproduzierbare Tests, Protokolle, Claims und aggregierte Ergebnisse;
- generische Architektur ohne private Routen, Credentials oder Topologie.

## Nicht erlaubt

- echte Moodle-Exporte, Nutzer-, Bewertungs-, Log- oder Kursdateien;
- E-Mail-Adressen, Telefonnummern, Matrikel-, Kunden- oder Gesundheitsdaten;
- CC10-Routen, interne Hostnamen, IP-Adressen, Tokens oder Secret-Werte;
- private DWH-, Nextcloud- oder Session-Inhalte;
- unveröffentlichte Materialien Dritter ohne Nutzungsrecht.

## Veröffentlichungsgate

Vor Merge oder Release:

1. Build, Tests, `cdd validate` und Doku-Synchronität sind grün.
2. Gitleaks und `scripts/check-public-data.sh` sind grün.
3. Research Claims sind nicht stärker formuliert als ihre Evidenz.
4. Fixtures und Screenshots sind synthetisch geprüft.
5. Ein Mensch prüft Diff, Quellen, Zahlen und Lizenzen.

Ein öffentlicher Issue-Text ist bereits veröffentlicht. Sicherheits- oder
Datenschutzprobleme gehören ausschließlich in GitHub Private Vulnerability Reporting.
