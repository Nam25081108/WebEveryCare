using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using EveryCare.Api.Domain.Entities;
using EveryCare.Api.Domain.Enums;
using EveryCare.Api.Infrastructure.Persistence;
using EveryCare.Api.Services;

namespace EveryCare.Api.Controllers;

public sealed class PartnerApplicationRequest
{
    public PartnerType PartnerType { get; set; }
    public string FullName { get; set; } = "";
    public string? TeamName { get; set; }
    public int TeamSize { get; set; } = 1;
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string IdentityNumber { get; set; } = "";
    public string ServiceAddress { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public List<string> ServiceGroupSlugs { get; set; } = [];
    public string TeamMembersJson { get; set; } = "[]";
    public IFormFile? IdentityFront { get; set; }
    public IFormFile? IdentityBack { get; set; }
}

public sealed record TeamMemberInput(string FullName, string Phone, string? IdentityNumber);
public sealed record ReviewPartnerRequest(string? Reason);
public sealed record ChangePartnerStatusRequest(UserStatus Status);

[ApiController]
[Route("api/partner-applications")]
public sealed class PartnerApplicationsController(AppDbContext db, IWebHostEnvironment environment) : ControllerBase
{
    private static readonly string[] AllowedImageTypes = ["image/jpeg", "image/png", "image/webp"];
    private static readonly string[] IndividualServices = ["ve-sinh-phong-le"];
    private static readonly string[] TeamServices = ["tong-ve-sinh", "ve-sinh-chuyen-nghiep"];

