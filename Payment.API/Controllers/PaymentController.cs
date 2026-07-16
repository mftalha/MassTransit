using Microsoft.AspNetCore.Mvc;
using Payment.Core.Event;
using Payment.Core.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace Payment.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly IConnectionMultiplexer _redis;

    public PaymentController(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    [HttpPost]
    public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequest request)
    {
        var paymentEvent = new PaymentEvent
        {
            PaymentId = Guid.NewGuid(),
            Amount = request.Amount,
            Email = request.Email,
            CreatedAt = DateTime.UtcNow
        };

        var jsonString = JsonSerializer.Serialize(paymentEvent);
        var db = _redis.GetDatabase();

        // DEĞİŞEN KISIM: ListRightPushAsync YERİNE StreamAddAsync KULLANIYORUZ
        // maxLength: 1000000 diyerek Stream'in belleği sonsuza kadar doldurmasını (OOM) engelliyoruz.
        await db.StreamAddAsync("payment-stream",
            new[] { new NameValueEntry("data", jsonString) },
            maxLength: 1000000);

        return Accepted(new { Message = "Ödeme işleminiz başarıyla sıraya alındı.", PaymentId = paymentEvent.PaymentId });
    }

}


// --- REDIS STREAM BUFFER & OOM KORUMASI ---
// 1. Neden Redis? Saniyede gelebilecek anlık yüksek istekleri (örn: 10.000/sn) MSSQL'e doğrudan 
//    basıp veritabanını kilitlememek (deadlock) için Redis'i bir tampon (Load Leveling) olarak kullanıyoruz.
// 2. Neden maxLength? Eğer arka planda bu kuyruğu işleyen Consumer (Worker) yavaşlar veya çökerse, 
//    sınırsız veri Redis'in RAM'ini doldurup sistemi çökertir (Out of Memory - OOM).
// 3. 1 Milyon Sınırı: Bu kuyrukta maksimum 1.000.000 kayıt tutulur. Bu sayıya ulaşılırsa, sistemin 
//    çökmemesi için FIFO (İlk giren ilk çıkar) mantığıyla en eski veriler silinip yenilerine yer açılır.
