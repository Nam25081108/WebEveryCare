"use client";

import Link from "next/link";
import { FormEvent, useState } from "react";
import { useRouter } from "next/navigation";
import { ArrowRight, BriefcaseBusiness, LoaderCircle, LockKeyhole, Phone } from "lucide-react";
import { API_BASE, PARTNER_TOKEN_KEY } from "@/lib/partner-auth";

export default function PartnerLoginPage(){
 const router=useRouter();const [error,setError]=useState("");const [loading,setLoading]=useState(false);
 async function submit(event:FormEvent<HTMLFormElement>){event.preventDefault();setLoading(true);setError("");const form=new FormData(event.currentTarget);try{const response=await fetch(`${API_BASE}/api/partner/auth/login`,{method:"POST",headers:{"Content-Type":"application/json"},body:JSON.stringify({phone:form.get("phone"),password:form.get("password")})});const data=await response.json();if(!response.ok)throw new Error(data.message??"Không thể đăng nhập.");localStorage.setItem(PARTNER_TOKEN_KEY,data.token);router.replace("/portal")}catch(reason){setError(reason instanceof TypeError?"Không thể kết nối backend ở cổng 5185.":reason instanceof Error?reason.message:"Không thể đăng nhập.")}finally{setLoading(false)}}
 return <main className="partner-auth-page"><section className="partner-auth-card"><Link href="/" className="partner-auth-brand"><span><BriefcaseBusiness/></span>Every<b>Care</b> Đối tác</Link><div className="partner-auth-heading"><span>TRANG DÀNH CHO NGƯỜI LÀM</span><h1>Đăng nhập đối tác</h1><p>Dùng số điện thoại và mật khẩu đã nhập khi gửi hồ sơ. Tài khoản chỉ sử dụng được sau khi admin duyệt.</p></div><form onSubmit={submit}><label><span>Số điện thoại</span><div><Phone/><input name="phone" required inputMode="numeric" pattern="0[0-9]{9}" placeholder="0901234567"/></div></label><label><span>Mật khẩu</span><div><LockKeyhole/><input name="password" required type="password" placeholder="Mật khẩu của bạn"/></div></label>{error&&<p className="partner-auth-error">{error}</p>}<button className="button button-large" disabled={loading}>{loading?<><LoaderCircle className="spin"/>Đang đăng nhập...</>:<>Đăng nhập <ArrowRight/></>}</button></form><p>Chưa có hồ sơ? <Link href="/">Đăng ký làm đối tác</Link></p></section></main>;
}
