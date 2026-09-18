import Link from "next/link";
import { ArrowLeft, BellRing, CalendarClock, MapPin, ShieldCheck, Sparkles } from "lucide-react";
import { notFound } from "next/navigation";
import { Footer } from "@/components/footer";
import { SiteHeader } from "@/components/site-header";
import { utilityServices } from "@/lib/service-catalog";
import { PARTNER_WEB_URL } from "@/lib/site-urls";

export default async function ServiceDetailPage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;
  const service = utilityServices.find((item) => item.slug === slug);
  if (!service) notFound();

  return <main className="ec-service-page">
    <SiteHeader />
    <section className="ec-service-detail"><div className="shell"><article className="ec-service-detail-card">
      <div className="ec-service-detail-icon"><Sparkles /></div>
      <span>{service.category.name}</span>
      <h1>{service.name}</h1>
      <p>{service.description}</p>
      <div className="ec-coming-soon"><strong>Nghiệp vụ và bảng giá đang được hoàn thiện</strong><small>EveryCare sẽ bổ sung các lựa chọn, phạm vi công việc và giá chi tiết trước khi mở đặt lịch dịch vụ này.</small></div>
      <div className="ec-detail-features">
        <div><MapPin /><strong>Phục vụ tại TP.HCM</strong><small>Xác nhận vị trí chính xác trên bản đồ</small></div>
        <div><CalendarClock /><strong>Chủ động thời gian</strong><small>Chọn ngày và giờ phù hợp khi mở dịch vụ</small></div>
        <div><ShieldCheck /><strong>Đối tác xác minh</strong><small>Hồ sơ được xét duyệt trước khi nhận việc</small></div>
      </div>
      <div className="ec-detail-actions"><Link href="/#dich-vu"><ArrowLeft size={16}/> Xem dịch vụ khác</Link><a href={PARTNER_WEB_URL}><BellRing size={16}/> Đăng ký làm đối tác</a></div>
    </article></div></section>
    <Footer />
  </main>;
}
