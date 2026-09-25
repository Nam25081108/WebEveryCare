"use client";

import Image from "next/image";
import { useRouter } from "next/navigation";
import { useEffect, useState, type CSSProperties } from "react";
import {
  AirVent, ArrowRight, Baby, BadgeDollarSign, BedDouble,
  BriefcaseBusiness, Building2, Clock3, CookingPot,
  HeartHandshake, Home, Shirt, ShieldCheck, ShieldPlus,
  ShoppingBasket, ShowerHead, SlidersHorizontal, Sparkles,
  Stethoscope, WandSparkles, WashingMachine, Wrench, X,
  type LucideIcon,
} from "lucide-react";
import { Reveal } from "@/components/reveal";
import { serviceCategories, type UtilityService } from "@/lib/service-catalog";
import {
  roomServiceBenefits,
  roomServiceLongDescription,
  type ServiceBenefit,
} from "@/lib/service-description-content";

type StoredGroup = {
  id: string;
  name?: string;
  longDescription?: string;
  benefits?: ServiceBenefit[];
};

type SelectedService = UtilityService & {
  category: (typeof serviceCategories)[number];
};

const icons: Record<string, LucideIcon> = {
  air: AirVent,
  baby: Baby,
  bed: BedDouble,
  briefcase: BriefcaseBusiness,
  building: Building2,
  cooking: CookingPot,
  heart: HeartHandshake,
  home: Home,
  shirt: Shirt,
  shield: ShieldPlus,
  basket: ShoppingBasket,
  shower: ShowerHead,
  sparkles: Sparkles,
  stethoscope: Stethoscope,
  wand: WandSparkles,
  washer: WashingMachine,
  wrench: Wrench,
};

const benefitIcons = [Clock3, BadgeDollarSign, ShieldCheck, SlidersHorizontal];

const categoryImages: Record<string, string> = {
  cleaning: "/images/everycare-hero.png",
  business: "/images/everycare-office-team.png",
  care: "/images/everycare-care.png",
  beauty: "/images/everycare-beauty.png",
  appliances: "/images/everycare-technical.png",
  advanced: "/images/everycare-advanced.png",
};

function storageId(service: UtilityService) {
  return service.bookingService ?? service.slug;
}

export function ServiceExplorer() {
  const [selected, setSelected] = useState<SelectedService | null>(null);
  const [storedGroups, setStoredGroups] = useState<StoredGroup[]>([]);

  useEffect(() => {
    try {
      setStoredGroups(
        JSON.parse(localStorage.getItem("everycare_admin_services_v2") ?? "[]") as StoredGroup[],
      );
    } catch {
      setStoredGroups([]);
    }
  }, []);

  useEffect(() => {
    if (!selected) return;

    const close = (event: KeyboardEvent) => {
      if (event.key === "Escape") setSelected(null);
    };

    document.body.style.overflow = "hidden";
    window.addEventListener("keydown", close);

    return () => {
      document.body.style.overflow = "";
      window.removeEventListener("keydown", close);
    };
  }, [selected]);

  return (
    <>
      <section className="ec-services ec-services-luxe" id="dich-vu">
        <div className="shell">
          <Reveal className="ec-section-heading">
            <div>
              <span>HỆ SINH THÁI EVERYCARE</span>
              <h2>Mọi tiện ích thiết yếu,<br/><em>trong một nơi.</em></h2>
            </div>
            <p>Chọn một dịch vụ để xem giới thiệu chi tiết, lợi ích và cách EveryCare đồng hành cùng bạn.</p>
          </Reveal>

          <div className="ec-category-grid">
            {serviceCategories.map((category, index) => (
              <Reveal key={category.id} delay={(index % 2) * 100}>
                <article
                  className={`ec-category-card ec-category-${category.id}`}
                  style={{ "--category-accent": category.accent, "--category-soft": category.soft } as CSSProperties}
                >
                  <div className="ec-category-visual">
                    <Image
                      src={categoryImages[category.id]}
                      alt={category.name}
                      fill
                      sizes="(max-width: 800px) 100vw, 50vw"
                    />
                    <span>0{index + 1}</span>
                  </div>

                  <header>
                    <span className="ec-category-number">0{index + 1}</span>
                    <div>
                      <small>{category.eyebrow}</small>
                      <h3>{category.name}</h3>
                      <p>{category.description}</p>
                    </div>
                  </header>

                  <div className="ec-service-list">
                    {category.services.map((service) => {
                      const Icon = icons[service.icon] ?? Sparkles;

                      return (
                        <button
                          type="button"
                          className="ec-service-item"
                          key={service.slug}
                          onClick={() => setSelected({ ...service, category })}
                        >
                          <span><Icon/></span>
                          <div>
                            <strong>{service.name}</strong>
                            <small>{service.description}</small>
                          </div>
                          <em>Xem chi tiết</em>
                          <ArrowRight/>
                        </button>
                      );
                    })}
                  </div>
                </article>
              </Reveal>
            ))}
          </div>
        </div>
      </section>

      {selected && (
        <ServiceDescriptionModal
          service={selected}
          stored={storedGroups.find((group) => group.id === storageId(selected))}
          onClose={() => setSelected(null)}
        />
      )}
    </>
  );
}

