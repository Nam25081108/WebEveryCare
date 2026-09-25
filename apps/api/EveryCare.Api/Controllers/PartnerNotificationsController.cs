using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EveryCare.Api.Infrastructure.Persistence;

namespace EveryCare.Api.Controllers;

[ApiController]
[Route("api/partner/notifications")]
public sealed class PartnerNotificationsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string phone, CancellationToken cancellationToken)
    {
        var notifications = await db.PartnerNotifications.AsNoTracking().Where(x => x.PartnerUser.Phone == phone).OrderByDescending(x => x.CreatedAt).Take(50).Select(x => new { x.Id, x.BookingId, x.Type, x.Title, x.Message, x.IsRead, x.CreatedAt }).ToListAsync(cancellationToken);
        return Ok(notifications);
    }
}