    [HttpPost]
    [RequestSizeLimit(12_000_000)]
    public async Task<IActionResult> Create([FromForm] PartnerApplicationRequest request, CancellationToken cancellationToken)
    {
        request.Phone = NormalizePhone(request.Phone);
        request.Email = request.Email.Trim().ToLowerInvariant();
        request.FullName = request.FullName.Trim();
        request.IdentityNumber = request.IdentityNumber.Trim();
        request.ServiceAddress = request.ServiceAddress.Trim();
        var validationError = Validate(request);
        if (validationError is not null) return BadRequest(new { message = validationError });

        if (await db.Users.AnyAsync(x => x.Phone == request.Phone || x.Email == request.Email, cancellationToken))
            return Conflict(new { message = "Số điện thoại hoặc Gmail này đã được đăng ký." });

        var slugs = request.ServiceGroupSlugs.Distinct().ToArray();
        var allowed = request.PartnerType == PartnerType.Individual ? IndividualServices : TeamServices;
        if (slugs.Length == 0 || slugs.Except(allowed).Any() || (request.PartnerType == PartnerType.Individual && slugs.Length != 1))
            return BadRequest(new { message = request.PartnerType == PartnerType.Individual
                ? "Đối tác cá nhân chỉ được đăng ký dịch vụ dọn dẹp nhà cửa."
                : "Đội nhóm chỉ được đăng ký tổng vệ sinh hoặc vệ sinh chuyên nghiệp." });

        var groups = await db.ServiceGroups.Where(x => slugs.Contains(x.Slug) && x.IsActive).ToListAsync(cancellationToken);
        if (groups.Count != slugs.Length) return BadRequest(new { message = "Có dịch vụ không tồn tại hoặc đang bị khóa." });

        List<TeamMemberInput> members = [];
        if (request.PartnerType == PartnerType.Team)
        {
            try { members = JsonSerializer.Deserialize<List<TeamMemberInput>>(request.TeamMembersJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? []; }
            catch (JsonException) { return BadRequest(new { message = "Danh sách thành viên đội nhóm không hợp lệ." }); }
            if (request.TeamSize < 2 || members.Count != request.TeamSize - 1 || members.Any(x => string.IsNullOrWhiteSpace(x.FullName) || !IsValidPhone(NormalizePhone(x.Phone))))
                return BadRequest(new { message = "Vui lòng nhập đủ thông tin các thành viên, không tính trưởng nhóm." });
        }

        var folder = Path.Combine(environment.ContentRootPath, "App_Data", "partner-identities");
        Directory.CreateDirectory(folder);
        var frontPath = await SaveIdentityAsync(request.IdentityFront!, folder, cancellationToken);
        var backPath = await SaveIdentityAsync(request.IdentityBack!, folder, cancellationToken);
        var point = new Point(request.Longitude, request.Latitude) { SRID = 4326 };

        var user = new AppUser
        {
            FullName = request.FullName,
            Phone = request.Phone,
            Email = request.Email,
            PasswordHash = PasswordService.Hash(request.Password),
            Role = UserRole.Partner,
            Status = UserStatus.Pending
        };
        var profile = new PartnerProfile
        {
            User = user,
            PartnerType = request.PartnerType,
            VerificationStatus = VerificationStatus.Pending,
            IdentityNumber = request.IdentityNumber,
            IdentityFrontUrl = frontPath,
            IdentityBackUrl = backPath,
            TeamName = request.PartnerType == PartnerType.Team ? request.TeamName?.Trim() : null,
            TeamSize = request.PartnerType == PartnerType.Team ? request.TeamSize : 1,
            IsAvailable = false,
            ServiceAddress = request.ServiceAddress,
            ServiceLocation = point,
            CurrentLocation = point,
            LocationUpdatedAt = DateTimeOffset.UtcNow
        };
        foreach (var group in groups) profile.ServiceCapabilities.Add(new PartnerServiceCapability { ServiceGroup = group });
        foreach (var member in members) profile.TeamMembers.Add(new PartnerTeamMember
        {
            FullName = member.FullName.Trim(), Phone = NormalizePhone(member.Phone), IdentityNumber = member.IdentityNumber?.Trim(), VerificationStatus = VerificationStatus.Pending
        });
        db.PartnerProfiles.Add(profile);
        await db.SaveChangesAsync(cancellationToken);
        return Created("", new { profile.Id, status = profile.VerificationStatus, message = "Hồ sơ đã được gửi tới quản trị viên để xét duyệt." });
    }

    private static string? Validate(PartnerApplicationRequest request)
    {
        if (request.FullName.Length < 3) return "Vui lòng nhập họ tên đầy đủ.";
        if (!IsValidPhone(request.Phone)) return "Số điện thoại Việt Nam không hợp lệ.";
        if (!Regex.IsMatch(request.Email, @"^[^\s@]+@gmail\.com$", RegexOptions.IgnoreCase)) return "Vui lòng sử dụng địa chỉ Gmail hợp lệ.";
        if (request.Password.Length < 8) return "Mật khẩu phải có ít nhất 8 ký tự.";
        if (!Regex.IsMatch(request.IdentityNumber, @"^\d{9}(\d{3})?$")) return "Số CCCD/CMND phải gồm 9 hoặc 12 chữ số.";
        if (request.ServiceAddress.Length < 10) return "Vui lòng nhập địa chỉ đầy đủ.";
        if (request.Latitude is < 10.3 or > 11.25 || request.Longitude is < 106.3 or > 107.1) return "Vị trí phải nằm trong phạm vi TP.HCM cũ.";
        if (request.PartnerType == PartnerType.Team && string.IsNullOrWhiteSpace(request.TeamName)) return "Vui lòng nhập tên đội nhóm.";
        if (!IsValidImage(request.IdentityFront) || !IsValidImage(request.IdentityBack)) return "Cần ảnh CCCD mặt trước và mặt sau định dạng JPG, PNG hoặc WEBP, tối đa 5MB mỗi ảnh.";
        return null;
    }

    private static bool IsValidImage(IFormFile? file) => file is { Length: > 0 and <= 5_000_000 } && AllowedImageTypes.Contains(file.ContentType.ToLowerInvariant());
    private static bool IsValidPhone(string phone) => Regex.IsMatch(phone, @"^0\d{9}$");
    private static string NormalizePhone(string phone) => Regex.Replace(phone ?? "", @"\D", "");
    private static async Task<string> SaveIdentityAsync(IFormFile file, string folder, CancellationToken cancellationToken)
    {
        var extension = file.ContentType.ToLowerInvariant() switch { "image/png" => ".png", "image/webp" => ".webp", _ => ".jpg" };
        var name = $"{Guid.NewGuid():N}{extension}";
        await using var stream = System.IO.File.Create(Path.Combine(folder, name));
        await file.CopyToAsync(stream, cancellationToken);
        return name;
    }
}

[ApiController]
[Route("api/admin/partner-applications")]
public sealed class AdminPartnerApplicationsController(AppDbContext db, IWebHostEnvironment environment, EmailService emailService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var profiles = await db.PartnerProfiles.AsNoTracking()
            .Include(x => x.User).Include(x => x.TeamMembers).Include(x => x.AvailabilityRules).Include(x => x.ServiceCapabilities).ThenInclude(x => x.ServiceGroup)
            .OrderBy(x => x.VerificationStatus).ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        var items = profiles.Select(x => new
            {
                x.Id, x.User.FullName, x.User.Phone, x.User.Email, x.User.Status, x.PartnerType, x.TeamName, x.TeamSize, x.IsAvailable,
                x.IdentityNumber, x.ServiceAddress, Latitude = x.ServiceLocation == null ? (double?)null : x.ServiceLocation.Y,
                Longitude = x.ServiceLocation == null ? (double?)null : x.ServiceLocation.X, x.VerificationStatus, x.RejectionReason,
                x.CreatedAt, x.ReviewedAt, x.ApprovalEmailSentAt, x.ApprovalEmailError,
                AvailabilityDays = x.AvailabilityRules.Count(c => c.IsActive), Services = x.ServiceCapabilities.Select(c => new { c.ServiceGroup.Slug, c.ServiceGroup.Name }),
                TeamMembers = x.TeamMembers.Select(m => new { m.Id, m.FullName, m.Phone, m.IdentityNumber })
            }).ToList();
        return Ok(items);
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        var profile = await db.PartnerProfiles.Include(x => x.User).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (profile is null) return NotFound(new { message = "Không tìm thấy hồ sơ." });
        profile.VerificationStatus = VerificationStatus.Approved;
        profile.User.Status = UserStatus.Active;
        profile.ReviewedAt = DateTimeOffset.UtcNow;
        profile.RejectionReason = null;
        var email = profile.User.Email is null ? new EmailResult(false, "Hồ sơ không có email.") : await emailService.SendPartnerApprovedAsync(profile.User.Email, profile.User.FullName, cancellationToken);
        profile.ApprovalEmailSentAt = email.Sent ? DateTimeOffset.UtcNow : null;
        profile.ApprovalEmailError = email.Error;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = email.Sent ? "Đã duyệt hồ sơ và gửi Gmail thông báo." : "Đã duyệt hồ sơ. Gmail chưa gửi được vì SMTP chưa được cấu hình hoặc gặp lỗi.", emailSent = email.Sent, emailError = email.Error });
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, ReviewPartnerRequest request, CancellationToken cancellationToken)
    {
        var profile = await db.PartnerProfiles.Include(x => x.User).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (profile is null) return NotFound(new { message = "Không tìm thấy hồ sơ." });
        profile.VerificationStatus = VerificationStatus.Rejected;
        profile.User.Status = UserStatus.Pending;
        profile.ReviewedAt = DateTimeOffset.UtcNow;
        profile.RejectionReason = string.IsNullOrWhiteSpace(request.Reason) ? "Hồ sơ chưa đáp ứng yêu cầu." : request.Reason.Trim();
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Đã từ chối hồ sơ." });
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> ChangeStatus(Guid id, ChangePartnerStatusRequest request, CancellationToken cancellationToken)
    {
        if (request.Status is not (UserStatus.Active or UserStatus.Locked)) return BadRequest(new { message = "Trạng thái không hợp lệ." });
        var profile = await db.PartnerProfiles.Include(x => x.User).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (profile is null) return NotFound(new { message = "Không tìm thấy đối tác." });
        if (profile.VerificationStatus != VerificationStatus.Approved) return BadRequest(new { message = "Chỉ khóa hoặc mở khóa hồ sơ đã duyệt." });
        profile.User.Status = request.Status;
        if (request.Status == UserStatus.Locked) profile.IsAvailable = false;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { profile.User.Status });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var profile = await db.PartnerProfiles.Include(x => x.User).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (profile is null) return NotFound();
        profile.IsDeleted = true;
        profile.User.IsDeleted = true;
        profile.IsAvailable = false;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/documents/{side}")]
    public async Task<IActionResult> GetDocument(Guid id, string side, CancellationToken cancellationToken)
    {
        var profile = await db.PartnerProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (profile is null) return NotFound();
        var fileName = side.Equals("front", StringComparison.OrdinalIgnoreCase) ? profile.IdentityFrontUrl : side.Equals("back", StringComparison.OrdinalIgnoreCase) ? profile.IdentityBackUrl : null;
        if (string.IsNullOrWhiteSpace(fileName)) return NotFound();
        var path = Path.Combine(environment.ContentRootPath, "App_Data", "partner-identities", Path.GetFileName(fileName));
        if (!System.IO.File.Exists(path)) return NotFound();
        return PhysicalFile(path, Path.GetExtension(path).ToLowerInvariant() switch { ".png" => "image/png", ".webp" => "image/webp", _ => "image/jpeg" });
    }
}
