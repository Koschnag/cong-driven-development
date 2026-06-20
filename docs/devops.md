# DevOps-Konzept

Solo-Maintainer-Setup mit professionellen Gates: leichtgewichtig genug, um nicht zu
bremsen — streng genug, dass nichts Ungetestetes auf `main` oder in ein Release kommt.

## 1. Git-Strategie: GitHub Flow

**Trunk-based mit kurzlebigen Feature-Branches.** Kein GitFlow — bei einem
Entwickler erzeugen `develop`/`release`-Branches nur Merge-Overhead ohne Nutzen.

```
main ──●────●────●────●──▶   immer releasebar, geschützt
        \  /      \  /
         ●●        ●●        feature/<slug>, fix/<slug> — kurzlebig (< 1 Woche)
```

### Branches

| Branch | Zweck | Regeln |
|---|---|---|
| `main` | Einziger langlebiger Branch, immer grün & releasebar | Geschützt, nur via PR |
| `feature/<slug>` | Neue Funktionalität | Von `main`, zurück via PR |
| `fix/<slug>` | Bugfixes | Von `main`, zurück via PR |
| `chore/<slug>` | Tooling, Doku, Dependencies | Von `main`, zurück via PR |

### Commits & PRs

- **Conventional Commits:** `feat:`, `fix:`, `chore:`, `docs:`, `test:`, `refactor:`
- **Squash-Merge** — lineare Historie auf `main`, ein Commit pro PR
- PR-Beschreibung folgt dem Template (`.github/pull_request_template.md`)

### Approvals

Als Solo-Entwickler ist ein menschliches Review nicht möglich — die Approval-Rolle
übernehmen die **Required Status Checks**: ein PR ist erst mergebar, wenn
Build + Tests + Sonar Quality Gate grün sind. Sobald ein zweiter Contributor
dazukommt: „Require 1 approval" in der Branch-Protection aktivieren.

## 2. Testing-Strategie

| Ebene | Werkzeug | Wann |
|---|---|---|
| Unit/Property | xUnit (`tests/Cdd.Tests`) | Jeder Push & PR |
| Coverage | Coverlet (XPlat Collector) | Jeder CI-Lauf, Report an Sonar |
| Statische Analyse | SonarCloud | Jeder Push auf `main` & PR |
| CLI-Smoke | `cdd init/validate/diff/derive-tests` im CI | Jeder CI-Lauf |

Regel: **Kein Merge mit rotem Check.** Spec-getriebene Tests (CDD-Kernidee) ersetzen
sukzessive handgeschriebene Tests — `cdd derive-tests` ist selbst Teil der Pipeline-Vision.

## 3. CI-Pipeline (`.github/workflows/ci.yml`)

Trigger: Push auf `main`, jeder Pull Request.

1. **build-test** — `dotnet build` (Release, Warnings as Errors) + `dotnet test`
   mit Coverage, CLI-Smoke-Test, Test- und Coverage-Artefakte
2. **sonar** — SonarCloud-Analyse mit Coverage-Import (läuft nur, wenn das
   Secret `SONAR_TOKEN` konfiguriert ist; Forks ohne Secret bleiben grün)

## 4. Release- & Deployment-Strategie

CDD ist eine CLI + Library — „Deployment" heißt: **versionierte Binaries auf
GitHub Releases**, später NuGet-Pakete.

### Kanäle

| Kanal | Trigger | Artefakt |
|---|---|---|
| **Continuous** | Merge auf `main` | CI-Artefakte (Build + Testreport) |
| **Release** | Git-Tag `vX.Y.Z` | GitHub Release mit Binaries (linux-x64, win-x64, osx-arm64) |

### Versionierung: SemVer

- Zentrale Version in `Directory.Build.props` (`<Version>`)
- `MAJOR` — Breaking Change am SPOT-Format oder CLI-Interface
- `MINOR` — neue Features, abwärtskompatibel
- `PATCH` — Bugfixes

### Release-Ablauf

```bash
# 1. Version in Directory.Build.props erhöhen, via PR auf main mergen
# 2. Tag setzen und pushen:
git tag -a v0.2.0 -m "v0.2.0"
git push origin v0.2.0
```

Der Tag triggert `release.yml`: Build + Tests → Self-contained-Binaries für
3 Plattformen → GitHub Release mit automatisch generierten Release Notes
(kategorisiert über `.github/release.yml`). Schlägt ein Schritt fehl, entsteht
kein Release — Tags zeigen nur auf verifizierte Stände.

### Rollback

Releases sind unveränderlich. Bei einem kaputten Release: Fix auf `main`,
neues PATCH-Release. Kein Überschreiben von Tags.

## 5. Qualitäts-Gates: SonarCloud + CodeQL

