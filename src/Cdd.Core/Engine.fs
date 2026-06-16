namespace Cdd.Core

/// Engine-Schicht: CDD liefert Struktur + Skelett + GUI + Entropie (den SPOT-Kontext);
/// die LLM-Engine läuft auf TERMINAL-Ebene im Hintergrund. Pluggable über IEngine:
///  - ClaudeCodeEngine  → treibt `claude --print --output-format stream-json` als Subprozess
///  - OpenAiCompatEngine → Mistral-EU-API oder lokales Ollama (OpenAI-kompatibel), kein Python
module Engine =

    open System
    open System.Diagnostics
    open System.Net.Http
    open System.Text
    open System.Text.Json
    open System.Text.Json.Nodes
    open System.Threading.Tasks

    /// Welche Engine die Anfrage ausführt.
    type EngineKind =
        | ClaudeCode
        | Mistral
        | Ollama

    /// Ereignisse, die jede Engine in denselben Vertrag normalisiert (für GUI + Pluggability).
    type EngineEvent =
        | Started     of session: string * model: string
        | Text        of string
        | ToolUse     of name: string * input: string
        | ToolResult  of string
        | Completed   of result: string * costUsd: float
        | EngineError of string
        | Raw         of string

    type EngineOptions =
        { Kind           : EngineKind
          Model          : string        // "" = Engine-Default
          Cwd            : string        // Arbeitsverzeichnis (Repo-Root)
          AllowedTools   : string list   // Claude: --allowedTools (unbeaufsichtigt → eng halten)
          PermissionMode : string        // Claude: acceptEdits | plan | default | dontAsk | …
          McpConfigJson  : string        // Claude: --mcp-config Payload (verbindet die SPOT-Tools)
          BaseUrl        : string        // Mistral/Ollama: OpenAI-kompatibler Endpoint
          ApiKey         : string }      // Mistral: Bearer-Token

    type EngineRequest =
        { Prompt    : string
          ContextMd : string             // SPOT-Export = die "Entropie", die die Engine bekommt
          Options   : EngineOptions }

    /// Ein Vertrag, beliebig viele Engines. Push-Modell: die Engine ruft `emit` pro Ereignis.
    type IEngine =
        abstract member Run: EngineRequest * (EngineEvent -> Task) -> Task

    // ---- GUI-freundliche JSON-Kodierung (ein flaches Objekt je Ereignis, robust escaped) ----
    let toGuiJson (ev: EngineEvent) : string =
        let o = JsonObject()
        let s (k: string) (v: string) = o.[k] <- JsonValue.Create(v)
        match ev with
        | Started(ses, m)  -> s "t" "started"; s "session" ses; s "model" m
        | Text t           -> s "t" "text"; s "text" t
        | ToolUse(n, i)    -> s "t" "tool"; s "name" n; s "input" i
        | ToolResult t     -> s "t" "toolresult"; s "text" t
        | Completed(r, c)  -> s "t" "done"; s "result" r; o.["cost"] <- JsonValue.Create(c)
        | EngineError e    -> s "t" "error"; s "error" e
        | Raw l            -> s "t" "raw"; s "line" l
        o.ToJsonString()

    /// Standard-MCP-Config: verbindet die Engine mit CDDs eigenem SPOT-MCP-Server,
    /// sodass die KI während des Laufs den SPOT lesen/mutieren kann (mcp__spot__*).
    let spotMcpConfig (root: string) : string =
        let args = JsonArray()
        for a in [ "run"; "--project"; "src/Cdd.Mcp"; "--"; "--root"; root ] do args.Add(JsonValue.Create(a))
        let server = JsonObject()
        server.["command"] <- JsonValue.Create("dotnet")
        server.["args"] <- args
        let servers = JsonObject()
        servers.["spot"] <- server
        let rootObj = JsonObject()
        rootObj.["mcpServers"] <- servers
        rootObj.ToJsonString()

    // ---- Parser für `claude --print --output-format stream-json` (eine JSON-Zeile je Ereignis) ----
    let private getStr (el: JsonElement) (name: string) =
        match el.TryGetProperty name with
        | true, v when v.ValueKind = JsonValueKind.String -> v.GetString()
        | _ -> ""

    let private parseClaudeLine (line: string) : EngineEvent list =
        try
            use doc = JsonDocument.Parse(line)
            let root = doc.RootElement
            match getStr root "type" with
            | "system" ->
                if getStr root "subtype" = "init"
                then [ Started(getStr root "session_id", getStr root "model") ]
                else []
            | "assistant" ->
                match root.TryGetProperty "message" with
                | true, msg ->
                    match msg.TryGetProperty "content" with
                    | true, content when content.ValueKind = JsonValueKind.Array ->
                        [ for item in content.EnumerateArray() do
                            match getStr item "type" with
                            | "text" -> yield Text(getStr item "text")
                            | "tool_use" ->
                                let inp =
                                    match item.TryGetProperty "input" with
                                    | true, i -> i.GetRawText()
                                    | _ -> "{}"
                                yield ToolUse(getStr item "name", inp)
                            | _ -> () ]
                    | _ -> []
                | _ -> []
            | "user" ->
                match root.TryGetProperty "message" with
                | true, msg ->
                    match msg.TryGetProperty "content" with
                    | true, content when content.ValueKind = JsonValueKind.Array ->
                        [ for item in content.EnumerateArray() do
                            if getStr item "type" = "tool_result" then
                                let c =
                                    match item.TryGetProperty "content" with
                                    | true, cc when cc.ValueKind = JsonValueKind.String -> cc.GetString()
                                    | true, cc -> cc.GetRawText()
                                    | _ -> ""
                                yield ToolResult c ]
                    | _ -> []
                | _ -> []
            | "stream_event" ->
                match root.TryGetProperty "event" with
                | true, ev ->
                    match ev.TryGetProperty "delta" with
                    | true, delta when getStr delta "type" = "text_delta" -> [ Text(getStr delta "text") ]
                    | _ -> []
                | _ -> []
            | "result" ->
                let cost =
                    match root.TryGetProperty "total_cost_usd" with
                    | true, c when c.ValueKind = JsonValueKind.Number -> c.GetDouble()
                    | _ -> 0.0
                [ Completed(getStr root "result", cost) ]
            | _ -> [ Raw line ]
        with _ -> [ Raw line ]

    /// Treibt Claude Code headless auf Terminal-Ebene: spawnt `claude`, schreibt Kontext+Prompt
    /// über stdin (vermeidet ARG_MAX) und streamt stdout-Zeilen als EngineEvent.
    type ClaudeCodeEngine() =
        interface IEngine with
            member _.Run(req, emit) =
                task {
                    let o = req.Options
                    let psi = ProcessStartInfo("claude")
                    psi.RedirectStandardInput <- true
                    psi.RedirectStandardOutput <- true
                    psi.RedirectStandardError <- true
                    psi.UseShellExecute <- false
                    if o.Cwd <> "" then psi.WorkingDirectory <- o.Cwd
                    let add (s: string) = psi.ArgumentList.Add(s)
                    let addPair (f: string) (v: string) = (add f; add v)
                    add "--print"
                    addPair "--output-format" "stream-json"
                    add "--verbose"
                    if o.Model <> "" then addPair "--model" o.Model
                    if o.PermissionMode <> "" then addPair "--permission-mode" o.PermissionMode
                    if not (List.isEmpty o.AllowedTools) then addPair "--allowedTools" (String.concat "," o.AllowedTools)
                    if o.McpConfigJson <> "" then addPair "--mcp-config" o.McpConfigJson
                    if o.Cwd <> "" then addPair "--add-dir" o.Cwd

                    use proc = new Process()
                    proc.StartInfo <- psi
                    let mutable started = false
                    try
                        started <- proc.Start()
                    with ex ->
                        do! emit (EngineError(sprintf "`claude` nicht startbar (im PATH? eingeloggt?): %s" ex.Message))
                    if started then
                        let errTask = proc.StandardError.ReadToEndAsync()
                        let fullPrompt =
                            sprintf "=== KONTEXT (SPOT) ===\n%s\n\n=== AUFGABE ===\n%s" req.ContextMd req.Prompt
                        do! proc.StandardInput.WriteAsync(fullPrompt)
                        proc.StandardInput.Close()
                        let mutable go = true
                        while go do
                            let! line = proc.StandardOutput.ReadLineAsync()
                            if isNull line then go <- false
                            else for ev in parseClaudeLine line do do! emit ev
                        let! err = errTask
                        do! proc.WaitForExitAsync()
                        if proc.ExitCode <> 0 && err.Trim() <> "" then
                            do! emit (EngineError(err.Trim()))
                }

    /// Mistral-EU-API oder lokales Ollama (OpenAI-kompatibel). MVP: ein Chat-Completion,
    /// gut für PLAN/IDEATE (Chat über den SPOT). Der agentische Tool-Loop folgt später.
    type private OpenAiCompatEngine(label: string, defaultBaseUrl: string, defaultModel: string) =
        interface IEngine with
            member _.Run(req, emit) =
                task {
                    let o = req.Options
                    let baseUrl = (if o.BaseUrl <> "" then o.BaseUrl else defaultBaseUrl).TrimEnd('/')
                    let model = if o.Model <> "" then o.Model else defaultModel
                    do! emit (Started("", sprintf "%s:%s" label model))
                    use http = new HttpClient()
                    http.Timeout <- TimeSpan.FromMinutes(5.0)
                    if o.ApiKey <> "" then
                        http.DefaultRequestHeaders.Authorization <-
                            System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", o.ApiKey)
                    let msg (role: string) (content: string) =
                        let m = JsonObject()
                        m.["role"] <- JsonValue.Create(role)
                        m.["content"] <- JsonValue.Create(content)
                        m :> JsonNode
                    let msgs = JsonArray()
                    msgs.Add(msg "system" (sprintf "Du arbeitest am SPOT-Kontext von CDD (cong-driven-development). Antworte präzise und auf Deutsch.\n\n%s" req.ContextMd))
                    msgs.Add(msg "user" req.Prompt)
                    let body = JsonObject()
                    body.["model"] <- JsonValue.Create(model)
                    body.["messages"] <- msgs
                    use content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json")
                    try
                        let! resp = http.PostAsync(baseUrl + "/chat/completions", content)
                        let! raw = resp.Content.ReadAsStringAsync()
                        if not resp.IsSuccessStatusCode then
                            do! emit (EngineError(sprintf "HTTP %d: %s" (int resp.StatusCode) (raw.Substring(0, min 400 raw.Length))))
                        else
                            let text =
                                try
                                    use doc = JsonDocument.Parse(raw)
                                    let choices = doc.RootElement.GetProperty("choices")
                                    choices.[0].GetProperty("message").GetProperty("content").GetString()
                                with _ -> raw
                            do! emit (Text text)
                            do! emit (Completed(text, 0.0))
                    with ex ->
                        do! emit (EngineError ex.Message)
                }

    /// Fabrik: liefert die Engine zum gewählten Kind (gleicher IEngine-Vertrag).
    let create (kind: EngineKind) : IEngine =
        match kind with
        | ClaudeCode -> ClaudeCodeEngine() :> IEngine
        | Mistral    -> OpenAiCompatEngine("mistral", "https://api.mistral.ai/v1", "mistral-large-latest") :> IEngine
        | Ollama     -> OpenAiCompatEngine("ollama", "http://localhost:11434/v1", "qwen2.5:7b") :> IEngine

    let kindOfString (s: string) =
        match (if isNull s then "" else s).ToLowerInvariant() with
        | "mistral" -> Mistral
        | "ollama"  -> Ollama
        | _         -> ClaudeCode
