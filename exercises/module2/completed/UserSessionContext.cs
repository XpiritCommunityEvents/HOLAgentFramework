namespace AgentFrameworkWorkshop.Module2.Completed;

public sealed record UserSessionContext(string? UserId)
{
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(UserId);
}
