using GloboTicket.Ordering.Model;
using GloboTicket.Ordering.Services;
using Microsoft.AspNetCore.Mvc;

namespace GloboTicket.Ordering.Controllers;

[ApiController]
[Route("[controller]")]
public class OrderController(
    ILogger<OrderController> logger,
    EmailSender emailSender) : ControllerBase
{
    [HttpPost("", Name = "SubmitOrder")]
    public async Task<IActionResult> Submit(OrderForCreation order, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received a new order from {CustomerName}", order.CustomerDetails.Name);
        await emailSender.SendEmailForOrder(order, cancellationToken);
        return Ok();
    }
}
