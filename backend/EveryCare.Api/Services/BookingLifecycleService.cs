using Microsoft.EntityFrameworkCore;
using EveryCare.Api.Domain.Enums;
using EveryCare.Api.Infrastructure.Persistence;

namespace EveryCare.Api.Services;

public sealed class BookingLifecycleService(AppDbContext db)
{
    public async Task<bool> CompleteAsync(Guid bookingId, bool confirmedByCustomer, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var booking = await db.Bookings.Include(x => x.Payment).Include(x => x.AssignedPartner)
            .SingleOrDefaultAsync(x => x.Id == bookingId, cancellationToken);
        if (booking is null || booking.Status != BookingStatus.AwaitingCustomerConfirmation || booking.IssueReportedAt is not null) return false;
        var now = DateTimeOffset.UtcNow;
        booking.Status = BookingStatus.Completed;
        booking.CompletedAt = now;
        if (confirmedByCustomer) booking.CustomerConfirmedAt = now;
        if (booking.Payment is not null && booking.Payment.ReleasedAt is null)
        {
            booking.Payment.Status = PaymentStatus.Paid;
            booking.Payment.RemainingPaidAt = now;
            booking.Payment.RemainingAmount = 0;
            booking.Payment.PaidAt = now;
            booking.Payment.PlatformFee = Math.Round(booking.Payment.Amount * .15m);
            booking.Payment.TaskerNetAmount = booking.Payment.Amount - booking.Payment.PlatformFee;
            booking.Payment.ReleasedAt = now;
            booking.Payment.BankTransactionReference ??= $"MOCK-{now:yyyyMMddHHmmss}";
            if (booking.AssignedPartner is not null)
            {
                booking.AssignedPartner.WalletBalance += booking.Payment.TaskerNetAmount;
                booking.AssignedPartner.CompletedBookings++;
            }
        }
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task RefundAsync(Guid bookingId, decimal rate, string reason, CancellationToken cancellationToken)
    {
        var booking = await db.Bookings.Include(x => x.Payment).SingleAsync(x => x.Id == bookingId, cancellationToken);
        if (booking.Payment is not null)
        {
            var paid = booking.Payment.Status == PaymentStatus.Paid ? booking.Payment.Amount : booking.Payment.DepositPaidAt is not null ? booking.Payment.DepositAmount : 0;
            booking.Payment.RefundedAmount = Math.Round(paid * rate);
            booking.Payment.Status = rate >= 1 ? PaymentStatus.Refunded : PaymentStatus.PartiallyRefunded;
        }
        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAt = DateTimeOffset.UtcNow;
        booking.CancellationReason = reason;
        await db.SaveChangesAsync(cancellationToken);
    }
}

public sealed class BookingLifecycleWorker(IServiceScopeFactory scopeFactory, ILogger<BookingLifecycleWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var dispatch = scope.ServiceProvider.GetRequiredService<BookingDispatchService>();
                var lifecycle = scope.ServiceProvider.GetRequiredService<BookingLifecycleService>();
                var now = DateTimeOffset.UtcNow;

                var unconfirmed = await db.Bookings.Include(x => x.Assignments)
                    .Where(x => x.Status == BookingStatus.Assigned && x.TaskerConfirmedAt == null && x.ScheduledStartAt <= now.AddHours(12) && x.ScheduledStartAt > now &&
                        x.Assignments.Any(a => a.Status == AssignmentStatus.Selected && a.RespondedAt <= now.AddMinutes(-5)))
                    .ToListAsync(stoppingToken);
                foreach (var booking in unconfirmed)
                {
                    foreach (var assignment in booking.Assignments.Where(x => x.Status == AssignmentStatus.Selected)) assignment.Status = AssignmentStatus.Released;
                    booking.AssignedPartnerId = null;
                    booking.Status = BookingStatus.Searching;
                    booking.CancellationReason = "Tasker không xác nhận lại trước giờ làm 12 tiếng.";
                    await db.SaveChangesAsync(stoppingToken);
                    await dispatch.DispatchAsync(booking.Id, stoppingToken);
                }

                var overdueSearches = await db.Bookings.Where(x => x.AssignedPartnerId == null && x.ScheduledStartAt <= now &&
                    (x.Status == BookingStatus.Searching || x.Status == BookingStatus.NoPartnerFound)).Select(x => x.Id).ToListAsync(stoppingToken);
                foreach (var id in overdueSearches) await lifecycle.RefundAsync(id, 1m, "Không tìm được Tasker trước giờ làm; hoàn 100% số tiền đã thanh toán.", stoppingToken);

                var autoComplete = await db.Bookings.Where(x => x.Status == BookingStatus.AwaitingCustomerConfirmation && x.IssueReportedAt == null && x.CompletionReportedAt <= now.AddHours(-24)).Select(x => x.Id).ToListAsync(stoppingToken);
                foreach (var id in autoComplete) await lifecycle.CompleteAsync(id, false, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception exception) { logger.LogError(exception, "Không thể xử lý vòng đời đơn hàng."); }
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
