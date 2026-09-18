using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using EveryCare.Api.Domain.Entities;
using EveryCare.Api.Infrastructure.Persistence;

namespace EveryCare.Api.Services;

public sealed class PartnerSessionService(AppDbContext db)
{
    public async Task<(string Token, PartnerSession Session)> CreateAsync(AppUser user, CancellationToken cancellationToken)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        var session = new PartnerSession { User = user, TokenHash = PasswordService.HashToken(token), ExpiresAt = DateTimeOffset.UtcNow.AddDays(30) };
        db.PartnerSessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);
        return (token, session);
    }

    public async Task<AppUser?> GetCurrentUserAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        var header = request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return null;
        var token = header[7..].Trim();
        try
        {
            var hash = PasswordService.HashToken(token);
            return await db.PartnerSessions
                .Where(x => x.TokenHash == hash && x.RevokedAt == null && x.ExpiresAt > DateTimeOffset.UtcNow)
                .Select(x => x.User)
                .SingleOrDefaultAsync(cancellationToken);
        }
        catch (FormatException) { return null; }
    }
}
