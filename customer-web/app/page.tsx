import Image from "next/image";
import Link from "next/link";
import { ArrowDown, ArrowRight, Baby, CalendarCheck, Check, Gem, HeartHandshake, Home, MapPin, Sparkles, Star, Wrench } from "lucide-react";
import { Footer } from "@/components/footer";
import { Reveal } from "@/components/reveal";
import { ServiceExplorer } from "@/components/service-explorer";
import { SiteHeader } from "@/components/site-header";
import { HeroCarousel } from "@/components/hero-carousel";
import { PARTNER_WEB_URL } from "@/lib/site-urls";

export default function HomePage() {
  return <main className="ec-luxe-home">
    <SiteHeader />

    <section className="ec-luxe-hero">
      <HeroCarousel />
      <div className="ec-luxe-veil" />
      <div className="ec-luxe-grain" />
      <div className="shell ec-luxe-hero-inner">
        <Reveal className="ec-luxe-copy">
          <div className="ec-luxe-kicker"><i><Gem /></i><span>Dịch vụ chăm sóc phong cách sống</span></div>
          <h1>Mọi điều bạn yêu quý,<br/> <em>đều xứng đáng được chăm sóc.</em></h1>
          <p>EveryCare kết nối bạn với những chuyên gia được xác minh — từ không gian sống, người thân đến từng tiện ích nhỏ trong gia đình.</p>
          <div className="ec-luxe-actions">
            <Link href="/#dich-vu">Khám phá dịch vụ <ArrowRight /></Link>
            <a href={PARTNER_WEB_URL}>Trở thành đối tác</a>
          </div>
          <div className="ec-luxe-proof">
            <div className="ec-luxe-avatars"><span>AN</span><span>TH</span><span>MN</span><span>+2k</span></div>
            <div><span className="ec-luxe-stars"><Star/><Star/><Star/><Star/><Star/></span><small>Được khách hàng tại TP.HCM tin chọn</small></div>
          </div>
        </Reveal>

      </div>

      <div className="shell ec-luxe-booking-bar">
        <div><span>01</span><MapPin/><p><small>Địa điểm</small><strong>Xác nhận vị trí trên bản đồ</strong></p></div>
        <div><span>02</span><Sparkles/><p><small>Dịch vụ</small><strong>Chọn điều bạn đang cần</strong></p></div>
        <div><span>03</span><CalendarCheck/><p><small>Thời gian</small><strong>Đặt lịch theo mong muốn</strong></p></div>
        <Link href="/#chon-dich-vu">Đặt dịch vụ <ArrowRight/></Link>
      </div>
      <a className="ec-scroll-cue" href="#trai-nghiem" aria-label="Xem tiếp"><span>Khám phá</span><ArrowDown /></a>
    </section>

    <div className="ec-luxe-marquee" aria-hidden="true"><div><span>CHĂM SÓC TẬN TÂM</span><i>✦</i><span>KHÔNG GIAN HOÀN HẢO</span><i>✦</i><span>ĐỐI TÁC TIN CẬY</span><i>✦</i><span>TIỆN ÍCH MỖI NGÀY</span><i>✦</i><span>CHĂM SÓC TẬN TÂM</span><i>✦</i><span>KHÔNG GIAN HOÀN HẢO</span><i>✦</i></div></div>

    <section className="ec-luxe-intro" id="trai-nghiem"><div className="shell">
      <Reveal className="ec-luxe-intro-head">
        <span>THE ART OF CARE</span>
        <h2>Không chỉ là một dịch vụ.<br/><em>Đó là cảm giác được thấu hiểu.</em></h2>
        <p>Mỗi kết nối đều được thiết kế để bạn tiết kiệm thời gian, an tâm hơn và tận hưởng trọn vẹn những điều quan trọng.</p>
      </Reveal>
      <div className="ec-editorial-grid">
        <Reveal className="ec-editorial-card ec-editorial-care" delay={80}>
          <Image src="/images/everycare-care.png" alt="Dịch vụ chăm sóc người cao tuổi và trẻ nhỏ của EveryCare" fill sizes="(max-width: 900px) 100vw, 58vw" />
          <div className="ec-editorial-shade"/>
          <div className="ec-editorial-copy"><span>CHĂM SÓC & HỖ TRỢ</span><h3>Yêu thương luôn<br/>có người đồng hành.</h3><p>Trông trẻ · Người cao tuổi · Người bệnh</p><Link href="/#dich-vu">Khám phá <ArrowRight/></Link></div>
          <div className="ec-editorial-index">01</div>
        </Reveal>
        <Reveal className="ec-editorial-card ec-editorial-tech" delay={180}>
          <Image src="/images/everycare-technical.png" alt="Kỹ thuật viên EveryCare bảo dưỡng máy lạnh" fill sizes="(max-width: 900px) 100vw, 42vw" />
          <div className="ec-editorial-shade"/>
          <div className="ec-editorial-copy"><span>BẢO DƯỠNG ĐIỆN MÁY</span><h3>Chuẩn xác trong<br/>từng thao tác.</h3><p>Máy lạnh · Máy giặt · Máy nóng lạnh</p><Link href="/#dich-vu">Khám phá <ArrowRight/></Link></div>
          <div className="ec-editorial-index">02</div>
        </Reveal>
      </div>
    </div></section>

    <ServiceExplorer />

    <section className="ec-luxe-method" id="quy-trinh"><div className="shell">
      <Reveal className="ec-luxe-method-heading"><span>EVERYCARE STANDARD</span><h2>Một trải nghiệm<br/><em>liền mạch từ đầu đến cuối.</em></h2><p>Minh bạch trong từng bước. Tinh tế trong từng điểm chạm.</p></Reveal>
      <div className="ec-luxe-method-grid">
        <Reveal className="ec-method-card" delay={0}><span>01</span><i><MapPin/></i><h3>Chọn nhu cầu</h3><p>Dịch vụ rõ ràng, vị trí chính xác trên bản đồ.</p></Reveal>
        <Reveal className="ec-method-card" delay={80}><span>02</span><i><CalendarCheck/></i><h3>Đặt thời gian</h3><p>Chọn lịch phù hợp và xem giá dự kiến tức thì.</p></Reveal>
        <Reveal className="ec-method-card" delay={160}><span>03</span><i><HeartHandshake/></i><h3>Kết nối chuyên gia</h3><p>Đúng kỹ năng, đúng lịch rảnh và gần khu vực.</p></Reveal>
        <Reveal className="ec-method-card" delay={240}><span>04</span><i><Check/></i><h3>Tận hưởng kết quả</h3><p>Xác nhận, thanh toán và lưu người yêu thích.</p></Reveal>
      </div>
    </div></section>

    <section className="ec-luxe-manifesto"><Image src="/images/everycare-hero.png" alt="Không gian sống sang trọng được EveryCare chăm sóc" fill sizes="100vw"/><div className="ec-luxe-manifesto-overlay"/><Reveal className="shell ec-luxe-manifesto-inner"><span>THE EVERYCARE PROMISE</span><h2>Thời gian của bạn<br/>là điều quý giá.</h2><p>Hãy dành nó cho những khoảnh khắc đáng nhớ.<br/>Phần còn lại, để EveryCare chăm lo.</p><Link href="/dat-lich">Bắt đầu trải nghiệm <ArrowRight/></Link></Reveal></section>

    <section className="ec-luxe-numbers"><div className="shell"><Reveal><span>18+</span><small>Dịch vụ thiết yếu</small></Reveal><Reveal delay={80}><span>100%</span><small>Đối tác được xác minh</small></Reveal><Reveal delay={160}><span>24/7</span><small>Lời mời được lưu</small></Reveal><Reveal delay={240}><span>TP.HCM</span><small>Khu vực phục vụ</small></Reveal></div></section>

    <section className="ec-luxe-partner"><div className="shell"><Reveal><span>CỘNG ĐỒNG EVERYCARE</span><h2>Trao kỹ năng.<br/><em>Nhận giá trị xứng đáng.</em></h2><p>Chủ động lịch làm việc, tiếp cận những công việc phù hợp và phát triển thu nhập cùng một nền tảng chỉn chu.</p><a href={PARTNER_WEB_URL}>Gia nhập đội ngũ <ArrowRight/></a></Reveal><div className="ec-luxe-partner-orbit"><i><Home/></i><i><Baby/></i><i><Wrench/></i><strong><HeartHandshake/></strong></div></div></section>

    <Footer />
  </main>;
}
