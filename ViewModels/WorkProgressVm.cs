using Carpet_Work_Progress.Models;

namespace Carpet_Work_Progress.ViewModels;

public class WorkProgressVm
{
    public string Barcode { get; set; } = string.Empty;
    public string? OrderNo { get; set; }
    public string? DesignName { get; set; }
    public string? OrderType { get; set; }
    public string? CnvId { get; set; }
    public string? CnvDesc { get; set; }
    public decimal? Width { get; set; }
    public decimal? Length { get; set; }
    public decimal? Sqm { get; set; }
    public int LogCount { get; set; }
    public List<ProgressLog> Logs { get; set; } = new();
}
