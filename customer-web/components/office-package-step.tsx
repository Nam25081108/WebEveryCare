"use client";

import { useMemo, useState } from "react";
import { Check, CheckCircle2, ChevronRight, HelpCircle, Info, ListChecks, Sparkles, Users, X } from "lucide-react";
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

export type OfficePlan = { id:string;tier:OfficeTier;area:number;people:number;hours:number;price:number };

export const defaultOfficeConfig: OfficeBookingConfig = { mode:"session",tier:"under200",planId:"o100",glass:false,carpet:false,days:[],startTime:"08:00",contractMonths:1 };

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

export function calculateOfficePlan(config:OfficeBookingConfig,plans:OfficePlan[]=officePlans):ServicePlan {
  const selected=plans.find(item=>item.id===config.planId)??plans[0]??officePlans[0];
  const extras=(config.glass?glassPricePerPerson*selected.people:0)+(config.carpet?100000*selected.people:0);
  const perVisit=selected.price+extras;
  const dayNumbers:Record<string,number>={sun:0,mon:1,tue:2,wed:3,thu:4,fri:5,sat:6};
  let visits=1;
  if(config.mode==="monthly"&&config.days.length){const earliest=new Date(Date.now()+2*24*60*60*1000);const firstCandidates=config.days.map(day=>{const value=new Date(earliest);value.setDate(value.getDate()+((dayNumbers[day]-value.getDay()+7)%7));value.setHours(Number(config.startTime.split(":")[0]),Number(config.startTime.split(":")[1]),0,0);if(value.getTime()<earliest.getTime())value.setDate(value.getDate()+7);return value}).sort((a,b)=>a.getTime()-b.getTime());const first=firstCandidates[0];const end=new Date(first);end.setMonth(end.getMonth()+config.contractMonths);visits=0;for(const cursor=new Date(first);cursor<end;cursor.setDate(cursor.getDate()+1))if(config.days.some(day=>dayNumbers[day]===cursor.getDay()))visits++}
  const discount=config.mode==="monthly"?discounts[config.contractMonths]:0;
  const discountedVisit=Math.round(selected.price*(1-discount)/1000)*1000+(config.glass?Math.round(glassPricePerPerson*selected.people*(1-discount)/1000)*1000:0)+(config.carpet?Math.round(100000*selected.people*(1-discount)/1000)*1000:0);
  const contractTotal=discountedVisit*visits;
  return {id:selected.id,name:`${config.mode==="monthly"?"Gói định kỳ":"Theo buổi"} · Tối đa ${selected.area}m²`,description:`${selected.people} người`,meta:config.mode==="monthly"?`${visits} lượt · ${config.contractMonths} tháng`:`${selected.people} người • ${selected.hours+(config.glass?1:0)} giờ`,price:config.mode==="monthly"?contractTotal:perVisit};
}

export function OfficePackageStep({config,onChange,plans=officePlans}:{config:OfficeBookingConfig;onChange:(config:OfficeBookingConfig,plan:ServicePlan)=>void;plans?:OfficePlan[]}) {
  const [glassHelp,setGlassHelp]=useState(false);
  const [workDetails,setWorkDetails]=useState(false);
  const plan=plans.find(item=>item.id===config.planId)??plans[0]??officePlans[0];
  const calculated=useMemo(()=>calculateOfficePlan(config,plans),[config,plans]);
  const update=(patch:Partial<OfficeBookingConfig>)=>{const next={...config,...patch};onChange(next,calculateOfficePlan(next,plans))};
  const chooseTier=(tier:OfficeTier)=>{const first=plans.find(item=>item.tier===tier);if(first)update({tier,planId:first.id})};

  return <div className="wizard-panel office-booking-step">
    <h2>Dịch vụ theo buổi / ngày</h2>
    <p>Linh hoạt chọn buổi cần làm và đặt lịch nhanh chóng.</p>

    <section className="office-config-section"><div className="office-section-title"><span>{config.mode==="monthly"?"02":"01"}</span><div><h3>Chọn thời lượng</h3><p>Ước tính diện tích cần dọn dẹp và chọn phương án phù hợp.</p></div></div><div className="office-tier-tabs">{tiers.map(tier=><button type="button" className={config.tier===tier.id?"active":""} onClick={()=>chooseTier(tier.id)} key={tier.id}>{tier.label}</button>)}</div><div className="office-plan-list">{plans.filter(item=>item.tier===config.tier).map(item=><button type="button" className={config.planId===item.id?"active":""} onClick={()=>update({planId:item.id})} key={item.id}>{config.planId===item.id&&<CheckCircle2/>}<span><strong>Tối đa {item.area}m²</strong><small>{item.people} người / {item.hours} giờ</small></span><b>{formatCurrency(item.price)}</b></button>)}</div></section>

    <section className="office-config-section"><div className="office-section-title"><span>{config.mode==="monthly"?"03":"02"}</span><div><h3>Dịch vụ thêm</h3><p>Bạn có thể chọn thêm cho mỗi lần làm việc.</p></div></div><div className="office-extras"><label className={config.glass?"active":""}><input type="checkbox" checked={config.glass} onChange={event=>update({glass:event.target.checked})}/><i><Sparkles/></i><span><strong>Lau kính</strong><small>+1 giờ/người · +{formatCurrency(glassPricePerPerson)}/người</small></span><button type="button" onClick={event=>{event.preventDefault();setGlassHelp(true)}}><HelpCircle/></button></label><label className={config.carpet?"active":""}><input type="checkbox" checked={config.carpet} onChange={event=>update({carpet:event.target.checked})}/><i><Sparkles/></i><span><strong>Hút bụi thảm văn phòng</strong><small>+100.000đ/người</small></span></label></div></section>

    <button className="office-work-button" type="button" onClick={()=>setWorkDetails(true)}><ListChecks/><span><strong>Chi tiết công việc</strong><small>Xem nội dung tại từng khu vực</small></span><ChevronRight/></button>
    <div className="office-inline-total"><span>{config.mode==="monthly"?`Tạm tính toàn bộ ${config.contractMonths} tháng`:"Tạm tính"}</span><strong>{formatCurrency(calculated.price)}</strong><small>{plan.people} người · {plan.hours+(config.glass?1:0)} giờ mỗi buổi{config.glass?" · đã gồm phí lau kính":""}</small></div>

    {glassHelp&&<div className="office-nested-backdrop"><div className="office-help-dialog"><Info/><h3>Dịch vụ lau kính</h3><p>Áp dụng cho văn phòng có tổng diện tích cửa kính, cửa sổ tối thiểu 100m².</p><p>Không hỗ trợ lau kính bên ngoài tòa nhà.</p><button type="button" onClick={()=>setGlassHelp(false)}>Đã hiểu</button></div></div>}
    {workDetails&&<div className="office-nested-backdrop details" onMouseDown={event=>{if(event.currentTarget===event.target)setWorkDetails(false)}}><div className="office-work-dialog"><header><div><span>PHẠM VI CÔNG VIỆC</span><h3>Chi tiết vệ sinh văn phòng</h3></div><button type="button" onClick={()=>setWorkDetails(false)}><X/></button></header><section><h4>Tổng quát</h4><ul>{overview.map(item=><li key={item}><Check/>{item}</li>)}</ul></section><div className="office-work-table">{workAreas.map(([title,copy])=><div key={title}><strong>{title}</strong><p>{copy}</p></div>)}</div><button className="office-understood" type="button" onClick={()=>setWorkDetails(false)}>Đã hiểu</button></div></div>}
  </div>;
}
