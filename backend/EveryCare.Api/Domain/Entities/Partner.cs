using NetTopologySuite.Geometries;
using EveryCare.Api.Domain.Common;
using EveryCare.Api.Domain.Enums;

namespace EveryCare.Api.Domain.Entities;

public sealed class PartnerProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public PartnerType PartnerType { get; set; }
    public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Pending;
    public string? IdentityNumber { get; set; }
    public string? IdentityFrontUrl { get; set; }
    public string? IdentityBackUrl { get; set; }
    public string? TeamName { get; set; }
    public int TeamSize { get; set; } = 1;
    public bool IsAvailable { get; set; }
    public required string ServiceAddress { get; set; }
    public Point? ServiceLocation { get; set; }
    public decimal ServiceRadiusKilometers { get; set; } = 10;
    public Point? CurrentLocation { get; set; }
    public DateTimeOffset? LocationUpdatedAt { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? RejectionReason { get; set; }
    public DateTimeOffset? ApprovalEmailSentAt { get; set; }
    public string? ApprovalEmailError { get; set; }
    public decimal AverageRating { get; set; }
    public int CompletedBookings { get; set; }
    public ICollection<PartnerTeamMember> TeamMembers { get; set; } = [];
    public ICollection<PartnerServiceCapability> ServiceCapabilities { get; set; } = [];
    public ICollection<PartnerAvailabilityRule> AvailabilityRules { get; set; } = [];
    public ICollection<PartnerAvailabilityOverride> AvailabilityOverrides { get; set; } = [];
}

public sealed class PartnerTeamMember : BaseEntity
{
    public Guid PartnerProfileId { get; set; }
    public PartnerProfile PartnerProfile { get; set; } = null!;
    public required string FullName { get; set; }
    public required string Phone { get; set; }
    public string? IdentityNumber { get; set; }
    public string? IdentityFrontUrl { get; set; }
    public string? IdentityBackUrl { get; set; }
    public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Pending;
}

public sealed class PartnerServiceCapability
{
    public Guid PartnerProfileId { get; set; }
    public PartnerProfile PartnerProfile { get; set; } = null!;
    public Guid ServiceGroupId { get; set; }
    public ServiceGroup ServiceGroup { get; set; } = null!;
}

public sealed class PartnerAvailabilityRule : BaseEntity
{
    public Guid PartnerProfileId { get; set; }
    public PartnerProfile PartnerProfile { get; set; } = null!;
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class PartnerAvailabilityOverride : BaseEntity
{
    public Guid PartnerProfileId { get; set; }
    public PartnerProfile PartnerProfile { get; set; } = null!;
    public DateOnly Date { get; set; }
    public bool IsUnavailable { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public string? Note { get; set; }
}

public sealed class PartnerSession : BaseEntity
{
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public required string TokenHash { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}
