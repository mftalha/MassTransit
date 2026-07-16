using MassTransit;
using Microsoft.EntityFrameworkCore;
using Payment.Writer.Worker.Data;
using Payment.Writer.Worker.Jobs; // Worker sýnýfýnýn olduðu namespace
using StackExchange.Redis; // Redis baðlantýsý için gerekli

var builder = Host.CreateApplicationBuilder(args);

// 1. APPSETTINGS'DEN BAÐLANTI BÝLGÝLERÝNÝ OKUYORUZ
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var redisConnString = builder.Configuration.GetConnectionString("RedisConnection");

// 2. REDIS BAÐLANTISINI EKLÝYORUZ (Worker'ýn içinde lazým olacak)
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnString!));

#region EF Core DbContext Ayarlarý
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.MaxBatchSize(500); // 500'lü batch insert ayarý asýl burada yapýlýr
    });
});
#endregion

#region MassTransit
builder.Services.AddMassTransit(x =>
{
    // Outbox Ayarý
    x.AddEntityFrameworkOutbox<AppDbContext>(o =>
    {
        o.UseSqlServer(); // Sadece SQL Server mantýðýyla çalýþacaðýný belirtiyoruz (Parametresiz)
        o.UseBusOutbox();
    });

    x.UsingRabbitMq((context, cfg) =>
    {
        // 3. RABBITMQ AYARLARINI APPSETTINGS'DEN OKUYORUZ
        cfg.Host(builder.Configuration["RabbitMQ:Host"], "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"]!);
            h.Password(builder.Configuration["RabbitMQ:Password"]!);
        });

        cfg.ConfigureEndpoints(context);
    });
});
#endregion

// 4. ÝÞÇÝYÝ (WORKER) SÝSTEME KAYDEDÝYORUZ (Bunu açmazsak Redis'i dinlemez)
builder.Services.AddHostedService<PaymentWriterWorker>();

var host = builder.Build();
host.Run();
