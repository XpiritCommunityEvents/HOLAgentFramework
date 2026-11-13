using GloboTicket.Ordering.Model;
using GloboTicket.Ordering.Services;
using Microsoft.AspNetCore.Mvc;

namespace GloboTicket.Ordering.Controllers;

[ApiController]
[Route("[controller]")]
public class OrderController(
    OrderingService ordering) : ControllerBase
{
    [HttpPost("quotes")]
    public Task<IActionResult> Quote(QuoteRequest request, CancellationToken cancellationToken) =>
        Execute(() => ordering.QuoteAsync(request, cancellationToken));

    [HttpGet("quotes/{id:guid}")]
    public Task<IActionResult> GetQuote(Guid id, CancellationToken cancellationToken) =>
        Execute(() => ordering.GetQuoteAsync(id, cancellationToken));

    [HttpPost("", Name = "SubmitOrder")]
    public Task<IActionResult> Submit(SubmitOrderRequest request, CancellationToken cancellationToken) =>
        Execute(() => ordering.SubmitAsync(request, cancellationToken));

    [HttpGet("{id:guid}")]
    public Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        Execute(() => ordering.GetOrderAsync(id, cancellationToken));

    private async Task<IActionResult> Execute<T>(Func<Task<T>> action)
    {
        try { return Ok(await action()); }
        catch (OrderException exception)
        {
            return Problem(detail: exception.Message, statusCode: exception.StatusCode);
        }
    }
}
