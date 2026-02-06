namespace Carpet_Work_Progress.ViewModels;

public class HistoryOpenVm
{
    public string? Q { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public bool Today { get; set; }
    public int Page { get; set; }
    public int TotalCount { get; set; }
    public int PageSize { get; set; }
    public List<ErpOpenItemVm> Items { get; set; } = new();
}

public class ErpOpenItemVm
{
    public string Barcode { get; set; } = string.Empty;
    public string? OrderNo { get; set; }
    public string? DesignName { get; set; }
    public string OrderType { get; set; } = string.Empty;
    public int LogCount { get; set; }
    public decimal? LatestTotalPercent { get; set; }
    public DateTime? LatestCreatedAt { get; set; }
}
