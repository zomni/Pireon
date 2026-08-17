using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pireon.API.Data;
using Pireon.API.Models;
using Pireon.API.Services;

namespace Pireon.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LocationsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly OrganizationAccessService _access;

    public LocationsController(AppDbContext context, OrganizationAccessService access)
    {
        _context = context;
        _access = access;
    }

    // GET /api/locations?campus=<campus>&floor=0
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? campus, [FromQuery] string? floor)
    {
        var query = _context.Locations
            .Include(l => l.Equipments)
            .Where(l => l.IsActive);

        if (!string.IsNullOrEmpty(campus))
            query = query.Where(l => l.Campus == campus);

        if (!string.IsNullOrEmpty(floor))
            query = query.Where(l => l.Floor == floor);

        var locations = await query.ToListAsync();

        // Devuelve GeoJSON compatible con Leaflet
        var geoJson = new
        {
            type = "FeatureCollection",
            features = locations.Select(l => new
            {
                type = "Feature",
                geometry = new
                {
                    type = "Point",
                    coordinates = new[] { l.Longitude, l.Latitude }
                },
                properties = new
                {
                    id = l.Id,
                    name = l.Name,
                    description = l.Description,
                    floor = l.Floor,
                    campus = l.Campus,
                    locationType = l.Type,
                    equipmentCount = l.Equipments.Count
                }
            })
        };

        return Ok(geoJson);
    }

    // GET /api/locations/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Location>> GetById(Guid id)
    {
        var location = await _context.Locations
            .Include(l => l.Equipments)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (location == null) return NotFound();
        return Ok(location);
    }

    // POST /api/locations
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.SuperAdmin}")]
    [HttpPost]
    public async Task<ActionResult<Location>> Create(Location location, CancellationToken cancellationToken)
    {
        if (!await _access.CanAccessCampusAsync(location.Campus, cancellationToken))
            return Forbid();

        _context.Locations.Add(location);
        await _context.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = location.Id }, location);
    }

    // PUT /api/locations/{id}
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.SuperAdmin}")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, Location location, CancellationToken cancellationToken)
    {
        if (id != location.Id) return BadRequest();

        if (!await _access.CanAccessCampusAsync(location.Campus, cancellationToken))
            return Forbid();

        _context.Entry(location).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Locations.Any(l => l.Id == id)) return NotFound();
            throw;
        }

        return NoContent();
    }

    // DELETE /api/locations/{id}
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.SuperAdmin}")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var location = await _context.Locations.FindAsync(new object?[] { id }, cancellationToken);
        if (location == null) return NotFound();

        if (!await _access.CanAccessCampusAsync(location.Campus, cancellationToken))
            return Forbid();

        location.SoftDelete(User.Identity?.Name);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
