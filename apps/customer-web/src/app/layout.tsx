import type { Metadata } from "next";
import { AuthProvider } from "@/features/auth/components/auth-provider";
import "./globals.css";
import "./auth.css";
import "./booking-enhancements.css";
import "./booking-request.css";
import "./customer-bookings.css";
import "./service-details.css";
import "./address-book.css";
import "./everycare-home.css";
import "./marketplace-home.css";
import "./marketplace-home-v2.css";
import "leaflet/dist/leaflet.css";

export const metadata: Metadata = {
  title: "EveryCare — Tiện ích và chăm sóc tại TP.HCM",
  description: "Nền tảng kết nối dịch vụ dọn dẹp, chăm sóc, làm đẹp, điện máy và tiện ích gia đình tại TP.HCM.",
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="vi">
      <body><AuthProvider>{children}</AuthProvider></body>
    </html>
  );
}
