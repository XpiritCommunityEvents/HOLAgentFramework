using GloboTicket.Catalog.MCP;
using GloboTicket.Catalog.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace GloboTicket.UnitTests;

[TestClass]
public sealed class CatalogMcpContextTests
{
    [TestMethod]
    public async Task Discovery_prompt_completes_artists_and_links_readable_event_resources()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Services.AddSingleton<IEventRepository, EventRepository>();
        builder.Services.AddMcpServer().WithHttpTransport()
            .WithResources<CatalogContext>().WithPrompts<CatalogContext>()
            .WithCompleteHandler(CatalogContext.CompleteArtist);
        await using var app = builder.Build();
        app.MapMcp("/mcp");
        await app.StartAsync();
        try
        {
            string address = app.Services.GetRequiredService<IServer>().Features
                .Get<IServerAddressesFeature>()!.Addresses.Single();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            await using var client = await McpClient.CreateAsync(new HttpClientTransport(
                new HttpClientTransportOptions { Endpoint = new Uri(address + "/mcp") }), cancellationToken: timeout.Token);
            var prompts = await client.ListPromptsAsync(cancellationToken: timeout.Token);
            Assert.IsTrue(prompts.Any(p => p.Name == "discover_concerts"));
            var prompt = await client.GetPromptAsync("discover_concerts",
                new Dictionary<string, object?> { ["artist"] = "John Egbert", ["city"] = "Rotterdam", ["budgetUsd"] = "200" },
                cancellationToken: timeout.Token);
            StringAssert.Contains(((TextContentBlock)prompt.Messages.Single().Content).Text, "Rotterdam");
            var completion = await client.CompleteAsync(new CompleteRequestParams
            {
                Ref = new PromptReference { Name = "discover_concerts" },
                Argument = new() { Name = "artist", Value = "jo" }
            }, timeout.Token);
            CollectionAssert.Contains(completion.Completion.Values.ToArray(), "John Egbert");
            var resources = await client.ListResourceTemplatesAsync(cancellationToken: timeout.Token);
            Assert.IsTrue(resources.Any(r => r.UriTemplate == "catalog://events/{eventId}"));
            var result = await client.ReadResourceAsync(new ReadResourceRequestParams
            {
                Uri = "catalog://events/cfb88e29-4744-48c0-94fa-b25b92dea317"
            }, timeout.Token);
            StringAssert.Contains(((TextResourceContents)result.Contents.Single()).Text, "John Egbert");
        }
        finally { await app.StopAsync(); }
    }
}
