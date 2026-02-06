using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Carpet_Work_Progress.Models;

public class ProgressLog
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Barcode { get; set; } = string.Empty;

    public DateOnly ProgressDate { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal NormalPercent { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal OtPercent { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal TotalPercent { get; set; }

    [MaxLength(255)]
    public string ImagePath { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
