using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

#region REDIS BAÐLANTISI
// Redis'e baðlanýyoruz ve projeye Singleton olarak enjekte ediyoruz.
// Ýleride bu localhost bilgisini appsettings.json'dan okuyabilirsin.
//var redisConnection = ConnectionMultiplexer.Connect("localhost:6379");

var redisConnString = builder.Configuration.GetConnectionString("RedisConnection");
var redisConnection = ConnectionMultiplexer.Connect(redisConnString);

builder.Services.AddSingleton<IConnectionMultiplexer>(redisConnection);
#endregion

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
