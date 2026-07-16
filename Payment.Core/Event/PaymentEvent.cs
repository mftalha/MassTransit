namespace Payment.Core.Event;

public record PaymentEvent
{
    public Guid PaymentId { get; init; }
    public decimal Amount { get; init; }
    public string Email { get; init; }
    public DateTime CreatedAt { get; init; }
}
