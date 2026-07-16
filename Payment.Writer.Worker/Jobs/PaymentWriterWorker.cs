using MassTransit;
using Payment.Core.Entities;
using Payment.Core.Event;
using Payment.Writer.Worker.Data;
using StackExchange.Redis;
using System.Text.Json;

namespace Payment.Writer.Worker.Jobs;

public class PaymentWriterWorker : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PaymentWriterWorker> _logger;
    private const int BatchSize = 500;

    // Kubernetes'te her Pod'un kendi hostname'i olur, bu da eşsiz bir Consumer Name demektir.
    private readonly string _consumerName = Environment.GetEnvironmentVariable("HOSTNAME") ?? Guid.NewGuid().ToString();
    private const string StreamName = "payment-stream";
    private const string ConsumerGroupName = "payment-writers-group";  // birden fazla worker varsa aynı gruba dahil edebiliriz, böylece yük paylaşımı olur.

    public PaymentWriterWorker(IConnectionMultiplexer redis, IServiceProvider serviceProvider, ILogger<PaymentWriterWorker> logger)
    {
        _redis = redis;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var db = _redis.GetDatabase();

        // 1. UYGULAMA KALKARKEN CONSUMER GROUP OLUŞTUR (Yoksa hata verir)
        try
        {
            await db.StreamCreateConsumerGroupAsync(StreamName, ConsumerGroupName, "0-0", createStream: true);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
        {
            // Grup zaten var, sorun yok devam et.
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            // 2. STREAM'DEN OKU (">" işareti: "Bana bu grubun henüz okumadığı YENİ mesajları getir" demek)
            var streamEntries = await db.StreamReadGroupAsync(StreamName, ConsumerGroupName, _consumerName, ">", count: BatchSize);

            if (streamEntries.Length == 0)
            {
                await Task.Delay(500, stoppingToken); // Veri yoksa kısa bir mola
                continue;
            }

            var batch = new List<(string MessageId, PaymentEvent Event)>();

            foreach (var entry in streamEntries)
            {
                var json = entry.Values.FirstOrDefault(v => v.Name == "data").Value.ToString();
                if (!string.IsNullOrEmpty(json))
                {
                    batch.Add((entry.Id.ToString(), JsonSerializer.Deserialize<PaymentEvent>(json)!));
                }
            }

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

                // Veritabanı Modellerini Hazırla
                var entities = batch.Select(b => new PaymentEntity
                {
                    Id = NewId.NextGuid(),
                    Amount = b.Event.Amount,
                    Email = b.Event.Email
                }).ToList();

                await dbContext.Payments.AddRangeAsync(entities, stoppingToken);

                // MassTransit Outbox'a Mesajları Bas
                foreach (var item in batch)
                {
                    await publishEndpoint.Publish(new PaymentCompletedEvent(item.Event.PaymentId, item.Event.Amount), stoppingToken);
                }
                // [OutboxMessage], [OutboxState] ve [Payments] tablolarına eklendiği satır 
                // 3. İŞLEMİ COMMIT ET
                await dbContext.SaveChangesAsync(stoppingToken);

                // 4. BAŞARILIYSA REDIS'E "BEN BUNLARI İŞLEDİM" (ACK) DE VE BELLEKTEN SİL
                var messageIds = batch.Select(b => (RedisValue)b.MessageId).ToArray();
                await db.StreamAcknowledgeAsync(StreamName, ConsumerGroupName, messageIds);
                await db.StreamDeleteAsync(StreamName, messageIds); // İşlenenleri temizle ki Redis şişmesin
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Toplu yazma işleminde hata oluştu. Veriler Redis'te askıda (Pending) kaldı.");
                // Burada ACK yapmadığımız için veriler kaybolmadı. 
                // Gerçek senaryoda "Pending" mesajları toplayan ayrı bir mekanizma veya Dead Letter mekanizması kurulabilir.
                await Task.Delay(2000, stoppingToken);
            }
        }
    }
}
