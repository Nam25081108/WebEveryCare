using EveryCare.Api.Domain.Common;
using EveryCare.Api.Domain.Enums;

namespace EveryCare.Api.Domain.Entities;

public sealed class AppUser : BaseEntity
{
    public required string FullName { get; set; }
    public required string Phone { get; set; }
    public string? Email { get; set; }
    public required string PasswordHash { get; set; }
    public UserRole Role { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Active;
    public DateTimeOffset? LastLoginAt { get; set; }
    public CustomerProfile? CustomerProfile { get; set; }
    public PartnerProfile? PartnerProfile { get; set; }
    public ICollection<Address> Addresses { get; set; } = [];
}

public sealed class CustomerProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public string? AvatarUrl { get; set; }
}
