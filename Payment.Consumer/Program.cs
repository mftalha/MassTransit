using MassTransit;
using Payment.Consumer.Worker.Consumers;

var builder = Host.CreateApplicationBuilder(args);

#region MassTransit

builder.Services.AddMassTransit(x =>
{
    // Consumer'ý buraya kaydediyoruz
    x.AddConsumer<PaymentCompletedEventConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        // RabbitMQ baðlantý bilgilerini appsettings'den okuyoruz
        cfg.Host(builder.Configuration["RabbitMQ:Host"], "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"]!);
            h.Password(builder.Configuration["RabbitMQ:Password"]!);
        });

        // Consumer için RabbitMQ üzerinde otomatik bir kuyruk oluþturur
        cfg.ReceiveEndpoint("payment-completed-queue", e =>
        {
            e.ConfigureConsumer<PaymentCompletedEventConsumer>(context);
        });
    });
});

#endregion

var host = builder.Build();
host.Run();
