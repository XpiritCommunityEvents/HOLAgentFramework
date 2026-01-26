using System.ComponentModel;

namespace AgentFramework101;

/// <summary>
/// Tools for generating discount codes for GloboTicket customers.
/// In Agent Framework, methods can be used directly without [KernelFunction] attributes.
/// Anonymous user filtering is handled by the AnonymousUserFilter middleware.
/// </summary>
public class DiscountTools()
{
    [Description("Generate a simple GloboTicket discount code for a user.")]
    public string GetDiscountCode([Description("The name of the user")] string userName = "guest")
    {
        var prefix = userName.ToUpper().Substring(0, Math.Min(4, userName.Length));
        var code = $"{prefix}{Random.Shared.Next(1000, 9999)}";
        return $"Here’s your GloboTicket code: GLOBO-{code}";
    }
}