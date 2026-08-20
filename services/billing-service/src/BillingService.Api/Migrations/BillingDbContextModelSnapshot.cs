using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using BillingService.Api.Data;
using BillingService.Api.Models;

#nullable disable

namespace BillingService.Api.Migrations
{
    [DbContext(typeof(BillingDbContext))]
    partial class BillingDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.7")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("BillingService.Api.Models.Invoice", b =>
                {
                    b.Property<Guid>("Id").HasColumnType("uuid").HasColumnName("id");
                    b.Property<int>("InvoiceNumber").HasColumnType("integer").HasColumnName("invoice_number")
                        .HasDefaultValueSql("nextval('invoice_number_seq')");
                    b.Property<int>("Status").HasColumnType("integer").HasColumnName("status").HasDefaultValue(1);
                    b.Property<string>("CustomerName").HasMaxLength(255).HasColumnType("character varying(255)").HasColumnName("customer_name");
                    b.Property<string>("Notes").HasMaxLength(1000).HasColumnType("character varying(1000)").HasColumnName("notes");
                    b.Property<string>("IdempotencyKey").HasMaxLength(100).HasColumnType("character varying(100)").HasColumnName("idempotency_key");
                    b.Property<decimal>("TotalAmount").HasPrecision(18, 4).HasColumnType("numeric(18,4)").HasColumnName("total_amount");
                    b.Property<DateTime?>("PrintedAt").HasColumnType("timestamp with time zone").HasColumnName("printed_at");
                    b.Property<DateTime>("CreatedAt").HasColumnType("timestamp with time zone").HasColumnName("created_at");
                    b.Property<DateTime>("UpdatedAt").HasColumnType("timestamp with time zone").HasColumnName("updated_at");
                    b.HasKey("Id").HasName("PK_invoices");
                    b.HasIndex("InvoiceNumber").IsUnique().HasDatabaseName("IX_invoices_number");
                    b.ToTable("invoices");
                });

            modelBuilder.Entity("BillingService.Api.Models.InvoiceItem", b =>
                {
                    b.Property<Guid>("Id").HasColumnType("uuid").HasColumnName("id");
                    b.Property<Guid>("InvoiceId").HasColumnType("uuid").HasColumnName("invoice_id");
                    b.Property<Guid>("ProductId").HasColumnType("uuid").HasColumnName("product_id");
                    b.Property<string>("ProductCode").IsRequired().HasMaxLength(50).HasColumnType("character varying(50)").HasColumnName("product_code");
                    b.Property<string>("ProductDescription").IsRequired().HasMaxLength(255).HasColumnType("character varying(255)").HasColumnName("product_description");
                    b.Property<decimal>("Quantity").HasPrecision(18, 4).HasColumnType("numeric(18,4)").HasColumnName("quantity");
                    b.Property<decimal>("UnitPrice").HasPrecision(18, 4).HasColumnType("numeric(18,4)").HasColumnName("unit_price");
                    b.HasKey("Id").HasName("PK_invoice_items");
                    b.HasIndex("InvoiceId").HasDatabaseName("IX_invoice_items_invoice_id");
                    b.ToTable("invoice_items");

                    b.HasOne("BillingService.Api.Models.Invoice", "Invoice")
                        .WithMany("Items")
                        .HasForeignKey("InvoiceId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
