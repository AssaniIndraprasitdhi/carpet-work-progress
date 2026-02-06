using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Carpet_Work_Progress.Data;
using Carpet_Work_Progress.Models;
using Carpet_Work_Progress.Services;
using Carpet_Work_Progress.ViewModels;

namespace Carpet_Work_Progress.Controllers;

public class ProgressController : Controller
{
    private readonly AppDbContext _db;
    private readonly IImageAnalysisService _imageService;

    public ProgressController(AppDbContext db, IImageAnalysisService imageService)
    {
        _db = db;
        _imageService = imageService;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Analyze(AnalyzeFormVm vm)
    {
        if (vm.Image is null || vm.Image.Length == 0)
            ModelState.AddModelError("Image", "Please capture a photo.");

        if (!ModelState.IsValid)
            return View("~/Views/Home/Index.cshtml", vm);

        var file = vm.Image!;
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (ext != ".jpg" && ext != ".jpeg" && ext != ".png")
        {
            ModelState.AddModelError("Image", "Only JPG or PNG files are accepted.");
            return View("~/Views/Home/Index.cshtml", vm);
        }

        if (file.Length > 10 * 1024 * 1024)
        {
            ModelState.AddModelError("Image", "File size must be under 10 MB.");
            return View("~/Views/Home/Index.cshtml", vm);
        }

        var uploadDir = Environment.GetEnvironmentVariable("UPLOAD_DIR") ?? "wwwroot/uploads";
        Directory.CreateDirectory(uploadDir);
        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        AnalyzeResultVm result;
        try
        {
            result = await _imageService.AnalyzeAsync(file);
        }
        catch
        {
            ModelState.AddModelError("Image", "Failed to analyze image. Please try a different photo.");
            return View("~/Views/Home/Index.cshtml", vm);
        }

        var confirmVm = new ConfirmProgressVm
        {
            Barcode = vm.Barcode.Trim(),
            ProgressDate = DateOnly.FromDateTime(DateTime.Today),
            NormalPercent = result.NormalPercent,
            OtPercent = result.OtPercent,
            TotalPercent = result.TotalPercent,
            ImagePath = $"/uploads/{fileName}"
        };

        return View("Analyze", confirmVm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(ConfirmProgressVm vm)
    {
        vm.TotalPercent = Math.Round(vm.NormalPercent + vm.OtPercent, 2);

        if (vm.TotalPercent > 100)
            ModelState.AddModelError("TotalPercent", "Total cannot exceed 100%.");

        if (!ModelState.IsValid)
            return View("Analyze", vm);

        var log = new ProgressLog
        {
            Id = Guid.NewGuid(),
            Barcode = vm.Barcode.Trim(),
            ProgressDate = vm.ProgressDate,
            NormalPercent = vm.NormalPercent,
            OtPercent = vm.OtPercent,
            TotalPercent = vm.TotalPercent,
            ImagePath = vm.ImagePath,
            CreatedAt = DateTime.UtcNow
        };

        _db.ProgressLogs.Add(log);
        await _db.SaveChangesAsync();

        return RedirectToAction("WorkProgress", new { barcode = vm.Barcode.Trim() });
    }

    [HttpGet]
    [Route("/WorkProgress/{barcode}")]
    public async Task<IActionResult> WorkProgress(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            return RedirectToAction("History");

        var trimmed = barcode.Trim();

        var erp = await _db.ErpBarcodeItems
            .FirstOrDefaultAsync(e => e.BarcodeNo == trimmed);

        var logs = await _db.ProgressLogs
            .Where(p => p.Barcode == trimmed)
            .OrderByDescending(p => p.ProgressDate)
            .ThenByDescending(p => p.CreatedAt)
            .Take(200)
            .ToListAsync();

        var vm = new WorkProgressVm
        {
            Barcode = trimmed,
            OrderNo = erp?.Orno,
            DesignName = erp?.DesignName,
            OrderType = erp?.OrderType,
            CnvId = erp?.CnvId,
            CnvDesc = erp?.CnvDesc,
            Width = erp?.Width,
            Length = erp?.Length,
            Sqm = erp?.Sqm,
            LogCount = logs.Count,
            Logs = logs
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> History(string? q, DateTime? from, DateTime? to, bool today = false, int page = 1)
    {
        const int pageSize = 50;
        if (page < 1) page = 1;

        var term = q?.Trim();

        DateOnly? filterFrom = null;
        DateOnly? filterTo = null;

        if (today)
        {
            var d = DateOnly.FromDateTime(DateTime.Today);
            filterFrom = d;
            filterTo = d;
        }
        else
        {
            if (from.HasValue)
                filterFrom = DateOnly.FromDateTime(from.Value);
            if (to.HasValue)
                filterTo = DateOnly.FromDateTime(to.Value);
        }

        var logs = _db.ProgressLogs.AsQueryable();

        if (filterFrom.HasValue)
            logs = logs.Where(p => p.ProgressDate >= filterFrom.Value);

        if (filterTo.HasValue)
            logs = logs.Where(p => p.ProgressDate <= filterTo.Value);

        var erpQuery = _db.ErpBarcodeItems
            .Where(e =>
                _db.ProgressLogs.Any(p =>
                    p.Barcode.Trim().ToUpper() == e.BarcodeNo.Trim().ToUpper()
                )
            );

        if (!string.IsNullOrWhiteSpace(term))
        {
            var pattern = $"%{term}%";
            erpQuery = erpQuery.Where(e =>
                EF.Functions.ILike(e.Orno, pattern) ||
                EF.Functions.ILike(e.BarcodeNo, pattern));
        }

        var totalCount = await erpQuery.CountAsync();

        var items = await erpQuery
            .Select(e => new ErpOpenItemVm
            {
                Barcode = e.BarcodeNo,
                OrderNo = e.Orno,
                DesignName = e.DesignName,
                OrderType = e.OrderType ?? "",
                LogCount = logs.Count(p =>
                    p.Barcode.Trim().ToUpper() == e.BarcodeNo.Trim().ToUpper()
                ),
                LatestCreatedAt = logs
                    .Where(p => p.Barcode.Trim().ToUpper() == e.BarcodeNo.Trim().ToUpper())
                    .Max(p => (DateTime?)p.CreatedAt),
                LatestTotalPercent = logs
                    .Where(p => p.Barcode.Trim().ToUpper() == e.BarcodeNo.Trim().ToUpper())
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => (decimal?)p.TotalPercent)
                    .FirstOrDefault()
            })
            .OrderByDescending(x => x.LatestCreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var vm = new HistoryOpenVm
        {
            Q = term,
            From = from,
            To = to,
            Today = today,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items
        };

        return View(vm);
    }
}
