using DigitalEvidenceSystem.Web.Data;
using DigitalEvidenceSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
namespace DigitalEvidenceSystem.Web.Pages.Analysis;
[Authorize]
public sealed class EditModel(EvidenceDbContext db, AuditService audit) : PageModel
{
    [BindProperty] public ForensicAnalysis Item { get; set; } = new();
    public async Task<IActionResult> OnGetAsync(int id){var item=await db.ForensicAnalyses.FindAsync(id);if(item is null)return NotFound();Item=item;return Page();}
    public async Task<IActionResult> OnPostAsync(){if(!ModelState.IsValid)return Page();var current=await db.ForensicAnalyses.FindAsync(Item.Id);if(current is null)return NotFound();current.Analyst=Item.Analyst;current.Status=Item.Status;current.ToolsUsed=Item.ToolsUsed;current.Findings=Item.Findings;current.Notes=Item.Notes;current.ReportReference=Item.ReportReference;current.UpdatedAt=DateTime.Now;if(current.Status is "Completed" or "Reviewed")current.CompletedAt??=DateTime.Now;await db.SaveChangesAsync();await audit.RecordAsync("Analysis updated",$"Analysis #{current.Id} updated.","ForensicAnalysis",current.Id.ToString());return RedirectToPage("Index");}
}
