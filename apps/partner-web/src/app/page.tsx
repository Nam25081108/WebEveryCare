"use client";

import dynamic from "next/dynamic";
import Link from "next/link";
import { FormEvent, useState } from "react";
import { AirVent, AlertCircle, ArrowRight, BadgeCheck, Boxes, Check, CircuitBoard, IdCard, LoaderCircle, ShieldCheck, Sparkles, Truck, UserRound, Users, Wrench } from "lucide-react";
import { PartnerHeader } from "@/components/layout/partner-header";
import type { MapLocation } from "@/features/location/partner-location-map";

const LocationMap=dynamic(()=>import("@/features/location/partner-location-map"),{ssr:false});
const API=process.env.NEXT_PUBLIC_API_BASE_URL??"http://localhost:5185";
const CUSTOMER_WEB=process.env.NEXT_PUBLIC_CUSTOMER_WEB_URL??"http://localhost:3000";
type PartnerType="Individual"|"Team";
type Member={fullName:string;phone:string;identityNumber:string};

export default function PartnerPage(){
 const [type,setType]=useState<PartnerType>("Individual");
 const [teamSize,setTeamSize]=useState(2);
 const [members,setMembers]=useState<Member[]>([{fullName:"",phone:"",identityNumber:""}]);
 const [services,setServices]=useState<string[]>(["ve-sinh-phong-le"]);
 const [serviceCategory,setServiceCategory]=useState("cleaning");
 const [location,setLocation]=useState<MapLocation|null>(null);
 const [submitting,setSubmitting]=useState(false);const [error,setError]=useState("");const [success,setSuccess]=useState(false);

 function changeType(next:PartnerType){setType(next);setServices(next==="Individual"?["ve-sinh-phong-le"]:["tong-ve-sinh"]);setTeamSize(2);setMembers([{fullName:"",phone:"",identityNumber:""}]);}
 function changeTeamSize(next:number){const size=Math.max(2,Math.min(12,next));setTeamSize(size);setMembers(current=>Array.from({length:size-1},(_,i)=>current[i]??{fullName:"",phone:"",identityNumber:""}));}
 function changeMember(index:number,key:keyof Member,value:string){setMembers(current=>current.map((item,i)=>i===index?{...item,[key]:value}:item));}
 function toggleService(slug:string){setServices(current=>current.includes(slug)?current.filter(x=>x!==slug):[...current,slug]);}

 async function submit(event:FormEvent<HTMLFormElement>){
  event.preventDefault();setError("");if(!location){setError("Vui lòng chọn vị trí chính xác trên bản đồ.");return;}if(!services.length){setError("Vui lòng chọn ít nhất một dịch vụ.");return;}
  const form=new FormData(event.currentTarget);form.set("partnerType",type);form.set("teamSize",String(type==="Team"?teamSize:1));form.set("latitude",String(location.latitude));form.set("longitude",String(location.longitude));form.set("teamMembersJson",JSON.stringify(type==="Team"?members:[]));form.delete("serviceGroupSlugs");services.forEach(slug=>form.append("serviceGroupSlugs",slug));
  setSubmitting(true);
  try{const response=await fetch(`${API}/api/partner-applications`,{method:"POST",body:form});const data=await response.json();if(!response.ok)throw new Error(data.message??"Không thể gửi hồ sơ.");setSuccess(true);window.scrollTo({top:0,behavior:"smooth"});}
  catch(reason){setError(reason instanceof TypeError?"Không thể kết nối backend. Hãy kiểm tra API đang chạy ở cổng 5185.":reason instanceof Error?reason.message:"Không thể gửi hồ sơ.");}
  finally{setSubmitting(false);}
 }

 if(success)return <main className="partner-page"><PartnerHeader/><section className="partner-result shell"><span><BadgeCheck/></span><h1>Đã gửi hồ sơ xét duyệt</h1><p>Hồ sơ đã được lưu vào hệ thống và chuyển tới quản trị viên. Sau khi được duyệt, bạn sẽ nhận thông báo qua Gmail và có thể đăng nhập bằng số điện thoại đã đăng ký.</p><div><Link className="button" href="/dang-nhap">Đến trang đăng nhập đối tác</Link><a className="button button-ghost" href={CUSTOMER_WEB}>Về trang khách hàng</a></div></section></main>;

 return <main className="partner-page"><PartnerHeader/><section className="partner-hero"><div className="shell partner-hero-grid"><div className="partner-intro"><span className="eyebrow light">Trở thành đối tác EveryCare</span><h1>Chủ động công việc.<br/><em>Gia tăng thu nhập.</em></h1><p>Đăng ký lịch rảnh, nhận những công việc phù hợp gần khu vực của bạn và không cần luôn mở website.</p><div className="partner-points"><span><Check/> Miễn phí đăng ký</span><span><Check/> Tự chọn lịch làm việc</span><span><Check/> Lời mời được lưu khi ngoại tuyến</span></div><Link href="/dang-nhap" className="partner-login-link">Đã có hồ sơ được duyệt? Đăng nhập đối tác →</Link></div>
 <form className="partner-form partner-application-form" onSubmit={submit}><div className="form-heading"><span><BadgeCheck/></span><div><h2>Đăng ký đối tác</h2><p>Thông tin được chuyển tới admin để xét duyệt</p></div></div>
 <label className="form-label">Bạn đăng ký theo hình thức nào?</label><div className="type-picker"><button type="button" className={type==="Individual"?"active":""} onClick={()=>changeType("Individual")}><UserRound/><strong>Cá nhân</strong><small>Dọn nhà, văn phòng hoặc buồng phòng</small></button><button type="button" className={type==="Team"?"active":""} onClick={()=>changeType("Team")}><Users/><strong>Đội nhóm</strong><small>Tổng vệ sinh hoặc chuyên nghiệp</small></button></div>
 <div className="form-grid">
  <label><span>Họ tên {type==="Team"&&"trưởng nhóm"}</span><input name="fullName" required minLength={3} placeholder="Nguyễn Văn A"/></label>
  <label><span>Số điện thoại đăng nhập</span><input name="phone" required inputMode="numeric" pattern="0[0-9]{9}" placeholder="0901234567"/></label>
  <label><span>Gmail nhận kết quả</span><input name="email" required type="email" pattern="[^@ ]+@gmail\.com" placeholder="tenban@gmail.com"/></label>
  <label><span>Số CCCD/CMND</span><input name="identityNumber" required inputMode="numeric" pattern="[0-9]{9}|[0-9]{12}" placeholder="12 chữ số"/></label>
  <label><span>Mật khẩu đăng nhập</span><input name="password" required type="password" minLength={8} placeholder="Tối thiểu 8 ký tự"/></label>
  {type==="Team"&&<label><span>Tên đội nhóm</span><input name="teamName" required placeholder="Đội An Tâm"/></label>}
  <label className="full"><span>Địa chỉ hiện tại/khu vực xuất phát</span><input name="serviceAddress" required minLength={10} placeholder="Số nhà, đường, phường/xã mới, TP.HCM"/></label>
  {type==="Team"&&<label className="full"><span>Số người trong đội (gồm trưởng nhóm)</span><div className="counter"><button type="button" onClick={()=>changeTeamSize(teamSize-1)}>−</button><strong>{teamSize} người</strong><button type="button" onClick={()=>changeTeamSize(teamSize+1)}>+</button></div></label>}
  <div className="full partner-service-picker"><span className="form-label">Chọn nhóm dịch vụ muốn đăng ký</span><div className="partner-service-categories">{[["cleaning","Vệ sinh",Sparkles,true],["repair","Gọi thợ",Wrench,false],["cooling","Điện lạnh",AirVent,false],["electronics","Điện tử gia dụng",CircuitBoard,false],["moving","Nội thất & vận chuyển",Truck,false],["other","Dịch vụ khác",Boxes,false]].map(([id,label,Icon,enabled])=>{const CategoryIcon=Icon as typeof Sparkles;return <button type="button" disabled={!enabled} className={serviceCategory===id?"active":""} onClick={()=>setServiceCategory(String(id))} key={String(id)}><CategoryIcon/><strong>{String(label)}</strong>{!enabled&&<small>Sắp mở</small>}</button>})}</div>{serviceCategory==="cleaning"&&<div className="partner-service-packages"><header><strong>Các dịch vụ thuộc nhóm Vệ sinh</strong><small>Chọn những công việc bạn có thể thực hiện</small></header><div className="check-options">{type==="Individual"?<><label><input type="checkbox" checked={services.includes("ve-sinh-phong-le")} onChange={()=>toggleService("ve-sinh-phong-le")}/><span>Dọn dẹp nhà cửa<small>Theo giờ · Một người thực hiện</small></span></label><label><input type="checkbox" checked={services.includes("don-dep-van-phong-dinh-ky")} onChange={()=>toggleService("don-dep-van-phong-dinh-ky")}/><span>Dọn dẹp văn phòng<small>Chỉ nhận gói cần 1 người</small></span></label><label><input type="checkbox" checked={services.includes("don-dep-buong-phong")} onChange={()=>toggleService("don-dep-buong-phong")}/><span>Dọn dẹp buồng phòng<small>Một người thực hiện</small></span></label></>:<><label><input type="checkbox" checked={services.includes("tong-ve-sinh")} onChange={()=>toggleService("tong-ve-sinh")}/><span>Tổng vệ sinh<small>Đội phải có đủ số người yêu cầu</small></span></label><label><input type="checkbox" checked={services.includes("ve-sinh-chuyen-nghiep")} onChange={()=>toggleService("ve-sinh-chuyen-nghiep")}/><span>Vệ sinh chuyên nghiệp<small>Chỉ dành cho đội nhóm</small></span></label><label><input type="checkbox" checked={services.includes("don-dep-van-phong-dinh-ky")} onChange={()=>toggleService("don-dep-van-phong-dinh-ky")}/><span>Dọn dẹp văn phòng<small>Nhận gói 2–3 người khi đội đủ người</small></span></label><label><input type="checkbox" checked={services.includes("ve-sinh-van-phong-chuyen-sau")} onChange={()=>toggleService("ve-sinh-van-phong-chuyen-sau")}/><span>Vệ sinh văn phòng chuyên sâu<small>Văn phòng quy mô lớn</small></span></label></>}</div></div>}</div>
 </div>
 <LocationMap value={location} onChange={setLocation}/>
 {type==="Team"&&<section className="team-members"><h3>Thông tin thành viên</h3><p>Không nhập lại trưởng nhóm. Cần đủ {teamSize-1} thành viên còn lại.</p>{members.map((member,index)=><div className="team-member-row" key={index}><strong>Thành viên {index+1}</strong><input required value={member.fullName} onChange={e=>changeMember(index,"fullName",e.target.value)} placeholder="Họ và tên"/><input required inputMode="numeric" pattern="0[0-9]{9}" value={member.phone} onChange={e=>changeMember(index,"phone",e.target.value)} placeholder="Số điện thoại"/><input inputMode="numeric" value={member.identityNumber} onChange={e=>changeMember(index,"identityNumber",e.target.value)} placeholder="Số CCCD"/></div>)}</section>}
 <div className="identity-uploads"><label className="upload-box"><IdCard/><span><strong>CCCD mặt trước</strong><small>JPG, PNG, WEBP • Tối đa 5MB</small></span><input name="identityFront" required type="file" accept="image/jpeg,image/png,image/webp"/></label><label className="upload-box"><IdCard/><span><strong>CCCD mặt sau</strong><small>JPG, PNG, WEBP • Tối đa 5MB</small></span><input name="identityBack" required type="file" accept="image/jpeg,image/png,image/webp"/></label></div>
 {error&&<div className="partner-form-error" role="alert"><AlertCircle/>{error}</div>}<button disabled={submitting} className="button button-large full-button">{submitting?<><LoaderCircle className="spin"/> Đang gửi hồ sơ...</>:<>Gửi đăng ký xét duyệt <ArrowRight/></>}</button><p className="form-terms">Bằng việc đăng ký, bạn đồng ý để EveryCare xác minh thông tin hồ sơ.</p></form></div></section>
 <section className="partner-benefits shell"><article><span><ShieldCheck/></span><h3>Hồ sơ được bảo mật</h3><p>Ảnh giấy tờ được lưu riêng và chỉ dùng để xét duyệt.</p></article><article><span><Users/></span><h3>Đúng dịch vụ, đúng khu vực</h3><p>Đối tác nhận lời mời phù hợp trong phạm vi tối đa 10 km.</p></article><article><span><BadgeCheck/></span><h3>Duyệt minh bạch</h3><p>Admin kiểm tra hồ sơ trước khi kích hoạt tài khoản.</p></article></section></main>;
}
