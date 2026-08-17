using System.ComponentModel.DataAnnotations;

namespace Pireon.API.ViewModels;

public class OrganizationSiteFormViewModel
{
    public Guid OrganizationId { get; set; }
    public Guid? SiteId { get; set; }

    [Required(ErrorMessage = "La clave del sitio es obligatoria.")]
    public string CampusKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre del sitio es obligatorio.")]
    public string Name { get; set; } = string.Empty;

    public string School { get; set; } = string.Empty;

    public string CenterLatitude { get; set; } = string.Empty;
    public string CenterLongitude { get; set; } = string.Empty;
    public string Zoom { get; set; } = string.Empty;
    public string MinZoom { get; set; } = string.Empty;
    public string MaxZoom { get; set; } = string.Empty;
    public string BoundsMinLatitude { get; set; } = string.Empty;
    public string BoundsMinLongitude { get; set; } = string.Empty;
    public string BoundsMaxLatitude { get; set; } = string.Empty;
    public string BoundsMaxLongitude { get; set; } = string.Empty;

    public string FloorsCsv { get; set; } = string.Empty;
    public string DefaultFloor { get; set; } = string.Empty;
}
