"use client";

import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { FormEvent, useMemo, useState } from "react";
import { AlertCircle, ArrowRight, Check, Eye, EyeOff, LoaderCircle, LockKeyhole, Mail, Phone, UserRound } from "lucide-react";
import { useAuth } from "@/components/auth-provider";
import { isValidEmail, isValidVietnamPhone, validatePassword } from "@/lib/auth";

type FormErrors = Partial<Record<"fullName" | "phone" | "email" | "password" | "confirmPassword" | "terms" | "general", string>>;

function safeReturnUrl(value: string | null) {
  return value?.startsWith("/") && !value.startsWith("//") ? value : "/";
}

export function AuthForm({ mode }: { mode: "login" | "register" }) {
  const { login, register } = useAuth();
  const router = useRouter();
  const searchParams = useSearchParams();
  const returnUrl = safeReturnUrl(searchParams.get("returnUrl"));
  const [fullName, setFullName] = useState("");
  const [phone, setPhone] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [remember, setRemember] = useState(true);
  const [terms, setTerms] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errors, setErrors] = useState<FormErrors>({});
  const passwordRules = useMemo(() => ({ length: password.length >= 8, upper: /[A-Z]/.test(password), lower: /[a-z]/.test(password), number: /\d/.test(password) }), [password]);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const nextErrors: FormErrors = {};
    if (mode === "register" && fullName.trim().split(/\s+/).length < 2) nextErrors.fullName = "Vui lòng nhập đầy đủ họ và tên.";
    if (!isValidVietnamPhone(phone)) nextErrors.phone = "Số điện thoại Việt Nam chưa đúng định dạng.";
    if (mode === "register" && !isValidEmail(email)) nextErrors.email = "Email chưa đúng định dạng.";
    if (!password) nextErrors.password = "Vui lòng nhập mật khẩu.";
    if (mode === "register" && password && !validatePassword(password)) nextErrors.password = "Mật khẩu chưa đáp ứng đủ điều kiện.";
    if (mode === "register" && password !== confirmPassword) nextErrors.confirmPassword = "Mật khẩu nhập lại không trùng khớp.";
    if (mode === "register" && !terms) nextErrors.terms = "Bạn cần đồng ý với điều khoản để đăng ký.";
    setErrors(nextErrors);
    if (Object.keys(nextErrors).length) return;

    setIsSubmitting(true);
    try {
      if (mode === "login") await login(phone, password, remember);
      else await register({ fullName, phone, email, password });
      router.replace("/");
    } catch (error) {
      setErrors({ general: error instanceof Error ? error.message : "Đã có lỗi xảy ra. Vui lòng thử lại." });
    } finally {
      setIsSubmitting(false);
    }
  }

  return <form className="auth-form" onSubmit={handleSubmit} noValidate>
    {errors.general && <div className="form-error-banner"><AlertCircle/>{errors.general}</div>}
    {mode === "register" && <div className="auth-field"><label htmlFor="fullName">Họ và tên</label><div className={errors.fullName ? "input-wrap invalid" : "input-wrap"}><UserRound/><input id="fullName" autoComplete="name" value={fullName} onChange={(event) => setFullName(event.target.value)} placeholder="Nguyễn Minh Anh"/></div>{errors.fullName && <small className="field-error">{errors.fullName}</small>}</div>}
    <div className="auth-field"><label htmlFor="phone">Số điện thoại</label><div className={errors.phone ? "input-wrap invalid" : "input-wrap"}><Phone/><input id="phone" type="tel" inputMode="numeric" autoComplete="tel" value={phone} onChange={(event) => setPhone(event.target.value)} placeholder="0901 234 567"/></div>{errors.phone && <small className="field-error">{errors.phone}</small>}</div>
    {mode === "register" && <div className="auth-field"><label htmlFor="email">Email <span>(không bắt buộc)</span></label><div className={errors.email ? "input-wrap invalid" : "input-wrap"}><Mail/><input id="email" type="email" autoComplete="email" value={email} onChange={(event) => setEmail(event.target.value)} placeholder="ban@example.com"/></div>{errors.email && <small className="field-error">{errors.email}</small>}</div>}
    <div className="auth-field"><div className="label-row"><label htmlFor="password">Mật khẩu</label>{mode === "login" && <Link href="/quen-mat-khau">Quên mật khẩu?</Link>}</div><div className={errors.password ? "input-wrap invalid" : "input-wrap"}><LockKeyhole/><input id="password" type={showPassword ? "text" : "password"} autoComplete={mode === "login" ? "current-password" : "new-password"} value={password} onChange={(event) => setPassword(event.target.value)} placeholder={mode === "login" ? "Nhập mật khẩu" : "Tối thiểu 8 ký tự"}/><button type="button" aria-label={showPassword ? "Ẩn mật khẩu" : "Hiện mật khẩu"} onClick={() => setShowPassword(!showPassword)}>{showPassword ? <EyeOff/> : <Eye/>}</button></div>{errors.password && <small className="field-error">{errors.password}</small>}</div>
    {mode === "register" && <><div className="password-rules"><span className={passwordRules.length ? "passed" : ""}><Check/> 8 ký tự</span><span className={passwordRules.upper && passwordRules.lower ? "passed" : ""}><Check/> Chữ hoa & thường</span><span className={passwordRules.number ? "passed" : ""}><Check/> Có chữ số</span></div><div className="auth-field"><label htmlFor="confirmPassword">Nhập lại mật khẩu</label><div className={errors.confirmPassword ? "input-wrap invalid" : "input-wrap"}><LockKeyhole/><input id="confirmPassword" type={showPassword ? "text" : "password"} autoComplete="new-password" value={confirmPassword} onChange={(event) => setConfirmPassword(event.target.value)} placeholder="Nhập lại mật khẩu"/></div>{errors.confirmPassword && <small className="field-error">{errors.confirmPassword}</small>}</div></>}
    {mode === "login" ? <label className="auth-checkbox"><input type="checkbox" checked={remember} onChange={(event) => setRemember(event.target.checked)}/><span><Check/></span> Duy trì đăng nhập trên thiết bị này</label> : <><label className="auth-checkbox terms"><input type="checkbox" checked={terms} onChange={(event) => setTerms(event.target.checked)}/><span><Check/></span><i>Tôi đồng ý với <a href="#">Điều khoản sử dụng</a> và <a href="#">Chính sách bảo mật</a>.</i></label>{errors.terms && <small className="field-error terms-error">{errors.terms}</small>}</>}
    <button className="button button-large full-button auth-submit" disabled={isSubmitting}>{isSubmitting ? <><LoaderCircle className="spin"/> Đang xử lý...</> : <>{mode === "login" ? "Đăng nhập" : "Tạo tài khoản"}<ArrowRight/></>}</button>
    <p className="switch-auth">{mode === "login" ? "Chưa có tài khoản?" : "Bạn đã có tài khoản?"} <Link href={`${mode === "login" ? "/dang-ky" : "/dang-nhap"}?returnUrl=${encodeURIComponent(returnUrl)}`}>{mode === "login" ? "Đăng ký miễn phí" : "Đăng nhập"}</Link></p>
  </form>;
}
