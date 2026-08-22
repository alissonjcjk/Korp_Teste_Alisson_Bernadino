using MassTransit;
using Microsoft.EntityFrameworkCore;
using BillingService.Api.Models;

namespace BillingService.Api.Data;

public class BillingDbContext : DbContext
{
    public BillingDbContext(DbContextOptions<BillingDbContext> options) : base(options) { }

    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Invoice>(entity =>
        {
            // Numeração sequencial única e auto-gerada pelo banco
            entity.HasIndex(i => i.InvoiceNumber)
                  .IsUnique()
                  .HasDatabaseName("IX_invoices_number");

            // Chave de idempotência única (para evitar dupla impressão)
            entity.HasIndex(i => i.IdempotencyKey)
                  .IsUnique()
                  .HasFilter("\"idempotency_key\" IS NOT NULL")
                  .HasDatabaseName("IX_invoices_idempotency_key");

            entity.Property(i => i.TotalAmount).HasPrecision(18, 4);
            entity.Property(i => i.Status).HasConversion<int>();
        });

        modelBuilder.Entity<InvoiceItem>(entity =>
        {
            // Relacionamento: Invoice tem muitos InvoiceItems
            entity.HasOne(ii => ii.Invoice)
                  .WithMany(i => i.Items)
                  .HasForeignKey(ii => ii.InvoiceId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(ii => ii.Quantity).HasPrecision(18, 4);
            entity.Property(ii => ii.UnitPrice).HasPrecision(18, 4);

            // TotalPrice é calculado, não persiste na coluna diretamente
            entity.Ignore(ii => ii.TotalPrice);
        });

        // ── MassTransit Outbox Tables ─────────────────────────────────────────────
        // Estas tabelas garantem o Outbox Pattern: a mensagem é salva no banco
        // na mesma transação da nota fiscal, e entregue ao RabbitMQ com garantia.
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
