using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryService.Api.Models;

[Table("products")]
public class Product
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    [Column("code")]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Column("stock_balance")]
    public decimal StockBalance { get; set; }

    [Column("unit")]
    [MaxLength(20)]
    public string Unit { get; set; } = "UN";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Versão para Optimistic Concurrency Control - evita saldo negativo em acessos simultâneos.
    /// </summary>
    [Column("row_version")]
    [Timestamp]
    public uint RowVersion { get; set; }
}
