using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using EveryCare.Api.Domain.Entities;
using EveryCare.Api.Domain.Enums;

namespace EveryCare.Api.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        var hasCatalog = await db.ServiceGroups.AnyAsync(cancellationToken);
        if (!hasCatalog)
        {

        var room = new ServiceGroup { Name = "Dọn dẹp nhà cửa", Slug = "ve-sinh-phong-le", Description = "Dọn nhanh theo giờ cho căn hộ và nhà ở đang sử dụng.", IconName = "sparkles", DisplayOrder = 1 };
        var deep = new ServiceGroup { Name = "Tổng vệ sinh", Slug = "tong-ve-sinh", Description = "Làm sạch toàn diện với đội ngũ phù hợp quy mô công việc.", IconName = "home", DisplayOrder = 2 };
        var professional = new ServiceGroup { Name = "Vệ sinh chuyên nghiệp", Slug = "ve-sinh-chuyen-nghiep", Description = "Dành cho nhà lâu năm, sau xây dựng và văn phòng.", IconName = "building", DisplayOrder = 3, IsProfessional = true };

        room.Packages.Add(CreatePackage("Gói 1 giờ", "goi-1-gio", 120_000, 60, 1, 30, 1, 1, RoomWorkItems()));
        room.Packages.Add(CreatePackage("Gói 2 giờ", "goi-2-gio", 210_000, 120, 1, 55, 2, 2, RoomWorkItems()));
        room.Packages.Add(CreatePackage("Gói 3 giờ", "goi-3-gio", 300_000, 180, 1, 85, 3, 3, RoomWorkItems()));
        room.Packages.Add(CreatePackage("Gói 4 giờ", "goi-4-gio", 380_000, 240, 1, 105, 4, 4, RoomWorkItems()));

        deep.Packages.Add(CreatePackage("Căn hộ 60m²", "can-ho-60m2", 690_000, 180, 2, 60, null, 1, DeepWorkItems()));
        deep.Packages.Add(CreatePackage("Nhà 80m²", "nha-80m2", 890_000, 240, 2, 80, null, 2, DeepWorkItems()));
        deep.Packages.Add(CreatePackage("Nhà 100m²", "nha-100m2", 1_090_000, 180, 3, 100, null, 3, DeepWorkItems()));
        deep.Packages.Add(CreatePackage("Nhà 150m²", "nha-150m2", 1_490_000, 240, 3, 150, null, 4, DeepWorkItems()));
        deep.Packages.Add(CreatePackage("Nhà 200m²", "nha-200m2", 1_990_000, 240, 4, 200, null, 5, DeepWorkItems()));
        deep.Packages.Add(CreatePackage("Công trình 400m²", "cong-trinh-400m2", 3_690_000, 480, 4, 400, null, 6, DeepWorkItems()));

        AddProfessionalContent(professional);
        db.ServiceGroups.AddRange(room, deep, professional);
        AddDemoPartners(db, room, deep, professional);
            await db.SaveChangesAsync(cancellationToken);
        }

        await AddMissingUtilityServices(db, cancellationToken);
    }

    private static async Task AddMissingUtilityServices(AppDbContext db, CancellationToken cancellationToken)
    {
        var office = await db.ServiceGroups.IgnoreQueryFilters()
            .Include(group => group.Packages)
            .SingleOrDefaultAsync(group => group.Slug == "don-dep-van-phong-dinh-ky", cancellationToken);
        if (office is null)
        {
            office = new ServiceGroup
            {
                Name = "Dọn dẹp văn phòng định kỳ", Slug = "don-dep-van-phong-dinh-ky",
                Description = "Nhân sự theo buổi, theo ngày hoặc lịch cố định hằng tháng.",
                CategorySlug = "business", IconName = "briefcase", DisplayOrder = 10
            };
            db.ServiceGroups.Add(office);
        }

        var officePackages = new (string Name, string Slug, decimal Price, int Minutes, int Workers, decimal Area)[]
        {
            ("Văn phòng tối đa 100m²", "van-phong-100m2", 260_000, 120, 1, 100),
            ("Văn phòng tối đa 150m²", "van-phong-150m2", 360_000, 180, 1, 150),
            ("Văn phòng tối đa 200m²", "van-phong-200m2", 450_000, 240, 1, 200),
            ("Văn phòng tối đa 250m²", "van-phong-250m2", 550_000, 300, 1, 250),
            ("Văn phòng tối đa 300m²", "van-phong-300m2", 690_000, 180, 2, 300),
            ("Văn phòng tối đa 400m²", "van-phong-400m2", 880_000, 240, 2, 400),
            ("Văn phòng tối đa 500m²", "van-phong-500m2", 1_050_000, 300, 2, 500),
            ("Văn phòng tối đa 750m²", "van-phong-750m2", 1_550_000, 300, 3, 750),
            ("Văn phòng tối đa 900m²", "van-phong-900m2", 1_850_000, 360, 3, 900)
        };
        for (var index = 0; index < officePackages.Length; index++)
        {
            var definition = officePackages[index];
            var package = office.Packages.SingleOrDefault(item => item.Slug == definition.Slug);
            if (package is null)
            {
                package = CreatePackage(definition.Name, definition.Slug, definition.Price, definition.Minutes, definition.Workers, definition.Area, null, index + 1, OfficeWorkItems());
                package.ServiceGroup = office;
                db.ServicePackages.Add(package);
                continue;
            }
        }

        var hospitality = await db.ServiceGroups.IgnoreQueryFilters()
            .SingleOrDefaultAsync(group => group.Slug == "don-dep-buong-phong", cancellationToken);
        if (hospitality is null)
        {
            hospitality = new ServiceGroup
            {
                Name = "Dọn dẹp buồng phòng", Slug = "don-dep-buong-phong",
                Description = "Dọn phòng cho khách sạn, homestay, căn hộ dịch vụ và villa.",
                CategorySlug = "business", IconName = "bed", DisplayOrder = 11
            };
            db.ServiceGroups.Add(hospitality);
        }

        var existingSlugs = (await db.ServiceGroups.IgnoreQueryFilters().Select(group => group.Slug).ToListAsync(cancellationToken)).ToHashSet();
        existingSlugs.Add("don-dep-van-phong-dinh-ky");
        existingSlugs.Add("don-dep-buong-phong");
        var planned = new (string Category, string Slug, string Name, string Description, string Icon)[]
        {
            ("business", "don-dep-van-phong-dinh-ky", "Dọn dẹp văn phòng định kỳ", "Nhân sự theo ca, theo ngày hoặc lịch cố định.", "briefcase"),
            ("business", "don-dep-buong-phong", "Dọn dẹp buồng phòng", "Dành cho căn hộ cho thuê, homestay và khách sạn.", "bed"),
            ("business", "ve-sinh-van-phong-chuyen-sau", "Vệ sinh văn phòng chuyên sâu", "Làm sạch định kỳ quy mô lớn và khu vực chuyên biệt.", "building"),
            ("care", "trong-tre", "Trông trẻ", "Hỗ trợ chăm sóc trẻ theo giờ tại nhà.", "baby"),
            ("care", "cham-soc-nguoi-cao-tuoi", "Chăm sóc người cao tuổi", "Bầu bạn và hỗ trợ sinh hoạt hằng ngày.", "heart"),
            ("care", "cham-soc-nguoi-benh", "Chăm sóc người bệnh", "Hỗ trợ tại nhà hoặc cơ sở y tế theo lịch.", "stethoscope"),
            ("beauty", "trang-diem-tai-nha", "Trang điểm", "Trang điểm sự kiện và phong cách cá nhân tại nhà.", "wand"),
            ("appliances", "ve-sinh-may-lanh", "Vệ sinh máy lạnh", "Làm sạch dàn lạnh, lưới lọc và kiểm tra vận hành.", "air"),
            ("appliances", "ve-sinh-may-nong-lanh", "Vệ sinh máy nóng lạnh", "Vệ sinh bình nóng lạnh trong phòng tắm.", "shower"),
            ("appliances", "ve-sinh-may-giat", "Vệ sinh máy giặt", "Làm sạch lồng giặt và cặn bẩn tích tụ.", "washer"),
            ("appliances", "thao-lap-may-lanh", "Tháo lắp máy lạnh", "Tháo, di dời và lắp đặt lại thiết bị.", "wrench"),
            ("advanced", "giat-ui", "Giặt ủi", "Thu gom, giặt, sấy và giao lại theo lịch.", "shirt"),
            ("advanced", "nau-an-gia-dinh", "Nấu ăn gia đình", "Chuẩn bị bữa ăn tại nhà theo khẩu vị.", "cooking"),
            ("advanced", "di-cho", "Đi chợ", "Mua thực phẩm và nhu yếu phẩm theo danh sách.", "basket"),
            ("advanced", "khu-khuan", "Khử khuẩn", "Khử khuẩn không gian nhà ở và nơi làm việc.", "shield")
        };

        var order = 10;
        foreach (var item in planned.Where(item => !existingSlugs.Contains(item.Slug)))
        {
            db.ServiceGroups.Add(new ServiceGroup
            {
                Name = item.Name,
                Slug = item.Slug,
                Description = item.Description,
                CategorySlug = item.Category,
                IconName = item.Icon,
                DisplayOrder = order++,
                IsComingSoon = true
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static ServicePackage CreatePackage(string name, string slug, decimal price, int minutes, int workers, decimal area, int? rooms, int order, IEnumerable<(WorkArea Area, string Text)> workItems)
    {
        var package = new ServicePackage { Name = name, Slug = slug, Description = $"Tối đa {area:0}m²", Price = price, DurationMinutes = minutes, RequiredWorkers = workers, MaximumAreaSquareMeters = area, MaximumRooms = rooms, DisplayOrder = order };
        var itemOrder = 1;
        foreach (var (workArea, text) in workItems) package.WorkItems.Add(new ServiceWorkItem { Area = workArea, Description = text, DisplayOrder = itemOrder++ });
        return package;
    }

    private static IEnumerable<(WorkArea, string)> RoomWorkItems() =>
    [
        (WorkArea.LivingRoom, "Quét bụi và lau sạch sàn"), (WorkArea.LivingRoom, "Lau bàn ghế, kệ và bề mặt dễ tiếp cận"), (WorkArea.LivingRoom, "Thu gom rác và sắp xếp đồ dùng gọn gàng"),
        (WorkArea.Bedroom, "Quét bụi, hút bụi và lau sàn"), (WorkArea.Bedroom, "Lau bàn, tủ và các bề mặt bên ngoài"), (WorkArea.Bedroom, "Sắp xếp giường ngủ và đồ dùng cơ bản"),
        (WorkArea.Kitchen, "Lau mặt bếp và khu vực chế biến"), (WorkArea.Kitchen, "Làm sạch bồn rửa và vòi nước"), (WorkArea.Kitchen, "Lau bên ngoài tủ bếp và thiết bị"),
        (WorkArea.Bathroom, "Làm sạch bồn cầu và lavabo"), (WorkArea.Bathroom, "Lau gương, vòi nước và phụ kiện"), (WorkArea.Bathroom, "Chà sàn và khu vực tường dễ tiếp cận")
    ];

    private static IEnumerable<(WorkArea, string)> DeepWorkItems() =>
    [
        (WorkArea.General, "Làm sạch toàn bộ không gian từ trên xuống dưới"), (WorkArea.General, "Phân chia nhân sự theo từng khu vực"),
        (WorkArea.Scope, "Bề mặt bên ngoài của nội thất và thiết bị"), (WorkArea.Scope, "Sàn, tường thấp, cửa và kính trong tầm với"),
        (WorkArea.LivingRoom, "Hút bụi sofa, thảm và khe ghế"), (WorkArea.LivingRoom, "Lau cửa kính, khung cửa và tay nắm"),
        (WorkArea.Bedroom, "Hút bụi nệm và gầm giường"), (WorkArea.Bedroom, "Lau bên ngoài tủ, bàn và đầu giường"),
        (WorkArea.Kitchen, "Tẩy dầu mỡ mặt bếp và tường bếp"), (WorkArea.Kitchen, "Làm sạch bồn rửa, vòi và mặt đá"),
        (WorkArea.Bathroom, "Tẩy cặn bồn cầu, lavabo và vòi nước"), (WorkArea.Bathroom, "Chà sàn, tường và vách kính")
    ];

    private static IEnumerable<(WorkArea, string)> OfficeWorkItems() =>
    [
        (WorkArea.General, "Quét bụi và lau sàn các khu vực làm việc, sảnh và hành lang"),
        (WorkArea.General, "Phủi bụi, lau dọn bàn ghế làm việc, tủ và kệ"),
        (WorkArea.General, "Lau cửa kính, cửa sổ trong phạm vi an toàn"),
        (WorkArea.General, "Dọn dẹp khu vực nhà vệ sinh"),
        (WorkArea.General, "Thu gom và đổ rác")
    ];

    private static void AddProfessionalContent(ServiceGroup group)
    {
        var steps = new[] { "Khảo sát nhanh hiện trạng và khoanh vùng thi công", "Thu gom rác thô, hút bụi công nghiệp", "Làm sạch chi tiết từ trên xuống dưới", "Chà sàn, xử lý vết bẩn và vệ sinh kính", "Kiểm tra chất lượng và chụp ảnh bàn giao" };
        for (var i = 0; i < steps.Length; i++) group.ProcessSteps.Add(new ServiceProcessStep { Title = steps[i], DisplayOrder = i + 1 });
        var tools = new[] { ("Máy hút bụi công nghiệp", "Hút bụi mịn và rác khô công suất cao"), ("Máy chà sàn", "Làm sạch sàn diện tích lớn"), ("Bộ dụng cụ kính", "Cây gạt, khăn kính và dụng cụ nối dài"), ("Khăn microfiber", "Phân màu riêng cho từng khu vực"), ("Bàn chải chuyên dụng", "Xử lý khe góc khó tiếp cận"), ("Hóa chất vệ sinh", "Dùng đúng loại theo từng bề mặt") };
        for (var i = 0; i < tools.Length; i++) group.Tools.Add(new ServiceTool { Name = tools[i].Item1, Description = tools[i].Item2, DisplayOrder = i + 1 });

        AddPricing(group, BuildingType.House, BuildingCondition.Existing, [2_100_000, 2_700_000, 3_300_000], 33_000);
        AddPricing(group, BuildingType.House, BuildingCondition.NewOrRenovated, [1_800_000, 2_400_000, 3_000_000], 30_000);
        AddPricing(group, BuildingType.Office, BuildingCondition.Existing, [2_600_000, 3_400_000, 4_200_000], 42_000);
        AddPricing(group, BuildingType.Office, BuildingCondition.NewOrRenovated, [2_300_000, 3_000_000, 3_800_000], 38_000);
    }

    private static void AddPricing(ServiceGroup group, BuildingType building, BuildingCondition condition, decimal[] fixedPrices, decimal perSquareMeter)
    {
        group.ProfessionalPricingRules.Add(new ProfessionalPricingRule { BuildingType = building, BuildingCondition = condition, AreaTier = AreaTier.Under60, MinimumAreaSquareMeters = 1, MaximumAreaSquareMeters = 59, FixedPrice = fixedPrices[0] });
        group.ProfessionalPricingRules.Add(new ProfessionalPricingRule { BuildingType = building, BuildingCondition = condition, AreaTier = AreaTier.From60To80, MinimumAreaSquareMeters = 60, MaximumAreaSquareMeters = 80, FixedPrice = fixedPrices[1] });
        group.ProfessionalPricingRules.Add(new ProfessionalPricingRule { BuildingType = building, BuildingCondition = condition, AreaTier = AreaTier.From81To100, MinimumAreaSquareMeters = 81, MaximumAreaSquareMeters = 100, FixedPrice = fixedPrices[2] });
        group.ProfessionalPricingRules.Add(new ProfessionalPricingRule { BuildingType = building, BuildingCondition = condition, AreaTier = AreaTier.Custom101To500, MinimumAreaSquareMeters = 101, MaximumAreaSquareMeters = 500, PricePerSquareMeter = perSquareMeter });
    }

    private static void AddDemoPartners(AppDbContext db, ServiceGroup room, ServiceGroup deep, ServiceGroup professional)
    {
        var ngocUser = new AppUser { FullName = "Nguyễn Thị Ngọc", Phone = "0909888777", Email = "ngoc.partner@everycare.vn", PasswordHash = "PENDING_AUTH_MIGRATION", Role = UserRole.Partner, Status = UserStatus.Active };
        var ngocLocation = new Point(106.681, 10.762) { SRID = 4326 };
        var ngoc = new PartnerProfile { User = ngocUser, PartnerType = PartnerType.Individual, VerificationStatus = VerificationStatus.Approved, IsAvailable = true, TeamSize = 1, ServiceAddress = "Quận 3, TP.HCM", ServiceLocation = ngocLocation, CurrentLocation = ngocLocation, LocationUpdatedAt = DateTimeOffset.UtcNow, AverageRating = 4.9m, CompletedBookings = 48 };
        ngoc.ServiceCapabilities.Add(new PartnerServiceCapability { ServiceGroup = room });

        var teamUser = new AppUser { FullName = "Đội Thanh Tâm", Phone = "0933777888", Email = "thanhtam.partner@everycare.vn", PasswordHash = "PENDING_AUTH_MIGRATION", Role = UserRole.Partner, Status = UserStatus.Active };
        var teamLocation = new Point(106.695, 10.776) { SRID = 4326 };
        var team = new PartnerProfile { User = teamUser, PartnerType = PartnerType.Team, TeamName = "Đội Thanh Tâm", VerificationStatus = VerificationStatus.Approved, IsAvailable = true, TeamSize = 4, ServiceAddress = "Quận 1, TP.HCM", ServiceLocation = teamLocation, CurrentLocation = teamLocation, LocationUpdatedAt = DateTimeOffset.UtcNow, AverageRating = 4.8m, CompletedBookings = 73 };
        team.ServiceCapabilities.Add(new PartnerServiceCapability { ServiceGroup = deep });
        team.ServiceCapabilities.Add(new PartnerServiceCapability { ServiceGroup = professional });
        foreach (var day in new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday })
        {
            ngoc.AvailabilityRules.Add(new PartnerAvailabilityRule { DayOfWeek = day, StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(17, 0) });
            team.AvailabilityRules.Add(new PartnerAvailabilityRule { DayOfWeek = day, StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(17, 0) });
        }
        db.PartnerProfiles.AddRange(ngoc, team);
    }
}
