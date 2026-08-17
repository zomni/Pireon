using Cronos;
using Microsoft.EntityFrameworkCore;
using Pireon.API.Data;
using Pireon.API.Models;
using Pireon.API.ViewModels;

namespace Pireon.API.Services;

public class TelemetryScanScheduleService
{
    private const string DefaultTimeZone = "America/Santiago";

    private readonly AppDbContext _context;

    public TelemetryScanScheduleService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<TelemetryScanScheduleDto>> GetSchedulesAsync(
        IReadOnlyCollection<string>? campusKeys = null,
        CancellationToken cancellationToken = default)
    {
        var schedulesQuery = _context.TelemetryScanSchedules
            .AsNoTracking();

        if (campusKeys is not null && campusKeys.Count > 0)
        {
            schedulesQuery = schedulesQuery.Where(s => s.CampusKey != null && campusKeys.Contains(s.CampusKey));
        }
        else if (campusKeys is not null)
        {
            schedulesQuery = schedulesQuery.Where(s => false);
        }

        var schedules = await schedulesQuery
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var nowUtc = DateTime.UtcNow;
        return schedules.Select(s => ToDto(s, nowUtc)).ToList();
    }

    public async Task<TelemetryScanScheduleDto> CreateAsync(
        TelemetryScanScheduleRequest request,
        string? actor,
        IReadOnlyCollection<string>? campusKeys = null,
        CancellationToken cancellationToken = default)
    {
        if (!campusKeyInScope(request.CampusKey, campusKeys))
        {
            throw new InvalidOperationException("El campus indicado no existe o no esta autorizado.");
        }

        var schedule = new TelemetryScanSchedule();
        Apply(schedule, request);
        schedule.CreatedBy = string.IsNullOrWhiteSpace(actor) ? "system" : actor.Trim();

        _context.TelemetryScanSchedules.Add(schedule);
        await _context.SaveChangesAsync(cancellationToken);

        return ToDto(schedule, DateTime.UtcNow);
    }

