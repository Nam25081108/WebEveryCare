"use client";

import { useEffect } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { LoaderCircle, ShieldCheck } from "lucide-react";
import { useAuth } from "@/features/auth/components/auth-provider";

export function AuthGate({ children }: { children: React.ReactNode }) {
  const { customer, isLoading } = useAuth();
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();

  useEffect(() => {
    if (!isLoading && !customer) {
      const query = searchParams.toString();
      const returnUrl = `${pathname}${query ? `?${query}` : ""}`;
      router.replace(`/dang-nhap?returnUrl=${encodeURIComponent(returnUrl)}`);
    }
  }, [customer, isLoading, pathname, router, searchParams]);

  if (isLoading || !customer) {
    return <div className="auth-gate-loading"><span><LoaderCircle className="spin"/></span><h2>Đang kiểm tra tài khoản</h2><p>Bạn cần đăng nhập để tiếp tục đặt dịch vụ.</p><div><ShieldCheck/> Thông tin của bạn luôn được bảo vệ</div></div>;
  }
  return children;
}
