using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EveryCare.Api.Infrastructure.Persistence;

namespace EveryCare.Api.Controllers;

[ApiController]
[Route("api/service-groups")]
public sealed class ServiceGroupsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var groups = await db.ServiceGroups
            .AsNoTracking()
            .Where(group => group.IsActive)
            .OrderBy(group => group.DisplayOrder)
            .Select(group => new
            {
                group.Id,
                group.Name,
                group.Slug,
                group.Description,
                group.CategorySlug,
                group.IsProfessional,
                group.IsComingSoon,
                Packages = group.Packages.Where(package => package.IsActive).OrderBy(package => package.DisplayOrder).Select(package => new
                {
                    package.Id, package.Name, package.Slug, package.Description, package.Price, package.DurationMinutes,
                    package.RequiredWorkers, package.MaximumAreaSquareMeters, package.MaximumRooms,
                    WorkItems = package.WorkItems.Where(item => item.IsActive).OrderBy(item => item.DisplayOrder).Select(item => new { item.Area, item.Description })
                }),
                ProcessSteps = group.ProcessSteps.OrderBy(step => step.DisplayOrder).Select(step => new { step.Title, step.Description }),
                Tools = group.Tools.Where(tool => tool.IsActive).OrderBy(tool => tool.DisplayOrder).Select(tool => new { tool.Name, tool.Description, tool.ImageUrl }),
                PricingRules = group.ProfessionalPricingRules.Where(rule => rule.IsActive).Select(rule => new { rule.BuildingType, rule.BuildingCondition, rule.AreaTier, rule.MinimumAreaSquareMeters, rule.MaximumAreaSquareMeters, rule.FixedPrice, rule.PricePerSquareMeter, rule.FurnishedMultiplier })
            })
            .ToListAsync(cancellationToken);
        return Ok(groups);
    }
}
