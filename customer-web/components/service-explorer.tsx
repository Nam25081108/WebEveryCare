"use client";

import Image from "next/image";
import { useRouter } from "next/navigation";
import { useEffect, useState, type CSSProperties } from "react";
import {
  AirVent, ArrowRight, Baby, BedDouble,
  BriefcaseBusiness, Building2, CookingPot,
  HeartHandshake, Home, Shirt, ShieldCheck, ShieldPlus,
  ShoppingBasket, ShowerHead, Sparkles,
  Stethoscope, WandSparkles, WashingMachine, Wrench, X,
  type LucideIcon,
} from "lucide-react";
import { Reveal } from "@/components/reveal";
import { serviceCategories, type UtilityService } from "@/lib/service-catalog";
import {
  deepCleaningBenefits,
  deepCleaningLongDescription,
  professionalCleaningBenefits,
  professionalCleaningLongDescription,
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

const categoryImages: Record<string, string> = {
  cleaning: "/images/everycare-hero.png",
  business: "/images/everycare-office-team.png",
  care: "/images/everycare-care.png",
  beauty: "/images/everycare-beauty.png",
  appliances: "/images/everycare-technical.png",
  advanced: "/images/everycare-advanced.png",
};

const businessServiceContent: Record<string, { introduction:string; benefits:ServiceBenefit[] }> = {
  "don-dep-van-phong-dinh-ky": {
    introduction: "Dịch vụ dọn dẹp văn phòng định kỳ giúp doanh nghiệp chủ động lựa chọn hình thức theo buổi, theo ngày hoặc theo lịch cố định hằng tháng. EveryCare kết nối đội ngũ phù hợp với diện tích, thời lượng và tần suất làm việc của từng văn phòng.",
    benefits: [
      { title:"Lịch làm việc linh hoạt", description:"Chọn dịch vụ theo buổi khi phát sinh nhu cầu hoặc thiết lập lịch cố định theo tuần với gói tháng." },
      { title:"Nhân sự phù hợp quy mô", description:"Số lượng Tasker và thời lượng được đề xuất theo diện tích văn phòng để đảm bảo hiệu quả công việc." },
      { title:"Chi phí minh bạch", description:"Giá gói và các dịch vụ thêm được hiển thị rõ ràng trước khi khách hàng xác nhận đặt lịch." },
      { title:"Ưu tiên Tasker cố định", description:"Gói tháng hỗ trợ duy trì nhân sự ổn định, giảm thời gian đăng việc và thanh toán nhiều lần." },
    ],
  },
  "don-dep-buong-phong": {
    introduction: "Dịch vụ dọn dẹp buồng phòng dành cho căn hộ cho thuê, homestay và khách sạn cần chuẩn bị không gian sạch sẽ, gọn gàng trước khi đón khách mới.",
    benefits: [
      { title:"Sẵn sàng đón khách", description:"Làm sạch và sắp xếp lại buồng phòng theo lịch nhận, trả phòng của cơ sở lưu trú." },
      { title:"Quy trình rõ ràng", description:"Các khu vực phòng ngủ, phòng tắm và bề mặt thường xuyên sử dụng được kiểm tra theo phạm vi công việc." },
      { title:"Linh hoạt theo số phòng", description:"Điều phối nhân sự phù hợp với số lượng phòng và thời gian bàn giao thực tế." },
    ],
  },
  "ve-sinh-van-phong-chuyen-sau": {
    introduction: "Dịch vụ vệ sinh văn phòng chuyên sâu phù hợp với không gian quy mô lớn, khu vực có nhiều bụi bẩn hoặc cần xử lý kỹ bằng thiết bị và dụng cụ chuyên dụng.",
    benefits: [
      { title:"Làm sạch chuyên sâu", description:"Xử lý kỹ sàn, kính, khu vực chung và các vị trí tích tụ bụi bẩn lâu ngày." },
      { title:"Thiết bị phù hợp", description:"Phương án dụng cụ và hóa chất được lựa chọn theo bề mặt và hiện trạng văn phòng." },
      { title:"Khảo sát theo thực tế", description:"Khối lượng nhân sự, thời gian và phạm vi công việc được xác nhận trước khi triển khai." },
    ],
  },
};

export function ServiceExplorer() {
  const [selected, setSelected] = useState<SelectedService | null>(null);
  const [pickerOpen, setPickerOpen] = useState(false);
  const [storedGroups, setStoredGroups] = useState<StoredGroup[]>([]);
  const [activeSlugs,setActiveSlugs]=useState<Set<string>|null>(null);

  useEffect(() => {
    fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL??"http://localhost:5185"}/api/service-groups`,{cache:"no-store"}).then(response=>response.ok?response.json():Promise.reject()).then((groups:Array<{slug:string;name:string;description:string}>)=>{setStoredGroups(groups.map(group=>({id:group.slug,name:group.name,longDescription:group.description})));setActiveSlugs(new Set(groups.map(group=>group.slug)))}).catch(()=>{setStoredGroups([]);setActiveSlugs(null)});
  }, []);

  useEffect(() => {
    const openFromHash = () => {
      if (window.location.hash === "#chon-dich-vu") setPickerOpen(true);
    };

    const openFromClick = (event: MouseEvent) => {
      if (event.button !== 0 || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;

      const target = event.target instanceof Element
        ? event.target.closest<HTMLAnchorElement>('a[href="/#chon-dich-vu"], a[href="#chon-dich-vu"]')
        : null;
      if (!target) return;

      event.preventDefault();
      window.history.replaceState(
        window.history.state,
        "",
        `${window.location.pathname}${window.location.search}#chon-dich-vu`,
      );
      setPickerOpen(true);
    };

    openFromHash();
    window.addEventListener("hashchange", openFromHash);
    document.addEventListener("click", openFromClick, true);

    return () => {
      window.removeEventListener("hashchange", openFromHash);
      document.removeEventListener("click", openFromClick, true);
    };
  }, []);

  useEffect(() => {
    if (!selected && !pickerOpen) return;

    const close = (event: KeyboardEvent) => {
      if (event.key !== "Escape") return;
      if (selected) {
        setSelected(null);
      } else {
        closePicker();
      }
    };

    document.body.style.overflow = "hidden";
    window.addEventListener("keydown", close);

    return () => {
      document.body.style.overflow = "";
      window.removeEventListener("keydown", close);
    };
  }, [pickerOpen, selected]);

  function clearPickerHash() {
    if (window.location.hash === "#chon-dich-vu") {
      window.history.replaceState(window.history.state, "", `${window.location.pathname}${window.location.search}`);
    }
  }

  function closePicker() {
    setPickerOpen(false);
    clearPickerHash();
  }

  function showService(service: UtilityService, category: (typeof serviceCategories)[number]) {
    setPickerOpen(false);
    clearPickerHash();
    setSelected({ ...service, category });
  }

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
                    {category.services.filter(service=>activeSlugs===null||activeSlugs.has(service.slug)).map((service) => {
                      const Icon = icons[service.icon] ?? Sparkles;

                      return (
                        <button
                          type="button"
                          className="ec-service-item"
                          key={service.slug}
                          onClick={() => showService(service, category)}
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

      {pickerOpen && (
        <div
          className="ec-service-picker-backdrop"
          role="presentation"
          onMouseDown={(event) => {
            if (event.currentTarget === event.target) closePicker();
          }}
        >
          <section
            className="ec-service-picker"
            role="dialog"
            aria-modal="true"
            aria-labelledby="service-picker-title"
          >
            <header>
              <div>
                <span>HỆ SINH THÁI EVERYCARE</span>
                <h2 id="service-picker-title">Bạn đang cần dịch vụ nào?</h2>
                <p>Chọn một dịch vụ bên dưới để xem thông tin mô tả và tiếp tục đặt lịch.</p>
              </div>
              <button type="button" onClick={closePicker} aria-label="Đóng danh sách dịch vụ"><X/></button>
            </header>

            <div className="ec-service-picker-grid">
              {serviceCategories.map((category, index) => (
                <article
                  key={category.id}
                  style={{ "--category-accent": category.accent, "--category-soft": category.soft } as CSSProperties}
                >
                  <div className="ec-service-picker-heading">
                    <span>0{index + 1}</span>
                    <div>
                      <small>{category.eyebrow}</small>
                      <h3>{category.name}</h3>
                    </div>
                  </div>
                  <div className="ec-service-picker-list">
                    {category.services.filter(service=>activeSlugs===null||activeSlugs.has(service.slug)).map((service) => {
                      const Icon = icons[service.icon] ?? Sparkles;

                      return (
                        <button
                          type="button"
                          key={service.slug}
                          onClick={() => showService(service, category)}
                        >
                          <i><Icon/></i>
                          <span>
                            <strong>{service.name}</strong>
                            <small>{service.description}</small>
                          </span>
                          <ArrowRight/>
                        </button>
                      );
                    })}
                  </div>
                </article>
              ))}
            </div>
          </section>
        </div>
      )}

      {selected && (
        <ServiceDescriptionModal
          service={selected}
          stored={storedGroups.find((group) => group.id === selected.slug)}
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
  const isDeepCleaning = service.bookingService === "deep";
  const isProfessionalCleaning = service.bookingService === "professional";
  const businessContent = businessServiceContent[service.slug];
  const introduction =
    stored?.longDescription?.trim() ||
    (isRoom
      ? roomServiceLongDescription
      : isDeepCleaning
        ? deepCleaningLongDescription
        : isProfessionalCleaning
          ? professionalCleaningLongDescription
          : businessContent?.introduction ?? service.description);
  const storedBenefits = stored?.benefits?.filter(
    (item) => item.title.trim() || item.description.trim(),
  );
  const benefits = storedBenefits?.length
    ? storedBenefits
    : isRoom
      ? roomServiceBenefits
      : isDeepCleaning
        ? deepCleaningBenefits
        : isProfessionalCleaning
          ? professionalCleaningBenefits
          : businessContent?.benefits ?? [];
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

          {introduction || benefits.length > 0 ? (
            <p className="ec-service-modal-prose">
              {introduction}
              {introduction && benefits.length > 0 ? " " : null}
              {benefits.map((benefit, index) => (
                <span key={`${benefit.title}-${index}`}>
                  <strong>{benefit.title}:</strong> {benefit.description}
                  {index < benefits.length - 1 ? " " : null}
                </span>
              ))}
            </p>
          ) : (
            <div className="ec-service-modal-empty">
              <Sparkles/>
              <div>
                <strong>Nội dung giới thiệu đang được cập nhật</strong>
                <p>Bạn có thể bổ sung mô tả và các lợi ích của dịch vụ này trong trang quản trị EveryCare.</p>
              </div>
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
