using System.Security.Cryptography;
using DigitalEvidenceSystem.Web.Data;
using DigitalEvidenceSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
namespace DigitalEvidenceSystem.Web.Pages.Files;
[Authorize]
public sealed class IndexModel(EvidenceDbContext db, IWebHostEnvironment env, AuditService audit) : PageModel
{
    public EvidenceItem? Evidence {get;set;}
    public List<EvidenceFile> Items {get;set;}=[];
    [BindProperty] public int EvidenceId{get;set;}
    [BindProperty] public IFormFile? Upload{get;set;}
    public async Task<IActionResult> OnGetAsync(int evidenceId){Evidence=await db.Evidence.FindAsync(evidenceId);if(Evidence is null)return NotFound();Items=await db.EvidenceFiles.Where(x=>x.EvidenceItemId==evidenceId).OrderByDescending(x=>x.UploadedAt).ToListAsync();return Page();}
    public async Task<IActionResult> OnPostAsync(){Evidence=await db.Evidence.FindAsync(EvidenceId);if(Evidence is null)return NotFound();if(Upload is null||Upload.Length==0){ModelState.AddModelError("Upload","Choose a file.");Items=await db.EvidenceFiles.Where(x=>x.EvidenceItemId==EvidenceId).ToListAsync();return Page();}if(Upload.Length>50*1024*1024){ModelState.AddModelError("Upload","Maximum file size is 50 MB.");Items=await db.EvidenceFiles.Where(x=>x.EvidenceItemId==EvidenceId).ToListAsync();return Page();}
        var safeName=Path.GetFileName(Upload.FileName);var ext=Path.GetExtension(safeName);var allowed=new[]{".pdf",".txt",".csv",".jpg",".jpeg",".png",".mp4",".wav",".docx",".zip"};if(!allowed.Contains(ext,StringComparer.OrdinalIgnoreCase)){ModelState.AddModelError("Upload","This file type is not allowed.");Items=await db.EvidenceFiles.Where(x=>x.EvidenceItemId==EvidenceId).ToListAsync();return Page();}
        var root=Path.Combine(env.ContentRootPath,"App_Data","EvidenceStorage",Evidence.EvidenceNumber);Directory.CreateDirectory(root);var stored=$"{Guid.NewGuid():N}{ext}";var full=Path.Combine(root,stored);await using(var input=Upload.OpenReadStream())await using(var output=System.IO.File.Create(full)){await input.CopyToAsync(output);}string hash;await using(var stream=System.IO.File.OpenRead(full)){hash=Convert.ToHexString(await SHA256.HashDataAsync(stream));}
        db.EvidenceFiles.Add(new EvidenceFile{EvidenceItemId=EvidenceId,FileName=safeName,ContentType=Upload.ContentType,FilePath=Path.Combine("App_Data","EvidenceStorage",Evidence.EvidenceNumber,stored),SizeBytes=Upload.Length,Sha256=hash,UploadedBy=User.Identity?.Name??"",UploadedAt=DateTime.Now});Evidence.HashSha256=hash;Evidence.HashGeneratedAt=DateTime.Now;Evidence.IntegrityStatus="Verified";Evidence.UpdatedAt=DateTime.Now;await db.SaveChangesAsync();await audit.RecordAsync("Evidence file uploaded",$"{safeName} attached to {Evidence.EvidenceNumber}; SHA-256 {hash}.","EvidenceFile",EvidenceId.ToString());return RedirectToPage(new{evidenceId=EvidenceId});
    }
    public async Task<IActionResult> OnGetDownloadAsync(int id){var item=await db.EvidenceFiles.Include(x=>x.EvidenceItem).SingleOrDefaultAsync(x=>x.Id==id);if(item is null)return NotFound();var full=Path.Combine(env.ContentRootPath,item.FilePath);if(!System.IO.File.Exists(full))return NotFound();await audit.RecordAsync("Evidence file accessed",$"{item.FileName} downloaded for {item.EvidenceItem?.EvidenceNumber}.","EvidenceFile",id.ToString());return PhysicalFile(full,item.ContentType,item.FileName);}
}
