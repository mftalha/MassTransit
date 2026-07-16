namespace Payment.Core.Event;


public record PaymentCompletedEvent(Guid PaymentId, decimal Amount);