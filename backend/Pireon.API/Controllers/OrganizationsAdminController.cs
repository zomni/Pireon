using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pireon.API.Data;
using Pireon.API.Models;
using Pireon.API.Services;
using Pireon.API.ViewModels;

namespace Pireon.API.Controllers;

[Authorize(Roles = AppRoles.SuperAdmin)]
[Route("admin/organizations")]
public class OrganizationsAdminController : Controller
{
    private readonly AppDbContext _context;
    private readonly OrganizationAccessService _access;
    private readonly IPasswordHasher<AuthUser> _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly FrontendSyncService _frontendSyncService;
    private readonly AuditLogService _auditLogService;

    public OrganizationsAdminController(
        AppDbContext context,
        OrganizationAccessService access,
        IPasswordHasher<AuthUser> passwordHasher,
        IConfiguration configuration,
        FrontendSyncService frontendSyncService,
        AuditLogService auditLogService)
    {
        _context = context;
        _access = access;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _frontendSyncService = frontendSyncService;
        _auditLogService = auditLogService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var organizations = await _context.Organizations
            .AsNoTracking()
            .OrderBy(org => org.Name)
            .Select(org => new OrganizationIndexViewModel
            {
                Id = org.Id,
                Name = org.Name,
                Slug = org.Slug,
                ContactEmail = org.ContactEmail,
                Notes = org.Notes,
                Color = org.Color,
                IsActive = org.IsActive,
                SitesCount = org.Sites.Count(site => site.IsActive)
            })
            .ToListAsync(cancellationToken);

        return View(organizations);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        return View(new Organization());
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Organization model, CancellationToken cancellationToken)
    {
        var name = (model.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError(nameof(model.Name), "El nombre de la organizacion es obligatorio.");
        }

        var slug = (model.Slug ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = Slugify(name);
        }

        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(slug))
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                ModelState.AddModelError(nameof(model.Slug), "No se pudo generar un slug valido.");
            }

            return View(model);
        }

        if (await _context.Organizations.AnyAsync(org => org.Slug == slug, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Slug), $"Ya existe una organizacion con el slug '{slug}'.");
            return View(model);
        }

        var now = DateTime.UtcNow;
        var requestedColor = OrganizationColorPalette.Normalize(model.Color);
        var usedColors = await _context.Organizations
            .AsNoTracking()
            .Where(org => org.IsActive)
            .Select(org => org.Color)
            .ToListAsync(cancellationToken);

        var organization = new Organization
        {
            Name = name,
            Slug = slug,
            ContactEmail = (model.ContactEmail ?? string.Empty).Trim(),
            Notes = (model.Notes ?? string.Empty).Trim(),
            Color = string.IsNullOrEmpty(requestedColor)
                ? OrganizationColorPalette.NextAvailable(usedColors)
                : requestedColor,
            IsActive = true,
            CreatedBy = _access.CurrentUsername ?? "system",
            UpdatedBy = _access.CurrentUsername ?? "system",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _context.Organizations.Add(organization);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogSecurityEventAsync(
            actionType: "organization-create",
            resource: "organizations",
            summary: $"Se creo la organizacion {organization.Name}",
            details: $"Slug: {organization.Slug}",
            result: "success",
            severity: "info",
            changedByUsername: _access.CurrentUsername ?? "system",
            cancellationToken: cancellationToken);

        TempData["SuccessMessage"] = $"Organizacion '{organization.Name}' creada.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var organization = await _context.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(org => org.Id == id, cancellationToken);
        if (organization is null)
        {
            return NotFound();
        }

        return View(organization);
    }

    [HttpPost("{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Organization model, CancellationToken cancellationToken)
    {
        var organization = await _context.Organizations
            .FirstOrDefaultAsync(org => org.Id == id, cancellationToken);
        if (organization is null)
        {
            return NotFound();
        }

        var name = (model.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError(nameof(model.Name), "El nombre de la organizacion es obligatorio.");
        }

        if (!ModelState.IsValid)
        {
            model.Id = id;
            model.Slug = organization.Slug;
            return View(model);
        }

        organization.Name = name;
        organization.ContactEmail = (model.ContactEmail ?? string.Empty).Trim();
        organization.Notes = (model.Notes ?? string.Empty).Trim();
        var requestedColor = OrganizationColorPalette.Normalize(model.Color);
        if (!string.IsNullOrEmpty(requestedColor))
        {
            organization.Color = requestedColor;
        }
        organization.IsActive = model.IsActive;
        organization.UpdatedBy = _access.CurrentUsername ?? "system";
        organization.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogSecurityEventAsync(
            actionType: "organization-update",
            resource: "organizations",
            summary: $"Se actualizo la organizacion {organization.Name}",
            details: $"Slug: {organization.Slug}",
            result: "success",
            severity: "info",
            changedByUsername: _access.CurrentUsername ?? "system",
            cancellationToken: cancellationToken);

        TempData["SuccessMessage"] = "Organizacion actualizada.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteOrganization(Guid id, CancellationToken cancellationToken)
    {
        var organization = await _context.Organizations
            .FirstOrDefaultAsync(org => org.Id == id, cancellationToken);
        if (organization is null)
        {
            return NotFound();
        }

        organization.SoftDelete(_access.CurrentUsername ?? "system");
        await _context.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogSecurityEventAsync(
            actionType: "organization-delete",
            resource: "organizations",
            summary: $"Se elimino la organizacion {organization.Name}",
            details: $"Slug: {organization.Slug}",
            result: "success",
            severity: "warning",
            changedByUsername: _access.CurrentUsername ?? "system",
            cancellationToken: cancellationToken);

        TempData["SuccessMessage"] = "Organizacion eliminada.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id:guid}/sites")]
    public async Task<IActionResult> Sites(Guid id, CancellationToken cancellationToken)
    {
        var organization = await _context.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(org => org.Id == id, cancellationToken);
        if (organization is null)
        {
            return NotFound();
        }

        var sites = await _context.CampusSites
            .AsNoTracking()
            .Where(site => site.OrganizationId == id && site.IsActive)
            .OrderBy(site => site.Name)
            .ToListAsync(cancellationToken);

        var users = await _context.AuthUsers
            .AsNoTracking()
            .Where(user => user.OrganizationId == id)
            .OrderBy(user => user.Username)
            .Select(user => new
            {
                user.Id,
                user.Username,
                user.Role,
                user.IsActive,
                user.LastLoginAtUtc
            })
            .ToListAsync(cancellationToken);

        ViewBag.Organization = organization;
        ViewBag.Sites = sites;
        ViewBag.Users = users;
        return View();
    }

    [HttpGet("{id:guid}/sites/create")]
    public async Task<IActionResult> CreateSite(Guid id, CancellationToken cancellationToken)
    {
        var organization = await _context.Organizations
            .AsNoTracking()
            .AnyAsync(org => org.Id == id && org.IsActive, cancellationToken);
        if (!organization)
        {
            return NotFound();
        }

        var model = new OrganizationSiteFormViewModel
        {
            OrganizationId = id,
            Zoom = "16"
        };

        return View(model);
    }

    [HttpPost("{id:guid}/sites/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSite(Guid id, OrganizationSiteFormViewModel model, CancellationToken cancellationToken)
    {
        var organization = await _context.Organizations
            .FirstOrDefaultAsync(org => org.Id == id && org.IsActive, cancellationToken);
        if (organization is null)
        {
            return NotFound();
        }

        model.OrganizationId = id;
        if (!ValidateSiteForm(model))
        {
            return View(model);
        }

        var campusKey = Slugify(model.CampusKey);
        if (await _context.CampusSites.AnyAsync(site => site.CampusKey == campusKey, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.CampusKey), $"Ya existe un sitio con la clave '{campusKey}'.");
            return View(model);
        }

        var floors = ParseFloorsCsv(model.FloorsCsv);
        var defaultFloor = string.IsNullOrWhiteSpace(model.DefaultFloor)
            ? (floors.Count > 0 ? floors[0] : string.Empty)
            : model.DefaultFloor.Trim();

        var now = DateTime.UtcNow;
        var site = new CampusSite
        {
            OrganizationId = id,
            CampusKey = campusKey,
            Name = model.Name.Trim(),
            School = (model.School ?? string.Empty).Trim(),
            CenterLatitude = ParseDouble(model.CenterLatitude, 0),
            CenterLongitude = ParseDouble(model.CenterLongitude, 0),
            Zoom = ParseInt(model.Zoom, 16),
            MinZoom = ParseInt(model.MinZoom, 0),
            MaxZoom = ParseInt(model.MaxZoom, 19),
            BoundsMinLatitude = ParseDouble(model.BoundsMinLatitude, 0),
            BoundsMinLongitude = ParseDouble(model.BoundsMinLongitude, 0),
            BoundsMaxLatitude = ParseDouble(model.BoundsMaxLatitude, 0),
            BoundsMaxLongitude = ParseDouble(model.BoundsMaxLongitude, 0),
            FloorsJson = JsonSerializer.Serialize(floors),
            DefaultFloor = defaultFloor,
            IsActive = true,
            CreatedBy = _access.CurrentUsername ?? "system",
            UpdatedBy = _access.CurrentUsername ?? "system",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _context.CampusSites.Add(site);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogSecurityEventAsync(
            actionType: "site-create",
            resource: "organizations/sites",
            summary: $"Se creo el sitio {site.Name}",
            details: $"CampusKey: {site.CampusKey}; Organizacion: {organization.Name}",
            result: "success",
            severity: "info",
            changedByUsername: _access.CurrentUsername ?? "system",
            cancellationToken: cancellationToken);

        TempData["SuccessMessage"] = $"Sitio '{site.Name}' creado.";
        return RedirectToAction(nameof(Sites), new { id });
    }

    [HttpGet("{id:guid}/sites/{siteId:guid}/edit")]
    public async Task<IActionResult> EditSite(Guid id, Guid siteId, CancellationToken cancellationToken)
    {
        var site = await _context.CampusSites
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == siteId && item.OrganizationId == id, cancellationToken);
        if (site is null)
        {
            return NotFound();
        }

        var model = new OrganizationSiteFormViewModel
        {
            OrganizationId = id,
            SiteId = site.Id,
            CampusKey = site.CampusKey,
            Name = site.Name,
            School = site.School,
            CenterLatitude = ToInvariant(site.CenterLatitude),
            CenterLongitude = ToInvariant(site.CenterLongitude),
            Zoom = site.Zoom.ToString(CultureInfo.InvariantCulture),
            MinZoom = site.MinZoom.ToString(CultureInfo.InvariantCulture),
            MaxZoom = site.MaxZoom.ToString(CultureInfo.InvariantCulture),
            BoundsMinLatitude = ToInvariant(site.BoundsMinLatitude),
            BoundsMinLongitude = ToInvariant(site.BoundsMinLongitude),
            BoundsMaxLatitude = ToInvariant(site.BoundsMaxLatitude),
            BoundsMaxLongitude = ToInvariant(site.BoundsMaxLongitude),
            FloorsCsv = FloorsJsonToCsv(site.FloorsJson),
            DefaultFloor = site.DefaultFloor
        };

        ViewBag.Site = site;
        ViewBag.Floors = ParseFloorsJson(site.FloorsJson);
        return View(model);
    }

    [HttpPost("{id:guid}/sites/{siteId:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditSite(Guid id, Guid siteId, OrganizationSiteFormViewModel model, CancellationToken cancellationToken)
    {
        var site = await _context.CampusSites
            .FirstOrDefaultAsync(item => item.Id == siteId && item.OrganizationId == id, cancellationToken);
        if (site is null)
        {
            return NotFound();
        }

        model.OrganizationId = id;
        model.SiteId = site.Id;
        if (!ValidateSiteForm(model))
        {
            ViewBag.Site = site;
            ViewBag.Floors = ParseFloorsJson(site.FloorsJson);
            return View(model);
        }

        var campusKey = Slugify(model.CampusKey);
        if (await _context.CampusSites.AnyAsync(item => item.CampusKey == campusKey && item.Id != siteId, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.CampusKey), $"Ya existe un sitio con la clave '{campusKey}'.");
            ViewBag.Site = site;
            ViewBag.Floors = ParseFloorsJson(site.FloorsJson);
            return View(model);
        }

        var floors = ParseFloorsCsv(model.FloorsCsv);
        site.CampusKey = campusKey;
        site.Name = model.Name.Trim();
        site.School = (model.School ?? string.Empty).Trim();
        site.CenterLatitude = ParseDouble(model.CenterLatitude, site.CenterLatitude);
        site.CenterLongitude = ParseDouble(model.CenterLongitude, site.CenterLongitude);
        site.Zoom = ParseInt(model.Zoom, site.Zoom);
        site.MinZoom = ParseInt(model.MinZoom, site.MinZoom);
        site.MaxZoom = ParseInt(model.MaxZoom, site.MaxZoom);
        site.BoundsMinLatitude = ParseDouble(model.BoundsMinLatitude, site.BoundsMinLatitude);
        site.BoundsMinLongitude = ParseDouble(model.BoundsMinLongitude, site.BoundsMinLongitude);
        site.BoundsMaxLatitude = ParseDouble(model.BoundsMaxLatitude, site.BoundsMaxLatitude);
        site.BoundsMaxLongitude = ParseDouble(model.BoundsMaxLongitude, site.BoundsMaxLongitude);
        site.FloorsJson = JsonSerializer.Serialize(floors);
        site.DefaultFloor = string.IsNullOrWhiteSpace(model.DefaultFloor)
            ? (floors.Count > 0 ? floors[0] : string.Empty)
            : model.DefaultFloor.Trim();
        site.UpdatedBy = _access.CurrentUsername ?? "system";
        site.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogSecurityEventAsync(
            actionType: "site-update",
            resource: "organizations/sites",
            summary: $"Se actualizo el sitio {site.Name}",
            details: $"CampusKey: {site.CampusKey}; Organizacion: {site.OrganizationId}",
            result: "success",
            severity: "info",
            changedByUsername: _access.CurrentUsername ?? "system",
            cancellationToken: cancellationToken);

        TempData["SuccessMessage"] = "Sitio actualizado.";
        return RedirectToAction(nameof(EditSite), new { id, siteId = site.Id });
    }

    [HttpPost("{id:guid}/sites/{siteId:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSite(Guid id, Guid siteId, CancellationToken cancellationToken)
    {
        var site = await _context.CampusSites
            .FirstOrDefaultAsync(item => item.Id == siteId && item.OrganizationId == id, cancellationToken);
        if (site is null)
        {
            return NotFound();
        }

        site.SoftDelete(_access.CurrentUsername ?? "system");
        await _context.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogSecurityEventAsync(
            actionType: "site-delete",
            resource: "organizations/sites",
            summary: $"Se elimino el sitio {site.Name}",
            details: $"CampusKey: {site.CampusKey}",
            result: "success",
            severity: "warning",
            changedByUsername: _access.CurrentUsername ?? "system",
            cancellationToken: cancellationToken);

        TempData["SuccessMessage"] = "Sitio eliminado.";
        return RedirectToAction(nameof(Sites), new { id });
    }

    [HttpPost("{id:guid}/sites/{siteId:guid}/floors/{floor}/geojson")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadFloorGeoJson(Guid id, Guid siteId, string floor, IFormFile? file, CancellationToken cancellationToken)
    {
        var site = await _context.CampusSites
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == siteId && item.OrganizationId == id && item.IsActive, cancellationToken);
        if (site is null)
        {
            return NotFound();
        }

        if (file is null || file.Length == 0)
        {
            TempData["ErrorMessage"] = "Debe adjuntar un archivo GeoJSON.";
            return RedirectToAction(nameof(EditSite), new { id, siteId });
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension != ".json" && extension != ".geojson")
        {
            TempData["ErrorMessage"] = "El archivo debe tener extension .json o .geojson.";
            return RedirectToAction(nameof(EditSite), new { id, siteId });
        }

        string content;
        using (var reader = new StreamReader(file.OpenReadStream()))
        {
            content = await reader.ReadToEndAsync(cancellationToken);
        }

        if (!IsValidGeoJson(content))
        {
            TempData["ErrorMessage"] = "El contenido no es un GeoJSON valido.";
            return RedirectToAction(nameof(EditSite), new { id, siteId });
        }

        var school = string.IsNullOrWhiteSpace(site.School) ? site.CampusKey : site.School;
        var dataRoot = _frontendSyncService.ResolveDataRoot();
        Directory.CreateDirectory(dataRoot);

        var fileName = $"{school}_{site.CampusKey}_{floor.Trim()}.json";
        var targetPath = Path.Combine(dataRoot, fileName);
        await System.IO.File.WriteAllTextAsync(targetPath, content, System.Text.Encoding.UTF8, cancellationToken);

        TempData["SuccessMessage"] = $"Plano GeoJSON del piso {floor.Trim()} subido correctamente.";
        return RedirectToAction(nameof(EditSite), new { id, siteId });
    }

    [HttpPost("{id:guid}/admin-users")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateOrgAdmin(Guid id, string username, string password, CancellationToken cancellationToken)
    {
        var organization = await _context.Organizations
            .FirstOrDefaultAsync(org => org.Id == id && org.IsActive, cancellationToken);
        if (organization is null)
        {
            return NotFound();
        }

        username = (username ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(username))
        {
            TempData["ErrorMessage"] = "El nombre de usuario es obligatorio.";
            return RedirectToAction(nameof(Sites), new { id });
        }

        var normalizedUsername = username.ToUpperInvariant();
        if (await _context.AuthUsers.AnyAsync(user => user.NormalizedUsername == normalizedUsername, cancellationToken))
        {
            TempData["ErrorMessage"] = $"Ya existe un usuario con el nombre '{username}'.";
            return RedirectToAction(nameof(Sites), new { id });
        }

        var policyError = PasswordPolicyService.Validate(password, username, _configuration);
        if (policyError is not null)
        {
            TempData["ErrorMessage"] = policyError;
            return RedirectToAction(nameof(Sites), new { id });
        }

        var now = DateTime.UtcNow;
        var user = new AuthUser
        {
            Username = username,
            NormalizedUsername = normalizedUsername,
            Role = AppRoles.Admin,
            OrganizationId = organization.Id,
            CanManageUsers = true,
            IsActive = true,
            CreatedBy = _access.CurrentUsername ?? "system",
            UpdatedBy = _access.CurrentUsername ?? "system",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, password);

        _context.AuthUsers.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogSecurityEventAsync(
            actionType: "org-admin-create",
            resource: "organizations",
            summary: $"Se creo el admin de organizacion {username}",
            details: $"Organizacion: {organization.Name}",
            result: "success",
            severity: "info",
            changedByUsername: _access.CurrentUsername ?? "system",
            cancellationToken: cancellationToken);

        TempData["SuccessMessage"] = $"Admin de organizacion '{username}' creado.";
        return RedirectToAction(nameof(Sites), new { id });
    }

    private bool ValidateSiteForm(OrganizationSiteFormViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.CampusKey))
        {
            ModelState.AddModelError(nameof(model.CampusKey), "La clave del sitio es obligatoria.");
        }

        if (string.IsNullOrWhiteSpace(model.Name))
        {
            ModelState.AddModelError(nameof(model.Name), "El nombre del sitio es obligatorio.");
        }

        TryParseRequiredDouble(model.CenterLatitude, nameof(model.CenterLatitude), "Latitud del centro");
        TryParseRequiredDouble(model.CenterLongitude, nameof(model.CenterLongitude), "Longitud del centro");
        TryParseRequiredDouble(model.BoundsMinLatitude, nameof(model.BoundsMinLatitude), "Latitud minima del limite");
        TryParseRequiredDouble(model.BoundsMinLongitude, nameof(model.BoundsMinLongitude), "Longitud minima del limite");
        TryParseRequiredDouble(model.BoundsMaxLatitude, nameof(model.BoundsMaxLatitude), "Latitud maxima del limite");
        TryParseRequiredDouble(model.BoundsMaxLongitude, nameof(model.BoundsMaxLongitude), "Longitud maxima del limite");

        if (string.IsNullOrWhiteSpace(model.Zoom))
        {
            ModelState.AddModelError(nameof(model.Zoom), "El zoom es obligatorio.");
        }
        else if (!int.TryParse(model.Zoom, NumberStyles.Integer, CultureInfo.InvariantCulture, out var zoom))
        {
            ModelState.AddModelError(nameof(model.Zoom), "El zoom debe ser un numero entero.");
        }
        else if (zoom < 1 || zoom > 21)
        {
            ModelState.AddModelError(nameof(model.Zoom), "El zoom debe estar entre 1 y 21.");
        }

        TryParseZoomField(model.MinZoom, nameof(model.MinZoom), "El zoom minimo");
        TryParseZoomField(model.MaxZoom, nameof(model.MaxZoom), "El zoom maximo");

        if (ModelState.IsValid &&
            int.TryParse(model.MinZoom, NumberStyles.Integer, CultureInfo.InvariantCulture, out var minZoom) &&
            int.TryParse(model.MaxZoom, NumberStyles.Integer, CultureInfo.InvariantCulture, out var maxZoom) &&
            minZoom > maxZoom)
        {
            ModelState.AddModelError(nameof(model.MaxZoom), "El zoom maximo debe ser mayor o igual al minimo.");
        }

        return ModelState.IsValid;
    }

    private void TryParseZoomField(string value, string field, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            ModelState.AddModelError(field, $"{label} es obligatorio.");
        }
        else if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            ModelState.AddModelError(field, $"{label} debe ser un numero entero.");
        }
        else if (parsed < 0 || parsed > 21)
        {
            ModelState.AddModelError(field, $"{label} debe estar entre 0 y 21.");
        }
    }

    private void TryParseRequiredDouble(string value, string field, string label)
    {
        if (string.IsNullOrWhiteSpace(value) || !double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
        {
            ModelState.AddModelError(field, $"'{label}' debe ser un numero valido.");
        }
    }

    private static double ParseDouble(string value, double fallback)
    {
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
    }

    private static int ParseInt(string value, int fallback)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
    }

    private static string ToInvariant(double value)
    {
        return value.ToString("0.0#######", CultureInfo.InvariantCulture);
    }

    private static List<string> ParseFloorsCsv(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            return new List<string>();
        }

        return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }

    private static List<string> ParseFloorsJson(string floorsJson)
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(floorsJson) ?? new List<string>();
        }
        catch (JsonException)
        {
            return new List<string>();
        }
    }

    private static string FloorsJsonToCsv(string floorsJson)
    {
        return string.Join(", ", ParseFloorsJson(floorsJson));
    }

    private static bool IsValidGeoJson(string content)
    {
        try
        {
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;
            return root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("type", out var type)
                && type.ValueKind == JsonValueKind.String;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string Slugify(string value)
    {
        var slug = (value ?? string.Empty).Trim().ToLowerInvariant();
        var builder = new System.Text.StringBuilder();
        foreach (var character in slug)
        {
            if (char.IsAsciiLetterOrDigit(character))
            {
                builder.Append(character);
            }
            else if (character == ' ')
            {
                builder.Append('-');
            }
        }

        var result = builder.ToString().Trim('-');
        return result.Length == 0 ? Guid.NewGuid().ToString("N")[..8] : result;
    }
}
