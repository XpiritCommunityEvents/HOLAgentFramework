#nullable enable
using System.Reflection;
using System.Text.Json.Nodes;
using ModelContextProtocol.Server;

namespace GloboTicket.McpUiPrototype;

// Throwaway MCP Apps experiment. Keep the original tools' schemas and safety annotations.
public static class PrototypeToolRegistration
{
    public static IEnumerable<McpServerTool> Create(Type type, IReadOnlyDictionary<string, string> resources)
    {
        foreach (var method in type.GetMethods())
        {
            if (method.GetCustomAttribute<McpServerToolAttribute>() is null) continue;
            var options = new McpServerToolCreateOptions();
            if (resources.TryGetValue(method.Name, out var uri))
                options.Meta = new JsonObject { ["ui"] = new JsonObject { ["resourceUri"] = uri } };
            yield return method.IsStatic
                ? McpServerTool.Create(method, target: null, options)
                : McpServerTool.Create(method, request =>
                    ActivatorUtilities.GetServiceOrCreateInstance(request.Services!, type), options);
        }
    }
}

[McpServerResourceType]
public sealed class PrototypeUiResources
{
    [McpServerResource(UriTemplate = "ui://globoticket/concerts", MimeType = "text/html;profile=mcp-app")]
    public static string Concerts() => Html();

    [McpServerResource(UriTemplate = "ui://globoticket/checkout", MimeType = "text/html;profile=mcp-app")]
    public static string Checkout() => Html();

    private static string Html()
    {
        using var stream = typeof(PrototypeUiResources).Assembly.GetManifestResourceStream(
            "GloboTicket.McpUiPrototype.tool-ui.html")
            ?? throw new InvalidOperationException("Build the MCP UI prototype with npm run build:chat.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
