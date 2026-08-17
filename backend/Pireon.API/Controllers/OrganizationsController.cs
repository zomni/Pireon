using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pireon.API.Data;
using Pireon.API.Models;
using Pireon.API.Services;

namespace Pireon.API.Controllers;

[ApiController]
[Route("api/organizations")]
[Authorize(Roles = AppRoles.SuperAdmin)]
public class OrganizationsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly OrganizationAccessService _access;
    private readonly IPasswordHasher<AuthUser> _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly AuditLogService _auditLogService;

    public OrganizationsController(
        AppDbContext context,
        OrganizationAccessService access,
        IPasswordHasher<AuthUser> passwordHasher,
        IConfiguration configuration,
        AuditLogService auditLogService)
    {
        _context = context;
        _access = access;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var organizations = await _context.Organizations
            .AsNoTracking()
            .OrderBy(org => org.Name)
            .Select(org => new
            {
                org.Id,
                org.Name,
                org.Slug,
                org.ContactEmail,
                org.Notes,
                org.Color,
                org.IsActive,
                org.CreatedAtUtc,
                SitesCount = org.Sites.Count(site => site.IsActive)
            })
            .ToListAsync(cancellationToken);

        return Ok(organizations);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var organization = await _context.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(org => org.Id == id, cancellationToken);

        if (organization is null)
        {
            return NotFound(new { message = "Organizacion no encontrada." });
        }

        return Ok(new
        {
            organization.Id,
            organization.Name,
            organization.Slug,
            organization.ContactEmail,
            organization.Notes,
            organization.Color,
            organization.IsActive,
            organization.CreatedAtUtc
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateOrganizationRequest request, CancellationToken cancellationToken)
    {
        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { message = "El nombre de la organizacion es obligatorio." });
        }

        var slug = (request.Slug ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = Slugify(name);
        }

        if (await _context.Organizations.AnyAsync(org => org.Slug == slug, cancellationToken))
        {
            return Conflict(new { message = $"Ya existe una organizacion con el slug '{slug}'." });
        }

        var now = DateTime.UtcNow;
        var requestedColor = OrganizationColorPalette.Normalize(request.Color);
        var usedColors = await _context.Organizations
            .AsNoTracking()
            .Where(org => org.IsActive)
            .Select(org => org.Color)
            .ToListAsync(cancellationToken);

        var organization = new Organization
        {
            Name = name,
            Slug = slug,
            ContactEmail = (request.ContactEmail ?? string.Empty).Trim(),
            Notes = (request.Notes ?? string.Empty).Trim(),
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

        return CreatedAtAction(nameof(GetById), new { id = organization.Id }, new
        {
            organization.Id,
            organization.Name,
            organization.Slug,
            organization.ContactEmail,
            organization.Notes,
            organization.Color,
            organization.IsActive
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateOrganizationRequest request, CancellationToken cancellationToken)
    {
        var organization = await _context.Organizations.FirstOrDefaultAsync(org => org.Id == id, cancellationToken);
        if (organization is null)
        {
            return NotFound(new { message = "Organizacion no encontrada." });
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            organization.Name = request.Name!.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.ContactEmail))
        {
            organization.ContactEmail = request.ContactEmail!.Trim();
        }

        if (request.Notes is not null)
        {
            organization.Notes = request.Notes.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.Color))
        {
            var requestedColor = OrganizationColorPalette.Normalize(request.Color);
            if (!string.IsNullOrEmpty(requestedColor))
            {
                organization.Color = requestedColor;
            }
        }

        if (request.IsActive.HasValue)
        {
            organization.IsActive = request.IsActive.Value;
            if (!organization.IsActive)
            {
                organization.SoftDelete(_access.CurrentUsername ?? "system");
            }
            else
            {
                organization.Restore(_access.CurrentUsername ?? "system");
            }
        }

        organization.UpdatedBy = _access.CurrentUsername ?? "system";
        organization.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogSecurityEventAsync(
            actionType: "organization-update",
            resource: "organizations",
            summary: $"Se actualizo la organizacion {organization.Name}",
            details: $"Activa: {organization.IsActive}",
            result: "success",
            severity: "info",
            changedByUsername: _access.CurrentUsername ?? "system",
            cancellationToken: cancellationToken);

        return Ok(new
        {
            organization.Id,
            organization.Name,
            organization.Slug,
            organization.ContactEmail,
            organization.Notes,
            organization.Color,
            organization.IsActive
        });
    }

    [HttpGet("{id:guid}/sites")]
    public async Task<IActionResult> GetSites(Guid id, CancellationToken cancellationToken)
    {
        if (!await _access.CanAccessOrganizationAsync(id, cancellationToken))
        {
            return Forbid();
        }

        var sites = await _context.CampusSites
            .AsNoTracking()
            .Where(site => site.OrganizationId == id)
            .OrderBy(site => site.Name)
            .Select(site => new
            {
                site.Id,
                site.CampusKey,
                site.Name,
                site.School,
                site.CenterLatitude,
                site.CenterLongitude,
                site.Zoom,
                site.BoundsMinLatitude,
                site.BoundsMinLongitude,
                site.BoundsMaxLatitude,
                site.BoundsMaxLongitude,
                site.FloorsJson,
                site.DefaultFloor,
                site.IsActive,
                site.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(sites);
    }

    [HttpPost("{id:guid}/sites")]
    public async Task<IActionResult> CreateSite(Guid id, CreateSiteRequest request, CancellationToken cancellationToken)
    {
        if (!await _access.CanAccessOrganizationAsync(id, cancellationToken))
        {
            return Forbid();
        }

        var organization = await _context.Organizations.FirstOrDefaultAsync(org => org.Id == id, cancellationToken);
        if (organization is null)
        {
            return NotFound(new { message = "Organizacion no encontrada." });
        }

        var campusKey = (request.CampusKey ?? string.Empty).Trim();
        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(campusKey))
        {
            return BadRequest(new { message = "La clave del sitio es obligatoria." });
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { message = "El nombre del sitio es obligatorio." });
        }

        campusKey = Slugify(campusKey);

        if (await _context.CampusSites.AnyAsync(site => site.CampusKey == campusKey, cancellationToken))
        {
            return Conflict(new { message = $"Ya existe un sitio con la clave '{campusKey}'." });
        }

        var floors = request.Floors ?? new List<string>();
        var floorsJson = JsonSerializer.Serialize(floors);
        var defaultFloor = string.IsNullOrWhiteSpace(request.DefaultFloor) ? (floors.Count > 0 ? floors[0] : string.Empty) : request.DefaultFloor;

        var now = DateTime.UtcNow;
        var site = new CampusSite
        {
            OrganizationId = id,
            CampusKey = campusKey,
            Name = name,
            School = (request.School ?? string.Empty).Trim(),
            CenterLatitude = request.CenterLatitude,
            CenterLongitude = request.CenterLongitude,
            Zoom = request.Zoom,
            BoundsMinLatitude = request.BoundsMinLatitude,
            BoundsMinLongitude = request.BoundsMinLongitude,
            BoundsMaxLatitude = request.BoundsMaxLatitude,
            BoundsMaxLongitude = request.BoundsMaxLongitude,
            FloorsJson = floorsJson,
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

        return CreatedAtAction(nameof(GetSites), new { id }, new
        {
            site.Id,
            site.CampusKey,
            site.Name,
            site.School,
            site.CenterLatitude,
            site.CenterLongitude,
            site.Zoom,
            site.BoundsMinLatitude,
            site.BoundsMinLongitude,
            site.BoundsMaxLatitude,
            site.BoundsMaxLongitude,
            site.FloorsJson,
            site.DefaultFloor,
            site.IsActive
        });
    }

    [HttpPut("{id:guid}/sites/{siteId:guid}")]
    public async Task<IActionResult> UpdateSite(Guid id, Guid siteId, CreateSiteRequest request, CancellationToken cancellationToken)
    {
        if (!await _access.CanAccessOrganizationAsync(id, cancellationToken))
        {
            return Forbid();
        }

        var site = await _context.CampusSites
            .FirstOrDefaultAsync(item => item.Id == siteId && item.OrganizationId == id, cancellationToken);
        if (site is null)
        {
            return NotFound(new { message = "Sitio no encontrado." });
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            site.Name = request.Name!.Trim();
        }

        if (request.School is not null)
        {
            site.School = request.School.Trim();
        }

        if (request.Floors is not null)
        {
            var floors = request.Floors;
            site.FloorsJson = JsonSerializer.Serialize(floors);
            if (string.IsNullOrWhiteSpace(request.DefaultFloor) && floors.Count > 0 && !floors.Contains(site.DefaultFloor))
            {
                site.DefaultFloor = floors[0];
            }
        }

        if (!string.IsNullOrWhiteSpace(request.DefaultFloor))
        {
            site.DefaultFloor = request.DefaultFloor!.Trim();
        }

        site.CenterLatitude = request.CenterLatitude;
        site.CenterLongitude = request.CenterLongitude;
        site.Zoom = request.Zoom;
        site.BoundsMinLatitude = request.BoundsMinLatitude;
        site.BoundsMinLongitude = request.BoundsMinLongitude;
        site.BoundsMaxLatitude = request.BoundsMaxLatitude;
        site.BoundsMaxLongitude = request.BoundsMaxLongitude;
        site.UpdatedBy = _access.CurrentUsername ?? "system";
        site.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogSecurityEventAsync(
            actionType: "site-update",
            resource: "organizations/sites",
            summary: $"Se actualizo el sitio {site.Name}",
            details: $"CampusKey: {site.CampusKey}",
            result: "success",
            severity: "info",
            changedByUsername: _access.CurrentUsername ?? "system",
            cancellationToken: cancellationToken);

        return Ok(new { message = $"Sitio {site.Name} actualizado." });
    }

    [HttpDelete("{id:guid}/sites/{siteId:guid}")]
    public async Task<IActionResult> DeleteSite(Guid id, Guid siteId, CancellationToken cancellationToken)
    {
        if (!await _access.CanAccessOrganizationAsync(id, cancellationToken))
        {
            return Forbid();
        }

        var site = await _context.CampusSites
            .FirstOrDefaultAsync(item => item.Id == siteId && item.OrganizationId == id, cancellationToken);
        if (site is null)
        {
            return NotFound(new { message = "Sitio no encontrado." });
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

        return NoContent();
    }

    [HttpPost("{id:guid}/sites/{siteId:guid}/floors/{floor}/geojson")]
    [RequestSizeLimit(25_000_000)]
    public async Task<IActionResult> UploadFloorGeoJson(
        Guid id,
        Guid siteId,
        string floor,
        IFormFile file,
        [FromServices] FrontendSyncService syncService,
        CancellationToken cancellationToken)
    {
        if (!await _access.CanAccessOrganizationAsync(id, cancellationToken))
        {
            return Forbid();
        }

        var site = await _context.CampusSites
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == siteId && item.OrganizationId == id && item.IsActive, cancellationToken);
        if (site is null)
        {
            return NotFound(new { message = "Sitio no encontrado." });
        }

        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "Debe adjuntar un archivo GeoJSON." });
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension != ".json" && extension != ".geojson")
        {
            return BadRequest(new { message = "El archivo debe tener extension .json o .geojson." });
        }

        string content;
        using (var reader = new StreamReader(file.OpenReadStream()))
        {
            content = await reader.ReadToEndAsync(cancellationToken);
        }

        if (!IsValidGeoJson(content))
        {
            return BadRequest(new { message = "El contenido no es un GeoJSON valido." });
        }

        var school = string.IsNullOrWhiteSpace(site.School) ? site.CampusKey : site.School;
        var dataRoot = syncService.ResolveDataRoot();
        Directory.CreateDirectory(dataRoot);

        var fileName = $"{school}_{site.CampusKey}_{floor.Trim()}.json";
        var targetPath = Path.Combine(dataRoot, fileName);
        await System.IO.File.WriteAllTextAsync(targetPath, content, System.Text.Encoding.UTF8, cancellationToken);

        return Ok(new
        {
            message = "Plano GeoJSON subido correctamente.",
            fileName,
            path = targetPath,
            sizeBytes = content.Length
        });
    }

    [HttpPost("{id:guid}/admin-users")]
    public async Task<IActionResult> CreateOrgAdmin(Guid id, CreateOrgAdminRequest request, CancellationToken cancellationToken)
    {
        if (!await _access.CanAccessOrganizationAsync(id, cancellationToken))
        {
            return Forbid();
        }

        var organization = await _context.Organizations.FirstOrDefaultAsync(org => org.Id == id && org.IsActive, cancellationToken);
        if (organization is null)
        {
            return NotFound(new { message = "Organizacion no encontrada o inactiva." });
        }

        var username = (request.Username ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(username))
        {
            return BadRequest(new { message = "El nombre de usuario es obligatorio." });
        }

        var normalizedUsername = username.ToUpperInvariant();
        if (await _context.AuthUsers.AnyAsync(user => user.NormalizedUsername == normalizedUsername, cancellationToken))
        {
            return Conflict(new { message = $"Ya existe un usuario con el nombre '{username}'." });
        }

        var password = request.Password ?? string.Empty;
        var policyError = PasswordPolicyService.Validate(password, username, _configuration);
        if (policyError is not null)
        {
            return BadRequest(new { message = policyError });
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

        return Ok(new
        {
            message = $"Admin de organizacion {username} creado.",
            user.Id,
            user.Username,
            user.Role,
            user.OrganizationId
        });
    }

    [HttpGet("{id:guid}/users")]
    public async Task<IActionResult> GetUsers(Guid id, CancellationToken cancellationToken)
    {
        if (!await _access.CanAccessOrganizationAsync(id, cancellationToken))
        {
            return Forbid();
        }

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
                user.CanManageUsers,
                user.MfaEnabled,
                user.LastLoginAtUtc,
                user.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(users);
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
        var slug = value.Trim().ToLowerInvariant();
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

    public record CreateOrganizationRequest(string Name, string? Slug, string? ContactEmail, string? Notes, string? Color);
    public record UpdateOrganizationRequest(string? Name, string? ContactEmail, string? Notes, bool? IsActive, string? Color);
    public record CreateSiteRequest(
        string? CampusKey,
        string? Name,
        string? School,
        double CenterLatitude,
        double CenterLongitude,
        int Zoom,
        double BoundsMinLatitude,
        double BoundsMinLongitude,
        double BoundsMaxLatitude,
        double BoundsMaxLongitude,
        List<string>? Floors,
        string? DefaultFloor);
    public record CreateOrgAdminRequest(string? Username, string? Password);
}
