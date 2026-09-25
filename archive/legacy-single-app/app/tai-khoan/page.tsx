"use client";

import Link from "next/link";
import { CalendarDays, ChevronRight, Heart, LogOut, MapPin, Phone, UserRound } from "lucide-react";
import { useRouter } from "next/navigation";
import { useEffect } from "react";
import { useAuth } from "@/components/auth-provider";
import { SiteHeader } from "@/components/site-header";

export default function AccountPage() {
  const { customer, isLoading, logout } = useAuth();
  const router = useRouter();
  useEffect(() => { if (!isLoading && !customer) router.replace("/dang-nhap?returnUrl=%2Ftai-khoan"); }, [customer, isLoading, router]);
  if (!customer) return <main className="subpage"><SiteHeader/><div className="auth-gate-loading"><p>Đang tải tài khoản...</p></div></main>;
  const initials = customer.fullName.split(" ").slice(-2).map((part) => part[0]).join("").toUpperCase();
  return <main className="subpage account-page"><SiteHeader/><div className="shell account-layout"><aside className="profile-card"><span className="profile-avatar">{initials}</span><h1>{customer.fullName}</h1><p><Phone/> {customer.phone}</p><span className="verified-customer">Tài khoản khách hàng</span><button onClick={() => { logout(); router.push("/"); }}><LogOut/> Đăng xuất</button></aside><section className="account-main"><div className="account-welcome"><div><span className="eyebrow">Tài khoản của tôi</span><h2>Xin chào, {customer.fullName.split(" ").at(-1)}!</h2><p>Quản lý lịch dọn và thông tin của bạn tại đây.</p></div><Link className="button" href="/dat-lich">Đặt lịch mới</Link></div><div className="account-menu-grid"><article><span><CalendarDays/></span><div><h3>Lịch dọn của tôi</h3><p>Theo dõi lịch sắp tới và lịch sử dịch vụ</p></div><ChevronRight/></article><article><span><MapPin/></span><div><h3>Địa chỉ đã lưu</h3><p>Thêm và quản lý các địa chỉ dọn dẹp</p></div><ChevronRight/></article><article><span><Heart/></span><div><h3>Người dọn yêu thích</h3><p>Ưu tiên những người bạn đã tin tưởng</p></div><ChevronRight/></article><article><span><UserRound/></span><div><h3>Thông tin cá nhân</h3><p>Cập nhật tên, email và mật khẩu</p></div><ChevronRight/></article></div></section></div></main>;
}
