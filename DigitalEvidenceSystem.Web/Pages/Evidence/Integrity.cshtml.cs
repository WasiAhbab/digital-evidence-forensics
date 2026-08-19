using System.Security.Cryptography;
using DigitalEvidenceSystem.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
namespace DigitalEvidenceSystem.Web.Pages.Evidence;
[Authorize]
public sealed class IntegrityModel(EvidenceDbContext db, IWebHostEnvironment env, AuditService audit) : PageModel
{
    public Models.EvidenceItem Item {get;set;}=new(); public List<(string File,string Expected,string Actual,bool Match)> Results{get;set;}=[];
    public async Task<IActionResult> OnGetAsync(int id){var item=await db.Evidence.Include(x=>x.CaseFile).SingleOrDefaultAsync(x=>x.Id==id);if(item is null)return NotFound();Item=item;return Page();}
    public async Task<IActionResult> OnPostAsync(int id){var item=await db.Evidence.Include(x=>x.CaseFile).SingleOrDefaultAsync(x=>x.Id==id);if(item is null)return NotFound();Item=item;var files=await db.EvidenceFiles.Where(x=>x.EvidenceItemId==id).ToListAsync();var all=true;foreach(var f in files){var full=Path.Combine(env.ContentRootPath,f.FilePath);if(!System.IO.File.Exists(full)){Results.Add((f.FileName,f.Sha256,"Missing",false));all=false;continue;}string actual;await using(var s=System.IO.File.OpenRead(full))actual=Convert.ToHexString(await SHA256.HashDataAsync(s));var match=string.Equals(actual,f.Sha256,StringComparison.OrdinalIgnoreCase);Results.Add((f.FileName,f.Sha256,actual,match));if(!match)all=false;}
        item.IntegrityStatus=files.Count==0?"Not verified":all?"Verified":"Hash mismatch";item.UpdatedAt=DateTime.Now;await db.SaveChangesAsync();await audit.RecordAsync("Evidence integrity verified",$"{item.EvidenceNumber}: {item.IntegrityStatus}.","Evidence",id.ToString());return Page();}
}