    public async Task<TelemetryScanScheduleDto?> UpdateAsync(
        Guid id,
        TelemetryScanScheduleRequest request,
        string? actor,
        IReadOnlyCollection<string>? campusKeys = null,
        CancellationToken cancellationToken = default)
    {
        var schedule = await _context.TelemetryScanSchedules.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (schedule is null)
        {
            return null;
        }

        if (!campusKeyInScope(schedule.CampusKey, campusKeys) || !campusKeyInScope(request.CampusKey, campusKeys))
        {
            throw new InvalidOperationException("El campus indicado no existe o no esta autorizado.");
        }

        Apply(schedule, request);
        schedule.UpdatedBy = string.IsNullOrWhiteSpace(actor) ? "system" : actor.Trim();
        schedule.Version++;
        await _context.SaveChangesAsync(cancellationToken);

        return ToDto(schedule, DateTime.UtcNow);
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        string? actor,
        IReadOnlyCollection<string>? campusKeys = null,
        CancellationToken cancellationToken = default)
    {
        var schedule = await _context.TelemetryScanSchedules.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (schedule is null)
        {
            return false;
        }

        if (!campusKeyInScope(schedule.CampusKey, campusKeys))
        {
            return false;
        }

        schedule.SoftDelete(string.IsNullOrWhiteSpace(actor) ? "system" : actor.Trim());
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static bool campusKeyInScope(string? campusKey, IReadOnlyCollection<string>? campusKeys)
    {
        if (string.IsNullOrWhiteSpace(campusKey))
        {
            return false;
        }

        return campusKeys is null || campusKeys.Contains(campusKey);
    }

    public static bool TryParseCron(string? cron, out CronExpression? expression)
    {
        expression = null;
        if (string.IsNullOrWhiteSpace(cron))
        {
            return false;
        }

        if (CronExpression.TryParse(cron, CronFormat.IncludeSeconds, out expression))
        {
            return true;
        }

        return CronExpression.TryParse(cron, CronFormat.Standard, out expression);
    }

    public static DateTime? GetNextOccurrenceUtc(string cron, string timeZoneId, DateTime fromUtc)
    {
        if (!TryParseCron(cron, out var expression) || expression is null)
        {
            return null;
        }

        var timeZone = ResolveTimeZone(timeZoneId);
        var nextUtc = expression.GetNextOccurrence(NormalizeUtc(fromUtc), timeZone);
        if (nextUtc is null)
        {
            return null;
        }

        return nextUtc.Value;
    }

    public static IReadOnlyList<DateTime> GetNextOccurrencesUtc(string cron, string timeZoneId, DateTime fromUtc, int count)
    {
        var results = new List<DateTime>();
        if (!TryParseCron(cron, out var expression) || expression is null || count <= 0)
        {
            return results;
        }

        var timeZone = ResolveTimeZone(timeZoneId);
        var cursor = NormalizeUtc(fromUtc);
        for (var i = 0; i < count; i++)
        {
            var nextUtc = expression.GetNextOccurrence(cursor, timeZone);
            if (nextUtc is null)
            {
                break;
            }

            results.Add(nextUtc.Value);
            cursor = nextUtc.Value.AddSeconds(1);
        }

        return results;
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        if (value.Kind == DateTimeKind.Local)
        {
            return value.ToUniversalTime();
        }

        if (value.Kind == DateTimeKind.Unspecified)
        {
            return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        return value;
    }

    public static TimeZoneInfo ResolveTimeZone(string? timeZoneId)
    {
        if (!string.IsNullOrWhiteSpace(timeZoneId))
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch
            {
            }
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(DefaultTimeZone);
        }
        catch
        {
            return TimeZoneInfo.Local;
        }
    }

    public static string ResolveScheduleTimeZone(string? timeZoneId)
    {
        if (!string.IsNullOrWhiteSpace(timeZoneId))
        {
            try
            {
                TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                return timeZoneId.Trim();
            }
            catch
            {
            }
        }

        return DefaultTimeZone;
    }

    private static void Apply(TelemetryScanSchedule schedule, TelemetryScanScheduleRequest request)
    {
        var cron = (request.Cron ?? string.Empty).Trim();
        if (!TryParseCron(cron, out _))
        {
            throw new InvalidOperationException($"La expresion cron no es valida: '{cron}'.");
        }

        schedule.Label = string.IsNullOrWhiteSpace(request.Label) ? cron : request.Label.Trim();
        schedule.Cron = cron;
        schedule.TimeZone = ResolveScheduleTimeZone(request.TimeZone);
        schedule.CampusKey = (request.CampusKey ?? string.Empty).Trim();
        schedule.IsEnabled = request.IsEnabled;
        schedule.SortOrder = request.SortOrder;
        schedule.UpdatedAtUtc = DateTime.UtcNow;
    }

    private static TelemetryScanScheduleDto ToDto(TelemetryScanSchedule schedule, DateTime nowUtc)
    {
        var isValid = TryParseCron(schedule.Cron, out _);
        DateTime? nextOccurrenceUtc = null;
        DateTime? nextOccurrenceLocal = null;

        if (isValid && schedule.IsEnabled)
        {
            nextOccurrenceUtc = GetNextOccurrenceUtc(schedule.Cron, schedule.TimeZone, nowUtc);
            if (nextOccurrenceUtc.HasValue)
            {
                nextOccurrenceLocal = TimeZoneInfo.ConvertTimeFromUtc(nextOccurrenceUtc.Value, ResolveTimeZone(schedule.TimeZone));
            }
        }

        return new TelemetryScanScheduleDto
        {
            Id = schedule.Id,
            Label = schedule.Label,
            Cron = schedule.Cron,
            TimeZone = schedule.TimeZone,
            CampusKey = schedule.CampusKey,
            IsEnabled = schedule.IsEnabled,
            SortOrder = schedule.SortOrder,
            IsValid = isValid,
            ValidationError = isValid ? string.Empty : "Expresion cron invalida.",
            NextOccurrenceUtc = nextOccurrenceUtc,
            NextOccurrenceLocal = nextOccurrenceLocal
        };
    }
}
