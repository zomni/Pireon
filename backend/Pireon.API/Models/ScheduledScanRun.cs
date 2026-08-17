namespace Pireon.API.Models;

public class ScheduledScanRun : AuditableEntity
{
    public string CampusKey { get; set; } = string.Empty;
    public DateTime ScheduledAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string Status { get; set; } = "pending";
    public string? ErrorMessage { get; set; }
    public Guid? SnapshotId { get; set; }
    public NetworkTelemetrySnapshot? Snapshot { get; set; }
    public string ScheduledTimeLocal { get; set; } = string.Empty;
    public string ScheduledDayLocal { get; set; } = string.Empty;
    public int? DeviceCount { get; set; }
    public int? UserCount { get; set; }
    public string NormalizedCron { get; set; } = string.Empty;
}
