"use client";

import { useEffect, useMemo, useState } from "react";
import { AlertCircle, ArrowLeft, ArrowRight, CalendarDays, Check, Clock3, Heart, LoaderCircle, MapPin, Plus, Sparkles, UserRound, Users } from "lucide-react";
import { useSearchParams } from "next/navigation";
import { formatCurrency, ServicePlan, services } from "@/lib/mock-data";
import { ServicePackageStep } from "@/components/service-package-step";
import { AddressStep } from "@/components/address-step";
import { useAuth } from "@/components/auth-provider";
import { calculateProfessionalPlan, defaultProfessionalConfig, isProfessionalConfigValid, ProfessionalConfig } from "@/lib/professional-pricing";
import type { MapLocation } from "@/components/partner-location-map";

const steps = ["Địa chỉ", "Dịch vụ", "Thời gian", "Người dọn", "Xác nhận"];

export function BookingWizard() {
  const { customer } = useAuth();
  const searchParams = useSearchParams();
  const initialService = searchParams.get("service") ?? "room";
  const initialPlan = searchParams.get("plan") ?? "";
  // Luôn bắt đầu bằng địa chỉ, kể cả khi khách đi vào từ một dịch vụ cụ thể.
  // Tham số `service` chỉ ghi nhớ nhóm dịch vụ để mở đúng gói ở bước kế tiếp.
  const [step, setStep] = useState(1);
  const [serviceId, setServiceId] = useState(initialService);
  const [planId, setPlanId] = useState(initialPlan);
  const [selectedPlan, setSelectedPlan] = useState<ServicePlan | null>(null);
  const [hasSelectedService, setHasSelectedService] = useState(false);
  const [serviceError, setServiceError] = useState("");
  const [professionalConfig, setProfessionalConfig] = useState<ProfessionalConfig>(defaultProfessionalConfig);
  const [professionalError, setProfessionalError] = useState("");
  const [address, setAddress] = useState("");
  const [addressLocation, setAddressLocation] = useState<MapLocation|null>(null);
  const [addressError, setAddressError] = useState("");
  const [date, setDate] = useState(2);
  const [time, setTime] = useState("14:00");
  const [scheduleError, setScheduleError] = useState("");
  const [choice, setChoice] = useState("auto");
  const [isConfirming, setIsConfirming] = useState(false);
  const [confirmError, setConfirmError] = useState("");
  const [bookingResult, setBookingResult] = useState<{ code:string; invitedPartners:number; message:string } | null>(null);
  const service = services.find((item) => item.id === serviceId) ?? services[0];
  const standardPlan = service.plans.find((item) => item.id === planId) ?? service.plans[0];
  const plan = serviceId === "professional" ? calculateProfessionalPlan(professionalConfig) : selectedPlan?.id === planId ? selectedPlan : standardPlan;
  const total = (plan.price ?? 0) + (choice === "choose" ? 30000 : 0);
  const dates = useMemo(() => Array.from({ length: 7 }, (_, index) => { const value = new Date(); value.setDate(value.getDate() + index); return value; }), []);

  useEffect(() => {
    if (searchParams.get("start") !== "address") return;

    const requestedService = searchParams.get("service");
    if (requestedService && services.some((item) => item.id === requestedService)) {
      setServiceId(requestedService);
      setPlanId("");
      setSelectedPlan(null);
    }

    setStep(1);
    setHasSelectedService(false);
    setServiceError("");
    setProfessionalError("");
  }, [searchParams]);

  function selectService(id: string) { setServiceId(id); setSelectedPlan(null); setPlanId(""); setHasSelectedService(id === "professional"); setServiceError(""); }

  function validateSchedule(dateIndex = date, selectedTime = time) {
    if (!selectedTime) return "Vui lòng nhập giờ bạn mong muốn.";
    const [hours, minutes] = selectedTime.split(":").map(Number);
    const selectedDateTime = new Date(dates[dateIndex]);
    selectedDateTime.setHours(hours, minutes, 0, 0);
    const earliestTime = new Date(Date.now() + 2 * 60 * 60 * 1000);
    if (selectedDateTime.getTime() < earliestTime.getTime()) {
      const earliestLabel = earliestTime.toLocaleString("vi-VN", { day: "2-digit", month: "2-digit", hour: "2-digit", minute: "2-digit" });
      return `Giờ bắt đầu phải cách thời điểm hiện tại ít nhất 2 tiếng. Thời gian sớm nhất có thể đặt là khoảng ${earliestLabel}.`;
    }
    return "";
  }

  function continueBooking() {
    if (step === 1 && (!address || !addressLocation)) {
      setAddressError(!address ? "Vui lòng thêm hoặc chọn một địa chỉ trước khi tiếp tục." : "Địa chỉ này chưa có tọa độ. Vui lòng thêm lại và xác nhận trên bản đồ.");
      return;
    }
    if (step === 2 && serviceId === "professional" && !isProfessionalConfigValid(professionalConfig)) {
      setProfessionalError("Diện tích phải nằm trong khoảng từ 101 đến 500m².");
      return;
    }
    if (step === 2 && !hasSelectedService) {
      setServiceError("Vui lòng chọn một gói dịch vụ trước khi tiếp tục.");
      return;
    }
    if (step === 3) {
      const error = validateSchedule();
      setScheduleError(error);
      if (error) return;
    }
    setStep(step + 1);
  }

  function updateTime(hours: string, minutes: string) {
    const value = `${hours}:${minutes}`;
    setTime(value);
    setScheduleError(validateSchedule(date, value));
  }

  async function confirmBooking() {
    if (!customer) return;
    setIsConfirming(true); setConfirmError("");
    const [hours, minutes] = time.split(":").map(Number);
    const scheduledStart = new Date(dates[date]);
    scheduledStart.setHours(hours, minutes, 0, 0);
    const [addressLabel, ...addressParts] = address.split("•");
    const groupSlugs: Record<string,string> = { room:"ve-sinh-phong-le", deep:"tong-ve-sinh", professional:"ve-sinh-chuyen-nghiep" };
    const packageSlugs: Record<string,string> = { r1:"goi-1-gio", r2:"goi-2-gio", r3:"goi-3-gio", r4:"goi-4-gio", d1:"can-ho-60m2", d2:"nha-80m2", d3:"nha-100m2", d4:"nha-150m2", d5:"nha-200m2", d6:"cong-trinh-400m2" };
    const area = professionalConfig.areaTier === "under60" ? 59 : professionalConfig.areaTier === "60to80" ? 70 : professionalConfig.areaTier === "81to100" ? 90 : professionalConfig.customArea;
    try {
      const response = await fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5185"}/api/bookings`, {
        method:"POST", headers:{"Content-Type":"application/json"}, body:JSON.stringify({
          customerName:customer.fullName, customerPhone:customer.phone, addressLabel:addressLabel.trim() || "Địa chỉ", fullAddress:addressParts.join("•").trim() || address,
          latitude:addressLocation?.latitude??null, longitude:addressLocation?.longitude??null, serviceGroupSlug:groupSlugs[serviceId], servicePackageSlug:serviceId === "professional" ? null : packageSlugs[planId],
          scheduledStartAt:scheduledStart.toISOString(), cleanerSelectionMode:choice === "choose" ? "CustomerChooses" : choice === "favorite" ? "FavoriteFirst" : "Automatic",
          buildingType:serviceId === "professional" ? professionalConfig.building === "house" ? "House" : "Office" : null,
          buildingCondition:serviceId === "professional" ? professionalConfig.condition === "old" ? "Existing" : "NewOrRenovated" : null,
          hasFurniture:serviceId === "professional" && professionalConfig.furnished, areaSquareMeters:serviceId === "professional" ? area : null
        })
      });
      const data = await response.json();
      if (!response.ok) throw new Error(data.message ?? "Không thể tạo đơn đặt lịch.");
      setBookingResult({ code:data.code, invitedPartners:data.invitedPartners, message:data.message });
    } catch (error) {
      setConfirmError(error instanceof TypeError ? "Không thể kết nối API. Hãy kiểm tra backend và PostgreSQL đang chạy." : error instanceof Error ? error.message : "Không thể tạo đơn đặt lịch.");
    } finally { setIsConfirming(false); }
  }

  return (
    <div className={hasSelectedService ? "booking-layout shell" : "booking-layout shell summary-hidden"}>
      <section className="booking-main">
        <div className="booking-top"><div><span className="eyebrow">Đặt lịch dọn nhà</span><h1>{steps[step - 1]}</h1></div><span className="step-count">Bước {step}/5</span></div>
        <div className="progress"><i style={{ width: `${step * 20}%` }}/></div>

        {step === 1 && <AddressStep value={address} error={addressError} onChange={setAddress} onLocationChange={setAddressLocation} onClearError={() => setAddressError("")}/>} 

        {step === 2 && <div><div className="booking-service-tabs">{services.map((item) => <button onClick={() => selectService(item.id)} className={serviceId === item.id ? "active" : ""} key={item.id}>{item.shortName}</button>)}</div><ServicePackageStep service={service} planId={planId} onPlanChange={(selected) => { setPlanId(selected.id); setSelectedPlan(selected); setHasSelectedService(true); setServiceError(""); }} professionalConfig={professionalConfig} onProfessionalChange={(value) => { setProfessionalConfig(value); setProfessionalError(""); setHasSelectedService(true); setServiceError(""); }} professionalError={professionalError}/>{serviceError && <div className="address-error" role="alert">{serviceError}</div>}</div>}

        {step === 3 && <div className="wizard-panel"><h2>Thời gian bạn mong muốn</h2><p>Lịch bắt đầu sớm nhất sau 2 giờ và có thể đặt trước tối đa 7 ngày.</p><div className="date-row">{dates.map((item, index) => <button key={index} className={date === index ? "date-card active" : "date-card"} onClick={() => { setDate(index); setScheduleError(validateSchedule(index, time)); }}><small>{index === 0 ? "Hôm nay" : item.toLocaleDateString("vi-VN", { weekday: "short" })}</small><strong>{item.getDate()}</strong><span>Tháng {item.getMonth()+1}</span></button>)}</div><h3 className="field-heading"><Clock3/> Chọn giờ bắt đầu theo định dạng 24 giờ</h3><div className={scheduleError ? "custom-time-input invalid" : "custom-time-input"}><Clock3/><div className="time-24-controls"><label><span>GIỜ</span><select value={time.split(":")[0]} onChange={(event) => updateTime(event.target.value, time.split(":")[1])} aria-label="Giờ bắt đầu">{Array.from({length:24},(_,index)=>String(index).padStart(2,"0")).map(hour=><option key={hour} value={hour}>{hour}</option>)}</select></label><b>:</b><label><span>PHÚT</span><select value={time.split(":")[1]} onChange={(event) => updateTime(time.split(":")[0], event.target.value)} aria-label="Phút bắt đầu">{Array.from({length:12},(_,index)=>String(index*5).padStart(2,"0")).map(minute=><option key={minute} value={minute}>{minute}</option>)}</select></label></div><span>Định dạng 24 giờ, ví dụ 14:30 là 2 giờ 30 chiều.</span></div>{scheduleError ? <div className="schedule-error" id="schedule-message" role="alert"><AlertCircle/><span><strong>Thời gian chưa hợp lệ</strong>{scheduleError}</span></div> : <div className="notice" id="schedule-message"><Clock3/><span>Người dọn cần ít nhất <strong>2 giờ chuẩn bị</strong> trước khi bắt đầu.</span></div>}</div>}

        {step === 4 && <div className="wizard-panel"><h2>Bạn muốn chọn người dọn thế nào?</h2><p>Hệ thống chỉ gửi yêu cầu đến đối tác phù hợp và đang sẵn sàng.</p><label className={choice === "auto" ? "choice-card selected" : "choice-card"}><input type="radio" checked={choice === "auto"} onChange={() => setChoice("auto")}/><span className="choice-icon"><Sparkles/></span><span><b>Hệ thống chọn giúp</b><small>Nhanh nhất • Ưu tiên người ở gần và phù hợp</small></span><strong>Miễn phí</strong></label><label className={choice === "choose" ? "choice-card selected" : "choice-card"}><input type="radio" checked={choice === "choose"} onChange={() => setChoice("choose")}/><span className="choice-icon coral"><Users/></span><span><b>Tôi muốn tự chọn</b><small>Xem hồ sơ những người đồng ý nhận việc</small></span><strong>+30.000đ</strong></label><label className={choice === "favorite" ? "choice-card selected" : "choice-card"}><input type="radio" checked={choice === "favorite"} onChange={() => setChoice("favorite")}/><span className="choice-icon pink"><Heart/></span><span><b>Ưu tiên người yêu thích</b><small>Gửi trước cho chị Ngọc trong 5 phút</small></span><strong>Miễn phí</strong></label></div>}

        {step === 5 && <div className="wizard-panel confirmation">{bookingResult ? <div className="booking-success"><div className="confirm-icon"><Check/></div><h2>Đặt lịch thành công!</h2><p>Mã đơn <strong>{bookingResult.code}</strong></p><div className="booking-search-result"><Users/><span><strong>{bookingResult.invitedPartners} cộng tác viên</strong><small>{bookingResult.message}</small></span></div><button className="button button-large full-button" onClick={() => window.location.href="/tai-khoan"}>Xem đơn hàng của tôi <ArrowRight/></button></div> : <><div className="confirm-icon"><Check/></div><h2>Kiểm tra lại lịch dọn</h2><p>Bạn chỉ thanh toán sau khi công việc hoàn thành.</p><div className="confirm-list"><div><MapPin/><span><small>Địa chỉ</small><strong>{address}</strong></span></div><div><Sparkles/><span><small>Dịch vụ</small><strong>{service.name} • {plan.name}</strong></span></div><div><CalendarDays/><span><small>Thời gian</small><strong>{dates[date].toLocaleDateString("vi-VN")} lúc {time}</strong></span></div><div><UserRound/><span><small>Phân công</small><strong>{choice === "auto" ? "Hệ thống chọn giúp" : choice === "choose" ? "Tự chọn người dọn" : "Ưu tiên người yêu thích"}</strong></span></div></div>{confirmError && <div className="schedule-error" role="alert"><AlertCircle/><span><strong>Không thể xác nhận</strong>{confirmError}</span></div>}<button className="button button-large full-button" onClick={confirmBooking} disabled={isConfirming}>{isConfirming ? <><LoaderCircle className="spin"/> Đang tạo đơn...</> : <>Xác nhận đặt lịch <Check/></>}</button></>}</div>}

        <div className="wizard-actions">{step > 1 ? <button className="back-button" onClick={() => setStep(step - 1)}><ArrowLeft/> Quay lại</button> : <span/>}{step < 5 && <button className="button" onClick={continueBooking}>Tiếp tục <ArrowRight/></button>}</div>
      </section>
      {hasSelectedService && <aside className="order-summary"><span className="summary-label">Lịch dọn của bạn</span><h3>{service.name}</h3><div className="summary-plan"><span><Sparkles/></span><div><strong>{plan.name}</strong><small>{plan.meta}</small></div></div><div className="summary-line"><span>Giá gói</span><strong>{formatCurrency(plan.price)}</strong></div>{choice === "choose" && <div className="summary-line"><span>Phí tự chọn người</span><strong>30.000đ</strong></div>}<div className="summary-line"><span>Dụng cụ & di chuyển</span><strong>Đã gồm</strong></div><div className="summary-total"><span>Tạm tính</span><strong>{formatCurrency(total)}</strong></div><p><Check/> Thanh toán sau khi hoàn thành</p><p><Check/> Đổi lịch miễn phí trước 2 giờ</p></aside>}
    </div>
  );
}
