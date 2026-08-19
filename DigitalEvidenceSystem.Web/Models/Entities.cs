using System.ComponentModel.DataAnnotations;

namespace DigitalEvidenceSystem.Web.Models;

public static class Roles
{
    public const string Administrator = "Administrator";
    public const string Investigator = "Investigator";
    public const string Analyst = "Forensic Analyst";
    public const string Custodian = "Evidence Custodian";
    public static readonly string[] All = [Administrator, Investigator, Analyst, Custodian];
}

public sealed class AppUser
{
    public int Id { get; set; }
    [Required, StringLength(80)] public string Username { get; set; } = "";
    [Required] public string PasswordHash { get; set; } = "";
    [Required, StringLength(120)] public string FullName { get; set; } = "";
    [Required, StringLength(60)] public string Role { get; set; } = Roles.Analyst;
    [StringLength(160)] public string Email { get; set; } = "";
    [StringLength(80)] public string Department { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? LastLoginAt { get; set; }
}

public sealed class CaseFile
{
    public int Id { get; set; }
    [Required, StringLength(30)] public string CaseNumber { get; set; } = "";
    [Required, StringLength(160)] public string Title { get; set; } = "";
    [StringLength(30)] public string CaseType { get; set; } = "Cybercrime";
    [StringLength(1000)] public string Description { get; set; } = "";
    [Required, StringLength(30)] public string Status { get; set; } = "Open";
    [Required, StringLength(20)] public string Priority { get; set; } = "Medium";
    [StringLength(120)] public string Investigator { get; set; } = "";
    public DateTime OpenedOn { get; set; } = DateTime.Today;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public DateTime? ClosedOn { get; set; }
    public ICollection<CaseNote> Notes { get; set; } = new List<CaseNote>();
}

public sealed class CaseNote
{
    public int Id { get; set; }
    public int CaseFileId { get; set; }
    public CaseFile? CaseFile { get; set; }
    [Required, StringLength(1000)] public string Note { get; set; } = "";
    [Required, StringLength(120)] public string Author { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public sealed class CasePerson
{
    public int Id { get; set; }
    public int CaseFileId { get; set; }
    public CaseFile? CaseFile { get; set; }
    [Required, StringLength(120)] public string Name { get; set; } = "";
    [Required, StringLength(40)] public string Relationship { get; set; } = "Relevant Person";
    [StringLength(250)] public string Notes { get; set; } = "";
}

public sealed class EvidenceItem
{
    public int Id { get; set; }
    [Required, StringLength(30)] public string EvidenceNumber { get; set; } = "";
    [Required, StringLength(160)] public string Name { get; set; } = "";
    [Required, StringLength(40)] public string Type { get; set; } = "Document";
    [StringLength(1000)] public string Description { get; set; } = "";
    [StringLength(160)] public string SourceDevice { get; set; } = "";
    [StringLength(120)] public string AssociatedPerson { get; set; } = "";
    [Required, StringLength(30)] public string Status { get; set; } = "Received";
    [StringLength(100)] public string StorageLocation { get; set; } = "";
    [StringLength(160)] public string CollectionLocation { get; set; } = "";
    [StringLength(120)] public string Collector { get; set; } = "";
    [StringLength(120)] public string CurrentCustodian { get; set; } = "";
    [StringLength(128)] public string HashSha256 { get; set; } = "";
    [StringLength(32)] public string HashMd5 { get; set; } = "";
    public DateTime? HashGeneratedAt { get; set; }
    [StringLength(30)] public string IntegrityStatus { get; set; } = "Not verified";
    [StringLength(500)] public string FileReference { get; set; } = "";
    [StringLength(1000)] public string Notes { get; set; } = "";
    public DateTime CollectedOn { get; set; } = DateTime.Today;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public int? CaseFileId { get; set; }
    public CaseFile? CaseFile { get; set; }
}

public sealed class CustodyRecord
{
    public int Id { get; set; }
    public int EvidenceItemId { get; set; }
    public EvidenceItem? EvidenceItem { get; set; }
    [Required, StringLength(100)] public string FromPerson { get; set; } = "";
    [Required, StringLength(100)] public string ToPerson { get; set; } = "";
    [Required, StringLength(160)] public string Purpose { get; set; } = "";
    [StringLength(160)] public string Location { get; set; } = "";
    [StringLength(40)] public string Condition { get; set; } = "Sealed / intact";
    [StringLength(120)] public string Authorization { get; set; } = "";
    [StringLength(500)] public string Notes { get; set; } = "";
    public DateTime TransferredAt { get; set; } = DateTime.Now;
    public DateTime RecordedAt { get; set; } = DateTime.Now;
}

public sealed class ForensicAnalysis
{
    public int Id { get; set; }
    public int EvidenceItemId { get; set; }
    public EvidenceItem? EvidenceItem { get; set; }
    [Required, StringLength(120)] public string Analyst { get; set; } = "";
    [Required, StringLength(30)] public string Status { get; set; } = "Pending";
    [StringLength(160)] public string ToolsUsed { get; set; } = "";
    [StringLength(2000)] public string Findings { get; set; } = "";
    [StringLength(2000)] public string Notes { get; set; } = "";
    [StringLength(500)] public string ReportReference { get; set; } = "";
    public DateTime StartedAt { get; set; } = DateTime.Now;
    public DateTime? CompletedAt { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

public sealed class EvidenceFile
{
    public int Id { get; set; }
    public int EvidenceItemId { get; set; }
    public EvidenceItem? EvidenceItem { get; set; }
    [Required, StringLength(160)] public string FileName { get; set; } = "";
    [StringLength(80)] public string ContentType { get; set; } = "application/octet-stream";
    [Required, StringLength(500)] public string FilePath { get; set; } = "";
    public long SizeBytes { get; set; }
    [StringLength(128)] public string Sha256 { get; set; } = "";
    public DateTime UploadedAt { get; set; } = DateTime.Now;
    [StringLength(120)] public string UploadedBy { get; set; } = "";
}

public sealed class Notification
{
    public int Id { get; set; }
    [Required, StringLength(120)] public string Recipient { get; set; } = "";
    [Required, StringLength(160)] public string Title { get; set; } = "";
    [Required, StringLength(500)] public string Message { get; set; } = "";
    [StringLength(20)] public string Severity { get; set; } = "Info";
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public sealed class AuditLog
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string Actor { get; set; } = "System";
    [Required, StringLength(80)] public string Action { get; set; } = "";
    [StringLength(80)] public string Entity { get; set; } = "";
    [StringLength(80)] public string EntityId { get; set; } = "";
    [Required, StringLength(220)] public string Details { get; set; } = "";
    [StringLength(80)] public string IpAddress { get; set; } = "";
    public DateTime OccurredAt { get; set; } = DateTime.Now;
}
