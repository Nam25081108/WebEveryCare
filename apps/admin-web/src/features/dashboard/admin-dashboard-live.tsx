"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { ArrowRight, BarChart3, CircleDollarSign, ClipboardList, Clock3, LoaderCircle, RefreshCw, RotateCcw, UserCheck, Users, WalletCards } from "lucide-react";

const API=process.env.NEXT_PUBLIC_API_BASE_URL??"http://localhost:5185";
const money=(value:number)=>new Intl.NumberFormat("vi-VN").format(value)+"đ";
const shortMoney=(value:number)=>value>=1_000_000?`${(value/1_000_000).toLocaleString("vi-VN",{maximumFractionDigits:1})}tr`:value>=1_000?`${Math.round(value/1_000)}k`:String(value);
const statusLabels:Record<string,string>={Draft:"Chờ thanh toán",Searching:"Chờ Tasker",AwaitingCustomerSelection:"Chờ khách chọn",Assigned:"Đã có Tasker",PartnerTravelling:"Tasker đã đến",InProgress:"Đang thực hiện",AwaitingCustomerConfirmation:"Chờ khách xác nhận",IssueReported:"Đang hỗ trợ",Completed:"Hoàn thành",Cancelled:"Đã hủy",NoPartnerFound:"Chưa tìm được Tasker"};

type Summary={totalOrders:number;ordersToday:number;completedOrders:number;activeOrders:number;cancelledOrders:number;customers:number;newCustomersThisMonth:number;approvedPartners:number;availablePartners:number;pendingPartners:number;serviceGroups:number;servicePackages:number;grossRevenue:number;platformRevenue:number;taskerPayout:number;monthGrossRevenue:number;monthPlatformRevenue:number;todayGrossRevenue:number;heldDeposits:number;totalRefunded:number;averageOrderValue:number};
type DashboardData={generatedAt:string;summary:Summary;orderStatuses:{status:string;count:number}[];dailyRevenue:{date:string;gross:number;platform:number;orders:number}[];monthlyRevenue:{month:string;gross:number;platform:number;tasker:number;orders:number}[];topServices:{service:string;orders:number;gross:number}[];recentBookings:{id:string;code:string;customer:string;service:string;status:string;estimatedTotal:number;createdAt:string}[];recentPayments:{code:string;customer:string;service:string;amount:number;platformFee:number;taskerNetAmount:number;refundedAmount:number;completedAt:string}[]};

function useDashboard(){
 const [data,setData]=useState<DashboardData|null>(null);const [loading,setLoading]=useState(true);const [error,setError]=useState("");
 const load=useCallback(async()=>{setLoading(true);setError("");try{const response=await fetch(`${API}/api/admin/dashboard`,{cache:"no-store"});const payload=await response.json();if(!response.ok)throw new Error(payload.message??"Không thể tải dữ liệu điều hành.");setData(payload)}catch(reason){setError(reason instanceof Error?reason.message:"Không thể tải dữ liệu điều hành.")}finally{setLoading(false)}},[]);
 useEffect(()=>{void load()},[load]);return{data,loading,error,load};
}

function Loading(){return <div className="live-dashboard-loading"><LoaderCircle className="spin"/><strong>Đang tổng hợp dữ liệu thực tế</strong><span>Hệ thống đang đọc đơn hàng và thanh toán từ PostgreSQL.</span></div>}
function ErrorState({message,onRetry}:{message:string;onRetry:()=>void}){return <div className="live-dashboard-error"><RotateCcw/><strong>Không thể tải bảng điều khiển</strong><span>{message}</span><button onClick={onRetry}>Thử lại</button></div>}

