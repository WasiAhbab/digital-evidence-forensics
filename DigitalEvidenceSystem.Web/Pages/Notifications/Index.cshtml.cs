using DigitalEvidenceSystem.Web.Data;
using DigitalEvidenceSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
namespace DigitalEvidenceSystem.Web.Pages.Notifications;
[Authorize]
public sealed class IndexModel(EvidenceDbContext db) : PageModel
{
    public List<Notification> Items { get; set; } = [];
    public async Task OnGetAsync(){var name=User.Identity?.Name??"";Items=await db.Notifications.Where(x=>x.Recipient==name||x.Recipient=="*").OrderByDescending(x=>x.CreatedAt).Take(100).ToListAsync();}
    public async Task<IActionResult> OnPostReadAsync(int id){var item=await db.Notifications.FindAsync(id);if(item is null)return NotFound();if(item.Recipient!=(User.Identity?.Name??"")&&item.Recipient!="*")return Forbid();item.IsRead=true;await db.SaveChangesAsync();return RedirectToPage();}
}
