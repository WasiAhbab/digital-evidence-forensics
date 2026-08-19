using DigitalEvidenceSystem.Web.Data;
using DigitalEvidenceSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DigitalEvidenceSystem.Web.Pages.Custody;

[Authorize]
public sealed class CreateModel(
    EvidenceDbContext db,
    AuditService audit,
    NotificationService notifications) : PageModel
{
    [BindProperty]
    public CustodyRecord Item { get; set; } = new()
    {
        TransferredAt = DateTime.Now,
        RecordedAt = DateTime.Now
    };

    public List<EvidenceItem> Evidence { get; set; } = [];

    public async Task OnGetAsync() =>
        Evidence = await db.Evidence.OrderBy(evidence => evidence.EvidenceNumber).ToListAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        Evidence = await db.Evidence.OrderBy(evidence => evidence.EvidenceNumber).ToListAsync();

        if (!ModelState.IsValid || !await db.Evidence.AnyAsync(evidence => evidence.Id == Item.EvidenceItemId))
        {
            ModelState.AddModelError("Item.EvidenceItemId", "Select a valid evidence item.");
            return Page();
        }

        Item.TransferredAt = TrimToMinute(Item.TransferredAt);
        Item.RecordedAt = DateTime.Now;
        db.CustodyRecords.Add(Item);

        var evidence = await db.Evidence.FindAsync(Item.EvidenceItemId);
        if (evidence is null) return NotFound();

        evidence.CurrentCustodian = Item.ToPerson;
        evidence.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync();

        await audit.RecordAsync(
            "Custody transfer recorded",
            $"{evidence.EvidenceNumber}: {Item.FromPerson} → {Item.ToPerson}.",
            "CustodyRecord",
            Item.Id.ToString());

        await notifications.NotifyAllAsync(
            "Chain of custody updated",
            $"{evidence.EvidenceNumber} was transferred from {Item.FromPerson} to {Item.ToPerson}.");

        return RedirectToPage("Index");
    }

    private static DateTime TrimToMinute(DateTime value) =>
        new(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0);
}