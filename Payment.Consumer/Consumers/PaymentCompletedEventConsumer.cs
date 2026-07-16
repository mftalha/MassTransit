using MassTransit;
using Payment.Core.Event;

namespace Payment.Consumer.Worker.Consumers;

public class PaymentCompletedEventConsumer : IConsumer<PaymentCompletedEvent>
{
    private readonly ILogger<PaymentCompletedEventConsumer> _logger;

    public PaymentCompletedEventConsumer(ILogger<PaymentCompletedEventConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentCompletedEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation($"Ödeme işleniyor: {message.PaymentId}, Tutar: {message.Amount}");

        // BURADA İŞ SÜREÇLERİNİ BAŞLAT:
        // 1. Fatura Servisine git
        // 2. Müşteriye E-posta gönder
        // 3. Stok güncellemesi yap

        await Task.CompletedTask;
    }
}
