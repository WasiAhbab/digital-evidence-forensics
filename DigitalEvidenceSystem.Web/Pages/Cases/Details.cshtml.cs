using DigitalEvidenceSystem.Web.Data;
using DigitalEvidenceSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
namespace DigitalEvidenceSystem.Web.Pages.Cases;
[Authorize]
public sealed class DetailsModel(EvidenceDbContext db, AuditService audit):PageModel
{
    public CaseFile Item{get;set;}=new(); public List<EvidenceItem> Evidence{get;set;}=[]; public List<CaseNote> Notes{get;set;}=[]; public List<CasePerson> People{get;set;}=[];
    [BindProperty] public string Note{get;set;}=""; [BindProperty] public string PersonName{get;set;}=""; [BindProperty] public string Relationship{get;set;}="Relevant Person"; [BindProperty] public string PersonNotes{get;set;}="";
    public async Task<IActionResult> OnGetAsync(int id){if(!await Load(id))return NotFound();return Page();}
    public async Task<IActionResult> OnPostNoteAsync(int id){if(string.IsNullOrWhiteSpace(Note))return RedirectToPage(new{id});if(!await db.Cases.AnyAsync(x=>x.Id==id))return NotFound();db.CaseNotes.Add(new CaseNote{CaseFileId=id,Note=Note.Trim(),Author=User.Identity?.Name??"",CreatedAt=DateTime.Now});await db.SaveChangesAsync();await audit.RecordAsync("Case note added",$"Note added to case #{id}.","Case",id.ToString());return RedirectToPage(new{id});}
    public async Task<IActionResult> OnPostPersonAsync(int id){if(string.IsNullOrWhiteSpace(PersonName))return RedirectToPage(new{id});if(!await db.Cases.AnyAsync(x=>x.Id==id))return NotFound();db.CasePeople.Add(new CasePerson{CaseFileId=id,Name=PersonName.Trim(),Relationship=Relationship,Notes=PersonNotes});await db.SaveChangesAsync();await audit.RecordAsync("Case person added",$"{PersonName} added to case #{id} as {Relationship}.","Case",id.ToString());return RedirectToPage(new{id});}
    private async Task<bool> Load(int id){var item=await db.Cases.FindAsync(id);if(item is null)return false;Item=item;Evidence=await db.Evidence.Where(x=>x.CaseFileId==id).OrderByDescending(x=>x.CollectedOn).ToListAsync();Notes=await db.CaseNotes.Where(x=>x.CaseFileId==id).OrderByDescending(x=>x.CreatedAt).ToListAsync();People=await db.CasePeople.Where(x=>x.CaseFileId==id).ToListAsync();return true;}
}
