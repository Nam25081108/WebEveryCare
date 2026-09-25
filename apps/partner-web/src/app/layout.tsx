import type { Metadata } from "next";
import "./globals.css";
import "./partner-site.css";
import "./partner-portal.css";
import "./partner-dispatch.css";
import "leaflet/dist/leaflet.css";

export const metadata: Metadata = {
  title: "EveryCare Đối tác — Chủ động công việc",
  description: "Đăng ký, quản lý lịch làm việc và nhận việc cùng EveryCare.",
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return <html lang="vi"><body>{children}</body></html>;
}
