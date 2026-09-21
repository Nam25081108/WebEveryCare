using EveryCare.Api.Domain.Entities;
using EveryCare.Api.Domain.Enums;
using EveryCare.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EveryCare.Api.Controllers;

public sealed record CustomerFavoriteRequest(string Phone);

[ApiController]
[Route("api/customer-favorites")]
public sealed class CustomerFavoritesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string phone, CancellationToken cancellationToken)
    {
        var customer = await FindCustomer(phone, cancellationToken);
        if (customer is null) return Ok(Array.Empty<object>());

        var favorites = await db.FavoritePartners.AsNoTracking()
            .Where(x => x.CustomerId == customer.Id)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                PartnerId = x.PartnerProfileId,
                Name = x.PartnerProfile.TeamName ?? x.PartnerProfile.User.FullName,
                x.PartnerProfile.AvatarUrl,
                x.PartnerProfile.AverageRating,
                x.PartnerProfile.CompletedBookings,
                x.PartnerProfile.PartnerType,
                IsAvailable = x.PartnerProfile.IsAvailable && x.PartnerProfile.VerificationStatus == VerificationStatus.Approved,
                Services = x.PartnerProfile.ServiceCapabilities.Select(capability => capability.ServiceGroup.Name),
                FavoritedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);
        return Ok(favorites);
    }

    [HttpPost("{partnerId:guid}")]
    public async Task<IActionResult> Add(Guid partnerId, CustomerFavoriteRequest request, CancellationToken cancellationToken)
    {
        var customer = await FindCustomer(request.Phone, cancellationToken);
        if (customer is null) return NotFound(new { message = "Không tìm thấy tài khoản khách hàng." });

        var completedTogether = await db.Bookings.AnyAsync(x => x.CustomerId == customer.Id &&
            x.AssignedPartnerId == partnerId && x.Status == BookingStatus.Completed, cancellationToken);
        if (!completedTogether)
            return BadRequest(new { message = "Bạn chỉ có thể yêu thích Tasker sau khi đã hoàn thành công việc cùng người đó." });

        var exists = await db.FavoritePartners.AnyAsync(x => x.CustomerId == customer.Id && x.PartnerProfileId == partnerId, cancellationToken);
        if (!exists)
        {
            db.FavoritePartners.Add(new FavoritePartner { CustomerId = customer.Id, PartnerProfileId = partnerId });
            await db.SaveChangesAsync(cancellationToken);
        }
        return Ok(new { isFavorite = true, message = exists ? "Tasker đã có trong danh sách yêu thích." : "Đã thêm Tasker vào danh sách yêu thích." });
    }

    [HttpDelete("{partnerId:guid}")]
    public async Task<IActionResult> Remove(Guid partnerId, [FromQuery] string phone, CancellationToken cancellationToken)
    {
        var customer = await FindCustomer(phone, cancellationToken);
        if (customer is null) return NotFound(new { message = "Không tìm thấy tài khoản khách hàng." });
        var favorite = await db.FavoritePartners.SingleOrDefaultAsync(x => x.CustomerId == customer.Id && x.PartnerProfileId == partnerId, cancellationToken);
        if (favorite is not null)
        {
            db.FavoritePartners.Remove(favorite);
            await db.SaveChangesAsync(cancellationToken);
        }
        return Ok(new { isFavorite = false, message = "Đã xóa Tasker khỏi danh sách yêu thích." });
    }

    private Task<AppUser?> FindCustomer(string phone, CancellationToken cancellationToken)
    {
        var normalized = new string((phone ?? "").Where(char.IsDigit).ToArray());
        return db.Users.SingleOrDefaultAsync(x => x.Phone == normalized && x.Role == UserRole.Customer, cancellationToken);
    }
}
