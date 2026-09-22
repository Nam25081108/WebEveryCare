using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using EveryCare.Api.Domain.Entities;
using EveryCare.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EveryCare.Api.Controllers;

public sealed record SaveServiceGroupRequest(string Name, string? Slug, string Description, string? CategorySlug, bool IsActive, bool IsComingSoon, bool IsProfessional);
public sealed record SaveServicePackageRequest(string Name, string? Slug, string? Description, decimal Price, int? DurationMinutes, int RequiredWorkers, decimal? MaximumAreaSquareMeters, int? MaximumRooms, bool IsActive);

[ApiController]
[Route("api/admin/service-groups")]
public sealed class AdminServiceGroupsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var items = await db.ServiceGroups.AsNoTracking().OrderBy(x => x.DisplayOrder).Select(group => new
        {
            group.Id, group.Name, group.Slug, group.Description, group.CategorySlug, group.IsActive, group.IsComingSoon, group.IsProfessional,
            Packages = group.Packages.OrderBy(x => x.DisplayOrder).Select(package => new { package.Id, package.Name, package.Slug, package.Description, package.Price, package.DurationMinutes, package.RequiredWorkers, package.MaximumAreaSquareMeters, package.MaximumRooms, package.IsActive })
        }).ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create(SaveServiceGroupRequest request, CancellationToken cancellationToken)
    {
        var error = ValidateGroup(request);
        if (error is not null) return BadRequest(new { message = error });
        var slug = await UniqueGroupSlug(request.Slug, request.Name, null, cancellationToken);
        var group = new ServiceGroup
        {
            Name = request.Name.Trim(), Slug = slug, Description = request.Description.Trim(), CategorySlug = string.IsNullOrWhiteSpace(request.CategorySlug) ? "cleaning" : request.CategorySlug.Trim(),
            IsActive = request.IsActive, IsComingSoon = request.IsComingSoon, IsProfessional = request.IsProfessional,
            DisplayOrder = (await db.ServiceGroups.IgnoreQueryFilters().MaxAsync(x => (int?)x.DisplayOrder, cancellationToken) ?? 0) + 1
        };
        db.ServiceGroups.Add(group);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = group.Id }, new { group.Id, group.Slug, message = "Đã thêm nhóm dịch vụ vào danh mục thực tế." });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, SaveServiceGroupRequest request, CancellationToken cancellationToken)
    {
        var error = ValidateGroup(request);
        if (error is not null) return BadRequest(new { message = error });
        var group = await db.ServiceGroups.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (group is null) return NotFound(new { message = "Không tìm thấy nhóm dịch vụ." });
        group.Name = request.Name.Trim(); group.Description = request.Description.Trim();
        group.CategorySlug = string.IsNullOrWhiteSpace(request.CategorySlug) ? group.CategorySlug : request.CategorySlug.Trim();
        group.IsActive = request.IsActive; group.IsComingSoon = request.IsComingSoon; group.IsProfessional = request.IsProfessional;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Đã cập nhật nhóm dịch vụ trên hệ thống." });
    }

    [HttpPatch("{id:guid}/active")]
    public async Task<IActionResult> Toggle(Guid id, CancellationToken cancellationToken)
    {
        var group = await db.ServiceGroups.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (group is null) return NotFound(new { message = "Không tìm thấy nhóm dịch vụ." });
        group.IsActive = !group.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { group.IsActive, message = group.IsActive ? "Đã mở lại nhóm dịch vụ." : "Đã khóa nhóm dịch vụ." });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var group = await db.ServiceGroups.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (group is null) return NotFound();
        if (await db.Bookings.AnyAsync(x => x.ServiceGroupId == id, cancellationToken))
            return Conflict(new { message = "Nhóm đã có đơn hàng nên chỉ có thể khóa, không thể xóa." });
        group.IsDeleted = true; group.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{groupId:guid}/packages")]
    public async Task<IActionResult> CreatePackage(Guid groupId, SaveServicePackageRequest request, CancellationToken cancellationToken)
    {
        var group = await db.ServiceGroups.Include(x => x.Packages).SingleOrDefaultAsync(x => x.Id == groupId, cancellationToken);
        if (group is null) return NotFound(new { message = "Không tìm thấy nhóm dịch vụ." });
        var error = ValidatePackage(request); if (error is not null) return BadRequest(new { message = error });
        var package = new ServicePackage
        {
            Name = request.Name.Trim(), Slug = await UniquePackageSlug(groupId, request.Slug, request.Name, null, cancellationToken), Description = request.Description?.Trim(),
            Price = request.Price, DurationMinutes = request.DurationMinutes, RequiredWorkers = request.RequiredWorkers, MaximumAreaSquareMeters = request.MaximumAreaSquareMeters,
            MaximumRooms = request.MaximumRooms, IsActive = request.IsActive, DisplayOrder = (group.Packages.Max(x => (int?)x.DisplayOrder) ?? 0) + 1
        };
        group.Packages.Add(package);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { package.Id, package.Slug, message = "Đã thêm gói dịch vụ." });
    }

    [HttpPut("{groupId:guid}/packages/{id:guid}")]
    public async Task<IActionResult> UpdatePackage(Guid groupId, Guid id, SaveServicePackageRequest request, CancellationToken cancellationToken)
    {
        var error = ValidatePackage(request); if (error is not null) return BadRequest(new { message = error });
        var package = await db.ServicePackages.SingleOrDefaultAsync(x => x.Id == id && x.ServiceGroupId == groupId, cancellationToken);
        if (package is null) return NotFound(new { message = "Không tìm thấy gói dịch vụ." });
        package.Name = request.Name.Trim(); package.Description = request.Description?.Trim(); package.Price = request.Price;
        package.DurationMinutes = request.DurationMinutes; package.RequiredWorkers = request.RequiredWorkers; package.MaximumAreaSquareMeters = request.MaximumAreaSquareMeters;
        package.MaximumRooms = request.MaximumRooms; package.IsActive = request.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Đã cập nhật gói dịch vụ trên hệ thống." });
    }

    [HttpDelete("{groupId:guid}/packages/{id:guid}")]
    public async Task<IActionResult> DeletePackage(Guid groupId, Guid id, CancellationToken cancellationToken)
    {
        var package = await db.ServicePackages.SingleOrDefaultAsync(x => x.Id == id && x.ServiceGroupId == groupId, cancellationToken);
        if (package is null) return NotFound();
        if (await db.Bookings.AnyAsync(x => x.ServicePackageId == id, cancellationToken))
        {
            package.IsActive = false;
            await db.SaveChangesAsync(cancellationToken);
            return Ok(new { message = "Gói đã có đơn hàng nên được chuyển sang trạng thái khóa." });
        }
        package.IsDeleted = true; package.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static string? ValidateGroup(SaveServiceGroupRequest request) => string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Description) ? "Tên và mô tả nhóm dịch vụ là bắt buộc." : null;
    private static string? ValidatePackage(SaveServicePackageRequest request) => string.IsNullOrWhiteSpace(request.Name) || request.Price < 0 || request.RequiredWorkers < 1 || request.DurationMinutes is < 1 ? "Thông tin gói, giá, thời lượng hoặc số người không hợp lệ." : null;
    private async Task<string> UniqueGroupSlug(string? requested, string name, Guid? excludingId, CancellationToken token)
    {
        var root = Slugify(string.IsNullOrWhiteSpace(requested) ? name : requested); var slug = root; var suffix = 2;
        while (await db.ServiceGroups.IgnoreQueryFilters().AnyAsync(x => x.Id != excludingId && x.Slug == slug, token)) slug = $"{root}-{suffix++}";
        return slug;
    }
    private async Task<string> UniquePackageSlug(Guid groupId, string? requested, string name, Guid? excludingId, CancellationToken token)
    {
        var root = Slugify(string.IsNullOrWhiteSpace(requested) ? name : requested); var slug = root; var suffix = 2;
        while (await db.ServicePackages.IgnoreQueryFilters().AnyAsync(x => x.ServiceGroupId == groupId && x.Id != excludingId && x.Slug == slug, token)) slug = $"{root}-{suffix++}";
        return slug;
    }
    private static string Slugify(string value)
    {
        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var character in normalized)
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark) builder.Append(character == 'đ' ? 'd' : character);
        return Regex.Replace(builder.ToString().Normalize(NormalizationForm.FormC), @"[^a-z0-9]+", "-").Trim('-');
    }
}
