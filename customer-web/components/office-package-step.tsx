"use client";

import { useMemo, useState } from "react";
import { ArrowLeft, CalendarDays, Check, CheckCircle2, ChevronRight, Clock3, HelpCircle, Info, ListChecks, Sparkles, Users, X } from "lucide-react";
import { formatCurrency, type ServicePlan } from "@/lib/mock-data";

export type OfficeMode = "session" | "monthly";
export type OfficeTier = "under200" | "under400" | "under900";
export type OfficeBookingConfig = {
  mode: OfficeMode | "";
  tier: OfficeTier;
  planId: string;
  glass: boolean;
  carpet: boolean;
  days: string[];
  startTime: string;
  contractMonths: number;
};

type OfficePlan = { id:string;tier:OfficeTier;area:number;people:number;hours:number;price:number };

export const defaultOfficeConfig: OfficeBookingConfig = { mode:"",tier:"under200",planId:"o100",glass:false,carpet:false,days:["mon"],startTime:"08:00",contractMonths:1 };

const officePlans: OfficePlan[] = [
  {id:"o100",tier:"under200",area:100,people:1,hours:2,price:260000},
  {id:"o150",tier:"under200",area:150,people:1,hours:3,price:360000},
  {id:"o200",tier:"under200",area:200,people:1,hours:4,price:450000},
  {id:"o250",tier:"under400",area:250,people:1,hours:5,price:550000},
  {id:"o300",tier:"under400",area:300,people:2,hours:3,price:690000},
  {id:"o400",tier:"under400",area:400,people:2,hours:4,price:880000},
  {id:"o500",tier:"under900",area:500,people:2,hours:5,price:1050000},
  {id:"o750",tier:"under900",area:750,people:3,hours:5,price:1550000},
  {id:"o900",tier:"under900",area:900,people:3,hours:6,price:1850000},
];

const tiers: {id:OfficeTier;label:string}[] = [{id:"under200",label:"< 200m²"},{id:"under400",label:"< 400m²"},{id:"under900",label:"< 900m²"}];
const weekdays = [["mon","Thứ 2"],["tue","Thứ 3"],["wed","Thứ 4"],["thu","Thứ 5"],["fri","Thứ 6"],["sat","Thứ 7"],["sun","Chủ nhật"]] as const;
const discounts:Record<number,number>={1:.08,3:.12,6:.16,12:.2};
const glassPricePerPerson=120000;

const overview=["Quét bụi và lau sàn các khu vực làm việc, sảnh, hành lang.","Phủi bụi, lau dọn bàn ghế làm việc, tủ và kệ.","Lau cửa kính, cửa sổ; không hỗ trợ lau kính bên ngoài tòa nhà.","Dọn dẹp khu vực nhà vệ sinh.","Thu gom và đổ rác."];
const workAreas=[
  ["Khu vực văn phòng","Quét bụi và lau sàn nhà. Phủi bụi, lau dọn bàn ghế làm việc, tủ, kệ. Lau cửa kính, cửa sổ. Thu gom và đổ rác."],
  ["Khu vực thang máy, thang bộ","Vệ sinh mặt ngoài thang máy. Quét bụi và lau cầu thang bộ, gầm cầu thang."],
  ["Khu vực hành lang","Quét bụi và lau sàn khu vực hành lang."],
  ["Khu vực phòng vệ sinh","Lau kính, vệ sinh bồn rửa mặt. Vệ sinh sàn nhà, tường. Vệ sinh, tẩy uế bồn cầu. Thu gom và đổ rác."],
  ["Khu vực sảnh và nhà ăn (nếu có)","Quét bụi và lau sàn nhà. Phủi bụi, lau bàn ghế, tủ, kệ. Thu gom và đổ rác."],
] as const;

