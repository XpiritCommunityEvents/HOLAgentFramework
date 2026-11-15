using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace GloboTicket.Frontend.Services.AI;

internal static class McpUiPrototypeEndpoints
{
    internal static void MapMcpUiPrototype(this WebApplication app,
        IList<McpClientTool> catalogTools, McpClient catalog,
        IList<McpClientTool> orderingTools, McpClient ordering)
    {
        if (!app.Environment.IsDevelopment() ||
            !app.Configuration.GetValue<bool>("McpUiPrototype:Enabled")) return;

        var tools = catalogTools.Select(tool => (Tool: tool.ProtocolTool, Client: catalog))
            .Concat(orderingTools.Select(tool => (Tool: tool.ProtocolTool, Client: ordering)))
            .Where(item => item.Tool.Meta?["ui"]?["resourceUri"] is not null).ToArray();
        app.MapGet("/mcp-ui-prototype/tools", () =>
            Results.Json(tools.Select(item => item.Tool), McpJsonUtilities.ProtocolOptions));
        app.MapGet("/mcp-ui-prototype/resource", async (string uri, CancellationToken token) =>
        {
            var source = tools.FirstOrDefault(item =>
                item.Tool.Meta?["ui"]?["resourceUri"]?.GetValue<string>() == uri);
            if (source.Client is null) return Results.NotFound();
            ReadResourceResult resource = await source.Client.ReadResourceAsync(uri, cancellationToken: token);
            return Results.Json(resource, McpJsonUtilities.ProtocolOptions);
        });
        app.MapGet("/mcp-ui-prototype/sandbox", (HttpContext context) =>
        {
            context.Response.Headers.ContentSecurityPolicy =
                "sandbox allow-scripts; default-src 'none'; script-src 'unsafe-inline'; style-src 'unsafe-inline'; frame-src 'self' about:; connect-src 'none'; form-action 'none'; base-uri 'none'";
            return Results.Content(File.ReadAllText(Path.Combine(app.Environment.WebRootPath,
                "js/mcp-ui-prototype-sandbox.html")), "text/html");
        });
    }
}
