export type AdminSession = {
  id: string;
  fullName: string;
  email: string;
  role: "super_admin";
};

const ADMIN_SESSION_KEY = "everycare_admin_session";

export async function loginAdmin(email: string, password: string): Promise<AdminSession> {
  await new Promise((resolve) => setTimeout(resolve, 350));
  const cleanEmail = email.trim().toLowerCase();
  const cleanPassword = password.trim();
  if (cleanEmail !== "admin@everycare.vn" || (cleanPassword !== "Admin@123" && cleanPassword !== "admin@123")) {
    throw new Error("Email hoặc mật khẩu quản trị không chính xác.");
  }
  return { id: "admin-001", fullName: "Linh Admin", email: "admin@everycare.vn", role: "super_admin" };
}

export function loadAdminSession(): AdminSession | null {
  if (typeof window === "undefined") return null;
  try {
    const value = sessionStorage.getItem(ADMIN_SESSION_KEY);
    return value ? JSON.parse(value) as AdminSession : null;
  } catch { return null; }
}

export function saveAdminSession(session: AdminSession) {
  sessionStorage.setItem(ADMIN_SESSION_KEY, JSON.stringify(session));
}

export function clearAdminSession() {
  sessionStorage.removeItem(ADMIN_SESSION_KEY);
}
