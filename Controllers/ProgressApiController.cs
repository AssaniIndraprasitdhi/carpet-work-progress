using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Carpet_Work_Progress.Data;

namespace Carpet_Work_Progress.Controllers;

[ApiController]
[Route("api/progress")]
public class ProgressApiController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProgressApiController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("{barcode}")]
    public async Task<IActionResult> GetByBarcode(string barcode)
    {
        var trimmed = barcode.Trim();

        var logs = await _db.ProgressLogs
            .AsNoTracking()
            .Where(p => p.Barcode == trimmed)
            .OrderByDescending(p => p.ProgressDate)
            .ThenByDescending(p => p.CreatedAt)
            .Take(200)
            .Select(p => new
            {
                progressDate = p.ProgressDate.ToString("yyyy-MM-dd"),
                normalPercent = p.NormalPercent,
                otPercent = p.OtPercent,
                totalPercent = p.TotalPercent,
                imagePath = p.ImagePath,
                createdAt = p.CreatedAt
            })
            .ToListAsync();

        return Ok(logs);
    }
}
