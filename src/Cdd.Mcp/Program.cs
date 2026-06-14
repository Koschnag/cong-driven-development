// CDD MCP-Server: macht den SPOT-Graphen für jeden MCP-Client (Claude Code,
// Claude Desktop, …) als Werkzeugkasten verfügbar. C# als dünner IO-Adapter
// über der F#-Domain (Cdd.Core) — gemäß Projektprämisse.
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.FSharp.Collections;
using ModelContextProtocol.Server;
using Cdd.Core;

var root = Environment.GetEnvironmentVariable("CDD_ROOT") ?? Directory.GetCurrentDirectory();
for (var i = 0; i < args.Length - 1; i++)
    if (args[i] == "--root") root = args[i + 1];
SpotTools.Root = Path.GetFullPath(root);

var builder = Host.CreateApplicationBuilder(args);
// stdio-Transport: stdout gehört dem Protokoll — Logs nur auf stderr.
builder.Logging.ClearProviders();
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();
await builder.Build().RunAsync();

[McpServerToolType]
public static class SpotTools
{
    public static string Root = ".";

    private static FSharpList<Spot.SpotEntry> Load() => Store.load(Root);

    [McpServerTool(Name = "spot_list")]
    [Description("Listet alle SPOT-Knoten mit Id, Art und Konvergenz-Status.")]
    public static string List()
    {
        var lines = Load().Select(e =>
            $"{Spot.idValue(e.Id)}  [{Spot.kindOf(e)}]  {e.Convergence}");
        return string.Join("\n", lines);
    }

    [McpServerTool(Name = "spot_get")]
    [Description("Liefert einen SPOT-Knoten als JSON.")]
    public static string Get([Description("Knoten-Id, z. B. term-spot")] string id)
    {
        var entry = Load().FirstOrDefault(e => Spot.idValue(e.Id) == id);
        return entry is null ? $"Fehler: Knoten '{id}' existiert nicht." : Json.serialize(entry);
    }

    [McpServerTool(Name = "spot_upsert")]
    [Description("Legt einen SPOT-Knoten an oder überschreibt ihn. Erwartet das vollständige Knoten-JSON " +
                 "({\"Id\":…,\"Payload\":{\"Case\":…,\"Fields\":{\"Item\":{…}}},\"Convergence\":…}). " +
                 "Antwortet mit den Validierungs-Befunden nach der Änderung.")]
    public static string Upsert([Description("Vollständiger Knoten als JSON")] string nodeJson)
    {
        Spot.SpotEntry entry;
        try
        {
            entry = Json.deserialize<Spot.SpotEntry>(nodeJson);
        }
        catch (Exception ex)
        {
            return $"Fehler: ungültiges Knoten-JSON ({ex.Message}).";
        }
        if (!Store.isValidId(entry.Id))
            return $"Fehler: ungültige Id '{Spot.idValue(entry.Id)}' (erlaubt: a-zA-Z0-9_-).";
        Store.save(Root, entry);
        return $"Gespeichert: {Spot.idValue(entry.Id)}\n{ValidationSummary()}";
    }

    [McpServerTool(Name = "spot_delete")]
    [Description("Löscht einen SPOT-Knoten.")]
    public static string Delete([Description("Knoten-Id")] string id) =>
        Store.delete(Root, Spot.EntityId.NewEntityId(id))
            ? $"Gelöscht: {id}\n{ValidationSummary()}"
            : $"Fehler: Knoten '{id}' existiert nicht.";

    [McpServerTool(Name = "spot_validate")]
    [Description("Validiert das Modell inklusive aller Invarianten (Governance).")]
    public static string ValidateModel() => ValidationSummary();

    [McpServerTool(Name = "spot_export_context")]
    [Description("Exportiert den gesamten SPOT-Graphen als Markdown-Kontextpaket " +
                 "(Ontologie, Prämissen, Invarianten, ADRs, Specs, Risiken, …).")]
    public static string ExportContext() => Export.toMarkdown(Load());

    [McpServerTool(Name = "spot_derive_tests")]
    [Description("Leitet aus den Spec-Akzeptanzkriterien Test-Knoten ab (idempotent) und persistiert sie.")]
    public static string DeriveTests()
    {
        var derived = Derive.deriveTests(Load()).ToList();
        foreach (var e in derived) Store.save(Root, e);
        return derived.Count == 0
            ? "Keine neuen Tests abzuleiten."
            : $"{derived.Count} Test-Knoten abgeleitet: {string.Join(", ", derived.Select(e => Spot.idValue(e.Id)))}";
    }

    [McpServerTool(Name = "spot_sync_code")]
    [Description("Round-Trip: gleicht Component-Knoten gegen die .fsproj-Referenzen unter <root>/src ab.")]
    public static string SyncCode()
    {
        var projects = Sync.scanRepo(Root);
        if (!projects.Any()) return "Keine .fsproj unter src/ gefunden.";
        var (results, _) = Sync.compare(projects, Load());
        return string.Join("\n", results.Select(r => $"{r.Status}  {Spot.idValue(r.Id)}  {r.Detail}"));
    }

    private static string ValidationSummary()
    {
        var findings = Validate.validate(Load()).ToList();
        if (findings.Count == 0) return "Validierung: ✓ keine Befunde.";
        var lines = findings.Select(f =>
            $"[{f.Severity}] {Spot.idValue(f.EntityId)}: {f.Message}");
        return "Validierung:\n" + string.Join("\n", lines);
    }
}
