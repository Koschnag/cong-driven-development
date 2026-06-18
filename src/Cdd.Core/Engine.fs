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
    open Cdd.Core.Spot

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
          ApiKey         : string        // Mistral: Bearer-Token
          SystemPrompt   : string }      // "" = congOsIdentity; sonst Spezial-Identität (z. B. Schmiede)

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
        // CWD-unabhängig: Pfad zu Cdd.Mcp ABSOLUT ab dem Cockpit-Assembly (…/src/Cdd.Web/bin/Release/net9.0/),
        // sonst startet der MCP-Server je nach WorkingDirectory nicht und zeigt auf den falschen --root.
        let baseDir = System.AppContext.BaseDirectory
        let up n = System.IO.Path.GetFullPath(System.IO.Path.Combine(Array.append [| baseDir |] (Array.create n "..")))
        let mcpDir = System.IO.Path.Combine(up 4, "Cdd.Mcp")                      // …/src/Cdd.Mcp
        let mcpDll = System.IO.Path.Combine(mcpDir, "bin", "Release", "net9.0", "Cdd.Mcp.dll")
        let argList =
            if System.IO.File.Exists mcpDll then [ mcpDll; "--root"; root ]       // gebaut → DLL direkt (schnell)
            else [ "run"; "--project"; mcpDir; "--"; "--root"; root ]             // Fallback: dotnet run
        let args = JsonArray()
        for a in argList do args.Add(JsonValue.Create(a))
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

    /// Cong-OS-Identität: die Engine weiß im Chat, was sie ist, worauf sie steht und wie sie arbeitet.
    /// Wird per --append-system-prompt an Claude Codes Default-System-Prompt gehängt (ersetzt ihn nicht).
    let congOsIdentity = """Du bist die Engine von Cong OS — einem souveränen, chat-primären KI-Cockpit von Cong (Cong Chanh Vinzenz Nguyen, .NET-Architekt). Du läufst headless im EINEN Gesprächsfaden des Cockpits; deine Tool-Calls erscheinen dem Nutzer als Karten, nicht in einem Terminal.

WAS CONG OS IST (ein Axiom): Ein SPOT-Modell (Single Point of Truth) ist die Wahrheit — ein getypter Graph aus F#-Discriminated-Unions. Jeder Knoten (Spec, Test, Term, Decision, Component, Invariant, Premise, Risk, Knowledge, Tool) trägt einen Konvergenz-Status (Aligned/Pending/Diverged/Orphaned) und Relationen (RelatesTo/DependsOn/SpecRef/Supersedes). Persistenz: ein JSON-File pro Knoten unter .spot/ (git-diffbar, serverlos). Alles andere — Plan, Dev, Infra, Prod, Doku — ist eine Projektion dieses einen Modells.

STACK: F# für Logik, C# für IO, .NET für alles. KEIN Python, nie. Engines: du (Claude Code) primär, Mistral-EU als Backup, lokales Ollama/Qwen für Offline-Souveränität. Das Modell erreichst du über die SPOT-MCP-Tools (mcp__spot__*) — damit liest/schreibst/löschst du Knoten, statt Dateien zu raten. Perspektivisch administrierst du auch Congs Data Center (Pi=Infra, Celsius=Services, Tower=Proxmox) und Produktion über MCP-Backends (Komodo/Coolify) — heute noch Adopt-Punkte.

ARBEITSWEISE (Congs Stil): direkt, knapp, keine Floskeln, keine Zusammenfassungen am Ende. Entscheidungen treffen statt rückfragen; zwischen zwei Lösungen die einfachere. Deutsch für Diskussion/Strategie, Englisch für Code/Commits (Conventional Commits). Der SPOT-Kontext unten ist dein Ground Truth — divergiert er vom Code, benenne das als 'Diverged'-Befund."""

    /// Schmiede-Identität: zerlegt EINE Prosa-Spielidee in Pending-Spec-Knoten — NUR Modell, KEIN Code.
    /// Die Tool-Allowlist (nur mcp__spot__*) erzwingt das strukturell; dieser Prompt gibt die Form vor.
    let schmiedeIdentity = """Du bist die SCHMIEDE von Cong OS — ein PROJEKT-AGNOSTISCHES Werkzeug. Du weißt vorab NICHTS über die Domäne des Projekts (Spiel oder nicht). Deine EINZIGE Aufgabe: EINE Prosa-Idee in 3–6 Pending-Spec-Knoten des SPOT-Modells zerlegen — ausschließlich über mcp__spot__upsert. KEIN Code, KEINE Tests, KEINE Datei-Edits/Bash (du hast die Schreib-Werkzeuge gar nicht). Du SCHREIBST NUR das WAS, der Loop baut später das WIE.

ZUERST die Domäne DIESES Projekts lernen (nicht raten, nicht erfinden):
- Lies das Modell: mcp__spot__list + mcp__spot__get. Die `Term`-Knoten SIND die ubiquitäre Sprache (verbindliche Begriffe); die bestehenden `Spec`-Knoten zeigen Stil + Granularität; die `Component`-Knoten die Architektur; die `Invariant`-Knoten die Regeln, die nie verletzt werden dürfen.
- Bei Bedarf den Code nur LESEND (Read/Grep/Glob), um die echten Domänen-Typen/Funktionen exakt zu treffen.
- Benutze NUR Begriffe, die das Projekt kennt. Erfinde keine Welt, die nicht existiert.

REGELN je Spec-Knoten:
- Convergence = "Pending" (nie Aligned — du misst nichts, du beschreibst nur).
- Id: Präfix `spec-`, kebab-case, kollisionsfrei (vorher mcp__spot__list prüfen).
- Title kurz. Intent: das WARUM (warum sinnvoll/wertvoll im Projekt-Kontext).
- Criteria: 2–4 GIVEN/WHEN/THEN, jedes testbar als REINE Funktion über die Domänen-Typen DIESES Projekts. KEINE Render-/Audio-/Maus-/Zeit-/IO-Kriterien — nur deterministische Zustandsübergänge.

LOGIK vs ASSET (die ehrliche Grenze):
- Deterministische Regel / Zustand / Berechnung → ein normaler Spec-Knoten (der Loop kann ihn bauen + testen).
- Kunst / 3D / Audio / Look-and-Feel / Balance-Gefühl → ein Knoten mit Title-Präfix "[ASSET] ", als Platzhalter markiert, den DU NIEMALS als erfüllbar ausgibst (kein Test kann „sieht gut aus" prüfen). Diese werden NICHT konvergiert.

Vor dem Abschluss: mcp__spot__validate, Konflikte mit bestehenden Invarianten benennen. Wenige scharfe Specs > viele vage. Antworte am Ende mit einer knappen Liste der angelegten Knoten-Ids."""

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
                    addPair "--append-system-prompt" (if o.SystemPrompt <> "" then o.SystemPrompt else congOsIdentity)
                    if o.Model <> "" then addPair "--model" o.Model
                    if o.PermissionMode <> "" then addPair "--permission-mode" o.PermissionMode
                    if not (List.isEmpty o.AllowedTools) then addPair "--allowedTools" (String.concat "," o.AllowedTools)
                    if o.McpConfigJson <> "" then
                        addPair "--mcp-config" o.McpConfigJson
                        add "--strict-mcp-config"   // NUR unser SPOT-Server (ignoriert fehlkonfigurierte Repo/Global-.mcp.json)
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

    // ---- In-Process-SPOT-Tools für die OpenAiCompat-Engine (kein MCP-Subprozess nötig) ----
    // Dieselben Operationen wie der Cdd.Mcp-Server, direkt auf Cdd.Core. Damit TREIBEN Mistral/Ollama
    // den SPOT (lesen/schreiben/validieren/ableiten), statt nur darüber zu chatten — der souveräne
    // Fallback wird agentisch. Der volle Export ist on-demand (spot_export_context), passend zum Slice.
    let private spotToolDefs () : JsonArray =
        let arr = JsonArray()
        let tool (name: string) (desc: string) (props: (string * string * string) list) (required: string list) =
            let propsObj = JsonObject()
            for (pn, pt, pd) in props do
                let po = JsonObject()
                po.["type"] <- JsonValue.Create(pt)
                po.["description"] <- JsonValue.Create(pd)
                propsObj.[pn] <- po
            let parameters = JsonObject()
            parameters.["type"] <- JsonValue.Create("object")
            parameters.["properties"] <- propsObj
            let reqArr = JsonArray()
            for r in required do reqArr.Add(JsonValue.Create(r))
            parameters.["required"] <- reqArr
            let f = JsonObject()
            f.["name"] <- JsonValue.Create(name)
            f.["description"] <- JsonValue.Create(desc)
            f.["parameters"] <- parameters
            let t = JsonObject()
            t.["type"] <- JsonValue.Create("function")
            t.["function"] <- f
            arr.Add(t)
        tool "spot_list" "Listet alle SPOT-Knoten (Id, Art, Konvergenz)." [] []
        tool "spot_get" "Liefert einen SPOT-Knoten als JSON." [ "id", "string", "Knoten-Id, z. B. term-spot" ] [ "id" ]
        tool "spot_validate" "Validiert das Modell inkl. aller Invarianten (Governance)." [] []
        tool "spot_export_context" "Exportiert den GANZEN SPOT als Markdown — volle Tiefe auf Abruf." [] []
        tool "spot_derive_tests" "Leitet aus Spec-Akzeptanzkriterien Test-Knoten ab (idempotent) und persistiert sie." [] []
        tool "spot_upsert" "Legt einen SPOT-Knoten an oder überschreibt ihn. Erwartet das VOLLSTÄNDIGE Knoten-JSON: {\"Id\":\"...\",\"Payload\":{\"Case\":\"SpecNode\",\"Fields\":{\"Item\":{...}}},\"Convergence\":\"Pending\"}." [ "nodeJson", "string", "Vollständiger Knoten als JSON" ] [ "nodeJson" ]
        tool "spot_delete" "Löscht einen SPOT-Knoten." [ "id", "string", "Knoten-Id" ] [ "id" ]
        arr

    let private execSpotTool (root: string) (name: string) (argsJson: string) : string =
        let arg (n: string) =
            try
                use d = JsonDocument.Parse(if String.IsNullOrWhiteSpace argsJson then "{}" else argsJson)
                match d.RootElement.TryGetProperty n with
                | true, v when v.ValueKind = JsonValueKind.String -> v.GetString()
                | true, v -> v.GetRawText()
                | _ -> ""
            with _ -> ""
        try
            match name with
            | "spot_list" ->
                Store.load root
                |> List.map (fun e -> sprintf "%s (%s, %A)" (idValue e.Id) (kindOf e) e.Convergence)
                |> String.concat "\n"
            | "spot_get" ->
                let id = arg "id"
                match Store.load root |> List.tryFind (fun e -> idValue e.Id = id) with
                | Some e -> Json.serialize e
                | None -> sprintf "Kein Knoten '%s'." id
            | "spot_validate" ->
                let fs = Validate.validate (Store.load root)
                if List.isEmpty fs then "Modell valide — keine Befunde."
                else fs |> List.map (fun f -> sprintf "[%A] %s: %s" f.Severity (idValue f.EntityId) f.Message) |> String.concat "\n"
            | "spot_export_context" -> Export.toMarkdown (Store.load root)
            | "spot_derive_tests" ->
                let derived = Derive.deriveTests (Store.load root)
                derived |> List.iter (Store.save root)
                sprintf "%d Test-Knoten abgeleitet und persistiert." (List.length derived)
            | "spot_upsert" ->
                let nodeJson = arg "nodeJson"
                try
                    let entry = Json.deserialize<SpotEntry> nodeJson
                    if not (Store.isValidId entry.Id) then "Ungültige Id (erlaubt: a-zA-Z0-9_-)."
                    else Store.save root entry; sprintf "Gespeichert: %s" (idValue entry.Id)
                with ex -> sprintf "JSON ungültig: %s" ex.Message
            | "spot_delete" ->
                let id = arg "id"
                if Store.delete root (EntityId id) then sprintf "Gelöscht: %s" id else sprintf "Nicht gefunden: %s" id
            | other -> sprintf "Unbekanntes Tool: %s" other
        with ex -> sprintf "Tool-Fehler (%s): %s" name ex.Message

    /// Mistral-EU-API oder lokales Ollama (OpenAI-kompatibel) — jetzt AGENTISCH: ein echter Tool-Loop
    /// gegen die In-Process-SPOT-Tools (parse tool_calls → ausführen → re-feed bis fertig, Iter-Cap).
    /// Damit ist die souveräne Fallback-Kette (Mistral-EU → Ollama) ein vollwertiger SPOT-Treiber,
    /// nicht nur ein Chat. Nicht-streamend (ein POST je Runde); jede Runde emittiert Text/Tool/Result.
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

                    // Kanonische, losgelöste Nachrichtenliste (wird je Runde in den Body geklont).
                    let msgs = JsonArray()
                    let sysMsg = JsonObject()
                    sysMsg.["role"] <- JsonValue.Create("system")
                    let identity = if o.SystemPrompt <> "" then o.SystemPrompt else congOsIdentity
                    sysMsg.["content"] <- JsonValue.Create(sprintf "%s\n\n=== KONTEXT (SPOT, Slice — volle Tiefe via spot_export_context) ===\n%s" identity req.ContextMd)
                    msgs.Add(sysMsg)
                    let usrMsg = JsonObject()
                    usrMsg.["role"] <- JsonValue.Create("user")
                    usrMsg.["content"] <- JsonValue.Create(req.Prompt)
                    msgs.Add(usrMsg)
                    let tools = spotToolDefs ()

                    let maxIter = 8
                    let mutable iter = 0
                    let mutable go = true
                    try
                        while go && iter < maxIter do
                            iter <- iter + 1
                            let body = JsonObject()
                            body.["model"] <- JsonValue.Create(model)
                            body.["messages"] <- msgs.DeepClone()
                            body.["tools"] <- tools.DeepClone()
                            body.["tool_choice"] <- JsonValue.Create("auto")
                            use content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json")
                            let! resp = http.PostAsync(baseUrl + "/chat/completions", content)
                            let! raw = resp.Content.ReadAsStringAsync()
                            if not resp.IsSuccessStatusCode then
                                do! emit (EngineError(sprintf "HTTP %d: %s" (int resp.StatusCode) (raw.Substring(0, min 500 raw.Length))))
                                go <- false
                            else
                                use doc = JsonDocument.Parse(raw)
                                let message = doc.RootElement.GetProperty("choices").[0].GetProperty("message")
                                let text =
                                    match message.TryGetProperty "content" with
                                    | true, c when c.ValueKind = JsonValueKind.String -> c.GetString()
                                    | _ -> ""
                                if text <> "" then do! emit (Text text)
                                let toolCalls =
                                    match message.TryGetProperty "tool_calls" with
                                    | true, tc when tc.ValueKind = JsonValueKind.Array && tc.GetArrayLength() > 0 -> Some tc
                                    | _ -> None
                                match toolCalls with
                                | None ->
                                    do! emit (Completed(text, 0.0))
                                    go <- false
                                | Some tc ->
                                    // Die Assistant-Nachricht (inkl. tool_calls) muss unverändert zurück in den Verlauf.
                                    match JsonNode.Parse(message.GetRawText()) with
                                    | null -> ()
                                    | node -> msgs.Add(node)
                                    for call in tc.EnumerateArray() do
                                        let callId = getStr call "id"
                                        let fn = call.GetProperty("function")
                                        let tname = getStr fn "name"
                                        let targs = getStr fn "arguments"   // OpenAI: arguments ist ein JSON-String
                                        do! emit (ToolUse(tname, targs))
                                        let result = execSpotTool o.Cwd tname targs
                                        do! emit (ToolResult result)
                                        let toolMsg = JsonObject()
                                        toolMsg.["role"] <- JsonValue.Create("tool")
                                        toolMsg.["tool_call_id"] <- JsonValue.Create(callId)
                                        toolMsg.["content"] <- JsonValue.Create(result)
                                        msgs.Add(toolMsg)
                        if go then
                            do! emit (Completed(sprintf "(Iterationslimit %d erreicht — Loop beendet)" maxIter, 0.0))
                    with ex ->
                        do! emit (EngineError ex.Message)
                }

    /// Fabrik: liefert die Engine zum gewählten Kind (gleicher IEngine-Vertrag).
    let create (kind: EngineKind) : IEngine =
        match kind with
        | ClaudeCode -> ClaudeCodeEngine() :> IEngine
        | Mistral    -> OpenAiCompatEngine("mistral", "https://api.mistral.ai/v1", "mistral-large-latest") :> IEngine
        | Ollama     -> OpenAiCompatEngine("ollama", "http://localhost:11434/v1", "qwen2.5:3b") :> IEngine

    /// Generischer OpenAI-kompatibler Provider (beliebiges Label) — BaseUrl/Model/ApiKey kommen
    /// vollständig aus den EngineOptions (Laufzeit-Provider). Derselbe agentische Tool-Loop.
    let openAiCompat (label: string) : IEngine = OpenAiCompatEngine(label, "", "") :> IEngine

    let kindOfString (s: string) =
        match (if isNull s then "" else s).ToLowerInvariant() with
        | "mistral" -> Mistral
        | "ollama"  -> Ollama
        | _         -> ClaudeCode
