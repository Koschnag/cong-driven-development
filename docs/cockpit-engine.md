# Cockpit-Engine — CDD als IDE mit pluggbarer Headless-KI

CDD wird die **eine IDE/Cockpit**: du planst, ideatest, entwickelst, monitorst und
deployst — **du über die GUI, die KI headless auf Terminal-Ebene im Hintergrund.**

| Du / CDD liefern | Die LLM-Engine liefert |
|---|---|
| **Struktur + Skelett** — SPOT-Graph (`Cdd.Core`) | **Reasoning + Ausführung** |
| **GUI** — `Cdd.Web` (Cockpit) | Tool-Use (Datei-Edits, Bash, SPOT-Mutation) |
| **Entropie/Kontext** — `Export.toMarkdown` | |
| **Werkzeugkasten** — `Cdd.Mcp` (SPOT via MCP) | |

Engine **pluggable** (`IEngine`): **Claude Code** (Terminal) oder **Mistral / Ollama**
(OpenAI-kompatibel). **Kein Python.**

## Architektur

```
GUI (engine.html)  --POST /api/engine/run (text/event-stream)-->  Cdd.Web
   Cdd.Web assembliert Kontext (Export.toMarkdown) und ruft Engine.create kind : IEngine
     ClaudeCodeEngine: spawnt  claude --print --output-format stream-json --verbose
        --mcp-config {spot: dotnet run --project src/Cdd.Mcp -- --root .}
        --allowedTools "Read,Edit,Write,Grep,Glob,Bash(git *),Bash(dotnet *),mcp__spot__*"
        --permission-mode acceptEdits   (Prompt+Kontext via stdin)
     OpenAiCompatEngine: Mistral-EU-API / lokales Ollama
   EngineEvent (Started|Text|ToolUse|ToolResult|Completed|EngineError|Raw) --SSE--> Live-Konsole
```

**Auth:** Claude Code nutzt den Host-`claude`-Login (kein API-Key im Browser).
Mistral: `MISTRAL_API_KEY`. Ollama: lokal.

## Code (erster Schnitt)

- `src/Cdd.Core/Engine.fs` — `IEngine`, `EngineEvent`, `EngineOptions`, `ClaudeCodeEngine`
  (Subprozess + stream-json-Parser), `OpenAiCompatEngine`, `spotMcpConfig`, `toGuiJson`.
- `src/Cdd.Web/Program.fs` — `POST /api/engine/run` (Server-Sent-Events).
- `src/Cdd.Web/wwwroot/engine.html` — eigenständige Engine-Konsole.

## Fünf Flächen (Roadmap)

| Fläche | dispatcht an Engine | zurück |
|---|---|---|
| **Plan** | "lies SPOT, schlage Specs/Risks vor + lege an" (mcp__spot__upsert) | Knoten, validate |
| **Ideate** | Frage + SPOT-Kontext (Mistral/Ollama) | Chat-Antwort |
| **Develop** | Claude Code headless im Repo (Read/Edit/Bash) | Diff + Konvergenz |
| **Monitor** | validate + diff + (DC) dc-model.fsx über Mesh | Fehlerliste, Drift |
| **Deploy** | Claude Code mit eng-gegateten Bash-Tools | Deploy-Log |

## Betrieb & Sicherheit

- **Wo:** VM 120 `codespace`, an `tailscale0` gebunden -> nur eigene Mesh-Geraete. systemd, im Backup.
- **Blast-Radius:** beliebiger Code mit Host-`claude`-Auth -> enge Allowlist, permission-mode,
  Denials brechen ab. VM = Sandbox (getrennt von Vault/Mail-VM).
- **Nicht** in der gemieteten Cloud, **nicht** auf dem Mac.

## Verifiziert

`dotnet build` gruen; 33/33 Tests gruen; `POST /api/engine/run` streamt `started -> ... -> done`.

## Naechster Schnitt

Dock-Integration (`cockpit.js`), `--permission-prompt-tool` -> GUI-Approval, agentischer
Tool-Loop fuer Mistral/Ollama, `--resume` fuer mehrstufige Develop-Sessions.
