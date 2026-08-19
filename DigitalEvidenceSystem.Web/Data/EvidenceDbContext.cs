using DigitalEvidenceSystem.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace DigitalEvidenceSystem.Web.Data;

public sealed class EvidenceDbContext(DbContextOptions<EvidenceDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<CaseFile> Cases => Set<CaseFile>();
    public DbSet<CaseNote> CaseNotes => Set<CaseNote>();
    public DbSet<CasePerson> CasePeople => Set<CasePerson>();
    public DbSet<EvidenceItem> Evidence => Set<EvidenceItem>();
    public DbSet<CustodyRecord> CustodyRecords => Set<CustodyRecord>();
    public DbSet<ForensicAnalysis> ForensicAnalyses => Set<ForensicAnalysis>();
    public DbSet<EvidenceFile> EvidenceFiles => Set<EvidenceFile>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<AppUser>().HasIndex(x => x.Username).IsUnique();
        b.Entity<CaseFile>().HasIndex(x => x.CaseNumber).IsUnique();
        b.Entity<EvidenceItem>().HasIndex(x => x.EvidenceNumber).IsUnique();
        b.Entity<EvidenceItem>().HasIndex(x => new { x.CaseFileId, x.Status });
        b.Entity<CaseFile>().HasIndex(x => new { x.Status, x.Priority });
        b.Entity<AuditLog>().HasIndex(x => x.OccurredAt);
        b.Entity<AuditLog>().HasIndex(x => new { x.Entity, x.EntityId });
        b.Entity<Notification>().HasIndex(x => new { x.Recipient, x.IsRead });
        b.Entity<CaseNote>().HasOne(x => x.CaseFile).WithMany(x => x.Notes).HasForeignKey(x => x.CaseFileId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<CasePerson>().HasOne(x => x.CaseFile).WithMany().HasForeignKey(x => x.CaseFileId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<EvidenceItem>().HasOne(x => x.CaseFile).WithMany().HasForeignKey(x => x.CaseFileId).OnDelete(DeleteBehavior.SetNull);
        b.Entity<CustodyRecord>().HasOne(x => x.EvidenceItem).WithMany().HasForeignKey(x => x.EvidenceItemId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ForensicAnalysis>().HasOne(x => x.EvidenceItem).WithMany().HasForeignKey(x => x.EvidenceItemId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<EvidenceFile>().HasOne(x => x.EvidenceItem).WithMany().HasForeignKey(x => x.EvidenceItemId).OnDelete(DeleteBehavior.Cascade);
    }
}
