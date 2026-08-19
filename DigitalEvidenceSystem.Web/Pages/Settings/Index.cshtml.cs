using System.ComponentModel.DataAnnotations;
using DigitalEvidenceSystem.Web.Data;
using DigitalEvidenceSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
namespace DigitalEvidenceSystem.Web.Pages.Settings;
[Authorize]
public sealed class IndexModel(EvidenceDbContext db, AuditService audit) : PageModel
{
    [BindProperty] public ProfileInput Input {get;set;}=new();
    public int Users{get;set;} public int Cases{get;set;} public int Evidence{get;set;} public int Analyses{get;set;}
    public sealed class ProfileInput { [StringLength(120)] public string FullName{get;set;}=""; [EmailAddress] public string Email{get;set;}=""; [StringLength(80)] public string Department{get;set;}=""; }
    public async Task OnGetAsync(){var u=await db.Users.FirstOrDefaultAsync(x=>x.Id==int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value));if(u is not null)Input=new(){FullName=u.FullName,Email=u.Email,Department=u.Department};Users=await db.Users.CountAsync();Cases=await db.Cases.CountAsync();Evidence=await db.Evidence.CountAsync();Analyses=await db.ForensicAnalyses.CountAsync();}
    public async Task<IActionResult> OnPostAsync(){var id=int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);var u=await db.Users.FindAsync(id);if(u is null)return NotFound();if(!ModelState.IsValid)return Page();u.FullName=Input.FullName;u.Email=Input.Email;u.Department=Input.Department;await db.SaveChangesAsync();await audit.RecordAsync("Profile updated","User profile updated.","User",id.ToString());return RedirectToPage();}
}
