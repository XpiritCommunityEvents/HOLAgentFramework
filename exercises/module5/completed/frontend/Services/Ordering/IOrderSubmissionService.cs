using GloboTicket.Frontend.Models.View;

namespace GloboTicket.Frontend.Services.Ordering
{
    public interface IOrderSubmissionService
    {
        Task<Models.Api.OrderQuote> QuoteOrder(Guid basketId, CancellationToken cancellationToken = default);
        Task<Models.Api.OrderQuote> GetQuote(Guid quoteId, CancellationToken cancellationToken = default);
        Task<Models.Api.OrderReceipt> GetOrder(Guid orderId, CancellationToken cancellationToken = default);
        Task<Guid> SubmitOrder(CheckoutViewModel checkoutViewModel);
    }
}
