using System.ComponentModel.DataAnnotations;

namespace GloboTicket.Ordering.Model;

public class OrderForCreation : IValidatableObject
{
    public Guid OrderId { get; set; }
    public DateTimeOffset Date { get; set; }

    [Required]
    public CustomerDetails CustomerDetails { get; set; } = new();

    [Required]
    [MinLength(1)]
    public List<OrderLine> Lines { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (OrderId == Guid.Empty)
        {
            yield return new ValidationResult(
                "OrderId must be a non-empty GUID.",
                [nameof(OrderId)]);
        }
    }
}
