import type { Metadata } from "next";
import { AuthProvider } from "@/components/auth-provider";
import { AdminAuthProvider } from "@/components/admin-auth-provider";
import "./globals.css";
import "./auth.css";
import "./admin.css";
import "./booking-enhancements.css";
import "./service-details.css";
import "./address-book.css";
import "./admin-orders.css";
import "./partner-portal.css";
import "./everycare-home.css";
import "leaflet/dist/leaflet.css";

export const metadata: Metadata = {
  title: "EveryCare — Tiện ích và chăm sóc tại TP.HCM",
  description: "Nền tảng kết nối dịch vụ dọn dẹp, chăm sóc, làm đẹp, điện máy và tiện ích gia đình tại TP.HCM.",
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="vi">
      <body><AdminAuthProvider><AuthProvider>{children}</AuthProvider></AdminAuthProvider></body>
    </html>
  );
}
