using DigitalEvidenceSystem.Web.Data;
using DigitalEvidenceSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
namespace DigitalEvidenceSystem.Web.Pages.Users;
[Authorize(Roles=Roles.Administrator)]
public sealed class IndexModel(EvidenceDbContext db, IPasswordHasher<AppUser> hasher, AuditService audit) : PageModel
{
    public List<AppUser> Items { get; set; }=[];
    [BindProperty] public AppUser Input {get;set;}=new();
    [BindProperty] public string Password {get;set;}="";
    public async Task OnGetAsync()=>Items=await db.Users.OrderBy(x=>x.FullName).ToListAsync();
    public async Task<IActionResult> OnPostCreateAsync(){if(string.IsNullOrWhiteSpace(Password)||Password.Length<8)ModelState.AddModelError("Password","Password must be at least 8 characters.");if(await db.Users.AnyAsync(x=>x.Username==Input.Username.Trim()))ModelState.AddModelError("Input.Username","Username already exists.");if(!ModelState.IsValid){Items=await db.Users.OrderBy(x=>x.FullName).ToListAsync();return Page();}Input.Username=Input.Username.Trim();Input.PasswordHash=hasher.HashPassword(Input,Password);Input.CreatedAt=DateTime.Now;db.Users.Add(Input);await db.SaveChangesAsync();await audit.RecordAsync("User created",$"{Input.Username} created with role {Input.Role}.","User",Input.Id.ToString());return RedirectToPage();}
    public async Task<IActionResult> OnPostRoleAsync(int id, string role){if(!Roles.All.Contains(role))return BadRequest();var item=await db.Users.FindAsync(id);if(item is null)return NotFound();if(item.Username==User.Identity?.Name)return BadRequest();item.Role=role;await db.SaveChangesAsync();await audit.RecordAsync("User role changed",$"{item.Username} is now {role}.","User",id.ToString());return RedirectToPage();}
    public async Task<IActionResult> OnPostToggleAsync(int id){var item=await db.Users.FindAsync(id);if(item is null)return NotFound();if(item.Username==User.Identity?.Name)return BadRequest();item.IsActive=!item.IsActive;await db.SaveChangesAsync();await audit.RecordAsync("User status changed",$"{item.Username} is {(item.IsActive?"active":"inactive")}.","User",id.ToString());return RedirectToPage();}
}
