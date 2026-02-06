using System.ComponentModel.DataAnnotations;

namespace Carpet_Work_Progress.ViewModels;

public class ConfirmProgressVm
{
    [Required(ErrorMessage = "Barcode is required.")]
    [MaxLength(50)]
    public string Barcode { get; set; } = string.Empty;

    [Required]
    public DateOnly ProgressDate { get; set; }

    [Range(0, 100, ErrorMessage = "Normal % must be between 0 and 100.")]
    public decimal NormalPercent { get; set; }

    [Range(0, 100, ErrorMessage = "OT % must be between 0 and 100.")]
    public decimal OtPercent { get; set; }

    public decimal TotalPercent { get; set; }

    [Required]
    public string ImagePath { get; set; } = string.Empty;
}
