using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EveryCare.Api.Infrastructure.Persistence;

namespace EveryCare.Api.Controllers;

[ApiController]
[Route("api/system")]
public sealed class SystemController(AppDbContext db) : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "ok", timestamp = DateTimeOffset.UtcNow });

    [HttpGet("database")]
    public async Task<IActionResult> Database(CancellationToken cancellationToken)
    {
        var connected = await db.Database.CanConnectAsync(cancellationToken);
        return connected ? Ok(new { status = "connected", provider = "PostgreSQL" }) : StatusCode(503, new { status = "unavailable" });
    }
}
