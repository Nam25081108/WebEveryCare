using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EveryCare.Api.Domain.Entities;
using EveryCare.Api.Domain.Enums;
using EveryCare.Api.Infrastructure.Persistence;
using EveryCare.Api.Services;

namespace EveryCare.Api.Controllers;

public sealed record CustomerBookingActionRequest(string Phone, string? Note);
public sealed record SelectBookingPartnerRequest(string Phone, Guid PartnerProfileId);
public sealed record CreateReviewRequest(string Phone, int Rating, string? Comment);

[ApiController]
[Route("api/customer-bookings")]
public sealed class CustomerBookingsController(AppDbContext db, BookingDispatchService dispatch, BookingLifecycleService lifecycle, PartnerAvailabilityService availability, ILogger<CustomerBookingsController> logger) : ControllerBase
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
                Contract = x.RecurringContract == null ? null : new { x.RecurringContract.Id, x.RecurringContract.Code, x.RecurringContract.Status, x.RecurringContract.ContractMonths, x.RecurringContract.TotalOccurrences, x.RecurringContract.CompletedOccurrences, x.RecurringContract.EstimatedTotal, x.RecurringContract.DepositAmount, x.RecurringContract.RemainingAmount },
                x.OccurrenceNumber,
                Candidates = x.Assignments.Where(a => x.CleanerSelectionMode == CleanerSelectionMode.CustomerChooses && a.Status == AssignmentStatus.Accepted).OrderBy(a => a.DistanceMetersAtInvitation).Select(a => new { PartnerId = a.PartnerProfileId, Name = a.PartnerProfile.TeamName ?? a.PartnerProfile.User.FullName, a.PartnerProfile.AvatarUrl, a.PartnerProfile.AverageRating, a.PartnerProfile.CompletedBookings, a.DistanceMetersAtInvitation }),
                Tasker = x.AssignedPartner == null ? null : new { Id = x.AssignedPartner.Id, Name = x.AssignedPartner.TeamName ?? x.AssignedPartner.User.FullName, x.AssignedPartner.AvatarUrl, x.AssignedPartner.AverageRating, Phone = MaskPhone(x.AssignedPartner.User.Phone), IsFavorite = db.FavoritePartners.Any(favorite => favorite.CustomerId == x.CustomerId && favorite.PartnerProfileId == x.AssignedPartnerId) },
                Review = x.Review == null ? null : new { x.Review.Rating, x.Review.Comment, x.Review.CreatedAt },
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
        var now = DateTimeOffset.UtcNow;
        var dispatchBookingId = booking.Id;
        decimal depositAmount;
        decimal remainingAmount;
        if (booking.RecurringContractId is Guid contractId)
        {
            var contract = await db.RecurringServiceContracts.Include(x => x.Occurrences).ThenInclude(x => x.Payment).SingleAsync(x => x.Id == contractId, cancellationToken);
            if (contract.Status != RecurringContractStatus.PendingDeposit) return BadRequest(new { message = "Hợp đồng không còn chờ thanh toán cọc." });
            foreach (var occurrence in contract.Occurrences)
            {
                occurrence.Payment!.Status = PaymentStatus.DepositPaid;
                occurrence.Payment.DepositPaidAt = now;
                occurrence.Payment.BankTransactionReference = $"MOCK-DEPOSIT-{contract.Code}";
                occurrence.Status = BookingStatus.Searching;
            }
            contract.Status = RecurringContractStatus.Searching;
            dispatchBookingId = contract.Occurrences.OrderBy(x => x.OccurrenceNumber).First().Id;
            depositAmount = contract.DepositAmount;
            remainingAmount = contract.RemainingAmount;
        }
        else
        {
            booking.Payment.Status = PaymentStatus.DepositPaid;
            booking.Payment.DepositPaidAt = now;
            booking.Payment.BankTransactionReference = $"MOCK-DEPOSIT-{now:yyyyMMddHHmmss}";
            booking.Status = BookingStatus.Searching;
            depositAmount = booking.Payment.DepositAmount;
            remainingAmount = booking.Payment.RemainingAmount;
        }
        await db.SaveChangesAsync(cancellationToken);
        var invited = 0;
        try
        {
            invited = await dispatch.DispatchAsync(dispatchBookingId, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Đơn {BookingId} đã thanh toán cọc nhưng chưa thể gửi lời mời Tasker ngay.", id);
            db.ChangeTracker.Clear();
        }
        return Ok(new { booking.Code, Status = BookingStatus.Searching, DepositAmount = depositAmount, RemainingAmount = remainingAmount, InvitedPartners = invited, message = invited > 0 ? $"Đặt cọc thành công. Đã gửi đơn cho {invited} Tasker phù hợp." : "Đặt cọc thành công. Hệ thống đang tìm Tasker phù hợp." });
    }

    [HttpPost("{id:guid}/select-partner")]
    public async Task<IActionResult> SelectPartner(Guid id, SelectBookingPartnerRequest request, CancellationToken cancellationToken)
    {
        var booking = await db.Bookings.Include(x => x.RecurringContract).Include(x => x.Assignments).ThenInclude(x => x.PartnerProfile).ThenInclude(x => x.User)
            .SingleOrDefaultAsync(x => x.Id == id && x.Customer.Phone == NormalizePhone(request.Phone), cancellationToken);
        if (booking is null) return NotFound(new { message = "Không tìm thấy đơn hàng." });
        if (booking.CleanerSelectionMode != CleanerSelectionMode.CustomerChooses || booking.Status != BookingStatus.AwaitingCustomerSelection)
            return BadRequest(new { message = "Đơn không ở trạng thái chờ khách chọn Tasker." });
        var selected = booking.Assignments.SingleOrDefault(x => x.PartnerProfileId == request.PartnerProfileId && x.Status == AssignmentStatus.Accepted);
        if (selected is null) return BadRequest(new { message = "Tasker này chưa đồng ý nhận công việc." });

        var assignWholeSeries = booking.RecurringContract is { Status: RecurringContractStatus.Searching } && booking.OccurrenceNumber == 1;
        var occurrences = assignWholeSeries && booking.RecurringContractId is Guid contractId
            ? await db.Bookings.Where(x => x.RecurringContractId == contractId && x.Status != BookingStatus.Cancelled).OrderBy(x => x.OccurrenceNumber).ToListAsync(cancellationToken)
            : [booking];
        foreach (var occurrence in occurrences)
        {
            var end = occurrence.ScheduledEndAt ?? occurrence.ScheduledStartAt.AddHours(4);
            if (!await availability.IsAvailableAsync(request.PartnerProfileId, occurrence.ScheduledStartAt, end, occurrence.Id, cancellationToken))
                return Conflict(new { message = "Tasker vừa được chọn không còn trống toàn bộ các khung giờ của lịch này." });
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var occurrenceIds = occurrences.Select(x => x.Id).ToArray();
        var claimed = await db.Bookings.Where(x => occurrenceIds.Contains(x.Id) && x.AssignedPartnerId == null &&
                (x.Status == BookingStatus.Searching || x.Status == BookingStatus.NoPartnerFound || x.Status == BookingStatus.AwaitingCustomerSelection))
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.AssignedPartnerId, request.PartnerProfileId).SetProperty(x => x.Status, BookingStatus.Assigned).SetProperty(x => x.TaskerConfirmedAt, (DateTimeOffset?)null), cancellationToken);
        if (claimed != occurrences.Count) { await transaction.RollbackAsync(cancellationToken); return Conflict(new { message = "Một lượt làm việc vừa được phân công hoặc thay đổi. Vui lòng tải lại." }); }
        selected.Status = AssignmentStatus.Selected;
        selected.RespondedAt = DateTimeOffset.UtcNow;
        if (assignWholeSeries)
        {
            foreach (var occurrence in occurrences.Skip(1))
                db.BookingAssignments.Add(new BookingAssignment
                {
                    BookingId = occurrence.Id,
                    PartnerProfileId = selected.PartnerProfileId,
                    Status = AssignmentStatus.Selected,
                    InvitedAt = selected.InvitedAt,
                    ExpiresAt = selected.ExpiresAt,
                    RespondedAt = selected.RespondedAt,
                    DistanceMetersAtInvitation = selected.DistanceMetersAtInvitation
                });
        }
        foreach (var other in booking.Assignments.Where(x => x.Id != selected.Id && x.Status is AssignmentStatus.Invited or AssignmentStatus.Accepted)) other.Status = AssignmentStatus.Released;
        if (assignWholeSeries && booking.RecurringContractId is Guid recurringId)
            await db.RecurringServiceContracts.Where(x => x.Id == recurringId).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Status, RecurringContractStatus.Active), cancellationToken);
        db.PartnerNotifications.Add(new PartnerNotification { PartnerUserId = selected.PartnerProfile.UserId, BookingId = booking.Id, Type = "customer_selected", Title = "Khách hàng đã chọn bạn", Message = $"Bạn đã được chọn thực hiện {occurrences.Count} lượt của lịch {booking.Code}. Vui lòng kiểm tra và xác nhận từng lịch trước giờ làm." });
        foreach (var other in booking.Assignments.Where(x => x.Id != selected.Id && x.Status == AssignmentStatus.Released && x.RespondedAt is not null))
            db.PartnerNotifications.Add(new PartnerNotification { PartnerUserId = other.PartnerProfile.UserId, BookingId = booking.Id, Type = "not_selected", Title = "Khách đã chọn Tasker khác", Message = $"Cảm ơn bạn đã phản hồi lịch {booking.Code}. Khách hàng đã lựa chọn một Tasker khác; khung giờ của bạn không bị giữ." });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(new { message = $"Đã chọn {selected.PartnerProfile.TeamName ?? selected.PartnerProfile.User.FullName}. Tasker đã nhận được thông báo và các khung giờ đã được giữ." });
    }

    [HttpPost("{id:guid}/review")]
    public async Task<IActionResult> Review(Guid id, CreateReviewRequest request, CancellationToken cancellationToken)
    {
        if (request.Rating is < 1 or > 5) return BadRequest(new { message = "Số sao phải từ 1 đến 5." });
        var booking = await db.Bookings.Include(x => x.Review).SingleOrDefaultAsync(x => x.Id == id && x.Customer.Phone == NormalizePhone(request.Phone), cancellationToken);
        if (booking is null) return NotFound(new { message = "Không tìm thấy đơn hàng." });
        if (booking.Status != BookingStatus.Completed || booking.AssignedPartnerId is null) return BadRequest(new { message = "Chỉ có thể đánh giá công việc đã hoàn thành." });
        if (booking.Review is not null) return Conflict(new { message = "Bạn đã đánh giá lượt làm việc này." });
        booking.Review = new Review { CustomerId = booking.CustomerId, PartnerProfileId = booking.AssignedPartnerId.Value, Rating = request.Rating, Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim() };
        await db.SaveChangesAsync(cancellationToken);
        var ratings = await db.Reviews.AsNoTracking().Where(x => x.PartnerProfileId == booking.AssignedPartnerId).Select(x => x.Rating).ToListAsync(cancellationToken);
        var profile = await db.PartnerProfiles.SingleAsync(x => x.Id == booking.AssignedPartnerId, cancellationToken);
        profile.AverageRating = ratings.Count == 0 ? 0 : Math.Round((decimal)ratings.Average(), 2);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Cảm ơn bạn đã đánh giá Tasker.", profile.AverageRating });
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
        var affectedBookings = booking.RecurringContractId is Guid contractId
            ? await db.Bookings.Where(x => x.RecurringContractId == contractId && x.Status != BookingStatus.Completed && x.Status != BookingStatus.Cancelled).OrderBy(x => x.ScheduledStartAt).ToListAsync(cancellationToken)
            : [booking];
        var hours = (affectedBookings[0].ScheduledStartAt - DateTimeOffset.UtcNow).TotalHours;
        var rate = hours >= 24 ? 1m : hours >= 12 ? .75m : .5m;
        foreach (var affected in affectedBookings)
        {
            await lifecycle.RefundAsync(affected.Id, rate, $"Khách hủy lịch; tỷ lệ hoàn tiền {rate:P0}.", cancellationToken);
            await db.BookingAssignments.Where(x => x.BookingId == affected.Id && (x.Status == AssignmentStatus.Invited || x.Status == AssignmentStatus.Accepted || x.Status == AssignmentStatus.Selected))
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Status, AssignmentStatus.Released), cancellationToken);
            await db.Bookings.Where(x => x.Id == affected.Id).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.AssignedPartnerId, (Guid?)null).SetProperty(x => x.TaskerConfirmedAt, (DateTimeOffset?)null), cancellationToken);
        }
        if (booking.RecurringContractId is Guid recurringId)
            await db.RecurringServiceContracts.Where(x => x.Id == recurringId).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Status, RecurringContractStatus.Cancelled), cancellationToken);
        return Ok(new { refundRate = rate, message = affectedBookings.Count > 1 ? $"Đã hủy hợp đồng và {affectedBookings.Count} lượt chưa hoàn thành; hoàn {rate:P0} tiền cọc tương ứng." : $"Đã hủy đơn và hoàn {rate:P0} số tiền đã thanh toán." });
    }

    private Task<Booking?> OwnedBooking(Guid id, string phone, CancellationToken cancellationToken)
    {
        var normalized = NormalizePhone(phone);
        return db.Bookings.SingleOrDefaultAsync(x => x.Id == id && x.Customer.Phone == normalized, cancellationToken);
    }

    private static string NormalizePhone(string phone) => new((phone ?? "").Where(char.IsDigit).ToArray());

    private static string MaskPhone(string phone) => phone.Length < 7 ? "******" : $"{phone[..3]}****{phone[^3..]}";
}
