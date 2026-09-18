import type { Metadata } from "next";
import { AdminAuthProvider } from "@/components/admin-auth-provider";
import "./globals.css";
import "./admin.css";
import "./admin-orders.css";

export const metadata: Metadata = {
  title: "EveryCare Admin — Hệ thống quản trị",
  description: "Quản lý vận hành nền tảng EveryCare.",
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="vi">
      <body><AdminAuthProvider>{children}</AdminAuthProvider></body>
    </html>
  );
}
