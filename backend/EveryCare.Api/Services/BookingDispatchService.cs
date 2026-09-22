using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using EveryCare.Api.Domain.Entities;
using EveryCare.Api.Domain.Enums;
using EveryCare.Api.Infrastructure.Persistence;

namespace EveryCare.Api.Services;

public sealed class BookingDispatchService(AppDbContext db, PartnerAvailabilityService availability)
{
    public const int BatchSize = 5;
    public const int MaximumInvitations = 20;
    private static readonly double[] RadiusStepsMeters = [10_000, 15_000, 20_000, 30_000];
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> BookingLocks = new();
    private sealed record OccurrenceWindow(Guid Id, DateTimeOffset ScheduledStartAt, DateTimeOffset? ScheduledEndAt);

    public async Task<int> DispatchAsync(Guid bookingId, CancellationToken cancellationToken)
    {
        var bookingLock = BookingLocks.GetOrAdd(bookingId, _ => new SemaphoreSlim(1, 1));
        await bookingLock.WaitAsync(cancellationToken);
        try
        {
            return await DispatchCoreAsync(bookingId, cancellationToken);
        }
        finally
        {
            bookingLock.Release();
        }
    }

    private async Task<int> DispatchCoreAsync(Guid bookingId, CancellationToken cancellationToken)
    {
        var booking = await db.Bookings
            .Include(x => x.ServiceGroup)
            .Include(x => x.Assignments)
            .Include(x => x.RecurringContract)
            .SingleOrDefaultAsync(x => x.Id == bookingId, cancellationToken);
        if (booking is null || booking.AssignedPartnerId is not null || booking.LocationSnapshot is null ||
            booking.ScheduledStartAt <= DateTimeOffset.UtcNow ||
            booking.Status is not (BookingStatus.Searching or BookingStatus.AwaitingCustomerSelection or BookingStatus.NoPartnerFound)) return 0;
        var isInitialSeriesDispatch = booking.RecurringContract is { Status: RecurringContractStatus.Searching } && booking.OccurrenceNumber == 1;
        if (booking.RecurringContractId is not null && !isInitialSeriesDispatch && booking.RecurringContract?.Status != RecurringContractStatus.Active) return 0;

        var now = DateTimeOffset.UtcNow;
        foreach (var expired in booking.Assignments.Where(x => x.Status == AssignmentStatus.Invited && x.ExpiresAt <= now))
            expired.Status = AssignmentStatus.Expired;
        if (booking.Assignments.Any(x => x.Status == AssignmentStatus.Invited && x.ExpiresAt > now))
        {
            await db.SaveChangesAsync(cancellationToken);
            return 0;
        }

        var attemptedIds = booking.Assignments.Select(x => x.PartnerProfileId).Distinct().ToHashSet();
        if (attemptedIds.Count >= MaximumInvitations)
        {
            booking.Status = BookingStatus.NoPartnerFound;
            await db.SaveChangesAsync(cancellationToken);
            return 0;
        }

        var individualService = booking.ServiceGroup.Slug is "ve-sinh-phong-le" or "don-dep-van-phong-dinh-ky" or "don-dep-buong-phong";
        var seriesOccurrences = isInitialSeriesDispatch && booking.RecurringContractId is Guid contractId
            ? await db.Bookings.AsNoTracking().Where(x => x.RecurringContractId == contractId && x.Status != BookingStatus.Cancelled)
                .Select(x => new OccurrenceWindow(x.Id, x.ScheduledStartAt, x.ScheduledEndAt)).OrderBy(x => x.ScheduledStartAt).ToListAsync(cancellationToken)
            : [];
        var favoriteIds = booking.CleanerSelectionMode == CleanerSelectionMode.FavoriteFirst
            ? await db.FavoritePartners.AsNoTracking().Where(x => x.CustomerId == booking.CustomerId).Select(x => x.PartnerProfileId).ToListAsync(cancellationToken)
            : [];

        async Task<List<PartnerProfile>> FindMatchesAsync(bool favoritesOnly)
        {
            var found = new List<PartnerProfile>();
            var startRadiusIndex = favoritesOnly ? 0 : Math.Min(attemptedIds.Count / BatchSize, RadiusStepsMeters.Length - 1);
            for (var radiusIndex = startRadiusIndex; radiusIndex < RadiusStepsMeters.Length && found.Count == 0; radiusIndex++)
            {
                var radius = RadiusStepsMeters[radiusIndex];
                var candidatesQuery = db.PartnerProfiles.AsNoTracking().Include(x => x.User)
                    .Where(x => x.IsAvailable && x.VerificationStatus == VerificationStatus.Approved && x.User.Status == UserStatus.Active &&
                        x.ServiceLocation != null && x.ServiceLocation.Distance(booking.LocationSnapshot) <= radius &&
                        x.ServiceCapabilities.Any(capability => capability.ServiceGroupId == booking.ServiceGroupId) &&
                        !attemptedIds.Contains(x.Id));
                if (favoritesOnly) candidatesQuery = candidatesQuery.Where(x => favoriteIds.Contains(x.Id));
                candidatesQuery = individualService
                    ? candidatesQuery.Where(x => x.PartnerType == PartnerType.Individual)
                    : candidatesQuery.Where(x => x.PartnerType == PartnerType.Team && x.TeamSize >= booking.RequiredWorkers);
                var candidates = await candidatesQuery.Select(x => new
                    {
                        Profile = x, Distance = x.ServiceLocation!.Distance(booking.LocationSnapshot),
                        ActiveJobs = db.Bookings.Count(job => job.AssignedPartnerId == x.Id && job.Status != BookingStatus.Completed && job.Status != BookingStatus.Cancelled && job.ScheduledStartAt >= now.AddDays(-1))
                    })
                    .OrderBy(x => x.Distance).ThenBy(x => x.ActiveJobs).ThenByDescending(x => x.Profile.AverageRating).Take(50).ToListAsync(cancellationToken);
                foreach (var candidate in candidates)
                {
                    var availableForEveryOccurrence = seriesOccurrences.Count > 0
                        ? await IsAvailableForSeries(candidate.Profile.Id, seriesOccurrences, cancellationToken)
                        : await availability.IsAvailableAsync(candidate.Profile.Id, booking.ScheduledStartAt, booking.ScheduledEndAt ?? booking.ScheduledStartAt.AddHours(4), booking.Id, cancellationToken);
                    if (!availableForEveryOccurrence) continue;
                    found.Add(candidate.Profile);
                    if (found.Count == Math.Min(BatchSize, MaximumInvitations - attemptedIds.Count)) break;
                }
            }
            return found;
        }

        var hasUnattemptedFavorites = favoriteIds.Any(id => !attemptedIds.Contains(id));
        var matches = hasUnattemptedFavorites ? await FindMatchesAsync(true) : [];
        if (matches.Count == 0) matches = await FindMatchesAsync(false);

        if (matches.Count == 0)
        {
            if (isInitialSeriesDispatch && booking.RecurringContractId is Guid missingContractId)
                await db.Bookings.Where(x => x.RecurringContractId == missingContractId && x.AssignedPartnerId == null && x.Status != BookingStatus.Cancelled)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Status, BookingStatus.NoPartnerFound), cancellationToken);
            else booking.Status = BookingStatus.NoPartnerFound;
            await db.SaveChangesAsync(cancellationToken);
            return 0;
        }

        var urgent = booking.ScheduledStartAt <= now.AddHours(4);
        var invitationExpiry = booking.ScheduledStartAt <= now.AddMinutes(2) ? booking.ScheduledStartAt : now.AddMinutes(2);
        foreach (var partner in matches)
        {
            var distance = HaversineMeters(partner.ServiceLocation!.Y, partner.ServiceLocation.X, booking.LocationSnapshot.Y, booking.LocationSnapshot.X);
            var assignment = new BookingAssignment
            {
                BookingId = booking.Id, PartnerProfileId = partner.Id, Status = AssignmentStatus.Invited, InvitedAt = now,
                ExpiresAt = invitationExpiry,
                DistanceMetersAtInvitation = distance
            };
            db.BookingAssignments.Add(assignment);
            db.PartnerNotifications.Add(new PartnerNotification
            {
                PartnerUserId = partner.UserId, BookingId = booking.Id, Type = urgent ? "urgent_replacement" : "new_booking",
                Title = urgent ? "Cần người thay thế gần giờ làm" : "Có đơn dọn dẹp phù hợp",
                Message = $"{booking.ServiceGroup.Name} tại {booking.AddressSnapshot}, bắt đầu {booking.ScheduledStartAt:dd/MM/yyyy HH:mm}. Vui lòng phản hồi trong 2 phút."
            });
        }
        booking.Status = booking.CleanerSelectionMode == CleanerSelectionMode.CustomerChooses ? BookingStatus.AwaitingCustomerSelection : BookingStatus.Searching;
        await db.SaveChangesAsync(cancellationToken);
        return matches.Count;
    }

    private async Task<bool> IsAvailableForSeries(Guid partnerId, IEnumerable<OccurrenceWindow> occurrences, CancellationToken cancellationToken)
    {
        foreach (var occurrence in occurrences)
        {
            var end = occurrence.ScheduledEndAt ?? occurrence.ScheduledStartAt.AddHours(4);
            if (!await availability.IsAvailableAsync(partnerId, occurrence.ScheduledStartAt, end, occurrence.Id, cancellationToken)) return false;
        }
        return true;
    }

    public async Task DispatchOpenBookingsAsync(CancellationToken cancellationToken)
    {
        var ids = await db.Bookings.AsNoTracking()
            .Where(x => x.AssignedPartnerId == null && x.ScheduledStartAt > DateTimeOffset.UtcNow &&
                (x.RecurringContractId == null || x.OccurrenceNumber == 1 || x.RecurringContract!.Status == RecurringContractStatus.Active) &&
                (x.Status == BookingStatus.Searching || x.Status == BookingStatus.AwaitingCustomerSelection || x.Status == BookingStatus.NoPartnerFound))
            .OrderBy(x => x.CreatedAt).Select(x => x.Id).Take(100).ToListAsync(cancellationToken);
        foreach (var id in ids)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await DispatchAsync(id, cancellationToken);
        }
    }

    private static double HaversineMeters(double latitude1, double longitude1, double latitude2, double longitude2)
    {
        const double earthRadius = 6_371_000;
        var lat1 = latitude1 * Math.PI / 180;
        var lat2 = latitude2 * Math.PI / 180;
        var deltaLat = (latitude2 - latitude1) * Math.PI / 180;
        var deltaLon = (longitude2 - longitude1) * Math.PI / 180;
        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);
        return earthRadius * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }
}

public sealed class BookingDispatchWorker(IServiceScopeFactory scopeFactory, ILogger<BookingDispatchWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<BookingDispatchService>().DispatchOpenBookingsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception exception) { logger.LogError(exception, "Không thể phân phối vòng đơn hàng tiếp theo."); }
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
