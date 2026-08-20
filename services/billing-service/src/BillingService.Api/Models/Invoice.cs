using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillingService.Api.Models;

public enum InvoiceStatus
{
    Open = 1,
    Closed = 2,
    Cancelled = 3
}

[Table("invoices")]
public class Invoice
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("invoice_number")]
    public int InvoiceNumber { get; set; }

    [Required]
    [Column("status")]
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Open;

    [Column("customer_name")]
    [MaxLength(255)]
    public string? CustomerName { get; set; }

    [Column("notes")]
    [MaxLength(1000)]
    public string? Notes { get; set; }

    [Column("idempotency_key")]
    [MaxLength(100)]
    public string? IdempotencyKey { get; set; }

    [Column("total_amount")]
    public decimal TotalAmount { get; set; }

    [Column("printed_at")]
    public DateTime? PrintedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<InvoiceItem> Items { get; set; } = new();
}
