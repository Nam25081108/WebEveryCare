"use client";

import { useEffect, useMemo, useState } from "react";
import {
  AtSign, BadgeCheck, BriefcaseBusiness, Building2, CalendarDays, ChevronDown,
  Clock3, ExternalLink, FileImage, IdCard, LoaderCircle, Lock, Mail, MapPin,
  Phone, RefreshCw, Search, ShieldCheck, Trash2, Unlock, UserCheck, Users,
  UserRoundCheck, UserRoundX, XCircle,
} from "lucide-react";

const API = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5185";

type VerificationStatus = "Pending" | "Approved" | "Rejected";
type StatusFilter = "All" | VerificationStatus;
type Application = {
  id: string;
  fullName: string;
  phone: string;
  email: string;
  status: "Pending" | "Active" | "Locked";
  partnerType: "Individual" | "Team";
  teamName?: string;
  teamSize: number;
  isAvailable: boolean;
  availabilityDays: number;
  identityNumber: string;
  serviceAddress: string;
  latitude: number | null;
  longitude: number | null;
  verificationStatus: VerificationStatus;
  rejectionReason?: string;
  createdAt: string;
  reviewedAt?: string;
  approvalEmailSentAt?: string;
  approvalEmailError?: string;
  services: { slug: string; name: string }[];
  teamMembers: { id: string; fullName: string; phone: string; identityNumber?: string }[];
};

async function readApiResponse<T>(response: Response): Promise<T> {
  const text = await response.text();
  let data: Record<string, unknown> | null = null;
  try { data = text ? JSON.parse(text) as Record<string, unknown> : null; } catch {}
  if (!response.ok) {
    const message = typeof data?.message === "string"
      ? data.message
      : text.includes("28P01")
        ? "Backend không đăng nhập được PostgreSQL. Hãy kiểm tra cấu hình database."
        : "Không thể tải hồ sơ cộng tác viên.";
    throw new Error(message);
  }
  if (!data && text) throw new Error("Backend trả về dữ liệu không đúng định dạng JSON.");
  return (data ?? {}) as T;
}

const statusMeta: Record<VerificationStatus, { label: string; hint: string }> = {
  Pending: { label: "Chờ duyệt", hint: "Cần kiểm tra hồ sơ" },
  Approved: { label: "Đã duyệt", hint: "Đủ điều kiện nhận việc" },
  Rejected: { label: "Từ chối", hint: "Hồ sơ cần bổ sung" },
};

