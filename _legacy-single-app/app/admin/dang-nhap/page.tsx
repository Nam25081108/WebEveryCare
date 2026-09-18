"use client";

import { FormEvent, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { AlertCircle, ArrowRight, Eye, EyeOff, LoaderCircle, LockKeyhole, Mail, ShieldCheck, Sparkles } from "lucide-react";
import { useAdminAuth } from "@/components/admin-auth-provider";

export default function AdminLoginPage() {
  const { admin, isLoading, login } = useAdminAuth();
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");
  useEffect(() => { if (!isLoading && admin) router.replace("/admin"); }, [admin, isLoading, router]);

  async function submit(event: FormEvent) {
    event.preventDefault(); setError("");
    if (!email || !password) { setError("Vui lòng nhập email và mật khẩu quản trị."); return; }
    setSubmitting(true);
    try { await login(email, password); router.replace("/admin"); }
    catch (reason) { setError(reason instanceof Error ? reason.message : "Không thể đăng nhập."); }
    finally { setSubmitting(false); }
  }

  return <main className="admin-login-page"><section className="admin-login-brand"><div className="admin-login-copy"><span className="admin-logo"><i><Sparkles/></i>Every<b>Care</b></span><div><span className="admin-eyebrow">Hệ thống vận hành</span><h1>Quản lý dịch vụ<br/>trên một nền tảng.</h1><p>Theo dõi đơn hàng, quản lý đối tác, khách hàng và toàn bộ danh mục dịch vụ tại một nơi.</p></div><small>© 2026 EveryCare • Khu vực dành riêng cho quản trị viên</small></div></section><section className="admin-login-panel"><form onSubmit={submit} className="admin-login-form"><span className="admin-shield"><ShieldCheck/></span><h2>Đăng nhập quản trị</h2><p>Sử dụng tài khoản quản trị được cấp để tiếp tục.</p>{error && <div className="form-error-banner"><AlertCircle/>{error}</div>}<label><span>Email quản trị</span><div className="admin-input"><Mail/><input type="email" autoComplete="username" value={email} onChange={(event)=>setEmail(event.target.value)} placeholder="admin@everycare.vn"/></div></label><label><span>Mật khẩu</span><div className="admin-input"><LockKeyhole/><input type={showPassword ? "text" : "password"} autoComplete="current-password" value={password} onChange={(event)=>setPassword(event.target.value)} placeholder="Nhập mật khẩu"/><button type="button" onClick={()=>setShowPassword(!showPassword)}>{showPassword?<EyeOff/>:<Eye/>}</button></div></label><button className="button button-large full-button" disabled={submitting}>{submitting?<><LoaderCircle className="spin"/>Đang xác thực...</>:<>Đăng nhập hệ thống <ArrowRight/></>}</button><div className="admin-demo-login"><strong>Tài khoản dùng thử</strong><span>Email: admin@everycare.vn</span><span>Mật khẩu: Admin@123</span></div><small className="admin-login-note"><ShieldCheck/> Phiên quản trị sẽ kết thúc khi đóng trình duyệt.</small></form></section></main>;
}
