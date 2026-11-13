using Aspire.Hosting.ApplicationModel;
using System.Text.Json;

namespace Aspire.Hosting;

public static class McpInspectorExtensions
{
    /// <summary>
    /// Adds the MCP Inspector developer UI for directly exploring the catalog and ordering
    /// servers' tools, resources, prompts, argument completion, and optional elicitation.
    /// Unlike DevUI, it tests MCP servers rather than the frontend agent.
    /// </summary>
    /// <remarks>
    /// Call only in run mode. Uses the official Inspector container with authentication enabled
    /// and a read-only server list generated from Aspire's container-network endpoints.
    /// The dashboard link uses localhost to match the Inspector's strict browser-origin allowlist.
    /// </remarks>
    public static IResourceBuilder<ContainerResource> AddMcpInspector(
        this IDistributedApplicationBuilder builder,
        string name,
        IResourceBuilder<ProjectResource> catalog,
        IResourceBuilder<ProjectResource> ordering)
    {
        var inspector = builder.AddContainer(name, "ghcr.io/modelcontextprotocol/inspector", "2.15.0")
            .WithHttpEndpoint(targetPort: 6274, name: "http")
            .WithContainerFiles("/inspector", (_, _) =>
                Task.FromResult<IEnumerable<ContainerFileSystemItem>>([
                    new ContainerFile
                    {
                        Name = "mcp.json",
                        Contents = JsonSerializer.Serialize(new
                        {
                            mcpServers = new
                            {
                                catalog = new
                                {
                                    type = "http",
                                    url = $"{catalog.GetEndpoint("http", KnownNetworkIdentifiers.DefaultAspireContainerNetwork).Url}/mcp"
                                },
                                ordering = new
                                {
                                    type = "http",
                                    url = $"{ordering.GetEndpoint("http", KnownNetworkIdentifiers.DefaultAspireContainerNetwork).Url}/mcp"
                                }
                            }
                        })
                    }
                ]))
            .WithArgs("--web", "--config", "/inspector/mcp.json")
            .WithHttpHealthCheck("/healthz")
            .WithUrls(context =>
            {
                context.Urls.Clear();
                context.Urls.Add(new()
                {
                    Url = context.GetEndpoint("http").Url,
                    DisplayText = "MCP Inspector"
                });
            })
            .WaitFor(catalog)
            .WaitFor(ordering);

        return inspector.WithEnvironment("ALLOWED_ORIGINS",
            inspector.GetEndpoint("http", KnownNetworkIdentifiers.LocalhostNetwork));
    }
}
