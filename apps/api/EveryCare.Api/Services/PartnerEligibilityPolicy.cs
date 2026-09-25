using EveryCare.Api.Domain.Entities;
using EveryCare.Api.Domain.Enums;

namespace EveryCare.Api.Services;

public static class PartnerEligibilityPolicy
{
    public static bool RequiresIndividual(string serviceGroupSlug, int requiredWorkers) =>
        serviceGroupSlug is "ve-sinh-phong-le" or "don-dep-buong-phong" ||
        serviceGroupSlug == "don-dep-van-phong-dinh-ky" && requiredWorkers == 1;

    public static bool IsEligible(PartnerProfile partner, string serviceGroupSlug, int requiredWorkers) =>
        RequiresIndividual(serviceGroupSlug, requiredWorkers)
            ? partner.PartnerType == PartnerType.Individual
            : partner.PartnerType == PartnerType.Team && partner.TeamSize >= requiredWorkers;
}
