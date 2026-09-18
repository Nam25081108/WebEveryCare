import Link from "next/link";
import { Facebook, Instagram, Mail, Phone } from "lucide-react";
import { EveryCareLogo } from "@/components/everycare-logo";
import { PARTNER_WEB_URL } from "@/lib/site-urls";

export function Footer(){return <footer className="footer ec-footer"><div className="shell footer-grid"><div><EveryCareLogo light/><p>Nền tảng kết nối dịch vụ chăm sóc nhà cửa, con người và tiện ích gia đình tại TP.HCM.</p><div className="ec-footer-contact"><span><Phone/>1900 0000</span><span><Mail/>hotro@everycare.vn</span></div></div><div><h4>Khám phá</h4><a href="/#dich-vu">Tất cả dịch vụ</a><a href="/#quy-trinh">Cách hoạt động</a><Link href="/dat-lich">Đặt dịch vụ</Link></div><div><h4>Đồng hành</h4><a href={PARTNER_WEB_URL}>Đăng ký đối tác</a><a href={`${PARTNER_WEB_URL}/dang-nhap`}>Đăng nhập đối tác</a><Link href="/tai-khoan">Tài khoản khách hàng</Link></div><div><h4>Kết nối</h4><div className="socials"><a href="#" aria-label="Facebook"><Facebook size={18}/></a><a href="#" aria-label="Instagram"><Instagram size={18}/></a></div></div></div><div className="shell footer-bottom"><span>© 2026 EveryCare</span><span>Điều khoản · Quyền riêng tư</span></div></footer>}
