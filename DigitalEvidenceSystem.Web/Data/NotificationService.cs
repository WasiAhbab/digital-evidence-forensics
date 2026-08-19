using DigitalEvidenceSystem.Web.Models;

namespace DigitalEvidenceSystem.Web.Data;

public sealed class NotificationService(EvidenceDbContext db)
{
    public async Task NotifyAllAsync(string title, string message, string severity = "Info")
    {
        db.Notifications.Add(new Notification
        {
            Recipient = "*",
            Title = title,
            Message = message,
            Severity = severity,
            CreatedAt = DateTime.Now
        });

        await db.SaveChangesAsync();
    }

    public async Task NotifyAsync(string recipient, string title, string message, string severity = "Info")
    {
        db.Notifications.Add(new Notification
        {
            Recipient = recipient,
            Title = title,
            Message = message,
            Severity = severity,
            CreatedAt = DateTime.Now
        });

        await db.SaveChangesAsync();
    }
}