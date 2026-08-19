using DigitalEvidenceSystem.Web.Data;
using DigitalEvidenceSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
namespace DigitalEvidenceSystem.Web.Pages.Audit;
[Authorize]
public sealed class IndexModel(EvidenceDbContext db) : PageModel
{
    public List<AuditLog> Items {get;set;}=[]; public string? Query{get;set;} public string? Action{get;set;}
    public async Task OnGetAsync(string? q,string? action){Query=q;Action=action;var x=db.AuditLogs.AsQueryable();if(!string.IsNullOrWhiteSpace(q))x=x.Where(a=>a.Actor.Contains(q)||a.Details.Contains(q)||a.Entity.Contains(q));if(!string.IsNullOrWhiteSpace(action))x=x.Where(a=>a.Action==action);Items=await x.OrderByDescending(a=>a.OccurredAt).Take(200).ToListAsync();}
}
