using System;
using System.Threading.Tasks;
using GloboTicket.Frontend.Extensions;
using GloboTicket.Frontend.Models;
using GloboTicket.Frontend.Models.Api;
using GloboTicket.Frontend.Models.View;
using GloboTicket.Frontend.Services;
using GloboTicket.Frontend.Services.Ordering;
using Microsoft.AspNetCore.Mvc;

namespace GloboTicket.Frontend.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly IShoppingBasketService shoppingBasketService;
        private readonly IOrderSubmissionService orderSubmissionService;
        private readonly Settings settings;
        private readonly ILogger<CheckoutController> logger;

        public CheckoutController(IShoppingBasketService shoppingBasketService, 
            IOrderSubmissionService orderSubmissionService,
            Settings settings,ILogger<CheckoutController> logger)
        {
            this.shoppingBasketService = shoppingBasketService;
            this.orderSubmissionService = orderSubmissionService;
            this.settings = settings;
            this.logger = logger;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var currentBasketId = Request.Cookies.GetCurrentBasketId(settings);
            //var basket = await shoppingBasketService.GetBasket(currentBasketId);

            try
            {
                var quote = await orderSubmissionService.QuoteOrder(currentBasketId, cancellationToken);
                return View(new CheckoutViewModel { BasketId = currentBasketId, QuoteId = quote.QuoteId, Quote = quote });
            }
            catch (HttpRequestException exception) when (exception.StatusCode is not null)
            {
                return Problem(detail: exception.Message, statusCode: (int)exception.StatusCode);
            }
        }

        public async Task<IActionResult> Thanks(Guid id, CancellationToken cancellationToken)
        {
            return View(await orderSubmissionService.GetOrder(id, cancellationToken));
        }

        [HttpGet("checkout/quote/{id:guid}")]
        public async Task<IActionResult> Quote(Guid id, CancellationToken cancellationToken)
        {
            try { return Json(await orderSubmissionService.GetQuote(id, cancellationToken)); }
            catch (HttpRequestException exception) when (exception.StatusCode is not null)
            {
                return Problem(detail: exception.Message, statusCode: (int)exception.StatusCode);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Purchase(CheckoutViewModel checkout)
        {
            ModelState.Remove(nameof(checkout.Quote));
            if (ModelState.IsValid)
            {
                var currentBasketId = Request.Cookies.GetCurrentBasketId(settings);
                checkout.BasketId = currentBasketId;
                
                Guid orderId;
                try { orderId = await orderSubmissionService.SubmitOrder(checkout); }
                catch (HttpRequestException exception) when (exception.StatusCode is not null)
                {
                    return Problem(detail: exception.Message + " Return to checkout for a new quote if expired.",
                        statusCode: (int)exception.StatusCode);
                }
                await shoppingBasketService.ClearBasket(currentBasketId);

                return RedirectToAction("Thanks", new { id = orderId });
            }
            else
            {
                checkout.Quote = await orderSubmissionService.GetQuote(checkout.QuoteId);
                return View("Index", checkout);
            }
        }

    }
}
