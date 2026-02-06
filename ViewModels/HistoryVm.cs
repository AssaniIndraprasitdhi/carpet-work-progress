using Carpet_Work_Progress.Models;

namespace Carpet_Work_Progress.ViewModels;

public class HistoryVm
{
    public string? BarcodeFilter { get; set; }
    public List<ProgressLog> Logs { get; set; } = new();
}
