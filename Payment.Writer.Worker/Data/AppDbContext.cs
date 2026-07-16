using MassTransit;
using Microsoft.EntityFrameworkCore;
using Payment.Core.Entities;

namespace Payment.Writer.Worker.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<PaymentEntity> Payments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PaymentEntity>()
            .Property(p => p.Amount)
            .HasPrecision(18, 2);

        // MassTransit Outbox tablolarını ekle
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
