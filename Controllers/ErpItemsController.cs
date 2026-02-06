using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Carpet_Work_Progress.Data;

namespace Carpet_Work_Progress.Controllers;

[ApiController]
[Route("api/erp-items")]
public class ErpItemsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ErpItemsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("{barcode}")]
    public async Task<IActionResult> GetByBarcode(string barcode)
    {
        barcode = barcode.Trim();

        var item = await _db.ErpBarcodeItems
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.BarcodeNo.Trim() == barcode);

        if (item is null)
            return NotFound(new { message = "Barcode not found", barcode });

        return Ok(new
        {
            barcodeNo = item.BarcodeNo.Trim(),
            orno = item.Orno.Trim(),
            designName = item.DesignName?.Trim(),
            listNo = item.ListNo?.Trim(),
            itemNo = item.ItemNo?.Trim(),
            cnvId = item.CnvId?.Trim(),
            cnvDesc = item.CnvDesc?.Trim(),
            asPlan = item.AsPlan?.Trim(),
            width = item.Width,
            length = item.Length,
            sqm = item.Sqm,
            qty = item.Qty,
            orderType = item.OrderType?.Trim(),
            syncedAt = item.SyncedAt
        });
    }
}