export function AdminDashboardLive({onNavigate}:{onNavigate:(section:"orders"|"partners"|"revenue")=>void}){
 const {data,loading,error,load}=useDashboard();
 if(loading&&!data)return <Loading/>;if(error&&!data)return <ErrorState message={error} onRetry={()=>void load()}/>;if(!data)return null;
 const maxDaily=Math.max(...data.dailyRevenue.map(item=>item.gross),1);const s=data.summary;
 return <div className="live-dashboard">
  <div className="live-dashboard-head"><div><span>DỮ LIỆU VẬN HÀNH THỰC TẾ</span><h1>Bảng điều khiển</h1><p>Cập nhật lúc {new Date(data.generatedAt).toLocaleString("vi-VN")}</p></div><button onClick={()=>void load()} disabled={loading}><RefreshCw className={loading?"spin":""}/>Cập nhật dữ liệu</button></div>
  <section className="live-kpis">
   <article className="revenue"><i><CircleDollarSign/></i><div><small>Doanh thu tháng này</small><strong>{money(s.monthGrossRevenue)}</strong><em>Phí nền tảng: {money(s.monthPlatformRevenue)}</em></div></article>
   <article className="orders"><i><ClipboardList/></i><div><small>Đơn hàng hôm nay</small><strong>{s.ordersToday}</strong><em>{s.activeOrders} đơn đang hoạt động</em></div></article>
   <article className="partners"><i><UserCheck/></i><div><small>Tasker đã duyệt</small><strong>{s.approvedPartners}</strong><em>{s.availablePartners} đang sẵn sàng</em></div></article>
   <article className="customers"><i><Users/></i><div><small>Khách hàng</small><strong>{s.customers}</strong><em>+{s.newCustomersThisMonth} trong tháng</em></div></article>
  </section>
  <div className="live-dashboard-grid">
   <section className="live-panel revenue-chart"><header><div><h2>Doanh thu 14 ngày gần nhất</h2><p>Tổng giá trị những đơn đã hoàn thành</p></div><button onClick={()=>onNavigate("revenue")}>Xem báo cáo <ArrowRight/></button></header><div className="live-bars">{data.dailyRevenue.map(item=><div key={item.date} className="live-bar"><span><i style={{height:`${Math.max(item.gross/maxDaily*100,item.gross?6:1)}%`}}/><b>{item.gross?shortMoney(item.gross):""}</b></span><small>{new Date(item.date+"T00:00:00").toLocaleDateString("vi-VN",{day:"2-digit",month:"2-digit"})}</small></div>)}</div>
   </section>
   <section className="live-panel order-health"><header><div><h2>Tình trạng đơn hàng</h2><p>{s.totalOrders} đơn được ghi nhận</p></div></header><div className="order-health-list">{data.orderStatuses.slice(0,7).map(item=><div key={item.status}><span className={`status-dot ${item.status.toLowerCase()}`}/><strong>{statusLabels[item.status]??item.status}</strong><b>{item.count}</b><i style={{width:`${s.totalOrders?item.count/s.totalOrders*100:0}%`}}/></div>)}</div></section>
  </div>
  <div className="live-dashboard-grid lower">
   <section className="live-panel recent-orders"><header><div><h2>Đơn hàng mới nhất</h2><p>Dữ liệu cập nhật trực tiếp</p></div><button onClick={()=>onNavigate("orders")}>Quản lý đơn <ArrowRight/></button></header><div className="live-table"><table><thead><tr><th>MÃ ĐƠN</th><th>KHÁCH HÀNG</th><th>DỊCH VỤ</th><th>TRẠNG THÁI</th><th>GIÁ TRỊ</th></tr></thead><tbody>{data.recentBookings.map(item=><tr key={item.id}><td><strong>{item.code}</strong><small>{new Date(item.createdAt).toLocaleDateString("vi-VN")}</small></td><td>{item.customer}</td><td>{item.service}</td><td><span className={`live-status ${item.status.toLowerCase()}`}>{statusLabels[item.status]??item.status}</span></td><td><strong>{money(item.estimatedTotal)}</strong></td></tr>)}</tbody></table>{!data.recentBookings.length&&<div className="live-empty">Chưa có đơn hàng.</div>}</div></section>
   <aside className="live-side-stack"><article><span><WalletCards/></span><small>Tiền cọc đang giữ</small><strong>{money(s.heldDeposits)}</strong><em>Chưa giải ngân cho Tasker</em></article><article><span><Clock3/></span><small>Giá trị đơn trung bình</small><strong>{money(s.averageOrderValue)}</strong><em>{s.completedOrders} đơn đã hoàn thành</em></article><button onClick={()=>onNavigate("partners")}><UserCheck/><span><strong>{s.pendingPartners} hồ sơ chờ duyệt</strong><small>Kiểm tra cộng tác viên</small></span><ArrowRight/></button></aside>
  </div>
 </div>
}

