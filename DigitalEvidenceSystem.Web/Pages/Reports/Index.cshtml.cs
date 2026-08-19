using System.Text;
using DigitalEvidenceSystem.Web.Data;
using DigitalEvidenceSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
namespace DigitalEvidenceSystem.Web.Pages.Reports;
[Authorize]
public sealed class IndexModel(EvidenceDbContext db):PageModel
{
    public int EvidenceCount{get;set;} public int CaseCount{get;set;} public int TransferCount{get;set;} public int AnalysisCount{get;set;} public List<AuditLog> AuditLogs{get;set;}=[];
    public async Task OnGetAsync(){EvidenceCount=await db.Evidence.CountAsync();CaseCount=await db.Cases.CountAsync();TransferCount=await db.CustodyRecords.CountAsync();AnalysisCount=await db.ForensicAnalyses.CountAsync();AuditLogs=await db.AuditLogs.OrderByDescending(x=>x.OccurredAt).Take(25).ToListAsync();}
    public async Task<FileContentResult> OnGetEvidenceCsvAsync(){var rows=await db.Evidence.Include(x=>x.CaseFile).OrderBy(x=>x.EvidenceNumber).ToListAsync();var csv=new StringBuilder("Evidence Number,Name,Type,Status,Case,Custodian,Storage,Integrity,SHA-256,Collected On\n");foreach(var row in rows)csv.AppendLine(string.Join(',',Escape(row.EvidenceNumber),Escape(row.Name),Escape(row.Type),Escape(row.Status),Escape(row.CaseFile?.CaseNumber??""),Escape(row.CurrentCustodian),Escape(row.StorageLocation),Escape(row.IntegrityStatus),Escape(row.HashSha256),row.CollectedOn.ToString("yyyy-MM-dd HH:mm")));return File(Encoding.UTF8.GetBytes(csv.ToString()),"text/csv","tracelock-evidence-report.csv");}
    public async Task<FileContentResult> OnGetCustodyCsvAsync(){var rows=await db.CustodyRecords.Include(x=>x.EvidenceItem).OrderBy(x=>x.TransferredAt).ToListAsync();var csv=new StringBuilder("Evidence,From,To,Purpose,Location,Condition,Authorization,Transferred At,Recorded At\n");foreach(var r in rows)csv.AppendLine(string.Join(',',Escape(r.EvidenceItem?.EvidenceNumber??""),Escape(r.FromPerson),Escape(r.ToPerson),Escape(r.Purpose),Escape(r.Location),Escape(r.Condition),Escape(r.Authorization),r.TransferredAt.ToString("yyyy-MM-dd HH:mm"),r.RecordedAt.ToString("yyyy-MM-dd HH:mm")));return File(Encoding.UTF8.GetBytes(csv.ToString()),"text/csv","tracelock-chain-of-custody.csv");}
    public async Task<FileContentResult> OnGetAuditCsvAsync(){var rows=await db.AuditLogs.OrderByDescending(x=>x.OccurredAt).ToListAsync();var csv=new StringBuilder("Timestamp,Actor,Action,Entity,Entity Id,Details,IP\n");foreach(var r in rows)csv.AppendLine(string.Join(',',r.OccurredAt.ToString("yyyy-MM-dd HH:mm:ss"),Escape(r.Actor),Escape(r.Action),Escape(r.Entity),Escape(r.EntityId),Escape(r.Details),Escape(r.IpAddress)));return File(Encoding.UTF8.GetBytes(csv.ToString()),"text/csv","tracelock-audit-history.csv");}
    private static string Escape(string text)=>$"\"{(text??"").Replace("\"","\"\"")}\"";
}
