using Microsoft.EntityFrameworkCore;
using EveryCare.Api.Domain.Entities;
using EveryCare.Api.Domain.Enums;
using EveryCare.Api.Infrastructure.Persistence;

namespace EveryCare.Api.Services;

public sealed class PartnerAvailabilityService(AppDbContext db)
{
    private static TimeZoneInfo VietnamTimeZone
    {
        get
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); }
            catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"); }
        }
    }

    public async Task<bool> IsAvailableAsync(Guid partnerProfileId, DateTimeOffset start, DateTimeOffset end, Guid? excludingBookingId, CancellationToken cancellationToken)
    {
        var localStart = TimeZoneInfo.ConvertTime(start, VietnamTimeZone);
        var localEnd = TimeZoneInfo.ConvertTime(end, VietnamTimeZone);
        if (localStart.Date != localEnd.Date) return false;
        var date = DateOnly.FromDateTime(localStart.DateTime);
        var startTime = TimeOnly.FromDateTime(localStart.DateTime);
        var endTime = TimeOnly.FromDateTime(localEnd.DateTime);

        var dayOverride = await db.PartnerAvailabilityOverrides.AsNoTracking()
            .SingleOrDefaultAsync(x => x.PartnerProfileId == partnerProfileId && x.Date == date, cancellationToken);
        var covered = dayOverride is not null
            ? !dayOverride.IsUnavailable && dayOverride.StartTime <= startTime && dayOverride.EndTime >= endTime
            : await db.PartnerAvailabilityRules.AsNoTracking().AnyAsync(x => x.PartnerProfileId == partnerProfileId && x.IsActive && x.DayOfWeek == localStart.DayOfWeek && x.StartTime <= startTime && x.EndTime >= endTime, cancellationToken);
        if (!covered) return false;

        var bufferStart = start.AddHours(-1);
        var bufferEnd = end.AddHours(1);
        var hasDirectConflict = await db.Bookings.AsNoTracking().AnyAsync(x =>
            x.AssignedPartnerId == partnerProfileId && x.Id != excludingBookingId &&
            (x.Status == BookingStatus.Assigned || x.Status == BookingStatus.PartnerTravelling || x.Status == BookingStatus.InProgress) &&
            x.ScheduledStartAt < bufferEnd && (x.ScheduledEndAt ?? x.ScheduledStartAt.AddHours(4)) > bufferStart,
            cancellationToken);
        if (hasDirectConflict) return false;

        var recurringBookings = await db.Bookings.AsNoTracking()
            .Where(x => x.AssignedPartnerId == partnerProfileId && x.Id != excludingBookingId && x.IsRecurring && x.RecurrenceRule != null &&
                x.Status != BookingStatus.Cancelled && x.Status != BookingStatus.NoPartnerFound)
            .Select(x => new { x.ScheduledStartAt, x.ScheduledEndAt, x.RecurrenceRule })
            .ToListAsync(cancellationToken);
        return recurringBookings.All(booking => !OverlapsRecurringBooking(booking.ScheduledStartAt, booking.ScheduledEndAt, booking.RecurrenceRule!, localStart, localEnd));
    }

    private static bool OverlapsRecurringBooking(DateTimeOffset firstStart, DateTimeOffset? firstEnd, string rule, DateTimeOffset targetStart, DateTimeOffset targetEnd)
    {
        var values = rule.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .Where(part => part.Length == 2)
            .ToDictionary(part => part[0], part => part[1], StringComparer.OrdinalIgnoreCase);
        if (!values.TryGetValue("BYDAY", out var byDay) || !values.TryGetValue("TIME", out var timeText) || !values.TryGetValue("MONTHS", out var monthsText)) return false;
        if (!TimeOnly.TryParse(timeText, out var recurringTime) || !int.TryParse(monthsText, out var months)) return false;

        var firstLocal = TimeZoneInfo.ConvertTime(firstStart, VietnamTimeZone);
        var lastLocalDate = firstLocal.AddMonths(months).Date;
        if (targetStart.Date < firstLocal.Date || targetStart.Date >= lastLocalDate) return false;
        var dayCode = targetStart.DayOfWeek switch { DayOfWeek.Monday=>"MO", DayOfWeek.Tuesday=>"TU", DayOfWeek.Wednesday=>"WE", DayOfWeek.Thursday=>"TH", DayOfWeek.Friday=>"FR", DayOfWeek.Saturday=>"SA", _=>"SU" };
        if (!byDay.Split(',').Contains(dayCode, StringComparer.OrdinalIgnoreCase)) return false;

        var duration = (firstEnd ?? firstStart.AddHours(4)) - firstStart;
        var occurrenceStart = new DateTimeOffset(targetStart.Year, targetStart.Month, targetStart.Day, recurringTime.Hour, recurringTime.Minute, 0, targetStart.Offset);
        var occurrenceEnd = occurrenceStart.Add(duration);
        return occurrenceStart < targetEnd.AddHours(1) && occurrenceEnd > targetStart.AddHours(-1);
    }
}
