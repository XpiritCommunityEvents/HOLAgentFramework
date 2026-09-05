#nullable enable

using System.Security.Claims;
using Microsoft.Agents.AI;
using Microsoft.AspNetCore.SignalR;

namespace GloboTicket.Frontend.Services.AI;

/// <summary>
/// Connects the browser chat directly to an Agent Framework agent.
/// </summary>
public sealed class ChatHub(
    AIAgent agent,
    ConversationStore conversations) : Hub
{
    internal const string AnonymousOwnerCookie = "GloboTicket.ChatOwner";

    public async Task SendMessage(string conversationId, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new HubException("Enter a message before sending.");
        }

        Conversation conversation = GetConversation(conversationId);
        CancellationToken cancellationToken = Context.ConnectionAborted;
        await conversation.TurnLock.WaitAsync(cancellationToken);

        try
        {
            conversation.Session ??= await agent.CreateSessionAsync(cancellationToken);
            await StreamAsync(
                agent.RunStreamingAsync(
                    message.Trim(),
                    conversation.Session,
                    cancellationToken: cancellationToken),
                cancellationToken);
        }
        finally
        {
            conversation.TurnLock.Release();
        }
    }

    private async Task StreamAsync(
        IAsyncEnumerable<AgentResponseUpdate> updates,
        CancellationToken cancellationToken)
    {
        await Clients.Caller.SendAsync("NewResponse", cancellationToken);

        await foreach (AgentResponseUpdate update in updates.WithCancellation(cancellationToken))
        {
            if (!string.IsNullOrEmpty(update.Text))
            {
                await Clients.Caller.SendAsync(
                    "ReceiveMessagePart",
                    update.Text,
                    cancellationToken);
            }
        }

        await Clients.Caller.SendAsync("ResponseDone", cancellationToken);
    }

    private Conversation GetConversation(string conversationId)
    {
        if (!Guid.TryParse(conversationId, out Guid id) || id == Guid.Empty)
        {
            throw new HubException("The conversation identifier is invalid.");
        }

        HttpContext httpContext = Context.GetHttpContext()
            ?? throw new HubException("The chat request has no HTTP context.");
        ClaimsPrincipal user = httpContext.User;

        if (user.Identity?.IsAuthenticated is true)
        {
            string subject = user.FindFirstValue("sub") ??
                user.FindFirstValue(ClaimTypes.NameIdentifier) ??
                throw new HubException("The signed-in user has no stable identifier.");
            string tenant = user.FindFirstValue("tid") ?? "default";
            return conversations.GetOrCreate(new ConversationKey(true, tenant, subject, id));
        }

        if (httpContext.Request.Cookies.TryGetValue(
            AnonymousOwnerCookie,
            out string? anonymousOwner) &&
            Guid.TryParseExact(anonymousOwner, "N", out _))
        {
            return conversations.GetOrCreate(
                new ConversationKey(false, string.Empty, anonymousOwner, id));
        }

        throw new HubException("Reload the chat page to start a conversation.");
    }
}
