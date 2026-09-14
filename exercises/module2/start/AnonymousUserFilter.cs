using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AgentFramework101;

public sealed class AnonymousUserFilter(UserSessionContext userContext)
{
    public async ValueTask<object?> InvokeAsync(
        AIAgent _,
        FunctionInvocationContext context,
        Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next,
        CancellationToken cancellationToken)
    {
        if (context.Function.Name == DiscountTools.ToolName && !userContext.IsAuthenticated)
        {
            return "Please sign in before requesting a discount code.";
        }

        return await next(context, cancellationToken);
    }
}
