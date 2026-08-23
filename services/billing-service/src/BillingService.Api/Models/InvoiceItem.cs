using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillingService.Api.Models;

[Table("invoice_items")]
public class InvoiceItem
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("invoice_id")]
    public Guid InvoiceId { get; set; }

    [ForeignKey(nameof(InvoiceId))]
    public Invoice Invoice { get; set; } = null!;

    [Required]
    [Column("product_id")]
    public Guid ProductId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("product_code")]
    public string ProductCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [Column("product_description")]
    public string ProductDescription { get; set; } = string.Empty;

    [Required]
    [Column("quantity")]
    public decimal Quantity { get; set; }

    [Required]
    [Column("unit_price")]
    public decimal UnitPrice { get; set; }

    [Column("total_price")]
    public decimal TotalPrice => InvoiceAmount.CalculateLineTotal(Quantity, UnitPrice);
}
