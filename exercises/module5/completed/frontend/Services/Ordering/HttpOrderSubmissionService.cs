using GloboTicket.Frontend.Models.Api;
using GloboTicket.Frontend.Models.View;

namespace GloboTicket.Frontend.Services.Ordering
{
    public class HttpOrderSubmissionService: IOrderSubmissionService
    {
        private readonly IShoppingBasketService shoppingBasketService;
        private readonly HttpClient orderingClient;

        public HttpOrderSubmissionService(IShoppingBasketService shoppingBasketService, HttpClient orderingClient)
        {
            this.shoppingBasketService = shoppingBasketService;
            this.orderingClient = orderingClient;
        }
        public async Task<Guid> SubmitOrder(CheckoutViewModel checkoutViewModel)
        {
            using var response = await orderingClient.PostAsJsonAsync("order", new
            {
                checkoutViewModel.QuoteId, checkoutViewModel.Name, checkoutViewModel.Email
            });
            await EnsureSuccess(response);
            return (await response.Content.ReadFromJsonAsync<OrderReceipt>())!.OrderId;
        }

        public async Task<OrderQuote> QuoteOrder(Guid basketId, CancellationToken cancellationToken = default)
        {
            var lines = await shoppingBasketService.GetLinesForBasket(basketId);
            var request = new QuoteRequest(
                lines.GroupBy(l => l.EventId).Select(g => new QuoteLine(g.Key, g.Sum(l => l.TicketAmount))).ToArray(),
                await shoppingBasketService.GetPromoCode(basketId));
            using var response = await orderingClient.PostAsJsonAsync("order/quotes", request, cancellationToken);
            await EnsureSuccess(response);
            return await response.Content.ReadFromJsonAsync<OrderQuote>(cancellationToken)
                ?? throw new InvalidOperationException("Ordering returned no quote.");
        }

        public async Task<OrderQuote> GetQuote(Guid quoteId, CancellationToken cancellationToken = default)
        {
            using var response = await orderingClient.GetAsync($"order/quotes/{quoteId}", cancellationToken);
            await EnsureSuccess(response);
            return await response.Content.ReadFromJsonAsync<OrderQuote>(cancellationToken)
                ?? throw new InvalidOperationException("Ordering returned no quote.");
        }

        public async Task<OrderReceipt> GetOrder(Guid orderId, CancellationToken cancellationToken = default)
        {
            using var response = await orderingClient.GetAsync($"order/{orderId}", cancellationToken);
            await EnsureSuccess(response);
            return await response.Content.ReadFromJsonAsync<OrderReceipt>(cancellationToken)
                ?? throw new InvalidOperationException("Ordering returned no receipt.");
        }

        private static async Task EnsureSuccess(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
                throw new HttpRequestException(problem?.Detail ?? "Ordering request failed.", null, response.StatusCode);
            }
        }
    }
}
