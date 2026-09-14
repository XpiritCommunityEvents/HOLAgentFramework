namespace AgentFramework101;

public sealed record UserSessionContext(string? UserId)
{
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(UserId);
}
