using System.Runtime.CompilerServices;
using GloboTicket.Frontend.Services.AI;
using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.AI;

namespace GloboTicket.UnitTests;

[TestClass]
public sealed class FrontendAgentContractTests
{
    private static readonly Guid ConversationId =
        Guid.Parse("d88d528f-79dd-4636-a187-e3304ec329c6");

    [TestMethod]
    public async Task ConversationStore_reuses_an_owner_session_and_isolates_other_owners()
    {
        using var client = new RecordingChatClient();
        AIAgent agent = CreateAgent(client);
        var conversations = new ConversationStore();
        Guid firstOwner = Guid.Parse("deabf457-498c-4e84-913d-f459c24fa048");

        (ChatHub firstConnection, RecordingClientProxy firstClient) =
            CreateHub(agent, conversations, firstOwner);
        await firstConnection.SendMessage(ConversationId.ToString(), "owner one first turn");

        (ChatHub reconnected, _) = CreateHub(agent, conversations, firstOwner);
        await reconnected.SendMessage(ConversationId.ToString(), "owner one second turn");

        (ChatHub differentOwner, _) = CreateHub(
            agent,
            conversations,
            Guid.Parse("aed71e9d-7359-4a7f-a925-66598a2cca9d"));
        await differentOwner.SendMessage(ConversationId.ToString(), "owner two turn");

        Assert.HasCount(3, client.Requests);
        CollectionAssert.Contains(client.Requests[1], "owner one first turn");
        CollectionAssert.Contains(client.Requests[1], "reply-1");
        CollectionAssert.Contains(client.Requests[1], "owner one second turn");
        CollectionAssert.DoesNotContain(client.Requests[2], "owner one first turn");
        CollectionAssert.Contains(client.Requests[2], "owner two turn");
        CollectionAssert.AreEqual(
            new[] { "NewResponse", "ReceiveMessagePart", "ResponseDone" },
            firstClient.Messages.Select(message => message.Method).ToArray());
        Assert.AreEqual("reply-1", firstClient.Messages[1].Arguments[0]);
    }

    private static AIAgent CreateAgent(RecordingChatClient client) =>
        client.AsAIAgent(new ChatClientAgentOptions
        {
            ChatOptions = new ChatOptions
            {
                Tools = []
            }
        });

    private static (ChatHub Hub, RecordingClientProxy Client) CreateHub(
        AIAgent agent,
        ConversationStore conversations,
        Guid owner)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Cookie = $"GloboTicket.ChatOwner={owner:N}";
        var features = new FeatureCollection();
        features.Set<IHttpContextFeature>(new TestHttpContextFeature(httpContext));
        var client = new RecordingClientProxy();

        return (new ChatHub(agent, conversations)
        {
            Context = new TestHubCallerContext(features),
            Clients = new TestHubCallerClients(client)
        }, client);
    }

    private sealed class RecordingChatClient : IChatClient
    {
        private int invocationCount;

        internal List<string[]> Requests { get; } = [];

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The frontend uses the streaming agent path.");

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ChatMessage[] request = messages.Select(message => message.Clone()).ToArray();
            Requests.Add(request.Select(message => message.Text).ToArray());
            int invocation = Interlocked.Increment(ref invocationCount);

            await Task.Yield();
            yield return new ChatResponseUpdate(ChatRole.Assistant, $"reply-{invocation}");
        }

        public object? GetService(Type serviceType, object? serviceKey = null) =>
            serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

        public void Dispose()
        {
        }
    }

    private sealed class RecordingClientProxy : IClientProxy
    {
        internal List<(string Method, object?[] Arguments)> Messages { get; } = [];

        public Task SendCoreAsync(
            string method,
            object?[] args,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Messages.Add((method, args));
            return Task.CompletedTask;
        }
    }

    private sealed class TestHubCallerClients(IClientProxy client) : IHubCallerClients
    {
        public IClientProxy All => client;
        public IClientProxy Caller => client;
        public IClientProxy Others => client;
        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => client;
        public IClientProxy Client(string connectionId) => client;
        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => client;
        public IClientProxy Group(string groupName) => client;
        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => client;
        public IClientProxy Groups(IReadOnlyList<string> groupNames) => client;
        public IClientProxy OthersInGroup(string groupName) => client;
        public IClientProxy User(string userId) => client;
        public IClientProxy Users(IReadOnlyList<string> userIds) => client;
    }

    private sealed class TestHubCallerContext(IFeatureCollection features) : HubCallerContext
    {
        private readonly IDictionary<object, object?> items = new Dictionary<object, object?>();

        public override string ConnectionId => "test-connection";
        public override string? UserIdentifier => null;
        public override System.Security.Claims.ClaimsPrincipal? User => null;
        public override IDictionary<object, object?> Items => items;
        public override IFeatureCollection Features => features;
        public override CancellationToken ConnectionAborted => CancellationToken.None;
        public override void Abort()
        {
        }
    }

    private sealed class TestHttpContextFeature(HttpContext httpContext) : IHttpContextFeature
    {
        public HttpContext? HttpContext { get; set; } = httpContext;
    }
}
