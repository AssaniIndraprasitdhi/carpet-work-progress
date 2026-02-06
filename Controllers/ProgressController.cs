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

        return RedirectToAction("History", new { barcode = vm.Barcode.Trim() });
    }

    [HttpGet]
    public async Task<IActionResult> History(string? barcode)
    {
        var query = _db.ProgressLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(barcode))
            query = query.Where(p => p.Barcode == barcode.Trim());

        var logs = await query
            .OrderByDescending(p => p.ProgressDate)
            .ThenByDescending(p => p.CreatedAt)
            .Take(200)
            .ToListAsync();

        var vm = new HistoryVm
        {
            BarcodeFilter = barcode?.Trim(),
            Logs = logs
        };

        return View(vm);
    }
}
