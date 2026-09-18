using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EveryCare.Api.Domain.Entities;
using EveryCare.Api.Domain.Enums;
using EveryCare.Api.Infrastructure.Persistence;
using EveryCare.Api.Services;

namespace EveryCare.Api.Controllers;

public sealed record CustomerRegisterRequest(string FullName, string Phone, string? Email, string Password);
public sealed record CustomerLoginRequest(string Phone, string Password);
public sealed record CustomerSyncRequest(string Id, string FullName, string Phone, string? Email, string CreatedAt);
public sealed record CustomerStatusRequest(UserStatus Status);
public sealed record CustomerUpdateRequest(string FullName, string? Email);

[ApiController]
[Route("api/customers")]
public sealed class CustomersController(AppDbContext db) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(CustomerRegisterRequest request, CancellationToken cancellationToken)
    {
        var phone = NormalizePhone(request.Phone);
        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim().ToLowerInvariant();
        var error = Validate(request.FullName, phone, email, request.Password);
        if (error is not null) return BadRequest(new { message = error });
        var user = await db.Users.Include(x => x.CustomerProfile).SingleOrDefaultAsync(x => x.Phone == phone, cancellationToken);
        if (user is not null && (user.Role != UserRole.Customer || (user.PasswordHash != "LOCAL_SESSION_MIGRATED" && user.PasswordHash != "PENDING_AUTH_MIGRATION")))
            return Conflict(new { message = "Số điện thoại này đã được đăng ký." });
        if (email is not null && await db.Users.AnyAsync(x => x.Email == email && (user == null || x.Id != user.Id), cancellationToken))
            return Conflict(new { message = "Email này đã được sử dụng." });
        if (user is null)
        {
            user = new AppUser { FullName = CleanName(request.FullName), Phone = phone, Email = email, PasswordHash = PasswordService.Hash(request.Password), Role = UserRole.Customer, Status = UserStatus.Active, CustomerProfile = new CustomerProfile() };
            db.Users.Add(user);
        }
        else
        {
            user.FullName = CleanName(request.FullName); user.Email = email; user.PasswordHash = PasswordService.Hash(request.Password); user.Status = UserStatus.Active;
            user.CustomerProfile ??= new CustomerProfile { User = user };
        }
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToCustomer(user));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(CustomerLoginRequest request, CancellationToken cancellationToken)
    {
        var phone = NormalizePhone(request.Phone);
        var user = await db.Users.SingleOrDefaultAsync(x => x.Phone == phone && x.Role == UserRole.Customer, cancellationToken);
        if (user is null || !PasswordService.Verify(request.Password, user.PasswordHash)) return Unauthorized(new { message = "Số điện thoại hoặc mật khẩu chưa chính xác." });
        if (user.Status == UserStatus.Locked) return StatusCode(403, new { message = "Tài khoản đang bị khóa." });
        user.LastLoginAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToCustomer(user));
    }

    [HttpPost("sync-local")]
    public async Task<IActionResult> SyncLocal(CustomerSyncRequest request, CancellationToken cancellationToken)
    {
        var phone = NormalizePhone(request.Phone);
        if (!Regex.IsMatch(phone, @"^0[35789]\d{8}$") || string.IsNullOrWhiteSpace(request.FullName)) return BadRequest();
        var user = await db.Users.SingleOrDefaultAsync(x => x.Phone == phone, cancellationToken);
        if (user is null)
        {
            user = new AppUser { FullName = CleanName(request.FullName), Phone = phone, Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim().ToLowerInvariant(), PasswordHash = "LOCAL_SESSION_MIGRATED", Role = UserRole.Customer, Status = UserStatus.Active, CustomerProfile = new CustomerProfile() };
            if (DateTimeOffset.TryParse(request.CreatedAt, out var createdAt)) user.CreatedAt = createdAt;
            db.Users.Add(user);
            await db.SaveChangesAsync(cancellationToken);
        }
        return Ok(ToCustomer(user));
    }

    private static object ToCustomer(AppUser user) => new { id = user.Id, fullName = user.FullName, phone = user.Phone, email = user.Email, createdAt = user.CreatedAt };
    private static string NormalizePhone(string value) => Regex.Replace(value ?? "", @"\D", "");
    private static string CleanName(string value) => Regex.Replace(value.Trim(), @"\s+", " ");
    private static string? Validate(string name, string phone, string? email, string password)
    {
        if (CleanName(name).Split(' ').Length < 2) return "Vui lòng nhập đầy đủ họ và tên.";
        if (!Regex.IsMatch(phone, @"^0[35789]\d{8}$")) return "Số điện thoại Việt Nam chưa đúng định dạng.";
        if (email is not null && !Regex.IsMatch(email, @"^[^\s@]+@[^\s@]+\.[^\s@]+$")) return "Email chưa đúng định dạng.";
        if (password.Length < 8 || !password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit)) return "Mật khẩu phải có ít nhất 8 ký tự, chữ hoa, chữ thường và chữ số.";
        return null;
    }
}

[ApiController]
[Route("api/admin/customers")]
public sealed class AdminCustomersController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var customers = await db.Users.AsNoTracking().Where(x => x.Role == UserRole.Customer)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new { x.Id, x.FullName, x.Phone, x.Email, x.Status, x.CreatedAt, x.LastLoginAt, Orders = db.Bookings.Count(b => b.CustomerId == x.Id) })
            .ToListAsync(cancellationToken);
        return Ok(customers);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> ChangeStatus(Guid id, CustomerStatusRequest request, CancellationToken cancellationToken)
    {
        if (request.Status is not (UserStatus.Active or UserStatus.Locked)) return BadRequest(new { message = "Trạng thái không hợp lệ." });
        var customer = await db.Users.SingleOrDefaultAsync(x => x.Id == id && x.Role == UserRole.Customer, cancellationToken);
        if (customer is null) return NotFound(new { message = "Không tìm thấy khách hàng." });
        customer.Status = request.Status;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { customer.Status });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, CustomerUpdateRequest request, CancellationToken cancellationToken)
    {
        var customer = await db.Users.SingleOrDefaultAsync(x => x.Id == id && x.Role == UserRole.Customer, cancellationToken);
        if (customer is null) return NotFound(new { message = "Không tìm thấy khách hàng." });
        var name = Regex.Replace(request.FullName.Trim(), @"\s+", " ");
        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim().ToLowerInvariant();
        if (name.Split(' ').Length < 2) return BadRequest(new { message = "Vui lòng nhập đầy đủ họ tên." });
        if (email is not null && (!Regex.IsMatch(email, @"^[^\s@]+@[^\s@]+\.[^\s@]+$") || await db.Users.AnyAsync(x => x.Id != id && x.Email == email, cancellationToken)))
            return BadRequest(new { message = "Email không hợp lệ hoặc đã được sử dụng." });
        customer.FullName = name; customer.Email = email;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Đã cập nhật khách hàng." });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var customer = await db.Users.SingleOrDefaultAsync(x => x.Id == id && x.Role == UserRole.Customer, cancellationToken);
        if (customer is null) return NotFound();
        if (await db.Bookings.AnyAsync(x => x.CustomerId == id, cancellationToken)) return Conflict(new { message = "Khách hàng đã có đơn hàng nên chỉ có thể khóa, không thể xóa." });
        customer.IsDeleted = true;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
