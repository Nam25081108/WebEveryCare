using EveryCare.Api.Domain.Common;
using EveryCare.Api.Domain.Enums;

namespace EveryCare.Api.Domain.Entities;

public sealed class ServiceGroup : BaseEntity
{
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public required string Description { get; set; }
    public string CategorySlug { get; set; } = "cleaning";
    public string? IconName { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsProfessional { get; set; }
    public bool IsComingSoon { get; set; }
    public ICollection<ServicePackage> Packages { get; set; } = [];
    public ICollection<ServiceProcessStep> ProcessSteps { get; set; } = [];
    public ICollection<ServiceTool> Tools { get; set; } = [];
    public ICollection<ProfessionalPricingRule> ProfessionalPricingRules { get; set; } = [];
}

public sealed class ServicePackage : BaseEntity
{
    public Guid ServiceGroupId { get; set; }
    public ServiceGroup ServiceGroup { get; set; } = null!;
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int? DurationMinutes { get; set; }
    public int RequiredWorkers { get; set; } = 1;
    public decimal? MaximumAreaSquareMeters { get; set; }
    public int? MaximumRooms { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<ServiceWorkItem> WorkItems { get; set; } = [];
}

public sealed class ServiceWorkItem : BaseEntity
{
    public Guid ServicePackageId { get; set; }
    public ServicePackage ServicePackage { get; set; } = null!;
    public WorkArea Area { get; set; }
    public required string Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class ServiceProcessStep : BaseEntity
{
    public Guid ServiceGroupId { get; set; }
    public ServiceGroup ServiceGroup { get; set; } = null!;
    public required string Title { get; set; }
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
}

public sealed class ServiceTool : BaseEntity
{
    public Guid ServiceGroupId { get; set; }
    public ServiceGroup ServiceGroup { get; set; } = null!;
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class ProfessionalPricingRule : BaseEntity
{
    public Guid ServiceGroupId { get; set; }
    public ServiceGroup ServiceGroup { get; set; } = null!;
    public BuildingType BuildingType { get; set; }
    public BuildingCondition BuildingCondition { get; set; }
    public AreaTier AreaTier { get; set; }
    public decimal MinimumAreaSquareMeters { get; set; }
    public decimal MaximumAreaSquareMeters { get; set; }
    public decimal? FixedPrice { get; set; }
    public decimal? PricePerSquareMeter { get; set; }
    public decimal FurnishedMultiplier { get; set; } = 1.25m;
    public bool IsActive { get; set; } = true;
}
