import type { ServicePlan } from "@/features/booking/lib/mock-data";

export type ProfessionalConfig = {
  building: "house" | "office";
  condition: "old" | "new";
  furnished: boolean;
  areaTier: "under60" | "60to80" | "81to100" | "custom";
  customArea: number;
};

export const defaultProfessionalConfig: ProfessionalConfig = {
  building: "house",
  condition: "old",
  furnished: false,
  areaTier: "under60",
  customArea: 101,
};

const fixedPrices = {
  house: { old: [2100000, 2700000, 3300000], new: [1800000, 2400000, 3000000], customOld: 33000, customNew: 30000 },
  office: { old: [2600000, 3400000, 4200000], new: [2300000, 3000000, 3800000], customOld: 42000, customNew: 38000 },
};

export function isProfessionalConfigValid(config: ProfessionalConfig) {
  return config.areaTier !== "custom" || (Number.isFinite(config.customArea) && config.customArea >= 101 && config.customArea <= 500);
}

export function calculateProfessionalPlan(config: ProfessionalConfig): ServicePlan {
  const priceConfig = fixedPrices[config.building];
  const tierIndex = { under60: 0, "60to80": 1, "81to100": 2 }[config.areaTier as Exclude<ProfessionalConfig["areaTier"], "custom">];
  let price = config.areaTier === "custom"
    ? config.customArea * (config.condition === "old" ? priceConfig.customOld : priceConfig.customNew)
    : priceConfig[config.condition][tierIndex];
  if (config.condition === "new" && config.furnished) price = Math.round(price * 1.25 / 10000) * 10000;
  const buildingLabel = config.building === "house" ? "Nhà ở" : "Công ty / văn phòng";
  const conditionLabel = config.condition === "old" ? "lâu năm" : "mới xây, sửa chữa";
  const areaLabel = config.areaTier === "under60" ? "dưới 60m²" : config.areaTier === "60to80" ? "60–80m²" : config.areaTier === "81to100" ? "81–100m²" : `${config.customArea || 0}m²`;
  return {
    id: `professional-${config.building}-${config.condition}-${config.areaTier}`,
    name: `${buildingLabel} ${conditionLabel}`,
    description: areaLabel,
    meta: `${areaLabel}${config.condition === "new" ? config.furnished ? " • Có nội thất" : " • Không nội thất" : ""}`,
    price: isProfessionalConfigValid(config) ? price : null,
  };
}
