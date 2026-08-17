namespace Pireon.API.Models;

public class CampusSite : AuditableEntity
{
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public string CampusKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string School { get; set; } = string.Empty;
    public double CenterLatitude { get; set; }
    public double CenterLongitude { get; set; }
    public int Zoom { get; set; } = 16;
    public int MinZoom { get; set; } = 0;
    public int MaxZoom { get; set; } = 19;
    public double BoundsMinLatitude { get; set; }
    public double BoundsMinLongitude { get; set; }
    public double BoundsMaxLatitude { get; set; }
    public double BoundsMaxLongitude { get; set; }
    public string FloorsJson { get; set; } = "[]";
    public string DefaultFloor { get; set; } = string.Empty;
}
