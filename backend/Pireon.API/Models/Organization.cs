namespace Pireon.API.Models;

public class Organization : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;

    public ICollection<CampusSite> Sites { get; set; } = new List<CampusSite>();
}
