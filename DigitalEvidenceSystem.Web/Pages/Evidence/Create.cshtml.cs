using DigitalEvidenceSystem.Web.Data;
using DigitalEvidenceSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DigitalEvidenceSystem.Web.Pages.Evidence;

[Authorize]
public sealed class CreateModel(
    EvidenceDbContext db,
    AuditService audit,
    NotificationService notifications) : PageModel
{
    [BindProperty]
    public EvidenceItem Item { get; set; } = new()
    {
        CollectedOn = RemoveMilliseconds(DateTime.Now)
    };

    public List<CaseFile> Cases { get; set; } = [];

    public async Task OnGetAsync()
    {
        var count = await db.Evidence.CountAsync();
        Item.EvidenceNumber = $"EV-{count + 1:D3}";
        Cases = await db.Cases
            .OrderBy(caseFile => caseFile.CaseNumber)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Cases = await db.Cases
            .OrderBy(caseFile => caseFile.CaseNumber)
            .ToListAsync();

        if (!ModelState.IsValid)
            return Page();

        if (await db.Evidence.AnyAsync(
                evidence => evidence.EvidenceNumber == Item.EvidenceNumber))
        {
            ModelState.AddModelError(
                "Item.EvidenceNumber",
                "That evidence number already exists. Please use the next available number.");

            return Page();
        }

        // Ensures no milliseconds are ever saved.
        Item.CollectedOn = RemoveMilliseconds(Item.CollectedOn);
        Item.CreatedAt = DateTime.Now;
        Item.UpdatedAt = DateTime.Now;

        db.Evidence.Add(Item);
        await db.SaveChangesAsync();

        await audit.RecordAsync(
            "Evidence added",
            $"{Item.EvidenceNumber} — {Item.Name}",
            "Evidence",
            Item.Id.ToString());

        await notifications.NotifyAllAsync(
            "Evidence registered",
            $"{Item.EvidenceNumber} — {Item.Name} was registered in the evidence system.");

        return RedirectToPage("Index");
    }

    private static DateTime RemoveMilliseconds(DateTime value) =>
        value.AddTicks(-(value.Ticks % TimeSpan.TicksPerSecond));
}