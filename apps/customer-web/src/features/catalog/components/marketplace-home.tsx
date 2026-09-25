"use client";

import Image from "next/image";
import { useRouter } from "next/navigation";
import { useEffect, useMemo, useState, type CSSProperties } from "react";
import {
  AirVent, ArrowRight, Baby, BedDouble, Boxes, BriefcaseBusiness, CheckCircle2,
  CircuitBoard, Clock3, Drill, HeartHandshake, Home, Laptop, MapPin, PackageOpen,
  PawPrint, Search, ShoppingBasket, Sparkles, Star, Trash2, Truck, UserRound,
  Wrench, X, type LucideIcon,
} from "lucide-react";
import { serviceCategories, type UtilityService } from "@/features/catalog/lib/service-catalog";

type CatalogGroup={slug:string;name:string;description:string;isComingSoon:boolean};
type Selected=UtilityService&{categoryName:string};
const HISTORY_KEY="everycare_service_search_history";

const categoryTiles:{id:string;name:string;icon:LucideIcon;color:string;soft:string;live?:boolean}[]=[
  {id:"cleaning",name:"Vệ sinh",icon:Sparkles,color:"#087c68",soft:"#e7f7f2",live:true},
  {id:"repair",name:"Gọi thợ",icon:Wrench,color:"#ef7a26",soft:"#fff2e8"},
  {id:"cooling",name:"Điện lạnh",icon:AirVent,color:"#248aca",soft:"#eaf6fd"},
  {id:"electronics",name:"Điện tử · Gia dụng",icon:CircuitBoard,color:"#7756d8",soft:"#f0edff"},
  {id:"interior",name:"Nội thất",icon:Home,color:"#bd7a18",soft:"#fff6df"},
  {id:"moving",name:"Vận chuyển",icon:Truck,color:"#28a06b",soft:"#e9f8ef"},
  {id:"living",name:"Sinh hoạt",icon:ShoppingBasket,color:"#e84e8b",soft:"#fff0f6"},
  {id:"care",name:"Chăm sóc con người",icon:HeartHandshake,color:"#e15e64",soft:"#fff0f1"},
  {id:"beauty",name:"Làm đẹp tại nhà",icon:Baby,color:"#c94fa8",soft:"#faeff8"},
  {id:"pets",name:"Thú cưng",icon:PawPrint,color:"#d89c18",soft:"#fff7df"},
  {id:"technology",name:"Công nghệ",icon:Laptop,color:"#2876cb",soft:"#edf5ff"},
  {id:"oddjobs",name:"Việc vặt",icon:Boxes,color:"#20a597",soft:"#e8f8f6"},
];

const iconBySlug:Record<string,LucideIcon>={
  "ve-sinh-phong-le":Sparkles,"tong-ve-sinh":Home,"ve-sinh-chuyen-nghiep":Drill,
  "don-dep-van-phong-dinh-ky":BriefcaseBusiness,"don-dep-buong-phong":BedDouble,
  "ve-sinh-van-phong-chuyen-sau":AirVent,
};

const fallbackDescription:Record<string,string>={
  "ve-sinh-phong-le":"Dọn dẹp nhà theo giờ, linh hoạt cho căn hộ và nhà ở.",
  "tong-ve-sinh":"Làm sạch toàn diện với đội nhóm phù hợp diện tích.",
  "ve-sinh-chuyen-nghiep":"Xử lý bụi mịn, vết sơn, xi măng và vết bẩn lâu ngày.",
  "don-dep-van-phong-dinh-ky":"Nhân sự vệ sinh văn phòng theo buổi hoặc theo ngày.",
  "don-dep-buong-phong":"Dành cho khách sạn, homestay, căn hộ dịch vụ và villa.",
  "ve-sinh-van-phong-chuyen-sau":"Làm sạch chuyên sâu cho văn phòng quy mô lớn.",
};

