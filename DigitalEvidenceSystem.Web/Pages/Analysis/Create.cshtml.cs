using DigitalEvidenceSystem.Web.Data;
using DigitalEvidenceSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DigitalEvidenceSystem.Web.Pages.Analysis;
[Authorize]
public sealed class CreateModel(EvidenceDbContext db, AuditService audit) : PageModel
{
    [BindProperty] public ForensicAnalysis Item { get; set; } = new();
    public List<EvidenceItem> Evidence { get; set; } = [];
    public async Task OnGetAsync() => Evidence = await db.Evidence.OrderBy(x => x.EvidenceNumber).ToListAsync();
    public async Task<IActionResult> OnPostAsync()
    {
        Evidence = await db.Evidence.OrderBy(x => x.EvidenceNumber).ToListAsync();
        if (!ModelState.IsValid || !await db.Evidence.AnyAsync(x => x.Id == Item.EvidenceItemId)) { ModelState.AddModelError("Item.EvidenceItemId", "Select valid evidence."); return Page(); }
        Item.Analyst = User.Identity?.Name ?? Item.Analyst; Item.StartedAt = DateTime.Now; Item.UpdatedAt = DateTime.Now;
        db.ForensicAnalyses.Add(Item); var evidence = await db.Evidence.FindAsync(Item.EvidenceItemId); if (evidence is not null) evidence.Status = "In Examination";
        await db.SaveChangesAsync(); await audit.RecordAsync("Analysis started", $"Analysis started for {evidence?.EvidenceNumber}.", "ForensicAnalysis", Item.Id.ToString());
        return RedirectToPage("Index");
    }
}
