using DigitalEvidenceSystem.Web.Models;

namespace DigitalEvidenceSystem.Web.Data;

public sealed class AuditService(EvidenceDbContext db, IHttpContextAccessor accessor)
{
    public async Task RecordAsync(string action, string details, string entity = "System", string entityId = "")
    {
        var context = accessor.HttpContext;
        var actor = context?.User.Identity?.Name ?? "System";
        var ip = context?.Connection.RemoteIpAddress?.ToString() ?? "";
        db.AuditLogs.Add(new AuditLog
        {
            Actor = actor,
            Action = action,
            Entity = entity,
            EntityId = entityId,
            Details = details,
            IpAddress = ip,
            OccurredAt = DateTime.Now
        });
        await db.SaveChangesAsync();
    }
}
