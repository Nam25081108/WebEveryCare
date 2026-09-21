using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EveryCare.Api.Domain.Entities;
using EveryCare.Api.Domain.Enums;
using EveryCare.Api.Infrastructure.Persistence;
using EveryCare.Api.Services;

namespace EveryCare.Api.Controllers;

public sealed record CustomerBookingActionRequest(string Phone, string? Note);

[ApiController]
[Route("api/customer-bookings")]
public sealed class CustomerBookingsController(AppDbContext db, BookingDispatchService dispatch, BookingLifecycleService lifecycle, ILogger<CustomerBookingsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string phone, CancellationToken cancellationToken)
    {
        var normalized = new string((phone ?? "").Where(char.IsDigit).ToArray());
        var bookings = await db.Bookings.AsNoTracking().Where(x => x.Customer.Phone == normalized).OrderByDescending(x => x.CreatedAt).Take(100)
            .Select(x => new
            {
                x.Id, x.Code, Service = x.ServiceGroup.Name, Package = x.ServicePackage != null ? x.ServicePackage.Name : null,
                x.AddressSnapshot, x.ScheduledStartAt, x.ScheduledEndAt, x.Status, x.EstimatedTotal, x.CustomerRequest,
                x.TaskerConfirmedAt, x.ArrivedAt, x.StartedAt, x.CompletionReportedAt, x.CompletedAt, x.IssueNote,
                Tasker = x.AssignedPartner == null ? null : new { Id = x.AssignedPartner.Id, Name = x.AssignedPartner.TeamName ?? x.AssignedPartner.User.FullName, x.AssignedPartner.AvatarUrl, x.AssignedPartner.AverageRating, Phone = MaskPhone(x.AssignedPartner.User.Phone), IsFavorite = db.FavoritePartners.Any(favorite => favorite.CustomerId == x.CustomerId && favorite.PartnerProfileId == x.AssignedPartnerId) },
                Payment = x.Payment == null ? null : new { x.Payment.Status, x.Payment.Amount, x.Payment.DepositAmount, x.Payment.RemainingAmount, x.Payment.RefundedAmount, x.Payment.PlatformFee, x.Payment.TaskerNetAmount }
            }).ToListAsync(cancellationToken);
        return Ok(bookings);
    }

    [HttpPost("{id:guid}/pay-deposit")]
    public async Task<IActionResult> PayDeposit(Guid id, CustomerBookingActionRequest request, CancellationToken cancellationToken)
    {
        var booking = await OwnedBooking(id, request.Phone, cancellationToken);
        if (booking is null) return NotFound(new { message = "Không tìm thấy đơn hàng." });
        await db.Entry(booking).Reference(x => x.Payment).LoadAsync(cancellationToken);
        if (booking.Status != BookingStatus.Draft || booking.Payment is null) return BadRequest(new { message = "Đơn không còn chờ thanh toán cọc." });
        booking.Payment.Status = PaymentStatus.DepositPaid;
        booking.Payment.DepositPaidAt = DateTimeOffset.UtcNow;
        booking.Payment.BankTransactionReference = $"MOCK-DEPOSIT-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
        booking.Status = BookingStatus.Searching;
        await db.SaveChangesAsync(cancellationToken);
        var invited = 0;
        try
        {
            invited = await dispatch.DispatchAsync(id, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Đơn {BookingId} đã thanh toán cọc nhưng chưa thể gửi lời mời Tasker ngay.", id);
            db.ChangeTracker.Clear();
        }
        return Ok(new { booking.Code, booking.Status, booking.Payment.DepositAmount, InvitedPartners = invited, message = invited > 0 ? $"Đặt cọc thành công. Đã gửi đơn cho {invited} Tasker phù hợp." : "Đặt cọc thành công. Hệ thống đang tìm Tasker phù hợp." });
    }

    [HttpPost("{id:guid}/confirm-completion")]
    public async Task<IActionResult> ConfirmCompletion(Guid id, CustomerBookingActionRequest request, CancellationToken cancellationToken)
    {
        var booking = await OwnedBooking(id, request.Phone, cancellationToken);
        if (booking is null) return NotFound(new { message = "Không tìm thấy đơn hàng." });
        if (!await lifecycle.CompleteAsync(id, true, cancellationToken)) return BadRequest(new { message = "Đơn chưa ở trạng thái chờ xác nhận hoàn thành." });
        return Ok(new { message = "Đã xác nhận hoàn thành và thanh toán phần còn lại thành công." });
    }

    [HttpPost("{id:guid}/report-issue")]
    public async Task<IActionResult> ReportIssue(Guid id, CustomerBookingActionRequest request, CancellationToken cancellationToken)
    {
        var booking = await OwnedBooking(id, request.Phone, cancellationToken);
        if (booking is null) return NotFound(new { message = "Không tìm thấy đơn hàng." });
        if (booking.Status != BookingStatus.AwaitingCustomerConfirmation) return BadRequest(new { message = "Đơn chưa ở trạng thái chờ xác nhận." });
        booking.Status = BookingStatus.IssueReported;
        booking.IssueReportedAt = DateTimeOffset.UtcNow;
        booking.IssueNote = string.IsNullOrWhiteSpace(request.Note) ? "Khách hàng yêu cầu hỗ trợ." : request.Note.Trim();
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Đã ghi nhận vấn đề. Khoản thanh toán còn lại đang được giữ để hỗ trợ xử lý." });
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CustomerBookingActionRequest request, CancellationToken cancellationToken)
    {
        var booking = await OwnedBooking(id, request.Phone, cancellationToken);
        if (booking is null) return NotFound(new { message = "Không tìm thấy đơn hàng." });
        if (booking.ScheduledStartAt <= DateTimeOffset.UtcNow || booking.Status is BookingStatus.InProgress or BookingStatus.AwaitingCustomerConfirmation or BookingStatus.Completed or BookingStatus.Cancelled)
            return BadRequest(new { message = "Đơn không còn có thể hủy." });
        var hours = (booking.ScheduledStartAt - DateTimeOffset.UtcNow).TotalHours;
        var rate = hours >= 24 ? 1m : hours >= 12 ? .75m : .5m;
        await lifecycle.RefundAsync(id, rate, $"Khách hủy lịch; tỷ lệ hoàn tiền {rate:P0}.", cancellationToken);
        return Ok(new { refundRate = rate, message = $"Đã hủy đơn và hoàn {rate:P0} số tiền đã thanh toán." });
    }

    private Task<Booking?> OwnedBooking(Guid id, string phone, CancellationToken cancellationToken)
    {
        var normalized = new string((phone ?? "").Where(char.IsDigit).ToArray());
        return db.Bookings.SingleOrDefaultAsync(x => x.Id == id && x.Customer.Phone == normalized, cancellationToken);
    }

    private static string MaskPhone(string phone) => phone.Length < 7 ? "******" : $"{phone[..3]}****{phone[^3..]}";
}
