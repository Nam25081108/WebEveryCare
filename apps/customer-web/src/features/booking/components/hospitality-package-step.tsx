"use client";

import { useMemo, useState } from "react";
import { ArrowLeft, Check, ChevronRight, Hotel, ListChecks, Minus, Plus, X } from "lucide-react";
import { formatCurrency, type ServicePlan } from "@/features/booking/lib/mock-data";

export type HospitalityKind = "hotel" | "apartment" | "villa";
export type HospitalityBookingConfig = {
  stage: "details" | "rooms";
  facilityName: string;
  contactName: string;
  contactPhone: string;
  kind: HospitalityKind;
  quantities: Record<string, number>;
};

type RoomOption = { id:string; name:string; description:string; price:number; durationMinutes:number };

export const defaultHospitalityConfig: HospitalityBookingConfig = {
  stage:"details", facilityName:"", contactName:"", contactPhone:"", kind:"hotel", quantities:{}
};

export const hospitalityKinds: {id:HospitalityKind;label:string;copy:string}[] = [
  {id:"hotel",label:"Khách sạn / Homestay",copy:"Phòng lưu trú và phòng DORM"},
  {id:"apartment",label:"Căn hộ dịch vụ",copy:"Căn hộ từ 1 đến 3 phòng ngủ"},
  {id:"villa",label:"Villa / Nhà nguyên căn",copy:"Phòng riêng trong villa hoặc nhà nguyên căn"},
];

export const hospitalityOptions:Record<HospitalityKind,RoomOption[]> = {
  hotel:[
    {id:"hotel-single",name:"Phòng đơn",description:"Tối đa 15m²",price:90000,durationMinutes:45},
    {id:"hotel-double",name:"Phòng đôi",description:"Tối đa 25m²",price:130000,durationMinutes:60},
    {id:"hotel-family",name:"Phòng gia đình",description:"Tối đa 50m²",price:190000,durationMinutes:90},
    {id:"hotel-dorm-under-8",name:"Phòng DORM dưới 8 giường",description:"Tối đa 7 giường",price:220000,durationMinutes:120},
    {id:"hotel-dorm-8-plus",name:"Phòng DORM từ 8 giường",description:"Từ 8 giường trở lên",price:300000,durationMinutes:150},
  ],
  apartment:[
    {id:"apartment-1br",name:"1 phòng ngủ",description:"Tối đa 50m²",price:250000,durationMinutes:120},
    {id:"apartment-2br",name:"2 phòng ngủ",description:"Tối đa 70m²",price:350000,durationMinutes:180},
    {id:"apartment-3br",name:"3 phòng ngủ",description:"Tối đa 100m²",price:450000,durationMinutes:240},
  ],
  villa:[
    {id:"villa-single",name:"Phòng đơn",description:"Tối đa 15m²",price:100000,durationMinutes:45},
    {id:"villa-double",name:"Phòng đôi",description:"Tối đa 25m²",price:140000,durationMinutes:60},
    {id:"villa-family",name:"Phòng gia đình",description:"Tối đa 50m²",price:200000,durationMinutes:90},
  ],
};

const overview=[
  "Tiếp nhận thông tin và các yêu cầu đặc biệt của khách hàng trước khi bắt đầu công việc.",
  "Tắt các thiết bị điện không cần thiết và thu dọn rác nổi trong phòng.",
  "Thay hàng vải sạch cho ga giường, vỏ gối, vỏ chăn.",
  "Vệ sinh các kệ, tủ, sàn nhà và khu vực nhà vệ sinh.",
  "Kiểm tra các thiết bị rò rỉ, hư hỏng.",
  "Thay hoặc bổ sung các đồ dùng nhà tắm.",
  "Kiểm tra toàn bộ phòng, bảo đảm không có mùi lạ, vết bẩn hay dụng cụ vệ sinh để quên.",
];
const workAreas=[
  ["Giường ngủ","Thay hàng vải bẩn: ga giường, vỏ gối, vỏ chăn. Kiểm tra miếng lót đệm và bề mặt đệm."],
  ["Nhà vệ sinh","Kiểm tra thiết bị rò rỉ, hư hỏng. Chà rửa bồn rửa mặt, phòng tắm, bồn tắm và bồn cầu. Thay hoặc bổ sung đồ dùng nhà tắm."],
  ["Vệ sinh chung","Vệ sinh các bề mặt và đồ nội thất."],
  ["Kiểm tra phòng","Kiểm tra đầy đủ vật dụng và không gian trong phòng."],
] as const;

