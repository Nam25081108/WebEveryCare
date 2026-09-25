using NetTopologySuite.Geometries;
using EveryCare.Api.Domain.Common;
using EveryCare.Api.Domain.Enums;

namespace EveryCare.Api.Domain.Entities;

public sealed class Booking : BaseEntity
{
    public required string Code { get; set; }
    public Guid CustomerId { get; set; }
    public AppUser Customer { get; set; } = null!;
    public Guid AddressId { get; set; }
    public Address Address { get; set; } = null!;
    public required string AddressSnapshot { get; set; }
    public Point? LocationSnapshot { get; set; }
    public Guid ServiceGroupId { get; set; }
    public ServiceGroup ServiceGroup { get; set; } = null!;
    public Guid? ServicePackageId { get; set; }
    public ServicePackage? ServicePackage { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Draft;
    public CleanerSelectionMode CleanerSelectionMode { get; set; }
    public DateTimeOffset ScheduledStartAt { get; set; }
    public DateTimeOffset? ScheduledEndAt { get; set; }
    public int RequiredWorkers { get; set; } = 1;
    public BuildingType? BuildingType { get; set; }
    public BuildingCondition? BuildingCondition { get; set; }
    public bool HasFurniture { get; set; }
    public decimal? AreaSquareMeters { get; set; }
    public decimal BasePrice { get; set; }
    public decimal SelectionFee { get; set; }
    public decimal ExtraChargeTotal { get; set; }
    public decimal CancellationFee { get; set; }
    public decimal EstimatedTotal { get; set; }
    public Guid? AssignedPartnerId { get; set; }
    public PartnerProfile? AssignedPartner { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? TaskerConfirmedAt { get; set; }
    public DateTimeOffset? ArrivedAt { get; set; }
    public DateTimeOffset? CompletionReportedAt { get; set; }
    public DateTimeOffset? CustomerConfirmedAt { get; set; }
    public DateTimeOffset? IssueReportedAt { get; set; }
    public string? IssueNote { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public bool IsRecurring { get; set; }
    public string? RecurrenceRule { get; set; }
    public Guid? RecurringContractId { get; set; }
    public RecurringServiceContract? RecurringContract { get; set; }
    public int? OccurrenceNumber { get; set; }
    public string? FacilityName { get; set; }
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public string? AccommodationType { get; set; }
    public string? CustomerRequest { get; set; }
    public ICollection<BookingAssignment> Assignments { get; set; } = [];
    public ICollection<BookingExtraCharge> ExtraCharges { get; set; } = [];
    public ICollection<BookingPhoto> Photos { get; set; } = [];
    public Payment? Payment { get; set; }
    public Review? Review { get; set; }
}

public sealed class RecurringServiceContract : BaseEntity
{
    public required string Code { get; set; }
    public Guid CustomerId { get; set; }
    public AppUser Customer { get; set; } = null!;
    public Guid ServiceGroupId { get; set; }
    public ServiceGroup ServiceGroup { get; set; } = null!;
    public Guid ServicePackageId { get; set; }
    public ServicePackage ServicePackage { get; set; } = null!;
    public RecurringContractStatus Status { get; set; } = RecurringContractStatus.PendingDeposit;
    public required string RecurrenceRule { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public int ContractMonths { get; set; }
    public int TotalOccurrences { get; set; }
    public int CompletedOccurrences { get; set; }
    public decimal EstimatedTotal { get; set; }
    public decimal DepositAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public ICollection<Booking> Occurrences { get; set; } = [];
}

public sealed class BookingAssignment : BaseEntity
{
    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;
    public Guid PartnerProfileId { get; set; }
    public PartnerProfile PartnerProfile { get; set; } = null!;
    public AssignmentStatus Status { get; set; } = AssignmentStatus.Invited;
    public DateTimeOffset InvitedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RespondedAt { get; set; }
    public double? DistanceMetersAtInvitation { get; set; }
}

public sealed class BookingExtraCharge : BaseEntity
{
    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;
    public required string Description { get; set; }
    public decimal Amount { get; set; }
    public ExtraChargeStatus Status { get; set; } = ExtraChargeStatus.PendingCustomerApproval;
    public DateTimeOffset? CustomerRespondedAt { get; set; }
}

public sealed class BookingPhoto : BaseEntity
{
    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;
    public PhotoType Type { get; set; }
    public required string ImageUrl { get; set; }
    public Guid UploadedByUserId { get; set; }
}

public sealed class Payment : BaseEntity
{
    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public decimal Amount { get; set; }
    public decimal DepositAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public decimal RefundedAmount { get; set; }
    public decimal PlatformFee { get; set; }
    public decimal TaskerNetAmount { get; set; }
    public string? BankTransactionReference { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset? DepositPaidAt { get; set; }
    public DateTimeOffset? RemainingPaidAt { get; set; }
    public DateTimeOffset? ReleasedAt { get; set; }
}

public sealed class Review : BaseEntity
{
    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;
    public Guid CustomerId { get; set; }
    public Guid PartnerProfileId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
}

public sealed class FavoritePartner : BaseEntity
{
    public Guid CustomerId { get; set; }
    public AppUser Customer { get; set; } = null!;
    public Guid PartnerProfileId { get; set; }
    public PartnerProfile PartnerProfile { get; set; } = null!;
}

public sealed class PartnerNotification : BaseEntity
{
    public Guid PartnerUserId { get; set; }
    public AppUser PartnerUser { get; set; } = null!;
    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;
    public required string Type { get; set; }
    public required string Title { get; set; }
    public required string Message { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
}