- **SonarCloud** (SaaS-SonarQube, kostenlos für Public Repos): Bugs, Code Smells,
  Security Hotspots, Coverage, Duplication. Quality Gate „Sonar way": Neue
  Bugs/Vulnerabilities = 0, Coverage auf neuem Code ≥ 80 %.
  *Ehrliche Einschränkung:* Sonars .NET-Analyzer deckt C#/VB ab — für F# importiert
  er Coverage, aber keine tiefe Regelanalyse. Sobald C#-IO-Adapter entstehen,
  greift Sonar dort voll; F#-Qualität sichern primär Typsystem + Tests + `-warnaserror`.
- **CodeQL** (`codeql.yml`): scannt die GitHub-Actions-Workflows selbst auf
  Injection-/Supply-Chain-Risiken (F#-Quellcode unterstützt CodeQL nicht).
  Befunde erscheinen unter *Security → Code scanning*.
- **Dependabot Alerts + Security Updates**: automatische CVE-Meldungen und Fix-PRs.
- **gitleaks** (`gitleaks.yml`): Secret-Scanning über die gesamte Git-Historie bei
  jedem Push/PR + wöchentlich — verhindert geleakte Tokens/Keys im öffentlichen Repo.
  Zusätzlich Privatsphäre-Regel: keine personenbezogenen Daten, lokalen Pfade oder
  internen Hostnamen in Commits, Issues oder `.spot/`-Beispieldaten.

## 6. GitHub-Features: Wofür nutzen wir was?

| Tab | Nutzung |
|---|---|
| **Code** | Quellcode, README als Einstieg, `AGENTS.md` für Coding-Agents |
| **Issues** | Bug/Feature-Templates (`.github/ISSUE_TEMPLATE/`), Labels nach Conventional-Commit-Typen |
| **Pull requests** | Einziger Weg auf `main`; Template + Required Checks + Squash |
| **Agents** | `AGENTS.md` gibt Copilot/Claude & Co. Build-, Test- und Stilregeln |
| **Actions** | `ci.yml` (Build/Test/Smoke/Sonar), `release.yml` (Tag→Release), `codeql.yml` |
| **Projects** | Ein Board „CDD Roadmap" (Backlog → In Progress → Done), Issues als Items |
| **Wiki** | Konzept-/Architektur-Notizen, die schneller iterieren als Code-Doku |
| **Security** | Private Vulnerability Reporting (SECURITY.md), CodeQL, Dependabot |
| **Insights** | Community Standards ✔ (alle Health-Files vorhanden), Pulse/Contributors |
| **Discussions** | Offene Fragen & Konzept-Diskussionen (Issue-Template verlinkt dorthin) |

## 7. Dependency-Management

`.github/dependabot.yml`: wöchentliche Updates für NuGet-Pakete und
GitHub Actions, jeweils als PR durch die normale CI-Pipeline.
`dependabot-automerge.yml` mergt minor/patch-Updates automatisch, sobald die
Pflicht-Checks grün sind — Major-Updates bleiben offen und brauchen ein
menschliches Review.

## 8. Einmaliges manuelles Setup (GitHub-Web-UI)

Diese Schritte kann nur der Repo-Owner ausführen:

1. **Default-Branch auf `main`** stellen:
   *Settings → General → Default branch* → `main`, danach `master` löschen
   (`git push origin --delete master`)
2. **Branch Protection** für `main`:
   *Settings → Branches → Add branch ruleset* (oder classic protection rule):
   - Require a pull request before merging (Approvals: 0, solange solo)
   - Require status checks to pass: `build-test`, `sonar`
   - Require linear history ✔, Force pushes ✖, Deletions ✖
3. **SonarCloud** verbinden:
   - Auf sonarcloud.io mit GitHub einloggen → Organisation `koschnag` importieren
   - Projekt `Koschnag_cong-driven-development` anlegen (Analysis method: **CI-based**, nicht Automatic — sonst kollidiert es mit dem Scanner in der Pipeline)
   - Token erzeugen → als Repo-Secret `SONAR_TOKEN` hinterlegen
     (*Settings → Secrets and variables → Actions*)
4. **Merge-Methoden einschränken:** *Settings → General → Pull Requests* →
   nur „Allow squash merging" aktivieren
5. **Features aktivieren:** *Settings → General → Features* →
   Issues ✔, Discussions ✔, Projects ✔, Wiki ✔
6. **Security aktivieren:** *Settings → Advanced Security* →
   Private vulnerability reporting ✔, Dependabot alerts ✔,
   Dependabot security updates ✔ (Code scanning kommt via `codeql.yml` automatisch)
7. **Projects-Board anlegen:** *Projects → New project* → Board „CDD Roadmap"
   mit Spalten Backlog / In Progress / Done; Roadmap-Punkte aus dem README
   als Issues anlegen und verknüpfen
8. **Wiki-Startseite:** einmal *Wiki → Create the first page* klicken
   (danach ist das Wiki auch per `git clone …wiki.git` beschreibbar)
9. **Labels prüfen:** *Issues → Labels* — Standard-Labels reichen
   (`bug`, `enhancement`, `dependencies`); zusätzlich `chore` anlegen,
   damit die Release-Notes-Kategorien (`.github/release.yml`) greifen
