using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Pireon.API.Data;
using Pireon.API.Models;

namespace Pireon.API.Services;

public sealed class OrganizationAccessService
{
    private const string OrganizationIdClaimType = "pireon:organization_id";
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public OrganizationAccessService(AppDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsSuperAdmin => string.Equals(
        _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role),
        AppRoles.SuperAdmin,
        StringComparison.OrdinalIgnoreCase);

    public bool IsAdmin => IsSuperAdmin || string.Equals(
        _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role),
        AppRoles.Admin,
        StringComparison.OrdinalIgnoreCase);

    public string? CurrentUsername => _httpContextAccessor.HttpContext?.User.Identity?.Name;

    public Guid? OrganizationId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User.FindFirstValue(OrganizationIdClaimType);
            return Guid.TryParse(value, out var parsed) ? parsed : null;
        }
    }

    public async Task<AuthUser?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var username = CurrentUsername;
        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        return await _context.AuthUsers
            .FirstOrDefaultAsync(user => user.NormalizedUsername == username.ToUpperInvariant(), cancellationToken);
    }

    public Task<bool> CanAccessOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        if (IsSuperAdmin)
        {
            return Task.FromResult(true);
        }

        var orgId = OrganizationId;
        return Task.FromResult(orgId.HasValue && orgId.Value == organizationId);
    }

    public async Task<bool> CanAccessCampusAsync(string campusKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(campusKey))
        {
            return false;
        }

        if (IsSuperAdmin)
        {
            return true;
        }

        var orgId = OrganizationId;
        if (!orgId.HasValue)
        {
            return false;
        }

        return await _context.CampusSites.AnyAsync(
            site => site.CampusKey == campusKey && site.OrganizationId == orgId.Value && site.IsActive,
            cancellationToken);
    }

    public IQueryable<CampusSite> ScopeSitesQuery(IQueryable<CampusSite> query)
    {
        if (IsSuperAdmin)
        {
            return query;
        }

        var orgId = OrganizationId;
        return orgId.HasValue
            ? query.Where(site => site.OrganizationId == orgId.Value)
            : query.Where(site => false);
    }

    public IQueryable<AuthUser> ScopeUsersQuery(IQueryable<AuthUser> query)
    {
        if (IsSuperAdmin)
        {
            return query;
        }

        var orgId = OrganizationId;
        return orgId.HasValue
            ? query.Where(user => user.OrganizationId == orgId.Value)
            : query.Where(user => false);
    }

    public Guid? EffectiveOrganizationId(Guid? requestedOrganizationId)
    {
        if (IsSuperAdmin)
        {
            return requestedOrganizationId;
        }

        return OrganizationId;
    }

    public async Task<IReadOnlyList<string>> ResolveCampusKeysAsync(
        Guid? organizationId,
        CancellationToken cancellationToken = default)
    {
        var effectiveOrgId = EffectiveOrganizationId(organizationId);
        if (!effectiveOrgId.HasValue)
        {
            return await _context.CampusSites
                .AsNoTracking()
                .Where(site => site.IsActive)
                .Select(site => site.CampusKey)
                .ToListAsync(cancellationToken);
        }

        return await _context.CampusSites
            .AsNoTracking()
            .Where(site => site.OrganizationId == effectiveOrgId.Value && site.IsActive)
            .Select(site => site.CampusKey)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Organization>> GetSelectableOrganizationsAsync(
        CancellationToken cancellationToken = default)
    {
        if (!IsSuperAdmin)
        {
            var orgId = OrganizationId;
            if (!orgId.HasValue)
            {
                return Array.Empty<Organization>();
            }

            return new[] { await _context.Organizations.SingleAsync(org => org.Id == orgId.Value, cancellationToken) };
        }

        return await _context.Organizations
            .AsNoTracking()
            .OrderBy(org => org.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> CanManageCampusKeyAsync(
        string campusKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(campusKey))
        {
            return false;
        }

        var effectiveOrgId = EffectiveOrganizationId(null);
        if (!effectiveOrgId.HasValue)
        {
            return true;
        }

        return await _context.CampusSites.AnyAsync(
            site => site.CampusKey == campusKey &&
                    site.OrganizationId == effectiveOrgId.Value &&
                    site.IsActive,
            cancellationToken);
    }
}
