using System.ComponentModel.DataAnnotations;

namespace Carpet_Work_Progress.ViewModels;

public class AnalyzeFormVm
{
    [Required(ErrorMessage = "Barcode is required.")]
    [MaxLength(50)]
    public string Barcode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please upload a photo.")]
    public IFormFile? Image { get; set; }
}
