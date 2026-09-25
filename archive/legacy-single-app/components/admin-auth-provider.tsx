"use client";

import { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react";
import { AdminSession, clearAdminSession, loadAdminSession, loginAdmin, saveAdminSession } from "@/lib/admin-auth";

type AdminAuthValue = {
  admin: AdminSession | null;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
};

const AdminAuthContext = createContext<AdminAuthValue | null>(null);

export function AdminAuthProvider({ children }: { children: React.ReactNode }) {
  const [admin, setAdmin] = useState<AdminSession | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  useEffect(() => { setAdmin(loadAdminSession()); setIsLoading(false); }, []);
  const login = useCallback(async (email: string, password: string) => { const session = await loginAdmin(email, password); saveAdminSession(session); setAdmin(session); }, []);
  const logout = useCallback(() => { clearAdminSession(); setAdmin(null); }, []);
  const value = useMemo(() => ({ admin, isLoading, login, logout }), [admin, isLoading, login, logout]);
  return <AdminAuthContext.Provider value={value}>{children}</AdminAuthContext.Provider>;
}

export function useAdminAuth() {
  const context = useContext(AdminAuthContext);
  if (!context) throw new Error("useAdminAuth must be used inside AdminAuthProvider");
  return context;
}