export function AdminRevenueReport(){
 const {data,loading,error,load}=useDashboard();const [range,setRange]=useState<6|12>(12);
 const months=useMemo(()=>data?.monthlyRevenue.slice(-range).reverse()??[],[data,range]);
 if(loading&&!data)return <Loading/>;if(error&&!data)return <ErrorState message={error} onRetry={()=>void load()}/>;if(!data)return null;const s=data.summary;
 return <div className="revenue-report">
  <div className="live-dashboard-head"><div><span>BÁO CÁO TÀI CHÍNH</span><h1>Doanh thu</h1><p>Đối soát doanh thu, phí nền tảng và phần thanh toán Tasker.</p></div><button onClick={()=>void load()} disabled={loading}><RefreshCw className={loading?"spin":""}/>Cập nhật dữ liệu</button></div>
  <section className="revenue-kpis"><article><small>Tổng tiền khách thanh toán</small><strong>{money(s.grossRevenue)}</strong><span>Tất cả đơn hoàn thành</span></article><article className="platform"><small>Doanh thu nền tảng</small><strong>{money(s.platformRevenue)}</strong><span>Phí EveryCare đã ghi nhận</span></article><article><small>Thanh toán Tasker</small><strong>{money(s.taskerPayout)}</strong><span>Đã cộng vào ví Tasker</span></article><article className="held"><small>Tiền cọc đang giữ</small><strong>{money(s.heldDeposits)}</strong><span>Đơn chưa hoàn thành</span></article><article className="refund"><small>Đã hoàn tiền</small><strong>{money(s.totalRefunded)}</strong><span>Toàn bộ lịch sử</span></article></section>
  <div className="revenue-layout"><section className="live-panel monthly-report"><header><div><h2>Đối soát theo tháng</h2><p>Doanh thu chỉ ghi nhận khi đơn hoàn thành</p></div><div className="range-switch"><button className={range===6?"active":""} onClick={()=>setRange(6)}>6 tháng</button><button className={range===12?"active":""} onClick={()=>setRange(12)}>12 tháng</button></div></header><div className="live-table"><table><thead><tr><th>THÁNG</th><th>ĐƠN HOÀN THÀNH</th><th>KHÁCH THANH TOÁN</th><th>PHÍ NỀN TẢNG</th><th>TRẢ TASKER</th></tr></thead><tbody>{months.map(item=><tr key={item.month}><td><strong>Tháng {item.month.slice(5)}/{item.month.slice(0,4)}</strong></td><td>{item.orders}</td><td>{money(item.gross)}</td><td className="platform-value">{money(item.platform)}</td><td>{money(item.tasker)}</td></tr>)}</tbody></table></div></section>
   <aside className="live-panel top-services"><header><div><h2>Dịch vụ dẫn đầu</h2><p>12 tháng gần nhất</p></div></header>{data.topServices.map((item,index)=><div key={item.service}><b>{String(index+1).padStart(2,"0")}</b><span><strong>{item.service}</strong><small>{item.orders} đơn hoàn thành</small></span><em>{money(item.gross)}</em></div>)}{!data.topServices.length&&<div className="live-empty">Chưa có doanh thu dịch vụ.</div>}</aside></div>
  <section className="live-panel payment-ledger"><header><div><h2>Giao dịch hoàn thành gần đây</h2><p>Chi tiết phân bổ tiền theo từng đơn</p></div></header><div className="live-table"><table><thead><tr><th>MÃ ĐƠN</th><th>KHÁCH HÀNG</th><th>DỊCH VỤ</th><th>HOÀN THÀNH</th><th>TỔNG TIỀN</th><th>PHÍ NỀN TẢNG</th><th>TASKER NHẬN</th></tr></thead><tbody>{data.recentPayments.map(item=>{const fee=item.platformFee||Math.round(item.amount*.15);const tasker=item.taskerNetAmount||item.amount-fee;return <tr key={item.code}><td><strong>{item.code}</strong></td><td>{item.customer}</td><td>{item.service}</td><td>{new Date(item.completedAt).toLocaleString("vi-VN")}</td><td><strong>{money(item.amount)}</strong></td><td className="platform-value">{money(fee)}</td><td>{money(tasker)}</td></tr>})}</tbody></table>{!data.recentPayments.length&&<div className="live-empty">Chưa có giao dịch hoàn thành.</div>}</div></section>
 </div>
}
