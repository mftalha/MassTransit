namespace Payment.Core.Event;

public record PaymentCompletedEvent
{
    public Guid PaymentId { get; init; }
    public decimal Amount { get; init; }
    public string Email { get; init; }
    public DateTime CompletedAt { get; init; }
}
