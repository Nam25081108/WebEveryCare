using NetTopologySuite.Geometries;
using EveryCare.Api.Domain.Common;

namespace EveryCare.Api.Domain.Entities;

public sealed class Address : BaseEntity
{
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public required string Label { get; set; }
    public required string FullAddress { get; set; }
    public string? WardCode { get; set; }
    public string? WardName { get; set; }
    public string CityName { get; set; } = "TP. Hồ Chí Minh";
    public string? Note { get; set; }
    public Point? Location { get; set; }
    public bool IsDefault { get; set; }
}
