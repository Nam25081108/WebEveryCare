import { Suspense } from "react";
import { AuthForm } from "@/components/auth-form";
import { AuthPageShell } from "@/components/auth-page-shell";

export default function RegisterPage() { return <AuthPageShell mode="register"><Suspense fallback={<div className="auth-form-loading">Đang tải...</div>}><AuthForm mode="register"/></Suspense></AuthPageShell>; }
