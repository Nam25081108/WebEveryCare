using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EveryCare.Api.Domain.Entities;
using EveryCare.Api.Domain.Enums;
using EveryCare.Api.Infrastructure.Persistence;
using EveryCare.Api.Services;

namespace EveryCare.Api.Controllers;

public sealed record PartnerLoginRequest(string Phone, string Password);
public sealed record AvailabilityRuleInput(DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, bool IsActive = true);
public sealed record AvailabilityOverrideInput(DateOnly Date, bool IsUnavailable, TimeOnly? StartTime, TimeOnly? EndTime, string? Note);
public sealed record SaveAvailabilityRequest(List<AvailabilityRuleInput> Rules, List<AvailabilityOverrideInput> Overrides);
public sealed record ToggleJobInvitationsRequest(bool Enabled);
public sealed record RespondInvitationRequest(bool Accept);
public sealed record CancelAssignedBookingRequest(string? Reason);

[ApiController]
[Route("api/partner")]
public sealed class PartnerPortalController(AppDbContext db, PartnerSessionService sessions, PartnerAvailabilityService availability, BookingDispatchService dispatch) : ControllerBase
{
    [HttpPost("auth/login")]
    public async Task<IActionResult> Login(PartnerLoginRequest request, CancellationToken cancellationToken)
    {
        var phone = new string(request.Phone.Where(char.IsDigit).ToArray());
        var user = await db.Users.Include(x => x.PartnerProfile).SingleOrDefaultAsync(x => x.Phone == phone && x.Role == UserRole.Partner, cancellationToken);
        if (user is null || !PasswordService.Verify(request.Password, user.PasswordHash)) return Unauthorized(new { message = "Số điện thoại hoặc mật khẩu không đúng." });
        if (user.PartnerProfile?.VerificationStatus == VerificationStatus.Pending) return StatusCode(403, new { message = "Hồ sơ của bạn đang chờ quản trị viên xét duyệt." });
        if (user.PartnerProfile?.VerificationStatus == VerificationStatus.Rejected) return StatusCode(403, new { message = $"Hồ sơ chưa được chấp nhận. {user.PartnerProfile.RejectionReason}" });
        if (user.Status == UserStatus.Locked) return StatusCode(403, new { message = "Tài khoản đối tác đang bị khóa." });
        if (user.Status != UserStatus.Active) return StatusCode(403, new { message = "Tài khoản chưa được kích hoạt." });
        user.LastLoginAt = DateTimeOffset.UtcNow;
        var (token, session) = await sessions.CreateAsync(user, cancellationToken);
        return Ok(new { token, expiresAt = session.ExpiresAt, user = new { user.Id, user.FullName, user.Phone, user.Email } });
    }