export function calculateHospitalityPlan(config:HospitalityBookingConfig):ServicePlan {
  const selected=hospitalityOptions[config.kind].filter(item=>(config.quantities[item.id]??0)>0);
  const rooms=selected.reduce((sum,item)=>sum+(config.quantities[item.id]??0),0);
  const price=selected.reduce((sum,item)=>sum+item.price*(config.quantities[item.id]??0),0);
  return {id:"hospitality-custom",name:hospitalityKinds.find(item=>item.id===config.kind)?.label??"Dọn dẹp buồng phòng",description:`${rooms} phòng/căn`,meta:`${rooms} hạng mục đã chọn`,price};
}

export function HospitalityPackageStep({config,onChange}:{config:HospitalityBookingConfig;onChange:(config:HospitalityBookingConfig,plan:ServicePlan)=>void}) {
  const [details,setDetails]=useState(false);
  const options=hospitalityOptions[config.kind];
  const calculated=useMemo(()=>calculateHospitalityPlan(config),[config]);
  const update=(patch:Partial<HospitalityBookingConfig>)=>{const next={...config,...patch};onChange(next,calculateHospitalityPlan(next))};
  const selectKind=(kind:HospitalityKind)=>update({kind,quantities:{}});
  const changeQuantity=(id:string,delta:number)=>update({quantities:{...config.quantities,[id]:Math.max(0,(config.quantities[id]??0)+delta)}});

  if(config.stage==="details")return <div className="wizard-panel hospitality-step"><h2>Xác nhận thông tin cơ sở lưu trú</h2><p>Thông tin này giúp đối tác đến đúng cơ sở và liên hệ thuận tiện trước khi làm việc.</p><div className="hospitality-form"><label><span>Tên khách sạn, homestay hoặc căn hộ</span><input value={config.facilityName} onChange={event=>update({facilityName:event.target.value})} placeholder="Ví dụ: EveryCare Riverside"/></label><label><span>Tên người liên hệ</span><input value={config.contactName} onChange={event=>update({contactName:event.target.value})} placeholder="Nhập họ và tên"/></label><label><span>Số điện thoại liên hệ</span><input inputMode="tel" value={config.contactPhone} onChange={event=>update({contactPhone:event.target.value})} placeholder="09xx xxx xxx"/></label></div></div>;

  return <div className="wizard-panel hospitality-step"><button type="button" className="office-inline-back" onClick={()=>update({stage:"details"})}><ArrowLeft/> Sửa thông tin cơ sở</button><h2>Chọn loại hình dịch vụ</h2><p>Chọn loại hình và tăng số lượng phòng hoặc căn cần dọn dẹp.</p><div className="hospitality-kind-grid">{hospitalityKinds.map(item=><button type="button" className={config.kind===item.id?"active":""} onClick={()=>selectKind(item.id)} key={item.id}><Hotel/><span><strong>{item.label}</strong><small>{item.copy}</small></span>{config.kind===item.id&&<Check/>}</button>)}</div><section className="hospitality-room-section"><h3>{config.kind==="apartment"?"Chọn loại căn hộ":"Chọn loại phòng"}</h3><p>Vui lòng chọn loại {config.kind==="apartment"?"căn hộ":"phòng"} và số lượng cần dọn dẹp.</p><div className="hospitality-room-list">{options.map(item=><div key={item.id}><span><strong>{item.name}</strong><small>{item.description} · {formatCurrency(item.price)}/{config.kind==="apartment"?"căn":"phòng"}</small></span><div className="quantity-control"><button type="button" onClick={()=>changeQuantity(item.id,-1)} disabled={!config.quantities[item.id]} aria-label={`Giảm ${item.name}`}><Minus/></button><b>{config.quantities[item.id]??0}</b><button type="button" onClick={()=>changeQuantity(item.id,1)} aria-label={`Tăng ${item.name}`}><Plus/></button></div></div>)}</div></section><button className="office-work-button" type="button" onClick={()=>setDetails(true)}><ListChecks/><span><strong>Chi tiết công việc</strong><small>Xem nội dung vệ sinh buồng phòng</small></span><ChevronRight/></button><div className="office-inline-total"><span>Tạm tính</span><strong>{formatCurrency(calculated.price)}</strong><small>{calculated.description}</small></div>{details&&<div className="office-nested-backdrop details" onMouseDown={event=>{if(event.currentTarget===event.target)setDetails(false)}}><div className="office-work-dialog"><header><div><span>PHẠM VI CÔNG VIỆC</span><h3>Chi tiết dọn dẹp buồng phòng</h3></div><button type="button" onClick={()=>setDetails(false)}><X/></button></header><section><h4>Tổng quát</h4><ul>{overview.map(item=><li key={item}><Check/>{item}</li>)}</ul></section><div className="office-work-table">{workAreas.map(([title,copy])=><div key={title}><strong>{title}</strong><p>{copy}</p></div>)}</div><button className="office-understood" type="button" onClick={()=>setDetails(false)}>Đã hiểu</button></div></div>}</div>;
}