function ServiceDescriptionModal({
  service,
  stored,
  onClose,
}: {
  service: SelectedService;
  stored?: StoredGroup;
  onClose: () => void;
}) {
  const router = useRouter();
  const isRoom = service.bookingService === "room";
  const introduction =
    stored?.longDescription?.trim() || (isRoom ? roomServiceLongDescription : "");
  const storedBenefits = stored?.benefits?.filter(
    (item) => item.title.trim() || item.description.trim(),
  );
  const benefits = storedBenefits?.length
    ? storedBenefits
    : isRoom
      ? roomServiceBenefits
      : [];
  const bookingHref = service.available
    ? `/dat-lich?service=${service.bookingService}&start=address`
    : `/dich-vu/${service.slug}`;

  return (
    <div
      className="ec-service-modal-backdrop"
      role="presentation"
      onMouseDown={(event) => {
        if (event.currentTarget === event.target) onClose();
      }}
    >
      <article
        className="ec-service-modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="service-modal-title"
      >
        <button className="ec-service-modal-close" onClick={onClose} aria-label="Đóng cửa sổ">
          <X/>
        </button>

        <div className="ec-service-modal-media">
          <Image
            src={categoryImages[service.category.id]}
            alt={service.name}
            fill
            sizes="(max-width: 850px) 100vw, 45vw"
          />
          <div/>
          <span><Sparkles/> {service.category.name}</span>
        </div>

        <div className="ec-service-modal-content">
          <span className="ec-service-modal-eyebrow">DỊCH VỤ EVERYCARE</span>
          <h2 id="service-modal-title">{stored?.name || service.name}</h2>

          {introduction ? (
            <p className="ec-service-modal-intro">{introduction}</p>
          ) : (
            <div className="ec-service-modal-empty">
              <Sparkles/>
              <div>
                <strong>Nội dung giới thiệu đang được cập nhật</strong>
                <p>Bạn có thể bổ sung mô tả và các lợi ích của dịch vụ này trong trang quản trị EveryCare.</p>
              </div>
            </div>
          )}

          {benefits.length > 0 && (
            <div className="ec-benefit-list">
              {benefits.map((benefit, index) => {
                const Icon = benefitIcons[index % benefitIcons.length];

                return (
                  <section key={`${benefit.title}-${index}`}>
                    <i><Icon/></i>
                    <div>
                      <h3>{benefit.title}</h3>
                      <p>{benefit.description}</p>
                    </div>
                    <span>{String(index + 1).padStart(2, "0")}</span>
                  </section>
                );
              })}
            </div>
          )}

          <footer>
            <div>
              <ShieldCheck/>
              <span>
                <strong>Yên tâm cùng EveryCare</strong>
                <small>Thông tin minh bạch · Đối tác được xét duyệt</small>
              </span>
            </div>
            <button
              type="button"
              onClick={() => {
                router.push(bookingHref);
                onClose();
              }}
            >
              {service.available ? "Đặt dịch vụ" : "Xem thông tin"}<ArrowRight/>
            </button>
          </footer>
        </div>
      </article>
    </div>
  );
}
