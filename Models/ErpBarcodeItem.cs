using System.ComponentModel.DataAnnotations.Schema;

namespace Carpet_Work_Progress.Models;

[Table("erp_barcode_items")]
public class ErpBarcodeItem
{
    [Column("barcode_no")]
    public string BarcodeNo { get; set; } = string.Empty;

    [Column("orno")]
    public string Orno { get; set; } = string.Empty;

    [Column("design_name")]
    public string? DesignName { get; set; }

    [Column("list_no")]
    public string? ListNo { get; set; }

    [Column("item_no")]
    public string? ItemNo { get; set; }

    [Column("cnv_id")]
    public string? CnvId { get; set; }

    [Column("cnv_desc")]
    public string? CnvDesc { get; set; }

    [Column("asplan")]
    public string? AsPlan { get; set; }

    [Column("width")]
    public decimal? Width { get; set; }

    [Column("length")]
    public decimal? Length { get; set; }

    [Column("sqm")]
    public decimal? Sqm { get; set; }

    [Column("qty")]
    public decimal? Qty { get; set; }

    [Column("order_type")]
    public string? OrderType { get; set; }

    [Column("synced_at")]
    public DateTime SyncedAt { get; set; }
}