    [HttpPost("auth/logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var header = Request.Headers.Authorization.ToString();
        if (header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var hash = PasswordService.HashToken(header[7..].Trim());
                var session = await db.PartnerSessions.SingleOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);
                if (session is not null) { session.RevokedAt = DateTimeOffset.UtcNow; await db.SaveChangesAsync(cancellationToken); }
            }
            catch (FormatException) { }
        }
        return NoContent();
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var user = await RequirePartnerAsync(cancellationToken);
        if (user is null) return Unauthorized(new { message = "Phiên đăng nhập đã hết hạn." });
        var profile = await db.PartnerProfiles.AsNoTracking().Include(x => x.ServiceCapabilities).ThenInclude(x => x.ServiceGroup).SingleAsync(x => x.UserId == user.Id, cancellationToken);
        return Ok(new { user.Id, user.FullName, user.Phone, user.Email, PartnerProfileId = profile.Id, profile.PartnerType, profile.TeamName, profile.TeamSize, profile.IsAvailable, profile.ServiceAddress, profile.AverageRating, profile.CompletedBookings, profile.WalletBalance, Services = profile.ServiceCapabilities.Select(x => x.ServiceGroup.Name) });
    }

    [HttpPatch("me/job-invitations")]
    public async Task<IActionResult> ToggleInvitations(ToggleJobInvitationsRequest request, CancellationToken cancellationToken)
    {
        var user = await RequirePartnerAsync(cancellationToken);
        if (user is null) return Unauthorized();
        var profile = await db.PartnerProfiles.Include(x => x.ServiceCapabilities).SingleAsync(x => x.UserId == user.Id, cancellationToken);
        profile.IsAvailable = request.Enabled;
        await db.SaveChangesAsync(cancellationToken);
        var invitationsCreated = request.Enabled ? await DispatchOpenAndCountAsync(profile.Id, cancellationToken) : 0;
        return Ok(new { profile.IsAvailable, invitationsCreated, message = request.Enabled
            ? invitationsCreated > 0 ? $"Đã bật nhận việc và gửi {invitationsCreated} lời mời phù hợp." : "Đã bật nhận lời mời công việc mới."
            : "Đã tắt lời mời mới; các đơn đã nhận vẫn được giữ." });
    }

    [HttpGet("me/availability")]
    public async Task<IActionResult> GetAvailability(CancellationToken cancellationToken)
    {
        var user = await RequirePartnerAsync(cancellationToken);
        if (user is null) return Unauthorized();
        var profileId = await db.PartnerProfiles.Where(x => x.UserId == user.Id).Select(x => x.Id).SingleAsync(cancellationToken);
        var rules = await db.PartnerAvailabilityRules.AsNoTracking().Where(x => x.PartnerProfileId == profileId).OrderBy(x => x.DayOfWeek).Select(x => new { x.Id, x.DayOfWeek, x.StartTime, x.EndTime, x.IsActive }).ToListAsync(cancellationToken);
        var overrides = await db.PartnerAvailabilityOverrides.AsNoTracking().Where(x => x.PartnerProfileId == profileId && x.Date >= DateOnly.FromDateTime(DateTime.Today)).OrderBy(x => x.Date).Select(x => new { x.Id, x.Date, x.IsUnavailable, x.StartTime, x.EndTime, x.Note }).ToListAsync(cancellationToken);
        var bookings = await db.Bookings.AsNoTracking().Where(x => x.AssignedPartnerId == profileId && x.Status != BookingStatus.Cancelled && x.Status != BookingStatus.Completed).OrderBy(x => x.ScheduledStartAt).Select(x => new { x.Id, x.Code, x.AddressSnapshot, x.ScheduledStartAt, x.ScheduledEndAt, x.Status, x.IsRecurring, x.RecurrenceRule, x.CustomerRequest, x.TaskerConfirmedAt, x.ArrivedAt, x.StartedAt, x.CompletionReportedAt }).ToListAsync(cancellationToken);
        return Ok(new { rules, overrides, bookings });
    }

    [HttpPut("me/availability")]
    public async Task<IActionResult> SaveAvailability(SaveAvailabilityRequest request, CancellationToken cancellationToken)
    {
        var user = await RequirePartnerAsync(cancellationToken);
        if (user is null) return Unauthorized();
        if (request.Rules.GroupBy(x => x.DayOfWeek).Any(x => x.Count() > 1) || request.Rules.Any(x => x.StartTime >= x.EndTime)) return BadRequest(new { message = "Mỗi ngày chỉ có một khung giờ và giờ kết thúc phải sau giờ bắt đầu." });
        if (request.Overrides.GroupBy(x => x.Date).Any(x => x.Count() > 1) || request.Overrides.Any(x => !x.IsUnavailable && (x.StartTime is null || x.EndTime is null || x.StartTime >= x.EndTime))) return BadRequest(new { message = "Ngày ngoại lệ không hợp lệ." });
        var profile = await db.PartnerProfiles.Include(x => x.ServiceCapabilities).SingleAsync(x => x.UserId == user.Id, cancellationToken);
        var profileId = profile.Id;
        db.PartnerAvailabilityRules.RemoveRange(await db.PartnerAvailabilityRules.Where(x => x.PartnerProfileId == profileId).ToListAsync(cancellationToken));
        db.PartnerAvailabilityOverrides.RemoveRange(await db.PartnerAvailabilityOverrides.Where(x => x.PartnerProfileId == profileId && x.Date >= DateOnly.FromDateTime(DateTime.Today)).ToListAsync(cancellationToken));
        db.PartnerAvailabilityRules.AddRange(request.Rules.Select(x => new PartnerAvailabilityRule { PartnerProfileId = profileId, DayOfWeek = x.DayOfWeek, StartTime = x.StartTime, EndTime = x.EndTime, IsActive = x.IsActive }));
        db.PartnerAvailabilityOverrides.AddRange(request.Overrides.Select(x => new PartnerAvailabilityOverride { PartnerProfileId = profileId, Date = x.Date, IsUnavailable = x.IsUnavailable, StartTime = x.StartTime, EndTime = x.EndTime, Note = x.Note?.Trim() }));
        await db.SaveChangesAsync(cancellationToken);
        var invitationsCreated = profile.IsAvailable && request.Rules.Any(x => x.IsActive)
            ? await DispatchOpenAndCountAsync(profile.Id, cancellationToken)
            : 0;
        var message = !profile.IsAvailable
            ? "Đã cập nhật lịch. Hãy bật ‘Nhận lời mời công việc mới’ ở trang Tổng quan để được phân phối đơn."
            : invitationsCreated > 0
                ? $"Đã cập nhật lịch và gửi {invitationsCreated} lời mời phù hợp đang chờ."
                : "Đã cập nhật lịch làm việc.";
        return Ok(new { message, invitationsCreated });
    }

    [HttpGet("me/invitations")]
    public async Task<IActionResult> GetInvitations(CancellationToken cancellationToken)
    {
        var user = await RequirePartnerAsync(cancellationToken);
        if (user is null) return Unauthorized();
        var profileId = await db.PartnerProfiles.Where(x => x.UserId == user.Id).Select(x => x.Id).SingleAsync(cancellationToken);
        var items = await db.BookingAssignments.AsNoTracking().Where(x => x.PartnerProfileId == profileId)
            .OrderByDescending(x => x.InvitedAt).Take(100)
            .Select(x => new { x.Id, x.Status, x.InvitedAt, x.ExpiresAt, x.DistanceMetersAtInvitation, x.Booking.Code, Service = x.Booking.ServiceGroup.Name, x.Booking.AddressSnapshot, x.Booking.ScheduledStartAt, x.Booking.ScheduledEndAt, x.Booking.RequiredWorkers, x.Booking.EstimatedTotal, x.Booking.IsRecurring, x.Booking.RecurrenceRule, x.Booking.CustomerRequest, BookingStatus = x.Booking.Status })
            .ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost("me/invitations/{assignmentId:guid}/respond")]
    public async Task<IActionResult> Respond(Guid assignmentId, RespondInvitationRequest request, CancellationToken cancellationToken)
    {
        var user = await RequirePartnerAsync(cancellationToken);
        if (user is null) return Unauthorized();
        var assignment = await db.BookingAssignments.AsNoTracking().Include(x => x.Booking).Include(x => x.PartnerProfile).SingleOrDefaultAsync(x => x.Id == assignmentId && x.PartnerProfile.UserId == user.Id, cancellationToken);
        if (assignment is null) return NotFound(new { message = "Không tìm thấy lời mời." });
        if (assignment.Status != AssignmentStatus.Invited) return BadRequest(new { message = "Lời mời này đã được phản hồi." });
        if (assignment.ExpiresAt <= DateTimeOffset.UtcNow) { await db.BookingAssignments.Where(x => x.Id == assignmentId).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Status, AssignmentStatus.Expired).SetProperty(x => x.RespondedAt, DateTimeOffset.UtcNow), cancellationToken); return BadRequest(new { message = "Lời mời đã hết hạn." }); }
        if (!request.Accept)
        {
            await db.BookingAssignments.Where(x => x.Id == assignmentId && x.Status == AssignmentStatus.Invited).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Status, AssignmentStatus.Rejected).SetProperty(x => x.RespondedAt, DateTimeOffset.UtcNow), cancellationToken);
            await dispatch.DispatchAsync(assignment.BookingId, cancellationToken);
            return Ok(new { message = "Đã từ chối lời mời." });
        }
        var end = assignment.Booking.ScheduledEndAt ?? assignment.Booking.ScheduledStartAt.AddHours(4);
        if (!await availability.IsAvailableAsync(assignment.PartnerProfileId, assignment.Booking.ScheduledStartAt, end, assignment.BookingId, cancellationToken)) return Conflict(new { message = "Lịch này không còn trống hoặc không nằm trong giờ làm việc bạn đã đăng ký." });
        if (assignment.Booking.CleanerSelectionMode == CleanerSelectionMode.CustomerChooses)
        {
            await db.BookingAssignments.Where(x => x.Id == assignmentId && x.Status == AssignmentStatus.Invited).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Status, AssignmentStatus.Accepted).SetProperty(x => x.RespondedAt, DateTimeOffset.UtcNow), cancellationToken);
            return Ok(new { message = "Đã gửi xác nhận nhận việc. Khách hàng sẽ chọn người thực hiện.", Status = AssignmentStatus.Accepted });
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var claimed = await db.Bookings.Where(x => x.Id == assignment.BookingId && x.AssignedPartnerId == null &&
            (x.Status == BookingStatus.Searching || x.Status == BookingStatus.NoPartnerFound))
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.AssignedPartnerId, assignment.PartnerProfileId).SetProperty(x => x.Status, BookingStatus.Assigned), cancellationToken);
        if (claimed == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Conflict(new { message = "Đơn này vừa được một đối tác khác nhận." });
        }
        await db.BookingAssignments.Where(x => x.Id == assignmentId && x.Status == AssignmentStatus.Invited)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Status, AssignmentStatus.Selected).SetProperty(x => x.RespondedAt, DateTimeOffset.UtcNow), cancellationToken);
        await db.BookingAssignments.Where(x => x.BookingId == assignment.BookingId && x.Id != assignmentId && x.Status == AssignmentStatus.Invited)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Status, AssignmentStatus.Released), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(new { message = "Bạn đã nhận đơn và khung giờ đã được khóa.", Status = AssignmentStatus.Selected });
    }

    [HttpPost("me/bookings/{bookingId:guid}/cancel")]
    public async Task<IActionResult> CancelAssignedBooking(Guid bookingId, CancelAssignedBookingRequest request, CancellationToken cancellationToken)
    {
        var user = await RequirePartnerAsync(cancellationToken);
        if (user is null) return Unauthorized();
        var profileId = await db.PartnerProfiles.Where(x => x.UserId == user.Id).Select(x => x.Id).SingleAsync(cancellationToken);
        var booking = await db.Bookings.Include(x => x.Assignments).SingleOrDefaultAsync(x => x.Id == bookingId && x.AssignedPartnerId == profileId, cancellationToken);
        if (booking is null) return NotFound(new { message = "Không tìm thấy đơn đã nhận." });
        if (booking.Status is not (BookingStatus.Assigned or BookingStatus.PartnerTravelling) || booking.ScheduledStartAt <= DateTimeOffset.UtcNow)
            return BadRequest(new { message = "Đơn đã bắt đầu hoặc không còn có thể hủy nhận." });
        foreach (var selected in booking.Assignments.Where(x => x.PartnerProfileId == profileId && x.Status == AssignmentStatus.Selected)) selected.Status = AssignmentStatus.Released;
        booking.AssignedPartnerId = null;
        booking.Status = BookingStatus.Searching;
        booking.CancellationReason = string.IsNullOrWhiteSpace(request.Reason) ? "Đối tác hủy nhận việc." : $"Đối tác hủy: {request.Reason.Trim()}";
        await db.SaveChangesAsync(cancellationToken);
        var invited = await dispatch.DispatchAsync(booking.Id, cancellationToken);
        return Ok(new { message = invited > 0 ? $"Đã hủy nhận và gửi yêu cầu thay thế cho {invited} đối tác." : "Đã hủy nhận; hệ thống đang tiếp tục tìm người thay thế.", invitedPartners = invited });
    }

    [HttpPost("me/bookings/{bookingId:guid}/confirm")]
    public async Task<IActionResult> ConfirmAssignedBooking(Guid bookingId, CancellationToken cancellationToken)
    {
        var booking = await AssignedBooking(bookingId, cancellationToken);
        if (booking is null) return NotFound(new { message = "Không tìm thấy đơn đã nhận." });
        if (booking.Status != BookingStatus.Assigned || booking.ScheduledStartAt <= DateTimeOffset.UtcNow) return BadRequest(new { message = "Không thể xác nhận đơn ở trạng thái hiện tại." });
        booking.TaskerConfirmedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Đã xác nhận sẽ thực hiện lịch hẹn." });
    }

    [HttpPost("me/bookings/{bookingId:guid}/arrive")]
    public async Task<IActionResult> Arrive(Guid bookingId, CancellationToken cancellationToken)
    {
        var booking = await AssignedBooking(bookingId, cancellationToken);
        if (booking is null) return NotFound(new { message = "Không tìm thấy đơn đã nhận." });
        if (booking.Status != BookingStatus.Assigned || booking.TaskerConfirmedAt is null) return BadRequest(new { message = "Tasker cần xác nhận lịch trước khi check-in." });
        booking.Status = BookingStatus.PartnerTravelling;
        booking.ArrivedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Đã check-in: Tasker đã đến địa chỉ." });
    }

    [HttpPost("me/bookings/{bookingId:guid}/start")]
    public async Task<IActionResult> Start(Guid bookingId, CancellationToken cancellationToken)
    {
        var booking = await AssignedBooking(bookingId, cancellationToken);
        if (booking is null) return NotFound(new { message = "Không tìm thấy đơn đã nhận." });
        if (booking.Status != BookingStatus.PartnerTravelling) return BadRequest(new { message = "Hãy check-in trước khi bắt đầu." });
        booking.Status = BookingStatus.InProgress;
        booking.StartedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Đã bắt đầu thực hiện công việc." });
    }

    [HttpPost("me/bookings/{bookingId:guid}/complete")]
    public async Task<IActionResult> ReportCompleted(Guid bookingId, CancellationToken cancellationToken)
    {
        var booking = await AssignedBooking(bookingId, cancellationToken);
        if (booking is null) return NotFound(new { message = "Không tìm thấy đơn đã nhận." });
        if (booking.Status != BookingStatus.InProgress) return BadRequest(new { message = "Đơn chưa ở trạng thái đang thực hiện." });
        booking.Status = BookingStatus.AwaitingCustomerConfirmation;
        booking.CompletionReportedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Đã báo hoàn thành. Đang chờ khách xác nhận trong 24 giờ." });
    }

    [HttpGet("me/notifications")]
    public async Task<IActionResult> GetNotifications(CancellationToken cancellationToken)
    {
        var user = await RequirePartnerAsync(cancellationToken);
        if (user is null) return Unauthorized();
        var items = await db.PartnerNotifications.AsNoTracking().Where(x => x.PartnerUserId == user.Id).OrderByDescending(x => x.CreatedAt).Take(100).Select(x => new { x.Id, x.Type, x.Title, x.Message, x.IsRead, x.CreatedAt, x.BookingId }).ToListAsync(cancellationToken);
        return Ok(items);
    }

    private async Task<AppUser?> RequirePartnerAsync(CancellationToken cancellationToken)
    {
        var user = await sessions.GetCurrentUserAsync(Request, cancellationToken);
        return user is { Role: UserRole.Partner, Status: UserStatus.Active } ? user : null;
    }

    private async Task<Booking?> AssignedBooking(Guid bookingId, CancellationToken cancellationToken)
    {
        var user = await RequirePartnerAsync(cancellationToken);
        if (user is null) return null;
        var profileId = await db.PartnerProfiles.Where(x => x.UserId == user.Id).Select(x => x.Id).SingleAsync(cancellationToken);
        return await db.Bookings.SingleOrDefaultAsync(x => x.Id == bookingId && x.AssignedPartnerId == profileId, cancellationToken);
    }

    private async Task<int> DispatchOpenAndCountAsync(Guid profileId, CancellationToken cancellationToken)
    {
        var before = await db.BookingAssignments.CountAsync(x => x.PartnerProfileId == profileId, cancellationToken);
        await dispatch.DispatchOpenBookingsAsync(cancellationToken);
        var after = await db.BookingAssignments.CountAsync(x => x.PartnerProfileId == profileId, cancellationToken);
        return Math.Max(0, after - before);
    }
}
