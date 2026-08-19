
using System.Security.Cryptography;
using DigitalEvidenceSystem.Web.Data;
using DigitalEvidenceSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DigitalEvidenceSystem.Web.Pages.Files;

[Authorize]
public sealed class IndexModel(
    EvidenceDbContext db,
    IWebHostEnvironment env,
    AuditService audit,
    NotificationService notifications) : PageModel
{
    public EvidenceItem? Evidence { get; set; }
    public List<EvidenceFile> Items { get; set; } = [];

    [BindProperty]
    public int EvidenceId { get; set; }

    [BindProperty]
    public IFormFile? Upload { get; set; }

    public async Task<IActionResult> OnGetAsync(int evidenceId)
    {
        Evidence = await db.Evidence.FindAsync(evidenceId);
        if (Evidence is null) return NotFound();

        Items = await db.EvidenceFiles
            .Where(item => item.EvidenceItemId == evidenceId)
            .OrderByDescending(item => item.UploadedAt)
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Evidence = await db.Evidence.FindAsync(EvidenceId);
        if (Evidence is null) return NotFound();

        if (Upload is null || Upload.Length == 0)
        {
            ModelState.AddModelError("Upload", "Choose a file.");
            Items = await db.EvidenceFiles.Where(item => item.EvidenceItemId == EvidenceId).ToListAsync();
            return Page();
        }

        if (Upload.Length > 50 * 1024 * 1024)
        {
            ModelState.AddModelError("Upload", "Maximum file size is 50 MB.");
            Items = await db.EvidenceFiles.Where(item => item.EvidenceItemId == EvidenceId).ToListAsync();
            return Page();
        }

        var safeName = Path.GetFileName(Upload.FileName);
        var extension = Path.GetExtension(safeName);
        var allowedExtensions = new[] { ".pdf", ".txt", ".csv", ".jpg", ".jpeg", ".png", ".mp4", ".wav", ".docx", ".zip" };

        if (!allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("Upload", "This file type is not allowed.");
            Items = await db.EvidenceFiles.Where(item => item.EvidenceItemId == EvidenceId).ToListAsync();
            return Page();
        }

        var storageFolder = Path.Combine(env.ContentRootPath, "App_Data", "EvidenceStorage", Evidence.EvidenceNumber);
        Directory.CreateDirectory(storageFolder);

        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(storageFolder, storedFileName);

        await using (var input = Upload.OpenReadStream())
        await using (var output = System.IO.File.Create(fullPath))
            await input.CopyToAsync(output);

        string hash;
        await using (var stream = System.IO.File.OpenRead(fullPath))
            hash = Convert.ToHexString(await SHA256.HashDataAsync(stream));

        db.EvidenceFiles.Add(new EvidenceFile
        {
            EvidenceItemId = EvidenceId,
            FileName = safeName,
            ContentType = Upload.ContentType,
            FilePath = Path.Combine("App_Data", "EvidenceStorage", Evidence.EvidenceNumber, storedFileName),
            SizeBytes = Upload.Length,
            Sha256 = hash,
            UploadedBy = User.Identity?.Name ?? "",
            UploadedAt = DateTime.Now
        });

        Evidence.HashSha256 = hash;
        Evidence.HashGeneratedAt = DateTime.Now;
        Evidence.IntegrityStatus = "Verified";
        Evidence.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync();

        await audit.RecordAsync(
            "Evidence file uploaded",
            $"{safeName} attached to {Evidence.EvidenceNumber}; SHA-256 {hash}.",
            "EvidenceFile",
            EvidenceId.ToString());

        await notifications.NotifyAllAsync(
            "Evidence file uploaded",
            $"{safeName} was attached to {Evidence.EvidenceNumber} and its SHA-256 fingerprint was recorded.");

        return RedirectToPage(new { evidenceId = EvidenceId });
    }

    public async Task<IActionResult> OnGetDownloadAsync(int id)
    {
        var item = await db.EvidenceFiles
            .Include(file => file.EvidenceItem)
            .SingleOrDefaultAsync(file => file.Id == id);

        if (item is null) return NotFound();

        var fullPath = Path.Combine(env.ContentRootPath, item.FilePath);
        if (!System.IO.File.Exists(fullPath)) return NotFound();

        await audit.RecordAsync(
            "Evidence file accessed",
            $"{item.FileName} downloaded for {item.EvidenceItem?.EvidenceNumber}.",
            "EvidenceFile",
            id.ToString());

        return PhysicalFile(fullPath, item.ContentType, item.FileName);
    }
}
