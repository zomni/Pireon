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
public class EquipmentsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly OrganizationAccessService _access;

    public EquipmentsController(AppDbContext context, OrganizationAccessService access)
    {
        _context = context;
        _access = access;
    }

    // GET /api/equipments?locationId=1&status=active
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Equipment>>> GetAll(
        [FromQuery] Guid? locationId,
        [FromQuery] string? status,
        [FromQuery] string? category)
    {
        var query = _context.Equipments.Include(e => e.Location).AsQueryable();

        if (locationId.HasValue)
            query = query.Where(e => e.LocationId == locationId.Value);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(e => e.Status == status);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(e => e.Category == category);

        return Ok(await query.ToListAsync());
    }

    // GET /api/equipments/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Equipment>> GetById(Guid id)
    {
        var equipment = await _context.Equipments
            .Include(e => e.Location)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (equipment == null) return NotFound();
        return Ok(equipment);
    }

    // POST /api/equipments
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.SuperAdmin}")]
    [HttpPost]
    public async Task<ActionResult<Equipment>> Create(Equipment equipment, CancellationToken cancellationToken)
    {
        var location = await _context.Locations.FindAsync(new object?[] { equipment.LocationId }, cancellationToken);
        if (location is null)
            return BadRequest(new { message = "El equipo debe asociarse a una ubicacion valida." });

        if (!await _access.CanAccessCampusAsync(location.Campus, cancellationToken))
            return Forbid();

        _context.Equipments.Add(equipment);
        await _context.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = equipment.Id }, equipment);
    }

    // PUT /api/equipments/{id}
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.SuperAdmin}")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, Equipment equipment, CancellationToken cancellationToken)
    {
        if (id != equipment.Id) return BadRequest();

        var location = await _context.Locations.FindAsync(new object?[] { equipment.LocationId }, cancellationToken);
        if (location is null)
            return BadRequest(new { message = "El equipo debe asociarse a una ubicacion valida." });

        if (!await _access.CanAccessCampusAsync(location.Campus, cancellationToken))
            return Forbid();

        _context.Entry(equipment).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Equipments.Any(e => e.Id == id)) return NotFound();
            throw;
        }

        return NoContent();
    }

    // DELETE /api/equipments/{id}
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.SuperAdmin}")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var equipment = await _context.Equipments.FindAsync(new object?[] { id }, cancellationToken);
        if (equipment == null) return NotFound();

        var location = await _context.Locations.FindAsync(new object?[] { equipment.LocationId }, cancellationToken);
        if (location is not null && !await _access.CanAccessCampusAsync(location.Campus, cancellationToken))
            return Forbid();

        equipment.SoftDelete(User.Identity?.Name);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    // GET /api/equipments/summary
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var summary = await _context.Equipments
            .GroupBy(e => e.Status)
            .Select(g => new { status = g.Key, count = g.Count() })
            .ToListAsync();

        return Ok(summary);
    }
}
