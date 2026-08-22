using MassTransit;
using Microsoft.EntityFrameworkCore;
using InventoryService.Api.Models;

namespace InventoryService.Api.Data;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(entity =>
        {
            // Índice único no código do produto
            entity.HasIndex(p => p.Code)
                  .IsUnique()
                  .HasDatabaseName("IX_products_code");

            // Configuração do Optimistic Concurrency com xmin do PostgreSQL
            entity.Property(p => p.RowVersion)
                  .HasColumnName("xmin")
                  .HasColumnType("xid")
                  .ValueGeneratedOnAddOrUpdate()
                  .IsConcurrencyToken();

            // Precisão decimal
            entity.Property(p => p.StockBalance)
                  .HasPrecision(18, 4);
        });

        // ── MassTransit Inbox/Outbox Tables ────────────────────────────────────────
        // InboxState: garante idempotência no Consumer (evita dupla dedução).
        // OutboxMessage/State: necessário pelo MassTransit EF mesmo no Consumer.
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
