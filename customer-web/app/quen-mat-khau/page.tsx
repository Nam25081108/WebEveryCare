"use client";

import { FormEvent, useState } from "react";
import Link from "next/link";
import { ArrowLeft, ArrowRight, CheckCircle2, Phone } from "lucide-react";
import { AuthPageShell } from "@/components/auth-page-shell";
import { isValidVietnamPhone } from "@/lib/auth";

export default function ForgotPasswordPage() {
  const [phone, setPhone] = useState("");
  const [error, setError] = useState("");
  const [sent, setSent] = useState(false);
  function submit(event: FormEvent) { event.preventDefault(); if (!isValidVietnamPhone(phone)) { setError("Vui lòng nhập đúng số điện thoại đã đăng ký."); return; } setError(""); setSent(true); }
  return <AuthPageShell mode="forgot">{sent ? <div className="forgot-success"><span><CheckCircle2/></span><h3>Đã ghi nhận yêu cầu</h3><p>Khi kết nối API, mã xác thực sẽ được gửi đến <strong>{phone}</strong>. Hiện đây là giao diện bản mẫu.</p><Link href="/dang-nhap" className="button full-button"><ArrowLeft/> Quay lại đăng nhập</Link></div> : <form className="auth-form forgot-form" onSubmit={submit}><div className="auth-field"><label htmlFor="forgotPhone">Số điện thoại</label><div className={error ? "input-wrap invalid" : "input-wrap"}><Phone/><input id="forgotPhone" type="tel" inputMode="numeric" autoComplete="tel" value={phone} onChange={(event) => setPhone(event.target.value)} placeholder="0901 234 567"/></div>{error && <small className="field-error">{error}</small>}</div><button className="button button-large full-button auth-submit">Gửi yêu cầu <ArrowRight/></button><p className="switch-auth"><Link href="/dang-nhap">Quay lại đăng nhập</Link></p></form>}</AuthPageShell>;
}
