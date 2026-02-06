namespace CarpetOps.Sync;

public class ErpBarcodeItemRow
{
    public string BarcodeNo { get; set; } = string.Empty;
    public string Orno { get; set; } = string.Empty;
    public string? DesignName { get; set; }
    public string? ListNo { get; set; }
    public string? ItemNo { get; set; }
    public string? CnvID { get; set; }
    public string? CnvDesc { get; set; }
    public string? ASPLAN { get; set; }
    public decimal? Width { get; set; }
    public decimal? Length { get; set; }
    public decimal? Sqm { get; set; }
    public decimal? Qty { get; set; }
    public string? OrderType { get; set; }
}
