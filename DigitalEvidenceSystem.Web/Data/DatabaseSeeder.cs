using DigitalEvidenceSystem.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

namespace DigitalEvidenceSystem.Web.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(EvidenceDbContext db)
    {
        await db.Database.EnsureCreatedAsync();
        if (db.Database.IsSqlite()) await UpgradeExistingSqliteAsync(db);

        if (!await db.Users.AnyAsync())
        {
            var hasher = new PasswordHasher<AppUser>();
            var admin = new AppUser
            {
                Username = "admin",
                FullName = "Wasi Ahbab",
                Role = Roles.Administrator,
                Department = "Digital Forensics",
                Email = "admin@tracelock.local"
            };
            admin.PasswordHash = hasher.HashPassword(admin, "Admin@123!");
            db.Users.Add(admin);

            var cases = new[]
            {
                new CaseFile { CaseNumber="CF-2026-001", Title="Cyber Fraud Investigation", CaseType="Cybercrime", Description="Financial cyber-fraud investigation.", Status="In Progress", Priority="High", Investigator="Wasi Ahbab", OpenedOn=DateTime.Today.AddDays(-12) },
                new CaseFile { CaseNumber="CF-2026-002", Title="Mobile Device Analysis", CaseType="Device Examination", Status="Open", Priority="Medium", Investigator="Wasi Ahbab", OpenedOn=DateTime.Today.AddDays(-7) },
                new CaseFile { CaseNumber="CF-2026-003", Title="Data Breach Review", CaseType="Incident Response", Status="Pending Review", Priority="High", Investigator="Wasi Ahbab", OpenedOn=DateTime.Today.AddDays(-3) }
            };
            db.Cases.AddRange(cases);
            await db.SaveChangesAsync();

            db.Evidence.AddRange(
                new EvidenceItem { EvidenceNumber="EV-001", Name="Laptop disk image", Type="Hard drive", Status="In Examination", StorageLocation="Locker A-04", CurrentCustodian="Wasi Ahbab", Collector="Intake Unit", CaseFileId=cases[0].Id, CollectedOn=DateTime.Today.AddDays(-11) },
                new EvidenceItem { EvidenceNumber="EV-002", Name="Mobile phone extraction", Type="Mobile phone", Status="Received", StorageLocation="Locker B-02", CurrentCustodian="Evidence Custodian", Collector="Intake Unit", CaseFileId=cases[1].Id, CollectedOn=DateTime.Today.AddDays(-6) },
                new EvidenceItem { EvidenceNumber="EV-003", Name="Network log archive", Type="Network capture", Status="Reviewed", StorageLocation="Digital Vault", CurrentCustodian="Forensic Analyst", Collector="SOC Team", CaseFileId=cases[2].Id, CollectedOn=DateTime.Today.AddDays(-2) });
            await db.SaveChangesAsync();

            db.ForensicAnalyses.Add(new ForensicAnalysis { EvidenceItemId = 1, Analyst = "Wasi Ahbab", Status = "In Progress", ToolsUsed = "Autopsy / FTK Imager", Findings = "Initial acquisition completed; review in progress." });
            db.Notifications.AddRange(
                new Notification { Recipient="Wasi Ahbab", Title="Evidence awaiting analysis", Message="EV-002 is ready to be assigned for forensic examination.", Severity="Info" },
                new Notification { Recipient="Wasi Ahbab", Title="Priority case", Message="CF-2026-001 is a high-priority active investigation.", Severity="Warning" });
            db.AuditLogs.Add(new AuditLog { Actor="System", Action="System initialized", Entity="System", Details="TraceLock evidence workspace initialized with demo records." });
            await db.SaveChangesAsync();
        }
    }

    private static async Task UpgradeExistingSqliteAsync(EvidenceDbContext db)
    {
        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync();
        var tables = new[]
        {
            "CREATE TABLE IF NOT EXISTS CaseNotes (Id INTEGER NOT NULL CONSTRAINT PK_CaseNotes PRIMARY KEY AUTOINCREMENT, CaseFileId INTEGER NOT NULL, Note TEXT NOT NULL, Author TEXT NOT NULL, CreatedAt TEXT NOT NULL)",
            "CREATE TABLE IF NOT EXISTS CasePeople (Id INTEGER NOT NULL CONSTRAINT PK_CasePeople PRIMARY KEY AUTOINCREMENT, CaseFileId INTEGER NOT NULL, Name TEXT NOT NULL, Relationship TEXT NOT NULL, Notes TEXT NOT NULL)",
            "CREATE TABLE IF NOT EXISTS ForensicAnalyses (Id INTEGER NOT NULL CONSTRAINT PK_ForensicAnalyses PRIMARY KEY AUTOINCREMENT, EvidenceItemId INTEGER NOT NULL, Analyst TEXT NOT NULL, Status TEXT NOT NULL, ToolsUsed TEXT NOT NULL, Findings TEXT NOT NULL, Notes TEXT NOT NULL, ReportReference TEXT NOT NULL, StartedAt TEXT NOT NULL, CompletedAt TEXT NULL, UpdatedAt TEXT NOT NULL)",
            "CREATE TABLE IF NOT EXISTS EvidenceFiles (Id INTEGER NOT NULL CONSTRAINT PK_EvidenceFiles PRIMARY KEY AUTOINCREMENT, EvidenceItemId INTEGER NOT NULL, FileName TEXT NOT NULL, ContentType TEXT NOT NULL, FilePath TEXT NOT NULL, SizeBytes INTEGER NOT NULL, Sha256 TEXT NOT NULL, UploadedAt TEXT NOT NULL, UploadedBy TEXT NOT NULL)",
            "CREATE TABLE IF NOT EXISTS Notifications (Id INTEGER NOT NULL CONSTRAINT PK_Notifications PRIMARY KEY AUTOINCREMENT, Recipient TEXT NOT NULL, Title TEXT NOT NULL, Message TEXT NOT NULL, Severity TEXT NOT NULL, IsRead INTEGER NOT NULL, CreatedAt TEXT NOT NULL)"
        };
        foreach (var sql in tables) await new SqliteCommand(sql, (SqliteConnection)connection).ExecuteNonQueryAsync();

        var additions = new Dictionary<string, Dictionary<string, string>>
        {
            ["Users"] = new() { ["Email"]="TEXT NOT NULL DEFAULT ''", ["Department"]="TEXT NOT NULL DEFAULT ''", ["CreatedAt"]="TEXT NOT NULL DEFAULT '2000-01-01 00:00:00'", ["LastLoginAt"]="TEXT NULL" },
            ["Cases"] = new() { ["CaseType"]="TEXT NOT NULL DEFAULT 'Cybercrime'", ["Description"]="TEXT NOT NULL DEFAULT ''", ["CreatedAt"]="TEXT NOT NULL DEFAULT '2000-01-01 00:00:00'", ["UpdatedAt"]="TEXT NOT NULL DEFAULT '2000-01-01 00:00:00'", ["ClosedOn"]="TEXT NULL" },
            ["Evidence"] = new() { ["Description"]="TEXT NOT NULL DEFAULT ''", ["SourceDevice"]="TEXT NOT NULL DEFAULT ''", ["AssociatedPerson"]="TEXT NOT NULL DEFAULT ''", ["CollectionLocation"]="TEXT NOT NULL DEFAULT ''", ["Collector"]="TEXT NOT NULL DEFAULT ''", ["CurrentCustodian"]="TEXT NOT NULL DEFAULT ''", ["HashSha256"]="TEXT NOT NULL DEFAULT ''", ["HashMd5"]="TEXT NOT NULL DEFAULT ''", ["HashGeneratedAt"]="TEXT NULL", ["IntegrityStatus"]="TEXT NOT NULL DEFAULT 'Not verified'", ["FileReference"]="TEXT NOT NULL DEFAULT ''", ["Notes"]="TEXT NOT NULL DEFAULT ''", ["CreatedAt"]="TEXT NOT NULL DEFAULT '2000-01-01 00:00:00'", ["UpdatedAt"]="TEXT NOT NULL DEFAULT '2000-01-01 00:00:00'" },
            ["CustodyRecords"] = new() { ["Location"]="TEXT NOT NULL DEFAULT ''", ["Condition"]="TEXT NOT NULL DEFAULT 'Sealed / intact'", ["Authorization"]="TEXT NOT NULL DEFAULT ''", ["Notes"]="TEXT NOT NULL DEFAULT ''", ["RecordedAt"]="TEXT NOT NULL DEFAULT '2000-01-01 00:00:00'" },
            ["AuditLogs"] = new() { ["Entity"]="TEXT NOT NULL DEFAULT 'System'", ["EntityId"]="TEXT NOT NULL DEFAULT ''", ["IpAddress"]="TEXT NOT NULL DEFAULT ''" }
        };
        foreach (var pair in additions)
        {
            var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using var pragma = connection.CreateCommand();
            pragma.CommandText = $"PRAGMA table_info([{pair.Key}])";
            using var reader = await pragma.ExecuteReaderAsync();
            while (await reader.ReadAsync()) existing.Add(reader.GetString(1));
            await reader.DisposeAsync();
            foreach (var column in pair.Value)
            {
                if (existing.Contains(column.Key)) continue;
                using var alter = connection.CreateCommand();
                alter.CommandText = $"ALTER TABLE [{pair.Key}] ADD COLUMN [{column.Key}] {column.Value}";
                await alter.ExecuteNonQueryAsync();
            }
        }
    }
}
