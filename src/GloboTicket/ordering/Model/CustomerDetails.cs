using System.ComponentModel.DataAnnotations;

namespace GloboTicket.Ordering.Model;

public class CustomerDetails
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Address { get; set; } = string.Empty;

    [Required]
    public string Town { get; set; } = string.Empty;

    [Required]
    public string PostalCode { get; set; } = string.Empty;

    [Required]
    public string CreditCardNumber { get; set; } = string.Empty;

    [Required]
    public string CreditCardExpiryDate { get; set; } = string.Empty;
}
