import { Suspense } from "react";
import { BookingWizard } from "@/components/booking-wizard";
import { AuthGate } from "@/components/auth-gate";
import { SiteHeader } from "@/components/site-header";

export default function BookingPage() { return <main className="subpage"><SiteHeader/><Suspense fallback={<div className="shell loading">Đang chuẩn bị lịch dọn...</div>}><AuthGate><BookingWizard/></AuthGate></Suspense></main>; }
