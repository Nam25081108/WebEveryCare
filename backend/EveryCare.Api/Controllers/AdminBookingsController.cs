using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EveryCare.Api.Infrastructure.Persistence;

namespace EveryCare.Api.Controllers;

[ApiController]
[Route("api/admin/bookings")]
public sealed class AdminBookingsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        var bookings = await db.Bookings.AsNoTracking().OrderByDescending(x => x.CreatedAt).Take(Math.Clamp(take, 1, 200)).Select(x => new
        {
            x.Id, x.Code, Customer = x.Customer.FullName, CustomerPhone = x.Customer.Phone,
            Service = x.ServicePackage != null ? x.ServiceGroup.Name + " • " + x.ServicePackage.Name : x.ServiceGroup.Name,
            x.AddressSnapshot, x.ScheduledStartAt, x.Status, Cleaner = x.AssignedPartner != null ? x.AssignedPartner.User.FullName : null,
            x.EstimatedTotal, InvitedPartners = x.Assignments.Count, x.CreatedAt, x.IsRecurring, x.RecurrenceRule,
            x.FacilityName, x.ContactName, x.ContactPhone, x.AccommodationType,
            x.CustomerRequest,
            Extras = x.ExtraCharges.Where(extra => extra.Status == EveryCare.Api.Domain.Enums.ExtraChargeStatus.Approved).Select(extra => new { extra.Description, extra.Amount })
        }).ToListAsync(cancellationToken);
        return Ok(bookings);
    }
}
