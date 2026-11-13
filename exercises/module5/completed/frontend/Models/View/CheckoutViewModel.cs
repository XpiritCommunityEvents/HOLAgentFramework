using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using GloboTicket.Frontend.Models.Api;

namespace GloboTicket.Frontend.Models.View
{
    public class CheckoutViewModel
    {
        public Guid BasketId { get; set; }
        public Guid QuoteId { get; set; }
        public OrderQuote Quote { get; set; }
        [Required]
        [StringLength(120)]
        public string Name { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
