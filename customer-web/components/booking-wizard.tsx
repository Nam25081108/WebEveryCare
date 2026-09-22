"use client";

import { useEffect, useMemo, useState } from "react";
import { AlertCircle, ArrowLeft, ArrowRight, CalendarDays, Check, Clock3, Heart, Hotel, LoaderCircle, MapPin, Plus, Sparkles, UserRound, Users } from "lucide-react";
import { useSearchParams } from "next/navigation";
import { formatCurrency, ServicePlan, services } from "@/lib/mock-data";
import { ServicePackageStep } from "@/components/service-package-step";
import { calculateOfficePlan, defaultOfficeConfig, OfficeBookingConfig, OfficePackageStep, type OfficePlan } from "@/components/office-package-step";
import { calculateHospitalityPlan, defaultHospitalityConfig, hospitalityOptions, HospitalityBookingConfig, HospitalityPackageStep } from "@/components/hospitality-package-step";
import { AddressStep } from "@/components/address-step";
import { useAuth } from "@/components/auth-provider";
import { calculateProfessionalPlan, defaultProfessionalConfig, isProfessionalConfigValid, ProfessionalConfig } from "@/lib/professional-pricing";
import type { MapLocation } from "@/components/partner-location-map";

const steps = ["Địa chỉ", "Dịch vụ", "Thời gian", "Người dọn", "Xác nhận"];
const officeWeekdayLabels: Record<string,string> = { mon:"Thứ 2", tue:"Thứ 3", wed:"Thứ 4", thu:"Thứ 5", fri:"Thứ 6", sat:"Thứ 7", sun:"Chủ nhật" };
const officeWeekdayNumbers: Record<string,number> = { sun:0, mon:1, tue:2, wed:3, thu:4, fri:5, sat:6 };
const groupSlugs: Record<string,string> = { room:"ve-sinh-phong-le", deep:"tong-ve-sinh", professional:"ve-sinh-chuyen-nghiep", office:"don-dep-van-phong-dinh-ky", hospitality:"don-dep-buong-phong" };

