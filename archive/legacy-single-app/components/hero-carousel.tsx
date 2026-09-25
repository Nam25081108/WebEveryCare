"use client";

import Image from "next/image";
import { useEffect, useState } from "react";

const slides = [
  { src: "/images/everycare-hero.png", alt: "Chuyên viên EveryCare chăm sóc không gian sống tại TP.HCM", label: "Chăm sóc nhà cửa" },
  { src: "/images/everycare-office-team.png", alt: "Đội ngũ EveryCare vệ sinh văn phòng chuyên nghiệp", label: "Dịch vụ doanh nghiệp" },
  { src: "/images/everycare-beauty.png", alt: "Chuyên viên EveryCare trang điểm cho khách hàng tại nhà", label: "Làm đẹp tại nhà" },
  { src: "/images/everycare-advanced.png", alt: "Trợ lý EveryCare hỗ trợ đi chợ và giặt giũ", label: "Tiện ích nâng cao" },
];

export function HeroCarousel() {
  const [active, setActive] = useState(0);

  useEffect(() => {
    const timer = window.setInterval(() => setActive((current) => (current + 1) % slides.length), 5000);
    return () => window.clearInterval(timer);
  }, []);

  return <>
    <div className="ec-hero-carousel" aria-live="polite">
      {slides.map((slide, index) => <Image
        key={slide.src}
        className={`ec-luxe-hero-image${active === index ? " active" : ""}`}
        src={slide.src}
        alt={slide.alt}
        fill
        priority={index === 0}
        sizes="100vw"
      />)}
    </div>
    <div className="ec-hero-slides" aria-label="Chọn hình ảnh giới thiệu">
      {slides.map((slide, index) => <button key={slide.src} className={active === index ? "active" : ""} onClick={() => setActive(index)} aria-label={`Xem ${slide.label}`} aria-current={active === index ? "true" : undefined}><span>{String(index + 1).padStart(2, "0")}</span><small>{slide.label}</small></button>)}
    </div>
  </>;
}
