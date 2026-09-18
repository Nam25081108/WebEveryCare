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

[ApiController]
[Route("api/partner")]
public sealed class PartnerPortalController(AppDbContext db, PartnerSessionService sessions, PartnerAvailabilityService availability) : ControllerBase
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
        return Ok(new { user.Id, user.FullName, user.Phone, user.Email, PartnerProfileId = profile.Id, profile.PartnerType, profile.TeamName, profile.TeamSize, profile.IsAvailable, profile.ServiceAddress, profile.AverageRating, profile.CompletedBookings, Services = profile.ServiceCapabilities.Select(x => x.ServiceGroup.Name) });
    }

    [HttpPatch("me/job-invitations")]
    public async Task<IActionResult> ToggleInvitations(ToggleJobInvitationsRequest request, CancellationToken cancellationToken)
    {
        var user = await RequirePartnerAsync(cancellationToken);
        if (user is null) return Unauthorized();
        var profile = await db.PartnerProfiles.Include(x => x.ServiceCapabilities).SingleAsync(x => x.UserId == user.Id, cancellationToken);
        profile.IsAvailable = request.Enabled;
        await db.SaveChangesAsync(cancellationToken);
        var invitationsCreated = request.Enabled ? await InviteToExistingBookingsAsync(profile, cancellationToken) : 0;
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
        var bookings = await db.Bookings.AsNoTracking().Where(x => x.AssignedPartnerId == profileId && x.ScheduledStartAt >= DateTimeOffset.UtcNow && x.Status != BookingStatus.Cancelled).OrderBy(x => x.ScheduledStartAt).Select(x => new { x.Id, x.Code, x.AddressSnapshot, x.ScheduledStartAt, x.ScheduledEndAt, x.Status, x.IsRecurring, x.RecurrenceRule }).ToListAsync(cancellationToken);
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
            ? await InviteToExistingBookingsAsync(profile, cancellationToken)
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
            .Select(x => new { x.Id, x.Status, x.InvitedAt, x.ExpiresAt, x.DistanceMetersAtInvitation, x.Booking.Code, Service = x.Booking.ServiceGroup.Name, x.Booking.AddressSnapshot, x.Booking.ScheduledStartAt, x.Booking.ScheduledEndAt, x.Booking.RequiredWorkers, x.Booking.EstimatedTotal, x.Booking.IsRecurring, x.Booking.RecurrenceRule, BookingStatus = x.Booking.Status })
            .ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost("me/invitations/{assignmentId:guid}/respond")]
    public async Task<IActionResult> Respond(Guid assignmentId, RespondInvitationRequest request, CancellationToken cancellationToken)
    {
        var user = await RequirePartnerAsync(cancellationToken);
        if (user is null) return Unauthorized();
        var assignment = await db.BookingAssignments.Include(x => x.Booking).ThenInclude(x => x.Assignments).Include(x => x.PartnerProfile).SingleOrDefaultAsync(x => x.Id == assignmentId && x.PartnerProfile.UserId == user.Id, cancellationToken);
        if (assignment is null) return NotFound(new { message = "Không tìm thấy lời mời." });
        if (assignment.Status != AssignmentStatus.Invited) return BadRequest(new { message = "Lời mời này đã được phản hồi." });
        if (assignment.ExpiresAt <= DateTimeOffset.UtcNow) { assignment.Status = AssignmentStatus.Expired; await db.SaveChangesAsync(cancellationToken); return BadRequest(new { message = "Lời mời đã hết hạn." }); }
        assignment.RespondedAt = DateTimeOffset.UtcNow;
        if (!request.Accept)
        {
            assignment.Status = AssignmentStatus.Rejected;
            await db.SaveChangesAsync(cancellationToken);
            return Ok(new { message = "Đã từ chối lời mời." });
        }
        var end = assignment.Booking.ScheduledEndAt ?? assignment.Booking.ScheduledStartAt.AddHours(4);
        if (!await availability.IsAvailableAsync(assignment.PartnerProfileId, assignment.Booking.ScheduledStartAt, end, assignment.BookingId, cancellationToken)) return Conflict(new { message = "Lịch này không còn trống hoặc không nằm trong giờ làm việc bạn đã đăng ký." });
        assignment.Status = AssignmentStatus.Accepted;
        if (assignment.Booking.CleanerSelectionMode != CleanerSelectionMode.CustomerChooses && assignment.Booking.AssignedPartnerId is null)
        {
            assignment.Status = AssignmentStatus.Selected;
            assignment.Booking.AssignedPartnerId = assignment.PartnerProfileId;
            assignment.Booking.Status = BookingStatus.Assigned;
            foreach (var other in assignment.Booking.Assignments.Where(x => x.Id != assignment.Id && x.Status == AssignmentStatus.Invited)) other.Status = AssignmentStatus.Released;
        }
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = assignment.Status == AssignmentStatus.Selected ? "Bạn đã nhận đơn và khung giờ đã được giữ." : "Đã gửi xác nhận nhận việc. Khách hàng sẽ chọn người thực hiện.", assignment.Status });
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

    private async Task<int> InviteToExistingBookingsAsync(PartnerProfile profile, CancellationToken cancellationToken)
    {
        if (profile.ServiceLocation is null) return 0;
        var serviceGroupIds = profile.ServiceCapabilities.Select(x => x.ServiceGroupId).ToArray();
        var query = db.Bookings
            .Include(x => x.ServiceGroup).Include(x => x.Assignments)
            .Where(x => x.ScheduledStartAt > DateTimeOffset.UtcNow &&
                (x.Status == BookingStatus.Searching || x.Status == BookingStatus.NoPartnerFound || x.Status == BookingStatus.AwaitingCustomerSelection) &&
                x.CleanerSelectionMode != CleanerSelectionMode.FavoriteFirst &&
                serviceGroupIds.Contains(x.ServiceGroupId) &&
                x.LocationSnapshot != null && x.LocationSnapshot.Distance(profile.ServiceLocation) <= (double)profile.ServiceRadiusKilometers * 1000 &&
                !x.Assignments.Any(a => a.PartnerProfileId == profile.Id));
        if (profile.PartnerType == PartnerType.Individual) query = query.Where(x => x.RequiredWorkers == 1);
        else query = query.Where(x => x.RequiredWorkers <= profile.TeamSize);
        var bookings = await query.OrderBy(x => x.ScheduledStartAt).Take(50).ToListAsync(cancellationToken);
        var created = 0;
        foreach (var booking in bookings)
        {
            var end = booking.ScheduledEndAt ?? booking.ScheduledStartAt.AddHours(4);
            if (!await availability.IsAvailableAsync(profile.Id, booking.ScheduledStartAt, end, booking.Id, cancellationToken)) continue;
            var now = DateTimeOffset.UtcNow;
            var expiresAt = new[] { now.AddHours(24), booking.ScheduledStartAt.AddHours(-1) }.Min();
            if (expiresAt < now.AddMinutes(15)) expiresAt = now.AddMinutes(15);
            db.BookingAssignments.Add(new BookingAssignment
            {
                Booking = booking, PartnerProfileId = profile.Id, Status = AssignmentStatus.Invited,
                InvitedAt = now, ExpiresAt = expiresAt,
                DistanceMetersAtInvitation = HaversineMeters(profile.ServiceLocation.Y, profile.ServiceLocation.X, booking.LocationSnapshot!.Y, booking.LocationSnapshot.X)
            });
            db.PartnerNotifications.Add(new PartnerNotification
            {
                PartnerUserId = profile.UserId, Booking = booking, Type = "new_booking", Title = "Có đơn dọn dẹp phù hợp",
                Message = $"{booking.ServiceGroup.Name} tại {booking.AddressSnapshot}, bắt đầu {booking.ScheduledStartAt:dd/MM/yyyy HH:mm}."
            });
            if (booking.Status == BookingStatus.NoPartnerFound)
                booking.Status = booking.CleanerSelectionMode == CleanerSelectionMode.CustomerChooses ? BookingStatus.AwaitingCustomerSelection : BookingStatus.Searching;
            created++;
        }
        if (created > 0) await db.SaveChangesAsync(cancellationToken);
        return created;
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
