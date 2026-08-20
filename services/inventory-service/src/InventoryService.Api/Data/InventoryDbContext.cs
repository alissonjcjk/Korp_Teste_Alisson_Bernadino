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
    }
}
