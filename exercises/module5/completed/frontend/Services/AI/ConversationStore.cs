#nullable enable

using System.Collections.Concurrent;
using Microsoft.Agents.AI;

namespace GloboTicket.Frontend.Services.AI;

/// <summary>
/// Keeps one Agent Framework session per browser owner and conversation. This workshop uses
/// process memory deliberately; a real multi-instance application would use shared storage.
/// </summary>
public sealed class ConversationStore
{
    private readonly ConcurrentDictionary<ConversationKey, Conversation> conversations = [];

    internal Conversation GetOrCreate(ConversationKey key) =>
        conversations.GetOrAdd(key, _ => new Conversation());
}

internal readonly record struct ConversationKey(
    bool IsAuthenticated,
    string TenantId,
    string OwnerId,
    Guid ConversationId);

internal sealed class Conversation
{
    internal SemaphoreSlim TurnLock { get; } = new(1, 1);
    internal AgentSession? Session { get; set; }
}
