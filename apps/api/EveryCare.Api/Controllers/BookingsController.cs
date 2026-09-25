using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using System.Globalization;
using System.Text.RegularExpressions;
using EveryCare.Api.Contracts.Bookings;
using EveryCare.Api.Domain.Entities;
using EveryCare.Api.Domain.Enums;
using EveryCare.Api.Infrastructure.Persistence;
using EveryCare.Api.Services;

namespace EveryCare.Api.Controllers;

[ApiController]
[Route("api/bookings")]
public sealed class BookingsController(AppDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateBookingRequest request, CancellationToken cancellationToken)
    {
        if (request.ScheduledStartAt < DateTimeOffset.UtcNow.AddDays(2))
            return BadRequest(new { message = "Lịch bắt đầu phải cách thời điểm hiện tại ít nhất 2 ngày." });

        if (!request.Latitude.HasValue || !request.Longitude.HasValue)
            return BadRequest(new { message = "Vui lòng xác nhận vị trí địa chỉ trên bản đồ." });

        var group = await db.ServiceGroups
            .Include(x => x.Packages)
            .Include(x => x.ProfessionalPricingRules)
            .SingleOrDefaultAsync(x => x.Slug == request.ServiceGroupSlug && x.IsActive, cancellationToken);
        if (group is null) return BadRequest(new { message = "Nhóm dịch vụ không tồn tại hoặc đã bị khóa." });

        string? recurrenceRule = null;
        if (request.IsRecurring)
        {
            return BadRequest(new { message = "Dịch vụ văn phòng hiện chỉ nhận đặt theo buổi/ngày; gói tháng đã ngừng áp dụng." });
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var customer = await db.Users.Include(x => x.CustomerProfile).SingleOrDefaultAsync(x => x.Phone == request.CustomerPhone, cancellationToken);
        if (customer is null)
        {
            customer = new AppUser { FullName = request.CustomerName.Trim(), Phone = request.CustomerPhone.Trim(), PasswordHash = "PENDING_AUTH_MIGRATION", Role = UserRole.Customer, Status = UserStatus.Active, CustomerProfile = new CustomerProfile() };
            db.Users.Add(customer);
        }
        else if (customer.Role != UserRole.Customer) return BadRequest(new { message = "Số điện thoại không thuộc tài khoản khách hàng." });

        var point = request.Latitude.HasValue && request.Longitude.HasValue ? new Point(request.Longitude.Value, request.Latitude.Value) { SRID = 4326 } : null;
        var address = await db.Addresses.FirstOrDefaultAsync(x => x.UserId == customer.Id && x.FullAddress == request.FullAddress, cancellationToken);
        if (address is null)
        {
            address = new Address { User = customer, Label = request.AddressLabel, FullAddress = request.FullAddress, Location = point, IsDefault = !await db.Addresses.AnyAsync(x => x.UserId == customer.Id, cancellationToken) };
            db.Addresses.Add(address);
        }
        else if (point is not null)
        {
            address.Location = point;
        }

        ServicePackage? package = null;
        decimal basePrice;
        decimal glassCleaningPrice = 0;
        decimal carpetVacuumPrice = 0;
        var hospitalityCharges = new List<(string Description, decimal Amount)>();
        var calculatedDurationMinutes = 240;
        var requiredWorkers = 1;
        if (group.Slug == "don-dep-buong-phong")
        {
            if (string.IsNullOrWhiteSpace(request.FacilityName) || string.IsNullOrWhiteSpace(request.ContactName) || string.IsNullOrWhiteSpace(request.ContactPhone))
                return BadRequest(new { message = "Vui lòng nhập đầy đủ tên cơ sở và thông tin người liên hệ." });
            var phone = Regex.Replace(request.ContactPhone, @"\s+", "");
            if (!Regex.IsMatch(phone, @"^0\d{9}$")) return BadRequest(new { message = "Số điện thoại liên hệ không hợp lệ." });
            if (request.AccommodationType is not ("hotel" or "apartment" or "villa")) return BadRequest(new { message = "Loại hình lưu trú không hợp lệ." });

            var definitions = new Dictionary<string, (string Kind, string Name, decimal Price, int Minutes)>
            {
                ["hotel-single"]=("hotel","Phòng đơn",90_000m,45), ["hotel-double"]=("hotel","Phòng đôi",130_000m,60),
                ["hotel-family"]=("hotel","Phòng gia đình",190_000m,90), ["hotel-dorm-under-8"]=("hotel","Phòng DORM dưới 8 giường",220_000m,120),
                ["hotel-dorm-8-plus"]=("hotel","Phòng DORM từ 8 giường",300_000m,150),
                ["apartment-1br"]=("apartment","1 phòng ngủ",250_000m,120), ["apartment-2br"]=("apartment","2 phòng ngủ",350_000m,180),
                ["apartment-3br"]=("apartment","3 phòng ngủ",450_000m,240),
                ["villa-single"]=("villa","Phòng đơn",100_000m,45), ["villa-double"]=("villa","Phòng đôi",140_000m,60),
                ["villa-family"]=("villa","Phòng gia đình",200_000m,90)
            };
            var selectedItems = request.HospitalityItems?.Where(item => item.Quantity > 0).ToArray() ?? [];
            if (selectedItems.Length == 0 || selectedItems.Any(item => item.Quantity > 50 || !definitions.TryGetValue(item.Code, out var definition) || definition.Kind != request.AccommodationType || !group.Packages.Any(package => package.Slug == item.Code && package.IsActive)))
                return BadRequest(new { message = "Danh sách phòng cần dọn dẹp không hợp lệ." });
            var totalRooms = selectedItems.Sum(item => item.Quantity);
            if (totalRooms > 100) return BadRequest(new { message = "Mỗi đơn chỉ hỗ trợ tối đa 100 phòng hoặc căn." });
            foreach (var item in selectedItems)
            {
                var definition = definitions[item.Code];
                var hospitalityPackage = group.Packages.Single(package => package.Slug == item.Code && package.IsActive);
                hospitalityCharges.Add(($"{definition.Name} × {item.Quantity}", hospitalityPackage.Price * item.Quantity));
            }
            basePrice = 0;
            requiredWorkers = 1;
            calculatedDurationMinutes = Math.Max(60, selectedItems.Sum(item => (group.Packages.Single(package => package.Slug == item.Code && package.IsActive).DurationMinutes ?? definitions[item.Code].Minutes) * item.Quantity));
        }
        else if (group.IsProfessional)
        {
            if (request.BuildingType is null || request.BuildingCondition is null || request.AreaSquareMeters is null)
                return BadRequest(new { message = "Thiếu thông tin công trình chuyên nghiệp." });
            if (request.AreaSquareMeters < 1 || request.AreaSquareMeters > 500)
                return BadRequest(new { message = "Diện tích công trình phải nằm trong khoảng từ 1 đến 500m²." });
            var areaTier = GetAreaTier(request.AreaSquareMeters.Value);
            var rule = group.ProfessionalPricingRules.SingleOrDefault(x => x.BuildingType == request.BuildingType && x.BuildingCondition == request.BuildingCondition && x.AreaTier == areaTier && x.IsActive);
            if (rule is null) return BadRequest(new { message = "Chưa có bảng giá phù hợp với công trình." });
            basePrice = rule.FixedPrice ?? rule.PricePerSquareMeter!.Value * request.AreaSquareMeters.Value;
            if (request.BuildingCondition == Domain.Enums.BuildingCondition.NewOrRenovated && request.HasFurniture) basePrice = Math.Round(basePrice * rule.FurnishedMultiplier / 10_000m) * 10_000m;
            requiredWorkers = request.AreaSquareMeters <= 80 ? 2 : request.AreaSquareMeters <= 150 ? 3 : 4;
            calculatedDurationMinutes = request.AreaSquareMeters > 100 ? 480 : 240;
        }
        else
        {
            package = group.Packages.SingleOrDefault(x => x.Slug == request.ServicePackageSlug && x.IsActive);
            if (package is null) return BadRequest(new { message = "Gói dịch vụ không tồn tại hoặc đã bị khóa." });
            basePrice = package.Price;
            requiredWorkers = package.RequiredWorkers;
            calculatedDurationMinutes = package.DurationMinutes ?? 240;
            glassCleaningPrice = request.HasGlassCleaning ? 120_000m * requiredWorkers : 0;
            carpetVacuumPrice = request.HasCarpetVacuum ? 100_000m * requiredWorkers : 0;
            if (request.IsRecurring)
            {
                var discount = request.RecurrenceMonths switch { 1 => .08m, 3 => .12m, 6 => .16m, 12 => .20m, _ => 0m };
                basePrice = RoundToThousand(package.Price * (1 - discount));
                glassCleaningPrice = RoundToThousand(glassCleaningPrice * (1 - discount));
                carpetVacuumPrice = RoundToThousand(carpetVacuumPrice * (1 - discount));
            }
        }

        var selectionMode = request.CleanerSelectionMode;
        if (selectionMode == CleanerSelectionMode.FavoriteFirst)
        {
            var requiresIndividual = PartnerEligibilityPolicy.RequiresIndividual(group.Slug, requiredWorkers);
            var hasEligibleFavorite = await db.FavoritePartners.AsNoTracking().AnyAsync(favorite =>
                favorite.CustomerId == customer.Id &&
                favorite.PartnerProfile.ServiceCapabilities.Any(capability => capability.ServiceGroupId == group.Id) &&
                (requiresIndividual
                    ? favorite.PartnerProfile.PartnerType == PartnerType.Individual
                    : favorite.PartnerProfile.PartnerType == PartnerType.Team && favorite.PartnerProfile.TeamSize >= requiredWorkers), cancellationToken);
            if (!hasEligibleFavorite) selectionMode = CleanerSelectionMode.Automatic;
        }
        var selectionFee = selectionMode == CleanerSelectionMode.CustomerChooses ? 30_000m : 0m;
        var extraChargeTotal = glassCleaningPrice + carpetVacuumPrice + hospitalityCharges.Sum(item => item.Amount);
        Booking CreateOccurrence(DateTimeOffset scheduledStart, int? occurrenceNumber, decimal occurrenceSelectionFee, RecurringServiceContract? contract = null)
        {
            var item = new Booking
            {
                Code = $"SN-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid():N}"[..19].ToUpperInvariant(),
                Customer = customer, Address = address, AddressSnapshot = request.FullAddress, LocationSnapshot = point,
                ServiceGroup = group, ServicePackage = package, ScheduledStartAt = scheduledStart,
                ScheduledEndAt = scheduledStart.AddMinutes(calculatedDurationMinutes), Status = BookingStatus.Draft,
                CleanerSelectionMode = selectionMode, RequiredWorkers = requiredWorkers,
                BuildingType = request.BuildingType, BuildingCondition = request.BuildingCondition, HasFurniture = request.HasFurniture,
                AreaSquareMeters = request.AreaSquareMeters, BasePrice = basePrice, SelectionFee = occurrenceSelectionFee,
                ExtraChargeTotal = extraChargeTotal, EstimatedTotal = basePrice + extraChargeTotal + occurrenceSelectionFee,
                IsRecurring = request.IsRecurring, RecurrenceRule = recurrenceRule, RecurringContract = contract, OccurrenceNumber = occurrenceNumber,
                FacilityName = group.Slug == "don-dep-buong-phong" ? request.FacilityName!.Trim() : null,
                ContactName = group.Slug == "don-dep-buong-phong" ? request.ContactName!.Trim() : null,
                ContactPhone = group.Slug == "don-dep-buong-phong" ? Regex.Replace(request.ContactPhone!, @"\s+", "") : null,
                AccommodationType = group.Slug == "don-dep-buong-phong" ? request.AccommodationType : null,
                CustomerRequest = string.IsNullOrWhiteSpace(request.CustomerRequest) ? null : request.CustomerRequest.Trim()
            };
            if (glassCleaningPrice > 0) item.ExtraCharges.Add(new BookingExtraCharge { Description = "Lau kính", Amount = glassCleaningPrice, Status = ExtraChargeStatus.Approved, CustomerRespondedAt = DateTimeOffset.UtcNow });
            if (carpetVacuumPrice > 0) item.ExtraCharges.Add(new BookingExtraCharge { Description = "Hút bụi thảm văn phòng", Amount = carpetVacuumPrice, Status = ExtraChargeStatus.Approved, CustomerRespondedAt = DateTimeOffset.UtcNow });
            foreach (var charge in hospitalityCharges) item.ExtraCharges.Add(new BookingExtraCharge { Description = charge.Description, Amount = charge.Amount, Status = ExtraChargeStatus.Approved, CustomerRespondedAt = DateTimeOffset.UtcNow });
            var itemDeposit = RoundToThousand(item.EstimatedTotal * .30m);
            item.Payment = new Payment { Method = PaymentMethod.BankTransfer, Status = PaymentStatus.Pending, Amount = item.EstimatedTotal, DepositAmount = itemDeposit, RemainingAmount = item.EstimatedTotal - itemDeposit };
            return item;
        }

        Booking booking;
        decimal depositAmount;
        decimal responseTotal;
        decimal responseRemaining;
        string? contractCode = null;
        int occurrenceCount = 1;
        if (request.IsRecurring)
        {
            var starts = BuildRecurringStarts(request.ScheduledStartAt, request.RecurrenceDays!, request.RecurrenceStartTime!, request.RecurrenceMonths!.Value);
            if (starts.Count == 0) return BadRequest(new { message = "Không tạo được lượt làm việc nào trong thời hạn hợp đồng." });
            var contract = new RecurringServiceContract
            {
                Code = $"HD-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid():N}"[..19].ToUpperInvariant(), Customer = customer,
                ServiceGroup = group, ServicePackage = package!, Status = RecurringContractStatus.PendingDeposit,
                RecurrenceRule = recurrenceRule!, StartsAt = starts[0], EndsAt = starts[^1].AddMinutes(calculatedDurationMinutes),
                ContractMonths = request.RecurrenceMonths.Value, TotalOccurrences = starts.Count
            };
            for (var index = 0; index < starts.Count; index++)
                contract.Occurrences.Add(CreateOccurrence(starts[index], index + 1, index == 0 ? selectionFee : 0, contract));
            contract.EstimatedTotal = contract.Occurrences.Sum(x => x.EstimatedTotal);
            contract.DepositAmount = contract.Occurrences.Sum(x => x.Payment!.DepositAmount);
            contract.RemainingAmount = contract.EstimatedTotal - contract.DepositAmount;
            db.RecurringServiceContracts.Add(contract);
            booking = contract.Occurrences.First();
            depositAmount = contract.DepositAmount;
            responseTotal = contract.EstimatedTotal;
            responseRemaining = contract.RemainingAmount;
            contractCode = contract.Code;
            occurrenceCount = contract.TotalOccurrences;
        }
        else
        {
            booking = CreateOccurrence(request.ScheduledStartAt, null, selectionFee);
            db.Bookings.Add(booking);
            depositAmount = booking.Payment!.DepositAmount;
            responseTotal = booking.EstimatedTotal;
            responseRemaining = booking.Payment.RemainingAmount;
        }
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return CreatedAtAction(nameof(GetByCode), new { code = booking.Code }, new { booking.Id, booking.Code, ContractCode = contractCode, booking.Status, EstimatedTotal = responseTotal, DepositAmount = depositAmount, RemainingAmount = responseRemaining, OccurrenceCount = occurrenceCount, InvitedPartners = 0, message = request.IsRecurring ? $"Hợp đồng đã tạo {occurrenceCount} lượt làm việc. Vui lòng thanh toán cọc để bắt đầu tìm Tasker cố định." : "Đơn đã được tạo. Vui lòng thanh toán cọc để bắt đầu tìm Tasker." });
    }

    [HttpGet("{code}")]
    public async Task<IActionResult> GetByCode(string code, CancellationToken cancellationToken)
    {
        var booking = await db.Bookings.AsNoTracking().Where(x => x.Code == code).Select(x => new { x.Id, x.Code, x.Status, x.ScheduledStartAt, x.EstimatedTotal }).SingleOrDefaultAsync(cancellationToken);
        return booking is null ? NotFound() : Ok(booking);
    }

    private static AreaTier GetAreaTier(decimal area) => area switch
    {
        < 60 => AreaTier.Under60,
        <= 80 => AreaTier.From60To80,
        <= 100 => AreaTier.From81To100,
        <= 500 => AreaTier.Custom101To500,
        _ => throw new ArgumentOutOfRangeException(nameof(area), "Diện tích phải từ 1 đến 500m².")
    };

    private static decimal RoundToThousand(decimal value) => Math.Round(value / 1_000m) * 1_000m;

    private static List<DateTimeOffset> BuildRecurringStarts(DateTimeOffset firstStart, IReadOnlyList<string> requestedDays, string timeText, int months)
    {
        var dayMap = new Dictionary<string, DayOfWeek>(StringComparer.OrdinalIgnoreCase)
        {
            ["mon"] = DayOfWeek.Monday, ["tue"] = DayOfWeek.Tuesday, ["wed"] = DayOfWeek.Wednesday,
            ["thu"] = DayOfWeek.Thursday, ["fri"] = DayOfWeek.Friday, ["sat"] = DayOfWeek.Saturday, ["sun"] = DayOfWeek.Sunday
        };
        var selectedDays = requestedDays.Select(day => dayMap[day]).ToHashSet();
        var time = TimeOnly.ParseExact(timeText, "HH:mm", CultureInfo.InvariantCulture);
        var vietnamOffset = TimeSpan.FromHours(7);
        var firstLocal = firstStart.ToOffset(vietnamOffset);
        var firstDate = DateOnly.FromDateTime(firstLocal.DateTime);
        var endDate = firstDate.AddMonths(months);
        var result = new List<DateTimeOffset>();
        for (var date = firstDate; date < endDate; date = date.AddDays(1))
        {
            if (!selectedDays.Contains(date.DayOfWeek)) continue;
            result.Add(new DateTimeOffset(date.Year, date.Month, date.Day, time.Hour, time.Minute, 0, vietnamOffset).ToUniversalTime());
        }
        return result;
    }

}
