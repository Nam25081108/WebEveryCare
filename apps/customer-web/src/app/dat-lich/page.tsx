import { Suspense } from "react";
import { BookingWizard } from "@/features/booking/components/booking-wizard";
import { AuthGate } from "@/features/auth/components/auth-gate";
import { SiteHeader } from "@/components/layout/site-header";

export default function BookingPage() { return <main className="subpage"><SiteHeader/><Suspense fallback={<div className="shell loading">Đang chuẩn bị lịch dọn...</div>}><AuthGate><BookingWizard/></AuthGate></Suspense></main>; }
