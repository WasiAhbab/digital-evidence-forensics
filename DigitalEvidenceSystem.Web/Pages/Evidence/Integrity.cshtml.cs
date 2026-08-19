using System.Security.Cryptography;
using DigitalEvidenceSystem.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DigitalEvidenceSystem.Web.Pages.Evidence;

[Authorize]
public sealed class IntegrityModel(
    EvidenceDbContext db,
    IWebHostEnvironment env,
    AuditService audit,
    NotificationService notifications) : PageModel
{
    public Models.EvidenceItem Item { get; set; } = new();
    public List<(string File, string Expected, string Actual, bool Match)> Results { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var item = await db.Evidence.Include(evidence => evidence.CaseFile).SingleOrDefaultAsync(evidence => evidence.Id == id);
        if (item is null) return NotFound();

        Item = item;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var item = await db.Evidence.Include(evidence => evidence.CaseFile).SingleOrDefaultAsync(evidence => evidence.Id == id);
        if (item is null) return NotFound();

        Item = item;
        var wasMismatch = item.IntegrityStatus == "Hash mismatch";
        var files = await db.EvidenceFiles.Where(file => file.EvidenceItemId == id).ToListAsync();
        var allMatch = true;

        foreach (var file in files)
        {
            var fullPath = Path.Combine(env.ContentRootPath, file.FilePath);
            if (!System.IO.File.Exists(fullPath))
            {
                Results.Add((file.FileName, file.Sha256, "Missing", false));
                allMatch = false;
                continue;
            }

            string actual;
            await using (var stream = System.IO.File.OpenRead(fullPath))
                actual = Convert.ToHexString(await SHA256.HashDataAsync(stream));

            var match = string.Equals(actual, file.Sha256, StringComparison.OrdinalIgnoreCase);
            Results.Add((file.FileName, file.Sha256, actual, match));
            if (!match) allMatch = false;
        }

        item.IntegrityStatus = files.Count == 0 ? "Not verified" : allMatch ? "Verified" : "Hash mismatch";
        item.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync();

        await audit.RecordAsync(
            "Evidence integrity verified",
            $"{item.EvidenceNumber}: {item.IntegrityStatus}.",
            "Evidence",
            id.ToString());

        if (item.IntegrityStatus == "Hash mismatch" && !wasMismatch)
        {
            await notifications.NotifyAllAsync(
                "Evidence integrity alert",
                $"{item.EvidenceNumber} failed an SHA-256 integrity check. Review it immediately.",
                "Warning");
        }

        return Page();
    }
}