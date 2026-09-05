using System.ComponentModel.DataAnnotations;

namespace GloboTicket.Ordering.Model;

public class OrderLine : IValidatableObject
{
    public Guid EventId { get; set; }

    [Range(1, int.MaxValue)]
    public int TicketCount { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal Price { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EventId == Guid.Empty)
        {
            yield return new ValidationResult(
                "EventId must be a non-empty GUID.",
                [nameof(EventId)]);
        }
    }
}
