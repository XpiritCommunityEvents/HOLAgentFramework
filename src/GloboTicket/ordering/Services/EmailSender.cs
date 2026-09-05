using GloboTicket.Ordering.Model;

namespace GloboTicket.Ordering.Services;

public class EmailSender(ILogger<EmailSender> logger)
{
    public Task SendEmailForOrder(OrderForCreation order, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        logger.LogInformation("Received a new order for {CustomerEmail}", order.CustomerDetails.Email);
        logger.LogWarning("Not using Dapr so no email sent");
        return Task.CompletedTask;
    }
}
