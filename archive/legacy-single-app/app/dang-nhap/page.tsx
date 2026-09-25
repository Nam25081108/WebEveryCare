import { Suspense } from "react";
import { AuthForm } from "@/components/auth-form";
import { AuthPageShell } from "@/components/auth-page-shell";

export default function LoginPage() { return <AuthPageShell mode="login"><Suspense fallback={<div className="auth-form-loading">Đang tải...</div>}><AuthForm mode="login"/></Suspense></AuthPageShell>; }