export function calculateOfficePlan(config:OfficeBookingConfig):ServicePlan {
  const selected=officePlans.find(item=>item.id===config.planId)??officePlans[0];
  const extras=(config.glass?glassPricePerPerson*selected.people:0)+(config.carpet?100000*selected.people:0);
  const perVisit=selected.price+extras;
  const visits=config.mode==="monthly"?Math.max(config.days.length,1)*4:1;
  const discount=config.mode==="monthly"?discounts[config.contractMonths]:0;
  const monthly=Math.round(perVisit*visits*(1-discount)/1000)*1000;
  return {id:selected.id,name:`${config.mode==="monthly"?"Gói tháng":"Theo buổi"} · Tối đa ${selected.area}m²`,description:`${selected.people} người`,meta:config.mode==="monthly"?`${visits} buổi/tháng · ${config.contractMonths} tháng`:`${selected.people} người • ${selected.hours+(config.glass?1:0)} giờ`,price:config.mode==="monthly"?monthly:perVisit};
}

export function OfficePackageStep({config,onChange}:{config:OfficeBookingConfig;onChange:(config:OfficeBookingConfig,plan:ServicePlan)=>void}) {
  const [glassHelp,setGlassHelp]=useState(false);
  const [workDetails,setWorkDetails]=useState(false);
  const plan=officePlans.find(item=>item.id===config.planId)??officePlans[0];
  const calculated=useMemo(()=>calculateOfficePlan(config),[config]);
  const update=(patch:Partial<OfficeBookingConfig>)=>{const next={...config,...patch};onChange(next,calculateOfficePlan(next))};
  const chooseTier=(tier:OfficeTier)=>{const first=officePlans.find(item=>item.tier===tier)!;update({tier,planId:first.id})};
  const toggleDay=(day:string)=>update({days:config.days.includes(day)?config.days.filter(item=>item!==day):[...config.days,day]});

  if(!config.mode)return <div className="wizard-panel office-booking-step"><h2>Chọn hình thức vệ sinh văn phòng</h2><p>Lựa chọn phương án phù hợp với tần suất vận hành của doanh nghiệp.</p><div className="office-mode-grid embedded"><button type="button" onClick={()=>update({mode:"session"})}><i><Clock3/></i><small>Linh hoạt theo nhu cầu</small><h3>Theo buổi / ngày</h3><p>Đăng việc nhanh chóng chỉ trong khoảng 60 giây, thuận tiện khi cần buổi nào đặt buổi đó.</p><strong>Chọn phương án <ChevronRight/></strong></button><button type="button" onClick={()=>update({mode:"monthly"})}><i><CalendarDays/></i><small>Lịch làm việc cố định</small><h3>Theo gói tháng</h3><p>Ưu tiên Tasker cố định, tiết kiệm thời gian đăng việc và tránh phải thanh toán nhiều lần.</p><strong>Chọn phương án <ChevronRight/></strong></button></div></div>;

  return <div className="wizard-panel office-booking-step">
    <button className="office-inline-back" type="button" onClick={()=>update({mode:""})}><ArrowLeft/> Chọn lại hình thức</button>
    <h2>{config.mode==="session"?"Dịch vụ theo buổi / ngày":"Dịch vụ theo gói tháng"}</h2>
    <p>{config.mode==="session"?"Linh hoạt chọn buổi cần làm và đặt lịch nhanh chóng.":"Thiết lập lịch làm việc cố định theo nhu cầu của quý khách."}</p>

    {config.mode==="monthly"&&<section className="office-config-section"><div className="office-section-title"><span>01</span><div><h3>Lịch làm việc theo tuần</h3><p>Chọn ngày cố định và giờ bắt đầu theo định dạng 24 giờ.</p></div></div><div className="office-weekdays">{weekdays.map(([value,label])=><button type="button" className={config.days.includes(value)?"active":""} onClick={()=>toggleDay(value)} key={value}>{config.days.includes(value)&&<Check/>}{label}</button>)}</div><div className="office-time-field office-time-24"><Clock3/><label><small>GIỜ</small><select value={config.startTime.split(":")[0]} onChange={event=>update({startTime:`${event.target.value}:${config.startTime.split(":")[1]}`})}>{Array.from({length:24},(_,index)=>String(index).padStart(2,"0")).map(hour=><option key={hour} value={hour}>{hour}</option>)}</select></label><b>:</b><label><small>PHÚT</small><select value={config.startTime.split(":")[1]} onChange={event=>update({startTime:`${config.startTime.split(":")[0]}:${event.target.value}`})}>{Array.from({length:60},(_,index)=>String(index).padStart(2,"0")).map(minute=><option key={minute} value={minute}>{minute}</option>)}</select></label><span>00:00–23:59</span></div><div className="office-contracts">{[1,3,6,12].map(months=><button type="button" className={config.contractMonths===months?"active":""} onClick={()=>update({contractMonths:months})} key={months}><strong>{months} tháng</strong><small>Giảm {discounts[months]*100}%</small></button>)}</div></section>}

    <section className="office-config-section"><div className="office-section-title"><span>{config.mode==="monthly"?"02":"01"}</span><div><h3>Chọn thời lượng</h3><p>Ước tính diện tích cần dọn dẹp và chọn phương án phù hợp.</p></div></div><div className="office-tier-tabs">{tiers.map(tier=><button type="button" className={config.tier===tier.id?"active":""} onClick={()=>chooseTier(tier.id)} key={tier.id}>{tier.label}</button>)}</div><div className="office-plan-list">{officePlans.filter(item=>item.tier===config.tier).map(item=><button type="button" className={config.planId===item.id?"active":""} onClick={()=>update({planId:item.id})} key={item.id}>{config.planId===item.id&&<CheckCircle2/>}<span><strong>Tối đa {item.area}m²</strong><small>{item.people} người / {item.hours} giờ</small></span><b>{formatCurrency(item.price)}</b></button>)}</div></section>

    <section className="office-config-section"><div className="office-section-title"><span>{config.mode==="monthly"?"03":"02"}</span><div><h3>Dịch vụ thêm</h3><p>Bạn có thể chọn thêm cho mỗi lần làm việc.</p></div></div><div className="office-extras"><label className={config.glass?"active":""}><input type="checkbox" checked={config.glass} onChange={event=>update({glass:event.target.checked})}/><i><Sparkles/></i><span><strong>Lau kính</strong><small>+1 giờ/người · +{formatCurrency(glassPricePerPerson)}/người</small></span><button type="button" onClick={event=>{event.preventDefault();setGlassHelp(true)}}><HelpCircle/></button></label><label className={config.carpet?"active":""}><input type="checkbox" checked={config.carpet} onChange={event=>update({carpet:event.target.checked})}/><i><Sparkles/></i><span><strong>Hút bụi thảm văn phòng</strong><small>+100.000đ/người</small></span></label></div></section>

    <button className="office-work-button" type="button" onClick={()=>setWorkDetails(true)}><ListChecks/><span><strong>Chi tiết công việc</strong><small>Xem nội dung tại từng khu vực</small></span><ChevronRight/></button>
    <div className="office-inline-total"><span>{config.mode==="monthly"?"Tạm tính mỗi tháng":"Tạm tính"}</span><strong>{formatCurrency(calculated.price)}</strong><small>{plan.people} người · {plan.hours+(config.glass?1:0)} giờ mỗi buổi{config.glass?" · đã gồm phí lau kính":""}</small></div>

    {glassHelp&&<div className="office-nested-backdrop"><div className="office-help-dialog"><Info/><h3>Dịch vụ lau kính</h3><p>Áp dụng cho văn phòng có tổng diện tích cửa kính, cửa sổ tối thiểu 100m².</p><p>Không hỗ trợ lau kính bên ngoài tòa nhà.</p><button type="button" onClick={()=>setGlassHelp(false)}>Đã hiểu</button></div></div>}
    {workDetails&&<div className="office-nested-backdrop details" onMouseDown={event=>{if(event.currentTarget===event.target)setWorkDetails(false)}}><div className="office-work-dialog"><header><div><span>PHẠM VI CÔNG VIỆC</span><h3>Chi tiết vệ sinh văn phòng</h3></div><button type="button" onClick={()=>setWorkDetails(false)}><X/></button></header><section><h4>Tổng quát</h4><ul>{overview.map(item=><li key={item}><Check/>{item}</li>)}</ul></section><div className="office-work-table">{workAreas.map(([title,copy])=><div key={title}><strong>{title}</strong><p>{copy}</p></div>)}</div><button className="office-understood" type="button" onClick={()=>setWorkDetails(false)}>Đã hiểu</button></div></div>}
  </div>;
}
