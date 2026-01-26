using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AgentFramework101;

/// <summary>
/// Function calling middleware that filters anonymous user discount requests.
/// This is the Agent Framework equivalent of the Semantic Kernel IFunctionInvocationFilter.
/// </summary>
public static class AnonymousUserFilter
{
    /// <summary>
    /// Middleware that intercepts function calls and blocks discount code generation for anonymous users.
    /// </summary>
    public static async ValueTask<object?> FilterAnonymousUsers(
        AIAgent agent,
        FunctionInvocationContext context,
        Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next,
        CancellationToken cancellationToken)
    {
        // Check if this is a discount code request for an anonymous user
        if (context.Function.Name == "GetDiscountCode")
        {
            // Try to get the userName argument
            var userNameArg = context.Arguments?.FirstOrDefault(a => a.Key == "userName");
            if (userNameArg?.Value?.ToString() == "guest")
            {
                // Block the function call for anonymous users
                return "No discounts for anonymous users allowed";
            }
        }

        // Continue with the function call
        var result = await next(context, cancellationToken);
        
        // You can also inspect/modify results here if needed
        return result;
    }
}