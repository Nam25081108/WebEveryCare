using EveryCare.Api.Domain.Enums;
using EveryCare.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EveryCare.Api.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
public sealed class AdminDashboardController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var vietnamOffset = TimeSpan.FromHours(7);
        var now = DateTimeOffset.UtcNow.ToOffset(vietnamOffset);
        var todayStart = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, vietnamOffset).ToUniversalTime();
        var monthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, vietnamOffset).ToUniversalTime();
        var twelveMonthStartLocal = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, vietnamOffset).AddMonths(-11);
        var twelveMonthStart = twelveMonthStartLocal.ToUniversalTime();

        var bookingCounts = await db.Bookings.AsNoTracking().GroupBy(x => x.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() }).ToListAsync(cancellationToken);
        var statusCounts = bookingCounts.ToDictionary(x => x.Status, x => x.Count);

        var revenueRows = await db.Payments.AsNoTracking()
            .Where(x => x.Booking.Status == BookingStatus.Completed)
            .Select(x => new
            {
                x.Amount, x.DepositAmount, x.PlatformFee, x.TaskerNetAmount, x.RefundedAmount,
                EffectiveAt = x.ReleasedAt ?? x.PaidAt ?? x.Booking.CompletedAt ?? x.CreatedAt
            }).ToListAsync(cancellationToken);

        var heldDeposits = await db.Payments.AsNoTracking().Where(x => x.Status == PaymentStatus.DepositPaid)
            .SumAsync(x => (decimal?)x.DepositAmount, cancellationToken) ?? 0;
        var totalRefunded = await db.Payments.AsNoTracking().SumAsync(x => (decimal?)x.RefundedAmount, cancellationToken) ?? 0;

        static decimal PlatformFee(decimal amount, decimal stored) => stored > 0 ? stored : Math.Round(amount * .15m);
        static decimal TaskerNet(decimal amount, decimal stored, decimal fee) => stored > 0 ? stored : amount - fee;

        var allGross = revenueRows.Sum(x => x.Amount);
        var allPlatform = revenueRows.Sum(x => PlatformFee(x.Amount, x.PlatformFee));
        var allTasker = revenueRows.Sum(x => TaskerNet(x.Amount, x.TaskerNetAmount, PlatformFee(x.Amount, x.PlatformFee)));
        var monthRows = revenueRows.Where(x => x.EffectiveAt >= monthStart).ToList();
        var todayRows = revenueRows.Where(x => x.EffectiveAt >= todayStart).ToList();

        var dailyRevenue = Enumerable.Range(0, 14).Select(index =>
        {
            var localDate = now.Date.AddDays(index - 13);
            var rows = revenueRows.Where(x => x.EffectiveAt.ToOffset(vietnamOffset).Date == localDate).ToList();
            return new { Date = localDate.ToString("yyyy-MM-dd"), Gross = rows.Sum(x => x.Amount), Platform = rows.Sum(x => PlatformFee(x.Amount, x.PlatformFee)), Orders = rows.Count };
        });

        var monthlyRevenue = Enumerable.Range(0, 12).Select(index =>
        {
            var month = twelveMonthStartLocal.AddMonths(index);
            var rows = revenueRows.Where(x => { var local = x.EffectiveAt.ToOffset(vietnamOffset); return local.Year == month.Year && local.Month == month.Month; }).ToList();
            var gross = rows.Sum(x => x.Amount);
            var platform = rows.Sum(x => PlatformFee(x.Amount, x.PlatformFee));
            return new { Month = month.ToString("yyyy-MM"), Gross = gross, Platform = platform, Tasker = gross - platform, Orders = rows.Count };
        });

        var topServices = await db.Bookings.AsNoTracking().Where(x => x.Status == BookingStatus.Completed && x.CompletedAt >= twelveMonthStart)
            .GroupBy(x => new { x.ServiceGroupId, x.ServiceGroup.Name })
            .Select(group => new { Service = group.Key.Name, Orders = group.Count(), Gross = group.Sum(x => x.EstimatedTotal) })
            .OrderByDescending(x => x.Gross).Take(6).ToListAsync(cancellationToken);

        var recentBookings = await db.Bookings.AsNoTracking().OrderByDescending(x => x.CreatedAt).Take(8)
            .Select(x => new { x.Id, x.Code, Customer = x.Customer.FullName, Service = x.ServiceGroup.Name, x.Status, x.EstimatedTotal, x.CreatedAt })
            .ToListAsync(cancellationToken);

        var recentPayments = await db.Payments.AsNoTracking().Where(x => x.Booking.Status == BookingStatus.Completed)
            .OrderByDescending(x => x.ReleasedAt ?? x.PaidAt ?? x.Booking.CompletedAt ?? x.CreatedAt).Take(50)
            .Select(x => new
            {
                x.Booking.Code, Customer = x.Booking.Customer.FullName, Service = x.Booking.ServiceGroup.Name,
                x.Amount, x.PlatformFee, x.TaskerNetAmount, x.RefundedAmount,
                CompletedAt = x.ReleasedAt ?? x.PaidAt ?? x.Booking.CompletedAt ?? x.CreatedAt
            }).ToListAsync(cancellationToken);

        var customers = await db.Users.AsNoTracking().CountAsync(x => x.Role == UserRole.Customer, cancellationToken);
        var newCustomersThisMonth = await db.Users.AsNoTracking().CountAsync(x => x.Role == UserRole.Customer && x.CreatedAt >= monthStart, cancellationToken);
        var approvedPartners = await db.PartnerProfiles.AsNoTracking().CountAsync(x => x.VerificationStatus == VerificationStatus.Approved, cancellationToken);
        var availablePartners = await db.PartnerProfiles.AsNoTracking().CountAsync(x => x.VerificationStatus == VerificationStatus.Approved && x.IsAvailable, cancellationToken);
        var pendingPartners = await db.PartnerProfiles.AsNoTracking().CountAsync(x => x.VerificationStatus == VerificationStatus.Pending, cancellationToken);
        var serviceGroups = await db.ServiceGroups.AsNoTracking().CountAsync(x => x.IsActive, cancellationToken);
        var servicePackages = await db.ServicePackages.AsNoTracking().CountAsync(x => x.IsActive, cancellationToken);

        return Ok(new
        {
            GeneratedAt = DateTimeOffset.UtcNow,
            Summary = new
            {
                TotalOrders = statusCounts.Values.Sum(),
                OrdersToday = await db.Bookings.AsNoTracking().CountAsync(x => x.CreatedAt >= todayStart, cancellationToken),
                CompletedOrders = statusCounts.GetValueOrDefault(BookingStatus.Completed),
                ActiveOrders = statusCounts.Where(x => x.Key is BookingStatus.Searching or BookingStatus.AwaitingCustomerSelection or BookingStatus.Assigned or BookingStatus.PartnerTravelling or BookingStatus.InProgress or BookingStatus.AwaitingCustomerConfirmation).Sum(x => x.Value),
                CancelledOrders = statusCounts.GetValueOrDefault(BookingStatus.Cancelled),
                Customers = customers, NewCustomersThisMonth = newCustomersThisMonth,
                ApprovedPartners = approvedPartners, AvailablePartners = availablePartners, PendingPartners = pendingPartners,
                ServiceGroups = serviceGroups, ServicePackages = servicePackages,
                GrossRevenue = allGross, PlatformRevenue = allPlatform, TaskerPayout = allTasker,
                MonthGrossRevenue = monthRows.Sum(x => x.Amount),
                MonthPlatformRevenue = monthRows.Sum(x => PlatformFee(x.Amount, x.PlatformFee)),
                TodayGrossRevenue = todayRows.Sum(x => x.Amount),
                HeldDeposits = heldDeposits, TotalRefunded = totalRefunded,
                AverageOrderValue = revenueRows.Count == 0 ? 0 : Math.Round(allGross / revenueRows.Count)
            },
            OrderStatuses = bookingCounts.OrderByDescending(x => x.Count),
            DailyRevenue = dailyRevenue,
            MonthlyRevenue = monthlyRevenue,
            TopServices = topServices,
            RecentBookings = recentBookings,
            RecentPayments = recentPayments
        });
    }
}
