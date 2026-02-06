using Carpet_Work_Progress.Models;

namespace Carpet_Work_Progress.ViewModels;

public class HistoryIndexVm
{
    public string? Barcode { get; set; }
    public string? Query { get; set; }
    public List<ErpHistoryItemVm> Items { get; set; } = new();
    public List<ProgressLog> Logs { get; set; } = new();
}

public class ErpHistoryItemVm
{
    public string Barcode { get; set; } = string.Empty;
    public string OrderNo { get; set; } = string.Empty;
    public string? DesignName { get; set; }
    public string? OrderType { get; set; }
    public int LogCount { get; set; }
    public decimal? LatestTotalPercent { get; set; }
    public DateTime? LatestCreatedAt { get; set; }
}
