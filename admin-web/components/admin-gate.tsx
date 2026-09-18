"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { LoaderCircle, ShieldCheck } from "lucide-react";
import { useAdminAuth } from "@/components/admin-auth-provider";

export function AdminGate({ children }: { children: React.ReactNode }) {
  const { admin, isLoading } = useAdminAuth();
  const router = useRouter();
  useEffect(() => { if (!isLoading && !admin) router.replace("/dang-nhap"); }, [admin, isLoading, router]);
  if (isLoading || !admin) return <div className="admin-gate"><LoaderCircle className="spin"/><h2>Đang kiểm tra quyền quản trị</h2><p><ShieldCheck/> Khu vực nội bộ EveryCare</p></div>;
  return children;
}
