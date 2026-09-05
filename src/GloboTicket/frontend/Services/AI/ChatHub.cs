#nullable enable

using Microsoft.AspNetCore.SignalR;

namespace GloboTicket.Frontend.Services.AI;

/// <summary>
/// Connects the browser chat to the workshop's assistant implementation.
/// </summary>
public sealed class ChatHub : Hub
{
    internal const string AnonymousOwnerCookie = "GloboTicket.ChatOwner";

    public async Task SendMessage(string conversationId, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new HubException("Enter a message before sending.");
        }

        if (!Guid.TryParse(conversationId, out Guid id) || id == Guid.Empty)
        {
            throw new HubException("The conversation identifier is invalid.");
        }

        CancellationToken cancellationToken = Context.ConnectionAborted;
        await Clients.Caller.SendAsync("NewResponse", cancellationToken);

        // TODO: Create or retrieve the conversation, run the Agent Framework agent, and
        // stream each response update to the caller instead of returning this placeholder.
        await Clients.Caller.SendAsync(
            "ReceiveMessagePart",
            "Hi, I am a dummy assistant. There is no AI here yet ☹️",
            cancellationToken);

        await Clients.Caller.SendAsync("ResponseDone", cancellationToken);
    }
}
