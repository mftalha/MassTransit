namespace Payment.Core.Entities;

public class PaymentEntity
{
    public Guid Id { get; set; } // NewId ile üretilecek

    public decimal Amount { get; set; }
    public string Email { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = "Pending";
}
