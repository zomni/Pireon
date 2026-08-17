namespace Pireon.API.ViewModels;

public class TelemetryScanScheduleDto
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Cron { get; set; } = string.Empty;
    public string TimeZone { get; set; } = "America/Santiago";
    public string CampusKey { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public int SortOrder { get; set; }
    public bool IsValid { get; set; }
    public string ValidationError { get; set; } = string.Empty;
    public DateTime? NextOccurrenceUtc { get; set; }
    public DateTime? NextOccurrenceLocal { get; set; }
}

public class TelemetryScanScheduleRequest
{
    public string Label { get; set; } = string.Empty;
    public string Cron { get; set; } = string.Empty;
    public string TimeZone { get; set; } = "America/Santiago";
    public string CampusKey { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public int SortOrder { get; set; }
}
