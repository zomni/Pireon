using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pireon.API.Data;
using Pireon.API.Models;
using Pireon.API.Services;

namespace Pireon.API.Controllers;

[ApiController]
[Route("api/sites")]
[Authorize]
public class SiteViewportController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly OrganizationAccessService _access;
    private readonly AuditLogService _auditLogService;

    public SiteViewportController(
        AppDbContext context,
        OrganizationAccessService access,
        AuditLogService auditLogService)
    {
        _context = context;
        _access = access;
        _auditLogService = auditLogService;
    }

    public sealed record UpdateViewportRequest(int MinZoom, int MaxZoom);

    // PUT /api/sites/{campusKey}/viewport
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.SuperAdmin}")]
    [HttpPut("{campusKey}/viewport")]
    public async Task<IActionResult> UpdateViewport(string campusKey, [FromBody] UpdateViewportRequest? request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(campusKey))
        {
            return BadRequest(new { message = "La clave del sitio es obligatoria." });
        }

        if (!await _access.CanAccessCampusAsync(campusKey, cancellationToken))
        {
            return Forbid();
        }

        if (request is null)
        {
            return BadRequest(new { message = "Se requieren los campos minZoom y maxZoom." });
        }

        var validationError = SiteViewportRules.Validate(request.MinZoom, request.MaxZoom);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var site = await _context.CampusSites
            .FirstOrDefaultAsync(item => item.CampusKey == campusKey && item.IsActive, cancellationToken);
        if (site is null)
        {
            return NotFound();
        }

        site.MinZoom = request.MinZoom;
        site.MaxZoom = request.MaxZoom;
        site.UpdatedBy = _access.CurrentUsername ?? "system";
        site.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogSecurityEventAsync(
            actionType: "site-viewport-update",
            resource: "sites/viewport",
            summary: $"Se actualizo el rango de zoom del sitio {site.Name}",
            details: $"CampusKey: {site.CampusKey}; MinZoom: {site.MinZoom}; MaxZoom: {site.MaxZoom}",
            result: "success",
            severity: "info",
            changedByUsername: _access.CurrentUsername ?? "system",
            cancellationToken: cancellationToken);

        return Ok(new
        {
            campusKey = site.CampusKey,
            minZoom = site.MinZoom,
            maxZoom = site.MaxZoom
        });
    }
}
