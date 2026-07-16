namespace Payment.Core.Models;

public record PaymentRequest
{
    public decimal Amount { get; init; }
    public string CardNumber { get; init; }
    public string Email { get; init; }
}