export function BookingWizard() {
  const { customer } = useAuth();
  const searchParams = useSearchParams();
  const initialService = searchParams.get("service") ?? "room";
  const requestedService = searchParams.get("service");
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
  const [officeConfig, setOfficeConfig] = useState<OfficeBookingConfig>(defaultOfficeConfig);
  const [hospitalityConfig, setHospitalityConfig] = useState<HospitalityBookingConfig>(defaultHospitalityConfig);
  const [professionalError, setProfessionalError] = useState("");
  const [address, setAddress] = useState("");
  const [addressLocation, setAddressLocation] = useState<MapLocation|null>(null);
  const [addressError, setAddressError] = useState("");
  const [date, setDate] = useState(0);
  const [time, setTime] = useState("14:00");
  const [scheduleError, setScheduleError] = useState("");
  const [customerRequest, setCustomerRequest] = useState("");
  const [choice, setChoice] = useState("auto");
  const [favoriteTaskers, setFavoriteTaskers] = useState<{partnerId:string;name:string}[]>([]);
  const [isConfirming, setIsConfirming] = useState(false);
  const [confirmError, setConfirmError] = useState("");
  const [bookingResult, setBookingResult] = useState<{ code:string; invitedPartners:number; message:string; depositAmount?:number; remainingAmount?:number } | null>(null);
  const [catalogServices,setCatalogServices]=useState(services);
  const [officeCatalogPlans,setOfficeCatalogPlans]=useState<OfficePlan[]>([]);
  const visibleServices = requestedService && catalogServices.some((item) => item.id === requestedService) ? catalogServices.filter((item) => item.id === requestedService) : catalogServices;
  const service = catalogServices.find((item) => item.id === serviceId) ?? catalogServices[0] ?? services[0];
  const bookingCategoryTitle = serviceId === "office" || serviceId === "hospitality"
    ? "Dịch vụ cho doanh nghiệp"
    : serviceId === "room" || serviceId === "deep" || serviceId === "professional"
      ? "Vệ sinh & dọn dẹp"
      : "Đặt dịch vụ";
  const standardPlan = service.plans.find((item) => item.id === planId) ?? service.plans[0];
  const plan = serviceId === "professional" ? calculateProfessionalPlan(professionalConfig) : serviceId === "office" ? selectedPlan ?? calculateOfficePlan(officeConfig,officeCatalogPlans.length?officeCatalogPlans:undefined) : serviceId === "hospitality" ? selectedPlan ?? calculateHospitalityPlan(hospitalityConfig) : selectedPlan?.id === planId ? selectedPlan : standardPlan;
  const total = (plan.price ?? 0) + (choice === "choose" ? 30000 : 0);
  const dates = useMemo(() => Array.from({ length: 7 }, (_, index) => { const value = new Date(); value.setDate(value.getDate() + index + 2); return value; }), []);

  useEffect(()=>{
    let active=true;
    fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5185"}/api/service-groups`,{cache:"no-store"}).then(response=>response.ok?response.json():Promise.reject()).then((groups:Array<{slug:string;name:string;description:string;packages:Array<{slug:string;name:string;description:string|null;price:number;durationMinutes:number|null;requiredWorkers:number;maximumAreaSquareMeters:number|null}>}>)=>{
      if(!active)return;
      const slugToId:Record<string,string>={"ve-sinh-phong-le":"room","tong-ve-sinh":"deep","ve-sinh-chuyen-nghiep":"professional","don-dep-van-phong-dinh-ky":"office","don-dep-buong-phong":"hospitality"};
      const planIds:Record<string,string>={"goi-1-gio":"r1","goi-2-gio":"r2","goi-3-gio":"r3","goi-4-gio":"r4","can-ho-60m2":"d1","nha-80m2":"d2","nha-100m2":"d3","nha-150m2":"d4","nha-200m2":"d5","cong-trinh-400m2":"d6"};
      const available=groups.filter(group=>slugToId[group.slug]).map(group=>{const id=slugToId[group.slug];const fallback=services.find(item=>item.id===id)!;if(id==="office"||id==="hospitality"||id==="professional")return{...fallback,name:group.name,description:group.description};return{...fallback,name:group.name,description:group.description,plans:group.packages.map(item=>{const mappedId=planIds[item.slug]??item.slug;const previous=fallback.plans.find(plan=>plan.id===mappedId);return{id:mappedId,name:item.name,description:item.description??(item.maximumAreaSquareMeters?`Tối đa ${item.maximumAreaSquareMeters}m²`:""),meta:`${item.requiredWorkers} người${item.durationMinutes?` • ${item.durationMinutes/60} giờ`:""}`,price:item.price,workDetails:previous?.workDetails}})}});
      setCatalogServices(available);
      if(available.length&&!available.some(item=>item.id===serviceId))setServiceId(available[0].id);
      const office=groups.find(group=>group.slug==="don-dep-van-phong-dinh-ky");
      if(office)setOfficeCatalogPlans(office.packages.map(item=>{const area=Number(item.maximumAreaSquareMeters??100);return{id:`o${area}`,tier:area<=200?"under200":area<=400?"under400":"under900",area,people:item.requiredWorkers,hours:(item.durationMinutes??60)/60,price:item.price}}));
    }).catch(()=>{});
    return()=>{active=false};
  },[]);

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

  useEffect(() => {
    if (!customer) { setFavoriteTaskers([]); return; }
    let active = true;
    const requiredWorkers = Number(plan.meta.match(/(\d+)\s*người/i)?.[1] ?? 1);
    fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5185"}/api/customer-favorites?phone=${encodeURIComponent(customer.phone)}&serviceGroupSlug=${encodeURIComponent(groupSlugs[serviceId])}&requiredWorkers=${requiredWorkers}`, { cache:"no-store" })
      .then(response => response.ok ? response.json() : [])
      .then(items => { if (active) setFavoriteTaskers(items); })
      .catch(() => { if (active) setFavoriteTaskers([]); });
    return () => { active = false; };
  }, [customer, serviceId, plan.meta]);

  useEffect(() => { if (choice === "favorite" && favoriteTaskers.length === 0) setChoice("auto"); }, [choice, favoriteTaskers.length]);

  function selectService(id: string) { setServiceId(id); setSelectedPlan(null); setPlanId(""); setHasSelectedService(id === "professional"); setOfficeConfig(defaultOfficeConfig); setHospitalityConfig(defaultHospitalityConfig); setServiceError(""); }

  function validateSchedule(dateIndex = date, selectedTime = time) {
    if (!selectedTime) return "Vui lòng nhập giờ bạn mong muốn.";
    const [hours, minutes] = selectedTime.split(":").map(Number);
    const selectedDateTime = new Date(dates[dateIndex]);
    selectedDateTime.setHours(hours, minutes, 0, 0);
    const earliestTime = new Date(Date.now() + 2 * 24 * 60 * 60 * 1000);
    if (selectedDateTime.getTime() < earliestTime.getTime()) {
      const earliestLabel = earliestTime.toLocaleString("vi-VN", { day: "2-digit", month: "2-digit", hour: "2-digit", minute: "2-digit" });
      return `Lịch bắt đầu phải cách thời điểm hiện tại ít nhất 2 ngày. Thời gian sớm nhất có thể đặt là khoảng ${earliestLabel}.`;
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
    if (step === 2 && serviceId === "office" && (!officeConfig.mode || (officeConfig.mode === "monthly" && officeConfig.days.length === 0))) {
      setServiceError(officeConfig.mode ? "Vui lòng chọn ít nhất một ngày làm việc trong tuần." : "Vui lòng chọn dịch vụ theo buổi/ngày hoặc theo gói tháng.");
      return;
    }
    if (step === 2 && serviceId === "hospitality" && hospitalityConfig.stage === "details") {
      const contactPhone = hospitalityConfig.contactPhone.replace(/\s/g, "");
      if (!hospitalityConfig.facilityName.trim() || !hospitalityConfig.contactName.trim() || !/^0\d{9}$/.test(contactPhone)) {
        setServiceError("Vui lòng nhập tên cơ sở, tên liên hệ và số điện thoại Việt Nam gồm 10 chữ số.");
        return;
      }
      setHospitalityConfig({...hospitalityConfig, stage:"rooms", contactPhone});
      setServiceError("");
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
    if (step === 2 && serviceId === "office" && officeConfig.mode === "monthly") {
      setStep(4);
      return;
    }
    setStep(step + 1);
  }

  function goBack() {
    if (step === 4 && serviceId === "office" && officeConfig.mode === "monthly") {
      setStep(2);
      return;
    }
    setStep(step - 1);
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
    if (serviceId === "office" && officeConfig.mode === "monthly") {
      const now = new Date();
      const earliest = new Date(now.getTime()+2*24*60*60*1000);
      const candidates=officeConfig.days.map(day=>{const candidate=new Date(earliest);const target=officeWeekdayNumbers[day];candidate.setDate(earliest.getDate()+((target-earliest.getDay()+7)%7));candidate.setHours(hours,minutes,0,0);if(candidate.getTime()<earliest.getTime())candidate.setDate(candidate.getDate()+7);return candidate}).sort((a,b)=>a.getTime()-b.getTime());
      scheduledStart.setTime(candidates[0].getTime());
    }
    const [addressLabel, ...addressParts] = address.split("•");
    const packageSlugs: Record<string,string> = { r1:"goi-1-gio", r2:"goi-2-gio", r3:"goi-3-gio", r4:"goi-4-gio", d1:"can-ho-60m2", d2:"nha-80m2", d3:"nha-100m2", d4:"nha-150m2", d5:"nha-200m2", d6:"cong-trinh-400m2", o100:"van-phong-100m2", o150:"van-phong-150m2", o200:"van-phong-200m2", o250:"van-phong-250m2", o300:"van-phong-300m2", o400:"van-phong-400m2", o500:"van-phong-500m2", o750:"van-phong-750m2", o900:"van-phong-900m2" };
    const area = professionalConfig.areaTier === "under60" ? 59 : professionalConfig.areaTier === "60to80" ? 70 : professionalConfig.areaTier === "81to100" ? 90 : professionalConfig.customArea;
    try {
      const response = await fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5185"}/api/bookings`, {
        method:"POST", headers:{"Content-Type":"application/json"}, body:JSON.stringify({
          customerName:customer.fullName, customerPhone:customer.phone, addressLabel:addressLabel.trim() || "Địa chỉ", fullAddress:addressParts.join("•").trim() || address,
          latitude:addressLocation?.latitude??null, longitude:addressLocation?.longitude??null, serviceGroupSlug:groupSlugs[serviceId], servicePackageSlug:serviceId === "professional" || serviceId === "hospitality" ? null : packageSlugs[planId]??planId,
          scheduledStartAt:scheduledStart.toISOString(), cleanerSelectionMode:choice === "choose" ? "CustomerChooses" : choice === "favorite" ? "FavoriteFirst" : "Automatic",
          buildingType:serviceId === "professional" ? professionalConfig.building === "house" ? "House" : "Office" : null,
          buildingCondition:serviceId === "professional" ? professionalConfig.condition === "old" ? "Existing" : "NewOrRenovated" : null,
          hasFurniture:serviceId === "professional" && professionalConfig.furnished, areaSquareMeters:serviceId === "professional" ? area : null,
          isRecurring:serviceId === "office" && officeConfig.mode === "monthly",
          recurrenceDays:serviceId === "office" && officeConfig.mode === "monthly" ? officeConfig.days : null,
          recurrenceStartTime:serviceId === "office" && officeConfig.mode === "monthly" ? officeConfig.startTime : null,
          recurrenceMonths:serviceId === "office" && officeConfig.mode === "monthly" ? officeConfig.contractMonths : null,
          hasGlassCleaning:serviceId === "office" && officeConfig.glass,
          hasCarpetVacuum:serviceId === "office" && officeConfig.carpet,
          facilityName:serviceId === "hospitality" ? hospitalityConfig.facilityName.trim() : null,
          contactName:serviceId === "hospitality" ? hospitalityConfig.contactName.trim() : null,
          contactPhone:serviceId === "hospitality" ? hospitalityConfig.contactPhone : null,
          accommodationType:serviceId === "hospitality" ? hospitalityConfig.kind : null,
          hospitalityItems:serviceId === "hospitality" ? hospitalityOptions[hospitalityConfig.kind].filter(item=>(hospitalityConfig.quantities[item.id]??0)>0).map(item=>({code:item.id,name:item.name,quantity:hospitalityConfig.quantities[item.id],unitPrice:item.price,durationMinutes:item.durationMinutes})) : null,
          customerRequest:customerRequest.trim()||null
        })
      });
      const data = await response.json();
      if (!response.ok) throw new Error(data.message ?? "Không thể tạo đơn đặt lịch.");
      const paymentResponse = await fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5185"}/api/customer-bookings/${data.id}/pay-deposit`, {method:"POST",headers:{"Content-Type":"application/json"},body:JSON.stringify({phone:customer.phone})});
      const paymentData = await paymentResponse.json();
      if (!paymentResponse.ok) throw new Error(paymentData.message ?? "Không thể thanh toán tiền cọc.");
      setBookingResult({ code:data.code, invitedPartners:paymentData.invitedPartners, message:paymentData.message, depositAmount:data.depositAmount, remainingAmount:data.remainingAmount });
    } catch (error) {
      setConfirmError(error instanceof TypeError ? "Không thể kết nối API. Hãy kiểm tra backend và PostgreSQL đang chạy." : error instanceof Error ? error.message : "Không thể tạo đơn đặt lịch.");
    } finally { setIsConfirming(false); }
  }

  return (
    <div className={hasSelectedService ? "booking-layout shell" : "booking-layout shell summary-hidden"}>
      <section className="booking-main">
        <div className="booking-top"><div><span className="eyebrow">{bookingCategoryTitle}</span><h1>{steps[step - 1]}</h1></div><span className="step-count">Bước {step}/5</span></div>
        <div className="progress"><i style={{ width: `${step * 20}%` }}/></div>

        {step === 1 && <AddressStep value={address} error={addressError} onChange={setAddress} onLocationChange={setAddressLocation} onClearError={() => setAddressError("")}/>} 

        {step === 2 && <div><div className="booking-service-tabs">{visibleServices.map((item) => <button onClick={() => selectService(item.id)} className={serviceId === item.id ? "active" : ""} key={item.id}>{item.shortName}</button>)}</div>{serviceId === "office" ? <OfficePackageStep config={officeConfig} plans={officeCatalogPlans.length?officeCatalogPlans:undefined} onChange={(value, selected) => { setOfficeConfig(value); setPlanId(selected.id); setSelectedPlan(selected); setHasSelectedService(Boolean(value.mode)); setServiceError(""); if (value.mode === "monthly") setTime(value.startTime); }}/> : serviceId === "hospitality" ? <HospitalityPackageStep config={hospitalityConfig} onChange={(value, selected) => { setHospitalityConfig(value); setPlanId(selected.id); setSelectedPlan(selected); setHasSelectedService((selected.price??0)>0); setServiceError(""); }}/> : <ServicePackageStep service={service} planId={planId} onPlanChange={(selected) => { setPlanId(selected.id); setSelectedPlan(selected); setHasSelectedService(true); setServiceError(""); }} professionalConfig={professionalConfig} onProfessionalChange={(value) => { setProfessionalConfig(value); setProfessionalError(""); setHasSelectedService(true); setServiceError(""); }} professionalError={professionalError}/>} {serviceError && <div className="address-error" role="alert">{serviceError}</div>}</div>}

        {step === 3 && <div className="wizard-panel"><h2>Thời gian bạn mong muốn</h2><p>Mọi dịch vụ cần được đặt trước ít nhất 2 ngày. Bạn có thể chọn một trong 7 ngày khả dụng bên dưới.</p><div className="date-row">{dates.map((item, index) => <button key={index} className={date === index ? "date-card active" : "date-card"} onClick={() => { setDate(index); setScheduleError(validateSchedule(index, time)); }}><small>{index === 0 ? "Sớm nhất" : item.toLocaleDateString("vi-VN", { weekday: "short" })}</small><strong>{item.getDate()}</strong><span>Tháng {item.getMonth()+1}</span></button>)}</div><h3 className="field-heading"><Clock3/> Chọn giờ bắt đầu theo định dạng 24 giờ</h3><div className={scheduleError ? "custom-time-input invalid" : "custom-time-input"}><Clock3/><div className="time-24-controls"><label><span>GIỜ</span><select value={time.split(":")[0]} onChange={(event) => updateTime(event.target.value, time.split(":")[1])} aria-label="Giờ bắt đầu">{Array.from({length:24},(_,index)=>String(index).padStart(2,"0")).map(hour=><option key={hour} value={hour}>{hour}</option>)}</select></label><b>:</b><label><span>PHÚT</span><select value={time.split(":")[1]} onChange={(event) => updateTime(time.split(":")[0], event.target.value)} aria-label="Phút bắt đầu">{Array.from({length:12},(_,index)=>String(index*5).padStart(2,"0")).map(minute=><option key={minute} value={minute}>{minute}</option>)}</select></label></div><span>Định dạng 24 giờ, ví dụ 14:30 là 2 giờ 30 chiều.</span></div>{scheduleError ? <div className="schedule-error" id="schedule-message" role="alert"><AlertCircle/><span><strong>Thời gian chưa hợp lệ</strong>{scheduleError}</span></div> : <div className="notice" id="schedule-message"><Clock3/><span>Đối tác cần ít nhất <strong>2 ngày chuẩn bị</strong> trước khi bắt đầu.</span></div>}<label className="booking-request"><span>Yêu cầu thêm (không bắt buộc)</span><textarea maxLength={1000} value={customerRequest} onChange={event=>setCustomerRequest(event.target.value)} placeholder="Ví dụ: gọi trước khi đến, ưu tiên khu vực bếp, có thú cưng trong nhà..."/><small>{customerRequest.length}/1000 ký tự</small></label></div>}

        {step === 4 && <div className="wizard-panel"><h2>Bạn muốn chọn người dọn thế nào?</h2><p>Hệ thống chỉ gửi yêu cầu đến đối tác phù hợp và đang sẵn sàng.</p><label className={choice === "auto" ? "choice-card selected" : "choice-card"}><input type="radio" checked={choice === "auto"} onChange={() => setChoice("auto")}/><span className="choice-icon"><Sparkles/></span><span><b>Hệ thống chọn giúp</b><small>Nhanh nhất • Ưu tiên người ở gần và phù hợp</small></span><strong>Miễn phí</strong></label><label className={choice === "choose" ? "choice-card selected" : "choice-card"}><input type="radio" checked={choice === "choose"} onChange={() => setChoice("choose")}/><span className="choice-icon coral"><Users/></span><span><b>Tôi muốn tự chọn</b><small>Xem hồ sơ những người đồng ý nhận việc</small></span><strong>+30.000đ</strong></label><label className={`${choice === "favorite" ? "choice-card selected" : "choice-card"}${favoriteTaskers.length === 0 ? " disabled" : ""}`}><input type="radio" disabled={favoriteTaskers.length === 0} checked={choice === "favorite"} onChange={() => setChoice("favorite")}/><span className="choice-icon pink"><Heart/></span><span><b>Ưu tiên Tasker yêu thích</b><small>{favoriteTaskers.length ? `${favoriteTaskers.length} Tasker đã lưu • ${favoriteTaskers.slice(0,2).map(item=>item.name).join(", ")}` : "Bạn chưa có Tasker yêu thích"}</small></span><strong>{favoriteTaskers.length ? "Miễn phí" : "Chưa có"}</strong></label></div>}

        {step === 5 && <div className="wizard-panel confirmation">{bookingResult ? <div className="booking-success"><div className="confirm-icon"><Check/></div><h2>Đặt cọc và đặt lịch thành công!</h2><p>Mã đơn <strong>{bookingResult.code}</strong></p>{bookingResult.depositAmount!=null&&<p>Đã cọc <strong>{formatCurrency(bookingResult.depositAmount)}</strong> • Còn lại {formatCurrency(bookingResult.remainingAmount??0)}</p>}<div className="booking-search-result"><Users/><span><strong>{bookingResult.invitedPartners} Tasker được mời</strong><small>{bookingResult.message}</small></span></div><button className="button button-large full-button" onClick={() => window.location.href="/tai-khoan"}>Theo dõi đơn hàng <ArrowRight/></button></div> : <><div className="confirm-icon"><Check/></div><h2>Kiểm tra lại lịch dọn</h2><p>Thanh toán cọc 30% để giữ lịch; phần còn lại thanh toán sau khi hoàn thành.</p><div className="confirm-list"><div><MapPin/><span><small>Địa chỉ</small><strong>{address}</strong></span></div>{serviceId === "hospitality"&&<div><Hotel/><span><small>Cơ sở và liên hệ</small><strong>{hospitalityConfig.facilityName} • {hospitalityConfig.contactName} • {hospitalityConfig.contactPhone}</strong></span></div>}<div><Sparkles/><span><small>Dịch vụ</small><strong>{service.name} • {plan.name}</strong></span></div><div><CalendarDays/><span><small>Thời gian</small><strong>{serviceId === "office" && officeConfig.mode === "monthly" ? `${officeConfig.days.map(day => officeWeekdayLabels[day]).join(", ")} hàng tuần lúc ${officeConfig.startTime}` : `${dates[date].toLocaleDateString("vi-VN")} lúc ${time}`}</strong></span></div><div><UserRound/><span><small>Phân công</small><strong>{choice === "auto" ? "Hệ thống chọn giúp" : choice === "choose" ? "Tự chọn người dọn" : "Ưu tiên người yêu thích"}</strong></span></div></div>{confirmError && <div className="schedule-error" role="alert"><AlertCircle/><span><strong>Không thể xác nhận</strong>{confirmError}</span></div>}<button className="button button-large full-button" onClick={confirmBooking} disabled={isConfirming}>{isConfirming ? <><LoaderCircle className="spin"/> Đang thanh toán...</> : <>Thanh toán cọc & đặt lịch <Check/></>}</button></>}</div>}

        <div className="wizard-actions">{step > 1 ? <button className="back-button" onClick={goBack}><ArrowLeft/> Quay lại</button> : <span/>}{step < 5 && <button className="button" onClick={continueBooking}>Tiếp tục <ArrowRight/></button>}</div>
      </section>
      {hasSelectedService && <aside className="order-summary"><span className="summary-label">Lịch dọn của bạn</span><h3>{service.name}</h3><div className="summary-plan"><span><Sparkles/></span><div><strong>{plan.name}</strong><small>{plan.meta}</small></div></div><div className="summary-line"><span>Giá gói</span><strong>{formatCurrency(plan.price)}</strong></div>{choice === "choose" && <div className="summary-line"><span>Phí tự chọn người</span><strong>30.000đ</strong></div>}<div className="summary-line"><span>Dụng cụ & di chuyển</span><strong>Đã gồm</strong></div><div className="summary-total"><span>Tạm tính</span><strong>{formatCurrency(total)}</strong></div><p><Check/> Cọc 30%, thanh toán phần còn lại sau hoàn thành</p><p><Check/> Hủy trước 24 giờ được hoàn toàn bộ tiền</p></aside>}
    </div>
  );
}
