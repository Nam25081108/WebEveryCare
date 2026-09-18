"use client";

import { useEffect, useState } from "react";
import { Bath, BedDouble, BriefcaseBusiness, Building2, Check, ChevronRight, CookingPot, FlaskConical, Home, Info, ListChecks, Sofa, Sparkles, SquareStack, Wrench, X } from "lucide-react";
import { formatCurrency, professionalInfo, Service, ServicePlan, WorkDetails } from "@/lib/mock-data";
import { calculateProfessionalPlan, ProfessionalConfig } from "@/lib/professional-pricing";

type DetailModal = { type: "work"; title: string; details: WorkDetails } | { type: "process" } | { type: "tools" } | null;
type StoredGroup = { id:string; status?:"active"|"locked"; process?:string[]; tools?:{name:string;description:string}[]; plans?:{id:string;name:string;price:number;details:string;status?:"active"|"locked";workDetails?:WorkDetails}[] };

export function ServicePackageStep({ service, planId, onPlanChange, professionalConfig, onProfessionalChange, professionalError }: {
  service: Service;
  planId: string;
  onPlanChange: (plan: ServicePlan) => void;
  professionalConfig: ProfessionalConfig;
  onProfessionalChange: (value: ProfessionalConfig) => void;
  professionalError: string;
}) {
  const [modal, setModal] = useState<DetailModal>(null);
  const [storedGroup, setStoredGroup] = useState<StoredGroup | null>(null);
  const professionalPlan = calculateProfessionalPlan(professionalConfig);
  const update = (patch: Partial<ProfessionalConfig>) => onProfessionalChange({ ...professionalConfig, ...patch });
  useEffect(() => {
    try {
      const groups = JSON.parse(localStorage.getItem("everycare_admin_services_v2") ?? localStorage.getItem("everycare_admin_services") ?? "[]") as StoredGroup[];
      setStoredGroup(groups.find((group) => group.id === service.id) ?? null);
    } catch { setStoredGroup(null); }
  }, [service.id]);
  const displayedPlans: ServicePlan[] = storedGroup?.plans?.filter((plan)=>plan.status!=="locked").map((plan)=>({id:plan.id,name:plan.name,description:plan.details,meta:plan.details,price:plan.price,workDetails:plan.workDetails??service.plans.find(item=>item.id===plan.id)?.workDetails})) ?? service.plans;
  const groupWorkDetails = displayedPlans.find((plan) => plan.workDetails)?.workDetails;
  const process = storedGroup?.process?.length ? storedGroup.process : professionalInfo.process;
  const tools = storedGroup?.tools?.length ? storedGroup.tools : professionalInfo.tools;

  return <div className="wizard-panel"><h2>Bạn cần dịch vụ nào?</h2><p>Chọn nhóm dịch vụ, sau đó cấu hình gói phù hợp với không gian.</p>
    {service.id !== "professional" ? <div><div className="booking-plans detailed-plans">{displayedPlans.map((item) => <div className={planId === item.id ? "booking-plan selected" : "booking-plan"} key={item.id} onClick={() => onPlanChange(item)} role="radio" aria-checked={planId === item.id} tabIndex={0}><input type="radio" name="plan" checked={planId === item.id} onChange={() => onPlanChange(item)}/><span><strong>{item.name}</strong><small>{item.description}{item.meta !== item.description ? ` • ${item.meta}` : ""}</small></span><b>{formatCurrency(item.price)}</b></div>)}</div>{groupWorkDetails && <button type="button" className="group-work-detail" onClick={() => setModal({ type:"work", title:service.name, details:groupWorkDetails })}><span><ListChecks/></span><span><strong>Chi tiết công việc</strong><small>Xem người dọn sẽ thực hiện những công việc gì tại từng phòng</small></span><ChevronRight/></button>}</div> : <div className="professional-config">
      <ConfigSection number="1" title="Loại công trình"><div className="professional-options two"><Option active={professionalConfig.building === "house"} icon={<Home/>} title="Nhà ở" copy="Căn hộ, nhà phố, biệt thự" onClick={() => update({building:"house"})}/><Option active={professionalConfig.building === "office"} icon={<BriefcaseBusiness/>} title="Công ty / văn phòng" copy="Không gian làm việc, trụ sở" onClick={() => update({building:"office"})}/></div></ConfigSection>
      <ConfigSection number="2" title="Tình trạng công trình"><div className="professional-options two"><Option active={professionalConfig.condition === "old"} icon={<Building2/>} title={`${professionalConfig.building === "house" ? "Nhà" : "Văn phòng"} lâu năm`} copy="Đang sử dụng, cần làm sạch sâu" onClick={() => update({condition:"old",furnished:false})}/><Option active={professionalConfig.condition === "new"} icon={<Sparkles/>} title="Mới xây / sửa chữa" copy="Bụi xây dựng và vết bẩn thi công" onClick={() => update({condition:"new"})}/></div>{professionalConfig.condition === "new" && <label className="furniture-toggle"><span><Sofa/><span><strong>Công trình có nội thất</strong><small>Tăng 25% do cần vệ sinh và bảo vệ nội thất</small></span></span><input type="checkbox" checked={professionalConfig.furnished} onChange={(event) => update({furnished:event.target.checked})}/><i/></label>}</ConfigSection>
      <ConfigSection number="3" title="Diện tích cần vệ sinh"><div className="area-options">{([['under60','< 60m²'],['60to80','60–80m²'],['81to100','81–100m²'],['custom','Diện tích khác']] as const).map(([value,label])=><button type="button" className={professionalConfig.areaTier===value?"active":""} onClick={()=>update({areaTier:value})} key={value}>{professionalConfig.areaTier===value&&<Check/>}{label}</button>)}</div>{professionalConfig.areaTier === "custom" && <div className={professionalError?"custom-area invalid":"custom-area"}><label><span>Nhập diện tích từ 101 đến 500m²</span><div><input type="number" min="101" max="500" value={professionalConfig.customArea || ""} onChange={(event)=>update({customArea:Number(event.target.value)})}/><b>m²</b></div></label><span>Đơn giá: {formatCurrency(professionalConfig.building === "house" ? professionalConfig.condition === "old" ? 33000 : 30000 : professionalConfig.condition === "old" ? 42000 : 38000)}/m²</span>{professionalError&&<small>{professionalError}</small>}</div>}</ConfigSection>
      <div className="professional-total"><div><small>Giá dự kiến đã gồm dụng cụ & di chuyển</small><strong>{formatCurrency(professionalPlan.price)}</strong></div><span>{professionalPlan.meta}</span></div>
      <div className="professional-info-links"><button type="button" onClick={()=>setModal({type:"process"})}><ListChecks/><span><strong>Quy trình vệ sinh chuyên nghiệp</strong><small>Xem các bước thực hiện và bàn giao</small></span><ChevronRight/></button><button type="button" onClick={()=>setModal({type:"tools"})}><Wrench/><span><strong>Công cụ, dụng cụ & hóa chất</strong><small>Xem thiết bị có thể được sử dụng</small></span><ChevronRight/></button></div>
    </div>}
    {modal && <DetailDialog modal={modal} process={process} tools={tools} onClose={()=>setModal(null)}/>} 
  </div>;
}

