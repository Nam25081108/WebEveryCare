using Microsoft.EntityFrameworkCore;
using EveryCare.Api.Domain.Common;
using EveryCare.Api.Domain.Entities;
using EveryCare.Api.Domain.Enums;

namespace EveryCare.Api.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<CustomerProfile> CustomerProfiles => Set<CustomerProfile>();
    public DbSet<PartnerProfile> PartnerProfiles => Set<PartnerProfile>();
    public DbSet<PartnerTeamMember> PartnerTeamMembers => Set<PartnerTeamMember>();
    public DbSet<PartnerAvailabilityRule> PartnerAvailabilityRules => Set<PartnerAvailabilityRule>();
    public DbSet<PartnerAvailabilityOverride> PartnerAvailabilityOverrides => Set<PartnerAvailabilityOverride>();
    public DbSet<PartnerSession> PartnerSessions => Set<PartnerSession>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<ServiceGroup> ServiceGroups => Set<ServiceGroup>();
    public DbSet<ServicePackage> ServicePackages => Set<ServicePackage>();
    public DbSet<ServiceWorkItem> ServiceWorkItems => Set<ServiceWorkItem>();
    public DbSet<ServiceProcessStep> ServiceProcessSteps => Set<ServiceProcessStep>();
    public DbSet<ServiceTool> ServiceTools => Set<ServiceTool>();
    public DbSet<ProfessionalPricingRule> ProfessionalPricingRules => Set<ProfessionalPricingRule>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingAssignment> BookingAssignments => Set<BookingAssignment>();
    public DbSet<BookingExtraCharge> BookingExtraCharges => Set<BookingExtraCharge>();
    public DbSet<BookingPhoto> BookingPhotos => Set<BookingPhoto>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<FavoritePartner> FavoritePartners => Set<FavoritePartner>();
    public DbSet<PartnerNotification> PartnerNotifications => Set<PartnerNotification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasPostgresExtension("postgis");

        ConfigureUsers(modelBuilder);
        ConfigurePartners(modelBuilder);
        ConfigureCatalog(modelBuilder);
        ConfigureBookings(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                entityType.AddSoftDeleteQueryFilter();
                entityType.FindProperty(nameof(BaseEntity.CreatedAt))?.SetDefaultValueSql("CURRENT_TIMESTAMP");
            }
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added) entry.Entity.CreatedAt = now;
            if (entry.State == EntityState.Modified) entry.Entity.UpdatedAt = now;
        }
        return await base.SaveChangesAsync(cancellationToken);
    }

    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("users");
            entity.HasIndex(x => x.Phone).IsUnique();
            entity.HasIndex(x => x.Email).IsUnique().HasFilter("\"Email\" IS NOT NULL");
            entity.Property(x => x.FullName).HasMaxLength(150);
            entity.Property(x => x.Phone).HasMaxLength(20);
            entity.Property(x => x.Email).HasMaxLength(255);
            entity.Property(x => x.PasswordHash).HasMaxLength(500);
            entity.Property(x => x.Role).HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            entity.HasOne(x => x.CustomerProfile).WithOne(x => x.User).HasForeignKey<CustomerProfile>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.PartnerProfile).WithOne(x => x.User).HasForeignKey<PartnerProfile>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.Addresses).WithOne(x => x.User).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<CustomerProfile>().ToTable("customer_profiles");
        modelBuilder.Entity<Address>(entity =>
        {
            entity.ToTable("addresses");
            entity.Property(x => x.Label).HasMaxLength(80);
            entity.Property(x => x.FullAddress).HasMaxLength(500);
            entity.Property(x => x.WardCode).HasMaxLength(30);
            entity.Property(x => x.WardName).HasMaxLength(150);
            entity.Property(x => x.CityName).HasMaxLength(100);
            entity.Property(x => x.Location).HasColumnType("geography (point)");
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.Location).HasMethod("gist");
        });
    }

    private static void ConfigurePartners(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PartnerProfile>(entity =>
        {
            entity.ToTable("partner_profiles");
            entity.Property(x => x.PartnerType).HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.VerificationStatus).HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.AverageRating).HasPrecision(3, 2);
            entity.Property(x => x.ServiceAddress).HasMaxLength(500);
            entity.Property(x => x.ServiceRadiusKilometers).HasPrecision(5, 2);
            entity.Property(x => x.WalletBalance).HasPrecision(14, 2);
            entity.Property(x => x.AvatarUrl).HasMaxLength(500);
            entity.Property(x => x.ServiceLocation).HasColumnType("geography (point)");
            entity.Property(x => x.CurrentLocation).HasColumnType("geography (point)");
            entity.Property(x => x.RejectionReason).HasMaxLength(1000);
            entity.Property(x => x.ApprovalEmailError).HasMaxLength(1000);
            entity.HasIndex(x => x.ServiceLocation).HasMethod("gist");
            entity.HasIndex(x => x.CurrentLocation).HasMethod("gist");
            entity.HasIndex(x => new { x.IsAvailable, x.VerificationStatus });
        });
        modelBuilder.Entity<PartnerTeamMember>(entity =>
        {
            entity.ToTable("partner_team_members");
            entity.Property(x => x.VerificationStatus).HasConversion<string>().HasMaxLength(30);
            entity.HasOne(x => x.PartnerProfile).WithMany(x => x.TeamMembers).HasForeignKey(x => x.PartnerProfileId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<PartnerServiceCapability>(entity =>
        {
            entity.ToTable("partner_service_capabilities");
            entity.HasKey(x => new { x.PartnerProfileId, x.ServiceGroupId });
            entity.HasQueryFilter(x => !x.PartnerProfile.IsDeleted && !x.ServiceGroup.IsDeleted);
            entity.HasOne(x => x.PartnerProfile).WithMany(x => x.ServiceCapabilities).HasForeignKey(x => x.PartnerProfileId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ServiceGroup).WithMany().HasForeignKey(x => x.ServiceGroupId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<PartnerAvailabilityRule>(entity =>
        {
            entity.ToTable("partner_availability_rules");
            entity.HasIndex(x => new { x.PartnerProfileId, x.DayOfWeek });
            entity.HasOne(x => x.PartnerProfile).WithMany(x => x.AvailabilityRules).HasForeignKey(x => x.PartnerProfileId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<PartnerAvailabilityOverride>(entity =>
        {
            entity.ToTable("partner_availability_overrides");
            entity.HasIndex(x => new { x.PartnerProfileId, x.Date }).IsUnique();
            entity.HasOne(x => x.PartnerProfile).WithMany(x => x.AvailabilityOverrides).HasForeignKey(x => x.PartnerProfileId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<PartnerSession>(entity =>
        {
            entity.ToTable("partner_sessions");
            entity.Property(x => x.TokenHash).HasMaxLength(128);
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasIndex(x => new { x.UserId, x.ExpiresAt });
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureCatalog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ServiceGroup>(entity =>
        {
            entity.ToTable("service_groups");
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(150);
            entity.Property(x => x.Slug).HasMaxLength(100);
            entity.Property(x => x.CategorySlug).HasMaxLength(50);
        });
        modelBuilder.Entity<ServicePackage>(entity =>
        {
            entity.ToTable("service_packages");
            entity.HasIndex(x => new { x.ServiceGroupId, x.Slug }).IsUnique();
            entity.Property(x => x.Price).HasPrecision(14, 2);
            entity.Property(x => x.MaximumAreaSquareMeters).HasPrecision(8, 2);
            entity.HasOne(x => x.ServiceGroup).WithMany(x => x.Packages).HasForeignKey(x => x.ServiceGroupId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ServiceWorkItem>(entity =>
        {
            entity.ToTable("service_work_items");
            entity.Property(x => x.Area).HasConversion<string>().HasMaxLength(30);
            entity.HasOne(x => x.ServicePackage).WithMany(x => x.WorkItems).HasForeignKey(x => x.ServicePackageId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ServiceProcessStep>(entity =>
        {
            entity.ToTable("service_process_steps");
            entity.HasOne(x => x.ServiceGroup).WithMany(x => x.ProcessSteps).HasForeignKey(x => x.ServiceGroupId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ServiceTool>(entity =>
        {
            entity.ToTable("service_tools");
            entity.HasOne(x => x.ServiceGroup).WithMany(x => x.Tools).HasForeignKey(x => x.ServiceGroupId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ProfessionalPricingRule>(entity =>
        {
            entity.ToTable("professional_pricing_rules");
            entity.Property(x => x.BuildingType).HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.BuildingCondition).HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.AreaTier).HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.MinimumAreaSquareMeters).HasPrecision(8, 2);
            entity.Property(x => x.MaximumAreaSquareMeters).HasPrecision(8, 2);
            entity.Property(x => x.FixedPrice).HasPrecision(14, 2);
            entity.Property(x => x.PricePerSquareMeter).HasPrecision(14, 2);
            entity.Property(x => x.FurnishedMultiplier).HasPrecision(5, 2);
            entity.HasIndex(x => new { x.ServiceGroupId, x.BuildingType, x.BuildingCondition, x.AreaTier }).IsUnique();
            entity.HasOne(x => x.ServiceGroup).WithMany(x => x.ProfessionalPricingRules).HasForeignKey(x => x.ServiceGroupId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureBookings(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.ToTable("bookings");
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => new { x.Status, x.ScheduledStartAt });
            entity.Property(x => x.Code).HasMaxLength(30);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.CleanerSelectionMode).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.BuildingType).HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.BuildingCondition).HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.LocationSnapshot).HasColumnType("geography (point)");
            entity.Property(x => x.FacilityName).HasMaxLength(200);
            entity.Property(x => x.ContactName).HasMaxLength(150);
            entity.Property(x => x.ContactPhone).HasMaxLength(20);
            entity.Property(x => x.AccommodationType).HasMaxLength(30);
            entity.Property(x => x.CustomerRequest).HasMaxLength(1000);
            entity.Property(x => x.IssueNote).HasMaxLength(1000);
            entity.Property(x => x.AreaSquareMeters).HasPrecision(8, 2);
            foreach (var property in new[] { nameof(Booking.BasePrice), nameof(Booking.SelectionFee), nameof(Booking.ExtraChargeTotal), nameof(Booking.CancellationFee), nameof(Booking.EstimatedTotal) })
                entity.Property(property).HasPrecision(14, 2);
            entity.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Address).WithMany().HasForeignKey(x => x.AddressId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ServiceGroup).WithMany().HasForeignKey(x => x.ServiceGroupId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ServicePackage).WithMany().HasForeignKey(x => x.ServicePackageId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.AssignedPartner).WithMany().HasForeignKey(x => x.AssignedPartnerId).OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity<BookingAssignment>(entity =>
        {
            entity.ToTable("booking_assignments");
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            entity.HasIndex(x => new { x.BookingId, x.PartnerProfileId }).IsUnique();
            entity.HasOne(x => x.Booking).WithMany(x => x.Assignments).HasForeignKey(x => x.BookingId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.PartnerProfile).WithMany().HasForeignKey(x => x.PartnerProfileId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<BookingExtraCharge>(entity => { entity.ToTable("booking_extra_charges"); entity.Property(x => x.Amount).HasPrecision(14, 2); entity.Property(x => x.Status).HasConversion<string>(); entity.HasOne(x => x.Booking).WithMany(x => x.ExtraCharges).HasForeignKey(x => x.BookingId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<BookingPhoto>(entity => { entity.ToTable("booking_photos"); entity.Property(x => x.Type).HasConversion<string>(); entity.HasOne(x => x.Booking).WithMany(x => x.Photos).HasForeignKey(x => x.BookingId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<Payment>(entity => { entity.ToTable("payments"); entity.HasIndex(x => x.BookingId).IsUnique(); entity.Property(x => x.Method).HasConversion<string>(); entity.Property(x => x.Status).HasConversion<string>(); foreach (var property in new[] { nameof(Payment.Amount), nameof(Payment.DepositAmount), nameof(Payment.RemainingAmount), nameof(Payment.RefundedAmount), nameof(Payment.PlatformFee), nameof(Payment.TaskerNetAmount) }) entity.Property(property).HasPrecision(14, 2); entity.HasOne(x => x.Booking).WithOne(x => x.Payment).HasForeignKey<Payment>(x => x.BookingId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<Review>(entity => { entity.ToTable("reviews", table => table.HasCheckConstraint("ck_reviews_rating", "\"Rating\" BETWEEN 1 AND 5")); entity.HasIndex(x => x.BookingId).IsUnique(); entity.HasOne(x => x.Booking).WithOne(x => x.Review).HasForeignKey<Review>(x => x.BookingId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<FavoritePartner>(entity => { entity.ToTable("favorite_partners"); entity.HasIndex(x => new { x.CustomerId, x.PartnerProfileId }).IsUnique(); entity.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Cascade); entity.HasOne(x => x.PartnerProfile).WithMany().HasForeignKey(x => x.PartnerProfileId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<PartnerNotification>(entity => { entity.ToTable("partner_notifications"); entity.HasIndex(x => new { x.PartnerUserId, x.IsRead, x.CreatedAt }); entity.Property(x => x.Type).HasMaxLength(50); entity.Property(x => x.Title).HasMaxLength(200); entity.HasOne(x => x.PartnerUser).WithMany().HasForeignKey(x => x.PartnerUserId).OnDelete(DeleteBehavior.Cascade); entity.HasOne(x => x.Booking).WithMany().HasForeignKey(x => x.BookingId).OnDelete(DeleteBehavior.Cascade); });
    }
}

internal static class SoftDeleteModelBuilderExtensions
{
    public static void AddSoftDeleteQueryFilter(this Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType entityType)
    {
        var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "entity");
        var property = System.Linq.Expressions.Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
        var filter = System.Linq.Expressions.Expression.Lambda(System.Linq.Expressions.Expression.Equal(property, System.Linq.Expressions.Expression.Constant(false)), parameter);
        entityType.SetQueryFilter(filter);
    }
}
