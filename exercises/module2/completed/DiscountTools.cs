using System.ComponentModel;

namespace AgentFrameworkWorkshop.Module2.Completed;

public sealed class DiscountTools(UserSessionContext userContext)
{
    public const string ToolName = "get_globoticket_discount_code";

    [Description("Generate a GloboTicket discount code for the signed-in user.")]
    public string GetDiscountCode()
    {
        // Anonymous calls never reach the tool because middleware blocks them.
        var userName = userContext.UserId!;
        var prefix = userName[..Math.Min(4, userName.Length)].ToUpperInvariant();
        var code = $"{prefix}{Random.Shared.Next(1000, 9999)}";
        return $"Here's your GloboTicket code: GLOBO-{code}";
    }
}