function ConfigSection({number,title,children}:{number:string;title:string;children:React.ReactNode}){return <section className="config-section"><h3><span>{number}</span>{title}</h3>{children}</section>}
function Option({active,icon,title,copy,onClick}:{active:boolean;icon:React.ReactNode;title:string;copy:string;onClick:()=>void}){return <button type="button" className={active?"professional-option active":"professional-option"} onClick={onClick}><i>{icon}</i><span><strong>{title}</strong><small>{copy}</small></span>{active&&<b><Check/></b>}</button>}

function DetailDialog({modal,process,tools,onClose}:{modal:NonNullable<DetailModal>;process:string[];tools:{name:string;description:string;icon?:string}[];onClose:()=>void}){
 return <div className="service-modal-backdrop" onMouseDown={(event)=>{if(event.currentTarget===event.target)onClose()}}><div className="service-detail-modal"><header><div><span className="eyebrow">Thông tin dịch vụ</span><h2>{modal.type==="work"?`Chi tiết công việc • ${modal.title}`:modal.type==="process"?"Quy trình vệ sinh chuyên nghiệp":"Công cụ, dụng cụ & hóa chất"}</h2></div><button onClick={onClose}><X/></button></header>{modal.type==="work"?<WorkDetailContent details={modal.details}/>:modal.type==="process"?<div className="professional-process">{process.map((item,index)=><div key={item}><span>{String(index+1).padStart(2,"0")}</span><p>{item}</p></div>)}</div>:<div className="tools-grid">{tools.map((tool)=><article key={tool.name}><span>{tool.icon==="chemical"?<FlaskConical/>:tool.icon==="window"?<SquareStack/>:tool.icon==="cloth"?<Sparkles/>:tool.icon==="machine"?<Building2/>:<Wrench/>}</span><h3>{tool.name}</h3><p>{tool.description}</p></article>)}</div>}<footer><Info/><span>Danh sách thực tế có thể thay đổi theo hiện trạng công trình và được xác nhận trước khi thực hiện.</span></footer></div></div>;
}

function WorkDetailContent({details}:{details:WorkDetails}){
 const rooms=[{key:"livingRoom" as const,title:"Phòng khách",icon:<Sofa/>},{key:"bedroom" as const,title:"Phòng ngủ",icon:<BedDouble/>},{key:"kitchen" as const,title:"Nhà bếp",icon:<CookingPot/>},{key:"bathroom" as const,title:"Nhà vệ sinh",icon:<Bath/>}];
 return <div className="work-details-content">{details.overview&&<DetailList title="Công việc tổng quát" items={details.overview} icon={<ListChecks/>}/>} {details.scope&&<DetailList title="Phạm vi công việc" items={details.scope} icon={<SquareStack/>}/>}<div className="room-detail-grid">{rooms.map(room=><DetailList key={room.key} title={room.title} items={details[room.key]} icon={room.icon}/>)}</div></div>;
}
function DetailList({title,items,icon}:{title:string;items:string[];icon:React.ReactNode}){return <section className="detail-list"><h3><span>{icon}</span>{title}</h3><ul>{items.map(item=><li key={item}><Check/>{item}</li>)}</ul></section>}
