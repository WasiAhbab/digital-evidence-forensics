using System.Security.Cryptography;
using DigitalEvidenceSystem.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace DigitalEvidenceSystem.Web.Data;

public sealed class EvidenceIntegrityMonitor(
    IServiceScopeFactory scopeFactory,
    IWebHostEnvironment environment,
    ILogger<EvidenceIntegrityMonitor> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await CheckEvidenceAsync(stoppingToken);

        using var timer = new PeriodicTimer(CheckInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await CheckEvidenceAsync(stoppingToken);
    }

    private async Task CheckEvidenceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EvidenceDbContext>();

            var files = await db.EvidenceFiles
                .Include(file => file.EvidenceItem)
                .ToListAsync(cancellationToken);

            foreach (var file in files)
            {
                if (file.EvidenceItem is null)
                    continue;

                var fullPath = Path.Combine(environment.ContentRootPath, file.FilePath);
                var actualHash = "";
                var isValid = false;

                if (File.Exists(fullPath))
                {
                    await using var stream = File.OpenRead(fullPath);
                    actualHash = Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken));
                    isValid = string.Equals(actualHash, file.Sha256, StringComparison.OrdinalIgnoreCase);
                }

                if (isValid || file.EvidenceItem.IntegrityStatus == "Hash mismatch")
                    continue;

                var problem = File.Exists(fullPath) ? "hash no longer matches" : "file is missing";
                file.EvidenceItem.IntegrityStatus = "Hash mismatch";
                file.EvidenceItem.UpdatedAt = DateTime.Now;

                db.Notifications.Add(new Notification
                {
                    Recipient = "*",
                    Title = "Evidence integrity alert",
                    Message = $"{file.EvidenceItem.EvidenceNumber}: {file.FileName} {problem}. Review this item immediately.",
                    Severity = "Warning",
                    CreatedAt = DateTime.Now
                });

                db.AuditLogs.Add(new AuditLog
                {
                    Actor = "Integrity monitor",
                    Action = "Evidence integrity alert generated",
                    Entity = "EvidenceFile",
                    EntityId = file.Id.ToString(),
                    Details = $"{file.EvidenceItem.EvidenceNumber}: {file.FileName} {problem}.",
                    OccurredAt = DateTime.Now
                });
            }

            await db.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "The evidence integrity monitor could not complete its check.");
        }
    }
}