const popularSlugs=["ve-sinh-phong-le","ve-sinh-chuyen-nghiep","don-dep-van-phong-dinh-ky","don-dep-buong-phong"];
const popularPhotos:Record<string,string>={
  "ve-sinh-phong-le":"/images/popular-home-cleaning.png",
  "ve-sinh-chuyen-nghiep":"/images/popular-professional-cleaning.png",
  "don-dep-van-phong-dinh-ky":"/images/popular-office-cleaning.png",
  "don-dep-buong-phong":"/images/popular-hospitality-cleaning.png",
};
const testimonials=[
  {name:"Chị Mai Anh",area:"Quận 7, TP.HCM",text:"Nhà cửa sạch bong, Tasker rất chuyên nghiệp và đúng giờ.",rating:"4,9"},
  {name:"Anh Quốc Huy",area:"Quận Bình Thạnh, TP.HCM",text:"Đặt lịch rất nhanh, giá hiển thị rõ và không phát sinh bất ngờ.",rating:"4,8"},
  {name:"Chị Thu Trang",area:"TP. Thủ Đức, TP.HCM",text:"Tôi thích việc có thể xem thông tin rồi mới quyết định đặt dịch vụ.",rating:"5,0"},
];

export function MarketplaceHome(){
  const router=useRouter();
  const [query,setQuery]=useState("");
  const [searchOpen,setSearchOpen]=useState(false);
  const [history,setHistory]=useState<string[]>([]);
  const [catalog,setCatalog]=useState<CatalogGroup[]>([]);
  const [selected,setSelected]=useState<Selected|null>(null);
  const [cleaningOpen,setCleaningOpen]=useState(false);
  const [comingSoon,setComingSoon]=useState<string|null>(null);

  useEffect(()=>{
    fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL??"http://localhost:5185"}/api/service-groups`,{cache:"no-store"}).then(r=>r.ok?r.json():Promise.reject()).then(setCatalog).catch(()=>setCatalog([]));
    try{setHistory(JSON.parse(localStorage.getItem(HISTORY_KEY)??"[]"))}catch{setHistory([])}
  },[]);

  const cleaning=useMemo(()=>serviceCategories.slice(0,2).flatMap(group=>group.services).map(service=>{const live=catalog.find(item=>item.slug===service.slug);return {...service,name:live?.name??service.name,description:live?.description??fallbackDescription[service.slug]??service.description}}),[catalog]);
  const suggestions=useMemo(()=>query.trim()?cleaning.filter(item=>(item.name+" "+item.description).toLocaleLowerCase("vi").includes(query.trim().toLocaleLowerCase("vi"))).slice(0,6):[],[cleaning,query]);
  const popular=popularSlugs.map(slug=>cleaning.find(item=>item.slug===slug)).filter(Boolean) as UtilityService[];

  function remember(value:string){const next=[value,...history.filter(item=>item!==value)].slice(0,6);setHistory(next);localStorage.setItem(HISTORY_KEY,JSON.stringify(next))}
  function choose(service:UtilityService){remember(service.name);setQuery(service.name);setSearchOpen(false);setCleaningOpen(false);setSelected({...service,categoryName:"Dịch vụ vệ sinh"})}
  function runSearch(){const match=suggestions[0]??cleaning.find(item=>item.name.toLocaleLowerCase("vi")===query.trim().toLocaleLowerCase("vi"));if(match)choose(match);else if(query.trim()){remember(query.trim());setCleaningOpen(true);setSearchOpen(false)}}
  function openCategory(id:string,name:string,live?:boolean){if(id==="cleaning"&&live)setCleaningOpen(true);else setComingSoon(name)}
  function clearHistory(){setHistory([]);localStorage.removeItem(HISTORY_KEY)}
  const bookingHref=(service:UtilityService)=>service.available?`/dat-lich?service=${service.bookingService}&start=address`:`/dich-vu/${service.slug}`;

  return <>
    <section className="market-hero" id="trang-chu"><div className="shell market-hero-grid"><div className="market-hero-copy"><span>EVERYCARE · DỊCH VỤ TẠI NHÀ</span><h1>Mọi dịch vụ trong nhà<br/><em>chỉ cách bạn một cú nhấp chuột</em></h1><p>Đặt dịch vụ nhanh chóng · Thợ uy tín · Giá minh bạch · Có mặt tận nơi</p><div className="market-search-wrap"><div className="market-search"><Search/><input value={query} onChange={e=>{setQuery(e.target.value);setSearchOpen(true)}} onFocus={()=>setSearchOpen(true)} onKeyDown={e=>{if(e.key==="Enter")runSearch()}} placeholder="Bạn cần dịch vụ gì hôm nay?"/><button onClick={runSearch}><Search/></button></div>{searchOpen&&<div className="market-search-dropdown">{query.trim()?<><header>Gợi ý dịch vụ</header>{suggestions.map(service=>{const Icon=iconBySlug[service.slug]??Sparkles;return <button key={service.slug} onMouseDown={e=>e.preventDefault()} onClick={()=>choose(service)}><i><Icon/></i><span><strong>{service.name}</strong><small>{service.description}</small></span><ArrowRight/></button>})}{suggestions.length===0&&<p>Không có gợi ý phù hợp. Bạn có thể xem toàn bộ dịch vụ vệ sinh.</p>}</>:<>{history.length>0&&<header><span>Tìm kiếm gần đây</span><button onClick={clearHistory}><Trash2/> Xóa</button></header>}{history.map(item=><button key={item} onClick={()=>{setQuery(item);setSearchOpen(false)}}><i><Clock3/></i><span><strong>{item}</strong></span></button>)}{history.length===0&&<p>Nhập tên dịch vụ để xem gợi ý.</p>}</>}</div>}</div><div className="market-popular"><small>Phổ biến:</small>{["Dọn dẹp nhà cửa","Tổng vệ sinh","Vệ sinh văn phòng"].map(label=><button key={label} onClick={()=>{setQuery(label);setSearchOpen(true)}}>{label}</button>)}</div></div><div className="market-hero-art"><Image src="/images/everycare-hero.png" alt="Dịch vụ vệ sinh EveryCare" fill priority sizes="(max-width: 800px) 100vw, 48vw"/></div></div></section>

    <span className="market-anchor" id="chon-dich-vu"/><section className="market-catalog shell" id="dich-vu"><div className="market-category-grid">{categoryTiles.map(item=>{const Icon=item.icon;return <button key={item.id} style={{"--tile-color":item.color,"--tile-soft":item.soft} as CSSProperties} onClick={()=>openCategory(item.id,item.name,item.live)}><i><Icon/></i><strong>{item.name}</strong>{!item.live&&<small>Sắp ra mắt</small>}</button>})}</div></section>

    <section className="market-promise shell" id="ve-chung-toi"><div><span>VỀ EVERYCARE</span><h2>Nhà sạch hơn.<br/>Cuộc sống nhẹ nhàng hơn.</h2><p>EveryCare kết nối khách hàng với Tasker phù hợp, minh bạch giá, trạng thái công việc và thanh toán trên cùng một hệ thống.</p><button onClick={()=>setCleaningOpen(true)}>Khám phá dịch vụ vệ sinh <ArrowRight/></button></div><Image src="/images/everycare-office-team.png" alt="Đội ngũ EveryCare" fill sizes="(max-width: 800px) 100vw, 50vw"/></section>
    <section className="market-stats shell"><article><strong>6</strong><span>Dịch vụ vệ sinh</span></article><article><strong>10 km</strong><span>Phạm vi tìm Tasker</span></article><article><strong>4,8/5</strong><span>Đánh giá trung bình</span></article><article><strong>100%</strong><span>Giá hiển thị minh bạch</span></article></section>

    <section className="market-featured"><div className="shell"><header className="market-section-head"><div><span>KHÁCH HÀNG THƯỜNG CHỌN</span><h2>Dịch vụ được ưa chuộng</h2><p>Những lựa chọn được đặt nhiều trong nhóm vệ sinh.</p></div><button onClick={()=>setCleaningOpen(true)}>Xem tất cả <ArrowRight/></button></header><div className="market-featured-grid">{popular.map((service,index)=><article key={service.slug}><div className="featured-cover"><Image src={popularPhotos[service.slug]} alt={`${service.name} do Tasker EveryCare thực hiện`} fill sizes="(max-width: 600px) 100vw, (max-width: 900px) 50vw, 25vw"/><span>TOP {index+1}</span></div><div><h3>{service.name}</h3><p>{service.description}</p><footer><span><Star/> 4,{9-index}</span><button onClick={()=>choose(service)}>Xem chi tiết <ArrowRight/></button></footer></div></article>)}</div></div></section>

    <section className="market-steps shell" id="quy-trinh"><header><h2>Chỉ 4 bước đơn giản</h2><p>Đặt dịch vụ dễ dàng, nhanh chóng</p></header><div>{[[Search,"Chọn dịch vụ","Tìm và chọn dịch vụ bạn cần"],[MapPin,"Nhập địa chỉ","Xác nhận vị trí thực hiện"],[BriefcaseBusiness,"Tasker nhận việc","Hệ thống tìm người phù hợp"],[CheckCircle2,"Hoàn thành & đánh giá","Thanh toán và chia sẻ trải nghiệm"]].map(([Icon,title,copy],index)=>{const StepIcon=Icon as LucideIcon;return <article key={String(title)}><b>{index+1}</b><StepIcon/><strong>{String(title)}</strong><span>{String(copy)}</span></article>})}</div></section>

    <section className="market-testimonials"><div className="shell"><header><span>TRẢI NGHIỆM THỰC TẾ</span><h2>Khách hàng nói gì về chúng tôi</h2><p>Sự hài lòng của khách hàng là tiêu chuẩn để EveryCare cải thiện mỗi ngày.</p></header><div>{testimonials.map((item,index)=><article key={item.name}><div className="testimonial-stars">{Array.from({length:5},(_,star)=><Star key={star}/>)}</div><blockquote>“{item.text}”</blockquote><footer><i><UserRound/></i><span><strong>{item.name}</strong><small>{item.area}</small></span><b>{item.rating}/5</b></footer></article>)}</div></div></section>
    <section className="market-support shell" id="ho-tro"><HeartHandshake/><div><h2>Bạn cần EveryCare hỗ trợ?</h2><p>Đội ngũ hỗ trợ sẵn sàng giải đáp về dịch vụ, đơn hàng và tài khoản.</p></div><a href="mailto:support@everycare.vn">Liên hệ hỗ trợ <ArrowRight/></a></section>

    {cleaningOpen&&<div className="market-modal-backdrop" onMouseDown={e=>{if(e.currentTarget===e.target)setCleaningOpen(false)}}><section className="cleaning-picker-modal"><header><div><span>DỊCH VỤ ĐANG HOẠT ĐỘNG</span><h2>Chọn dịch vụ vệ sinh</h2><p>Chọn một dịch vụ để xem mô tả và tiếp tục đặt lịch.</p></div><button onClick={()=>setCleaningOpen(false)}><X/></button></header><div className="cleaning-logo-grid">{cleaning.map(service=>{const Icon=iconBySlug[service.slug]??Sparkles;return <button key={service.slug} onClick={()=>choose(service)}><i><Icon/></i><span><strong>{service.name}</strong><small>{service.description}</small></span><ArrowRight/></button>})}</div></section></div>}
    {comingSoon&&<div className="market-modal-backdrop" onMouseDown={e=>{if(e.currentTarget===e.target)setComingSoon(null)}}><article className="coming-modal"><button onClick={()=>setComingSoon(null)}><X/></button><div><PackageOpen/></div><span>DANH MỤC MINH HỌA</span><h2>{comingSoon}</h2><p>Nhóm dịch vụ này đang được hoàn thiện quy trình, bảng giá và đội ngũ đối tác.</p><button className="primary" onClick={()=>{setComingSoon(null);setCleaningOpen(true)}}>Xem dịch vụ vệ sinh <ArrowRight/></button></article></div>}
    {selected&&<div className="market-modal-backdrop" onMouseDown={e=>{if(e.currentTarget===e.target)setSelected(null)}}><article className="market-modal"><button className="market-modal-close" onClick={()=>setSelected(null)}><X/></button><div className="market-modal-icon">{(()=>{const Icon=iconBySlug[selected.slug]??Sparkles;return <Icon/>})()}</div><span>{selected.categoryName}</span><h2>{selected.name}</h2><p>{selected.description}</p><div className="market-modal-points"><span><CheckCircle2/> Giá dịch vụ minh bạch trước khi xác nhận</span><span><CheckCircle2/> Tasker được kiểm tra dịch vụ và lịch rảnh</span><span><CheckCircle2/> Theo dõi tiến độ và đánh giá sau công việc</span></div><footer><button onClick={()=>{setSelected(null);setCleaningOpen(true)}}>Xem dịch vụ khác</button><button className="primary" onClick={()=>router.push(bookingHref(selected))}>{selected.available?"Tiếp tục đặt lịch":"Xem thông tin"}<ArrowRight/></button></footer></article></div>}
  </>;
}
