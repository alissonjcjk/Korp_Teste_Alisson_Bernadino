using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using InventoryService.Api.Data;

#nullable disable

namespace InventoryService.Api.Migrations
{
    [DbContext(typeof(InventoryDbContext))]
    [Migration("20260820000001_InitialCreate")]
    partial class InitialCreate
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.7")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("InventoryService.Api.Models.Product", b =>
                {
                    b.Property<Guid>("Id").HasColumnType("uuid").HasColumnName("id");
                    b.Property<string>("Code").IsRequired().HasMaxLength(50).HasColumnType("character varying(50)").HasColumnName("code");
                    b.Property<DateTime>("CreatedAt").HasColumnType("timestamp with time zone").HasColumnName("created_at");
                    b.Property<string>("Description").IsRequired().HasMaxLength(255).HasColumnType("character varying(255)").HasColumnName("description");
                    b.Property<decimal>("StockBalance").HasPrecision(18, 4).HasColumnType("numeric(18,4)").HasColumnName("stock_balance");
                    b.Property<string>("Unit").IsRequired().HasMaxLength(20).HasColumnType("character varying(20)").HasColumnName("unit");
                    b.Property<DateTime>("UpdatedAt").HasColumnType("timestamp with time zone").HasColumnName("updated_at");
                    b.HasKey("Id").HasName("PK_products");
                    b.HasIndex("Code").IsUnique().HasDatabaseName("IX_products_code");
                    b.ToTable("products");
                });
#pragma warning restore 612, 618
        }
    }
}
