"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { ChevronDown, LogOut, Menu, UserRound, X } from "lucide-react";
import { useState } from "react";
import { useAuth } from "@/components/auth-provider";
import { EveryCareLogo } from "@/components/everycare-logo";
import { PARTNER_WEB_URL } from "@/lib/site-urls";

export function SiteHeader(){
 const [open,setOpen]=useState(false);const [accountOpen,setAccountOpen]=useState(false);const pathname=usePathname();const {customer,isLoading,logout}=useAuth();const initials=customer?.fullName.split(" ").slice(-2).map(x=>x[0]).join("").toUpperCase();
 return <header className="site-header ec-site-header"><div className="shell header-inner"><EveryCareLogo/><nav className={open?"nav-links is-open":"nav-links"} aria-label="Điều hướng chính"><Link href="/#dich-vu" onClick={()=>setOpen(false)}>Dịch vụ</Link><Link href="/#quy-trinh" onClick={()=>setOpen(false)}>Cách hoạt động</Link><a href={PARTNER_WEB_URL} onClick={()=>setOpen(false)}>Trở thành đối tác</a></nav><div className="header-actions">{!isLoading&&!customer&&<Link className="text-button login-link" href="/dang-nhap">Đăng nhập</Link>}{!isLoading&&customer&&<div className="account-menu"><button className="account-trigger" onClick={()=>setAccountOpen(!accountOpen)}><span>{initials}</span><div><small>Xin chào</small><strong>{customer.fullName.split(" ").at(-1)}</strong></div><ChevronDown/></button>{accountOpen&&<div className="account-dropdown"><div className="account-summary"><span>{initials}</span><div><strong>{customer.fullName}</strong><small>{customer.phone}</small></div></div><Link href="/tai-khoan" onClick={()=>setAccountOpen(false)}><UserRound/>Tài khoản của tôi</Link><button onClick={()=>{logout();setAccountOpen(false)}}><LogOut/>Đăng xuất</button></div>}</div>}<Link className="button button-small ec-header-cta" href={pathname==="/"?"/#chon-dich-vu":"/dat-lich"}>Đặt dịch vụ</Link><button className="menu-button" aria-label="Mở menu" onClick={()=>setOpen(!open)}>{open?<X/>:<Menu/>}</button></div></div></header>;
}