export function AdminPartnerApplications() {
  const [items, setItems] = useState<Application[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [query, setQuery] = useState("");
  const [filter, setFilter] = useState<StatusFilter>("All");
  const [working, setWorking] = useState("");
  const [message, setMessage] = useState("");

  async function load() {
    setLoading(true);
    setError("");
    try {
      const response = await fetch(`${API}/api/admin/partner-applications`, { cache: "no-store" });
      setItems(await readApiResponse<Application[]>(response));
    } catch (reason) {
      setError(reason instanceof TypeError
        ? "Không thể kết nối API ở cổng 5185."
        : reason instanceof Error ? reason.message : "Không thể tải dữ liệu.");
    } finally { setLoading(false); }
  }

  useEffect(() => { void load(); }, []);

  const counts = useMemo(() => ({
    All: items.length,
    Pending: items.filter(item => item.verificationStatus === "Pending").length,
    Approved: items.filter(item => item.verificationStatus === "Approved").length,
    Rejected: items.filter(item => item.verificationStatus === "Rejected").length,
    Teams: items.filter(item => item.partnerType === "Team").length,
  }), [items]);

  const filtered = useMemo(() => {
    const keyword = query.trim().toLocaleLowerCase("vi");
    return items.filter(item => {
      if (filter !== "All" && item.verificationStatus !== filter) return false;
      if (!keyword) return true;
      const searchable = [
        item.fullName, item.teamName, item.phone, item.email, item.identityNumber,
        item.serviceAddress, ...item.services.map(service => service.name),
      ].filter(Boolean).join(" ").toLocaleLowerCase("vi");
      return searchable.includes(keyword);
    });
  }, [filter, items, query]);

  async function action(item: Application, kind: "approve" | "reject" | "status" | "delete") {
    let body: string | undefined;
    let method = "POST";
    let url = `${API}/api/admin/partner-applications/${item.id}/${kind}`;
    if (kind === "reject") {
      const reason = prompt("Nhập lý do từ chối hồ sơ:", "Thông tin hồ sơ chưa đầy đủ.");
      if (reason === null) return;
      body = JSON.stringify({ reason });
    }
    if (kind === "status") {
      method = "PATCH";
      body = JSON.stringify({ status: item.status === "Locked" ? "Active" : "Locked" });
    }
    if (kind === "delete") {
      if (!confirm(`Xóa hồ sơ của ${item.teamName || item.fullName}? Dữ liệu sẽ được ẩn khỏi hệ thống.`)) return;
      method = "DELETE";
      url = `${API}/api/admin/partner-applications/${item.id}`;
    }
    setWorking(item.id + kind);
    setMessage("");
    setError("");
    try {
      const response = await fetch(url, { method, headers: body ? { "Content-Type": "application/json" } : undefined, body });
      const data = response.status === 204 ? {} : await readApiResponse<{ message?: string }>(response);
      setMessage(data.message ?? "Đã cập nhật hồ sơ.");
      await load();
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Thao tác thất bại.");
    } finally { setWorking(""); }
  }

  return <div className="partner-admin-page">
    <div className="partner-admin-heading">
      <div>
        <span className="partner-admin-kicker">TRUNG TÂM ĐỐI TÁC</span>
        <h1>Quản lý cộng tác viên</h1>
        <p>Kiểm tra hồ sơ, dịch vụ đăng ký và trạng thái hoạt động của đối tác.</p>
      </div>
      <button className="partner-refresh" onClick={() => void load()} disabled={loading}>
        <RefreshCw className={loading ? "spin" : ""}/> Làm mới dữ liệu
      </button>
    </div>

    <section className="partner-overview">
      <article className="total"><span><Users/></span><div><small>Tổng hồ sơ</small><strong>{counts.All}</strong><em>Tất cả cộng tác viên</em></div></article>
      <article className="pending"><span><Clock3/></span><div><small>Đang chờ duyệt</small><strong>{counts.Pending}</strong><em>Cần xử lý sớm</em></div></article>
      <article className="approved"><span><UserRoundCheck/></span><div><small>Đã xác minh</small><strong>{counts.Approved}</strong><em>Có thể nhận việc</em></div></article>
      <article className="teams"><span><Building2/></span><div><small>Đối tác đội nhóm</small><strong>{counts.Teams}</strong><em>Dịch vụ chuyên sâu</em></div></article>
    </section>

    {message && <div className="partner-admin-alert success"><BadgeCheck/>{message}</div>}
    {error && <div className="partner-admin-alert error"><XCircle/>{error}</div>}

    <div className="partner-control-bar">
      <div className="partner-search"><Search/><input value={query} onChange={event => setQuery(event.target.value)} placeholder="Tìm theo tên, số điện thoại, CCCD hoặc dịch vụ..."/></div>
      <div className="partner-filters" aria-label="Lọc trạng thái hồ sơ">
        {(["All", "Pending", "Approved", "Rejected"] as StatusFilter[]).map(value =>
          <button key={value} className={filter === value ? "active" : ""} onClick={() => setFilter(value)}>
            {value === "All" ? "Tất cả" : statusMeta[value].label}<b>{counts[value]}</b>
          </button>)}
      </div>
    </div>

    <div className="partner-result-heading">
      <div><strong>{filtered.length} hồ sơ</strong><span>{filter === "All" ? "trong toàn bộ danh sách" : statusMeta[filter].hint}</span></div>
      <small>Sắp xếp theo ngày đăng ký mới nhất</small>
    </div>

    {loading
      ? <div className="partner-loading"><LoaderCircle className="spin"/><strong>Đang tải hồ sơ cộng tác viên</strong><span>Vui lòng chờ trong giây lát...</span></div>
      : <div className="partner-application-list">
        {filtered.map(item => {
          const meta = statusMeta[item.verificationStatus];
          const displayName = item.teamName || item.fullName;
          const initials = displayName.split(" ").filter(Boolean).slice(-2).map(word => word[0]).join("");
          const isWorking = working.startsWith(item.id);
          return <article key={item.id} className={`partner-application-card ${item.verificationStatus.toLowerCase()}`}>
            <div className="partner-card-accent"/>
            <header className="partner-card-header">
              <div className="partner-identity">
                <span className="partner-avatar">{initials || (item.partnerType === "Team" ? <Users/> : <UserCheck/>)}</span>
                <div>
                  <div className="partner-name-line"><h2>{displayName}</h2><span className={`review-badge ${item.verificationStatus.toLowerCase()}`}>{meta.label}</span></div>
                  <p>{item.partnerType === "Team" ? `Đội nhóm · ${item.teamSize} thành viên · Trưởng nhóm ${item.fullName}` : "Cộng tác viên cá nhân"}</p>
                </div>
              </div>
              <div className={`partner-availability ${item.isAvailable ? "online" : "offline"}`}><i/>{item.isAvailable ? "Đang nhận việc" : "Tạm ngưng nhận việc"}</div>
            </header>

            <div className="partner-card-body">
              <section className="partner-main-info">
                <div className="partner-contact-grid">
                  <p><span><Phone/></span><small>Số điện thoại</small><strong>{item.phone}</strong></p>
                  <p><span><AtSign/></span><small>Email</small><strong>{item.email || "Chưa cung cấp"}</strong></p>
                  <p><span><IdCard/></span><small>CCCD</small><strong>{item.identityNumber}</strong></p>
                  <p><span><CalendarDays/></span><small>Lịch hoạt động</small><strong>{item.availabilityDays} ngày / tuần</strong></p>
                </div>

                <div className="partner-info-block">
                  <div className="partner-block-title"><BriefcaseBusiness/><span><strong>Dịch vụ đăng ký</strong><small>{item.services.length} nhóm dịch vụ</small></span></div>
                  <div className="partner-service-tags">{item.services.map(service => <span key={service.slug}>{service.name}</span>)}</div>
                </div>

                <div className="partner-info-block address">
                  <div className="partner-block-title"><MapPin/><span><strong>Khu vực xuất phát</strong><small>{item.serviceAddress}</small></span></div>
                  {item.latitude && item.longitude && <a href={`https://www.openstreetmap.org/?mlat=${item.latitude}&mlon=${item.longitude}#map=17/${item.latitude}/${item.longitude}`} target="_blank" rel="noreferrer">Mở bản đồ <ExternalLink/></a>}
                </div>

                {item.teamMembers.length > 0 && <details className="partner-team-members">
                  <summary><span><Users/>Danh sách thành viên</span><b>{item.teamMembers.length} người</b><ChevronDown/></summary>
                  <div>{item.teamMembers.map(member => <p key={member.id}><span>{member.fullName.split(" ").slice(-2).map(word => word[0]).join("")}</span><strong>{member.fullName}</strong><small>{member.phone}{member.identityNumber ? ` · CCCD ${member.identityNumber}` : ""}</small></p>)}</div>
                </details>}
              </section>

              <aside className="partner-review-panel">
                <div className="review-panel-title"><ShieldCheck/><span><strong>Thông tin xét duyệt</strong><small>{meta.hint}</small></span></div>
                <dl>
                  <div><dt>Ngày đăng ký</dt><dd>{new Date(item.createdAt).toLocaleDateString("vi-VN")}</dd></div>
                  <div><dt>Hình thức</dt><dd>{item.partnerType === "Team" ? "Đội nhóm" : "Cá nhân"}</dd></div>
                  <div><dt>Tài khoản</dt><dd className={item.status === "Locked" ? "locked" : "active"}>{item.status === "Locked" ? "Đã khóa" : "Đang hoạt động"}</dd></div>
                </dl>
                <div className="partner-documents">
                  <span>Giấy tờ xác minh</span>
                  <a href={`${API}/api/admin/partner-applications/${item.id}/documents/front`} target="_blank" rel="noreferrer"><FileImage/>Mặt trước CCCD<ExternalLink/></a>
                  <a href={`${API}/api/admin/partner-applications/${item.id}/documents/back`} target="_blank" rel="noreferrer"><FileImage/>Mặt sau CCCD<ExternalLink/></a>
                </div>
              </aside>
            </div>

            {item.rejectionReason && <div className="partner-card-notice rejected"><UserRoundX/><span><strong>Lý do từ chối</strong><small>{item.rejectionReason}</small></span></div>}
            {item.approvalEmailSentAt
              ? <div className="partner-card-notice mailed"><Mail/><span><strong>Email xác nhận đã được gửi</strong><small>{new Date(item.approvalEmailSentAt).toLocaleString("vi-VN")}</small></span></div>
              : item.verificationStatus === "Approved" && <div className="partner-card-notice warning"><Mail/><span><strong>Chưa gửi được email xác nhận</strong><small>{item.approvalEmailError || "Hệ thống sẽ thử gửi lại sau."}</small></span></div>}

            <footer className="partner-card-actions">
              <span>Mã hồ sơ: {item.id.slice(0, 8).toUpperCase()}</span>
              <div>
                {item.verificationStatus === "Pending" && <>
                  <button className="partner-reject" disabled={!!working} onClick={() => void action(item, "reject")}><XCircle/>Từ chối</button>
                  <button className="partner-approve" disabled={!!working} onClick={() => void action(item, "approve")}><BadgeCheck/>Duyệt hồ sơ</button>
                </>}
                {item.verificationStatus === "Approved" && <button className="partner-lock" disabled={!!working} onClick={() => void action(item, "status")}>{item.status === "Locked" ? <><Unlock/>Mở khóa tài khoản</> : <><Lock/>Khóa tài khoản</>}</button>}
                <button className="partner-delete" title="Xóa hồ sơ" disabled={!!working} onClick={() => void action(item, "delete")}><Trash2/></button>
                {isWorking && <LoaderCircle className="spin partner-action-loader"/>}
              </div>
            </footer>
          </article>;
        })}
        {filtered.length === 0 && <div className="partner-empty"><Search/><strong>Không tìm thấy hồ sơ phù hợp</strong><span>Hãy thử từ khóa khác hoặc thay đổi bộ lọc trạng thái.</span><button onClick={() => { setQuery(""); setFilter("All"); }}>Xóa bộ lọc</button></div>}
      </div>}
  </div>;
}
