using DigitalEvidenceSystem.Web.Data;
using DigitalEvidenceSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DigitalEvidenceSystem.Web.Pages.Analysis;

[Authorize]
public sealed class IndexModel(EvidenceDbContext db, AuditService audit) : PageModel
{
    public List<ForensicAnalysis> Items { get; set; } = [];
    public string? Status { get; set; }
    public string? Query { get; set; }
    public string[] Statuses => ["Pending", "In Progress", "Completed", "Reviewed"];

    public async Task OnGetAsync(string? q, string? status)
    {
        Query = q?.Trim(); Status = status;
        var query = db.ForensicAnalyses.Include(x => x.EvidenceItem).ThenInclude(x => x!.CaseFile).AsQueryable();
        if (!string.IsNullOrWhiteSpace(Query)) query = query.Where(x => x.Analyst.Contains(Query) || x.EvidenceItem!.EvidenceNumber.Contains(Query) || x.EvidenceItem.Name.Contains(Query));
        if (!string.IsNullOrWhiteSpace(Status)) query = query.Where(x => x.Status == Status);
        Items = await query.OrderByDescending(x => x.UpdatedAt).ToListAsync();
    }

    public async Task<IActionResult> OnPostStatusAsync(int id, string status)
    {
        if (!Statuses.Contains(status)) return BadRequest();
        var item = await db.ForensicAnalyses.Include(x => x.EvidenceItem).SingleOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound();
        item.Status = status; item.UpdatedAt = DateTime.Now;
        if (status == "In Progress" && item.StartedAt == default) item.StartedAt = DateTime.Now;
        if (status is "Completed" or "Reviewed") item.CompletedAt ??= DateTime.Now;
        if (item.EvidenceItem is not null) item.EvidenceItem.Status = status is "Completed" or "Reviewed" ? "Reviewed" : "In Examination";
        await db.SaveChangesAsync();
        await audit.RecordAsync("Analysis status changed", $"Analysis #{id} is now {status}.", "ForensicAnalysis", id.ToString());
        return RedirectToPage(new { q = Query, status = Status });
    }
}
