namespace Pireon.API.ViewModels;

public class OrganizationIndexViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int SitesCount { get; set; }
}
