"use client";

import Link from "next/link";
import { EveryCareLogo } from "@/components/everycare-logo";

const CUSTOMER_WEB = process.env.NEXT_PUBLIC_CUSTOMER_WEB_URL ?? "http://localhost:3000";

export function PartnerHeader() {
  return (
    <header className="site-header partner-site-header">
      <div className="shell header-inner">
        <EveryCareLogo/>
        <nav className="partner-site-nav" aria-label="Điều hướng đối tác">
          <Link href="/">Đăng ký đối tác</Link>
          <Link href="/dang-nhap">Đăng nhập</Link>
          <a href={CUSTOMER_WEB}>Trang khách hàng</a>
        </nav>
      </div>
    </header>
  );
}
