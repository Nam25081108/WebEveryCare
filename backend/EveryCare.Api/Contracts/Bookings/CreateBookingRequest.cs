using EveryCare.Api.Domain.Enums;

namespace EveryCare.Api.Contracts.Bookings;

public sealed record CreateBookingRequest(
    string CustomerName,
    string CustomerPhone,
    string AddressLabel,
    string FullAddress,
    double? Latitude,
    double? Longitude,
    string ServiceGroupSlug,
    string? ServicePackageSlug,
    DateTimeOffset ScheduledStartAt,
    CleanerSelectionMode CleanerSelectionMode,
    BuildingType? BuildingType,
    BuildingCondition? BuildingCondition,
    bool HasFurniture,
    decimal? AreaSquareMeters,
    bool IsRecurring,
    IReadOnlyList<string>? RecurrenceDays,
    string? RecurrenceStartTime,
    int? RecurrenceMonths,
    bool HasGlassCleaning,
    bool HasCarpetVacuum,
    string? FacilityName,
    string? ContactName,
    string? ContactPhone,
    string? AccommodationType,
    IReadOnlyList<HospitalityItemRequest>? HospitalityItems,
    string? CustomerRequest);

public sealed record HospitalityItemRequest(
    string Code,
    string Name,
    int Quantity,
    decimal UnitPrice,
    int DurationMinutes);
