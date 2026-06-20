# Contributing

Schön, dass du beitragen willst! So kommst du rein:

## Setup (2 Minuten)

```bash
git clone git@github.com:Koschnag/cong-driven-development.git
cd cong-driven-development
dotnet build        # .NET 9 SDK erforderlich
dotnet test
```

Kein weiteres Tooling nötig. Die CLI lokal ausprobieren:

```bash
dotnet run --project src/Cdd.Cli -- init
dotnet run --project src/Cdd.Cli -- list
```

## Workflow

1. **Issue zuerst** — für alles außer Tippfehlern bitte ein Issue öffnen
   (Templates gibt es für Bug und Feature), damit wir die Richtung abstimmen,
   bevor Arbeit reinfließt.
2. **Branch** von `main`: `feature/<slug>`, `fix/<slug>` oder `chore/<slug>`.
3. **Commits** im [Conventional-Commits](https://www.conventionalcommits.org/)-Format
   (`feat: …`, `fix: …`).
4. **PR** gegen `main` — das Template führt dich durch. CI (Build, Tests,
   Smoke-Test, Sonar) muss grün sein, gemergt wird per Squash.

Details zur Git- und Release-Strategie: [docs/devops.md](docs/devops.md).

## Code-Richtlinien

- **F# first**: Domain-Logik in `Cdd.Core` als typsichere F#-Module
  (Discriminated Unions, keine Vererbungshierarchien). C# nur für IO-Adapter.
- **Kein Python.** (Projektprinzip, siehe README.)
- Jede Logik-Änderung kommt mit Tests in `tests/Cdd.Tests`.
- Öffentliche Funktionen bekommen `///`-Doc-Kommentare.

## Fragen?

Einfach ein Issue öffnen — auch für „Verstehe ich das richtig, dass …?"
