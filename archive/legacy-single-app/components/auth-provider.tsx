"use client";

import { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react";
import { clearSession, Customer, loadSession, loginCustomer, RegisterInput, registerCustomer, saveSession, syncLocalCustomer } from "@/lib/auth";

type AuthContextValue = {
  customer: Customer | null;
  isLoading: boolean;
  login: (phone: string, password: string, persistent?: boolean) => Promise<Customer>;
  register: (input: RegisterInput) => Promise<Customer>;
  logout: () => void;
};

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [customer, setCustomer] = useState<Customer | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const session = loadSession();
    setCustomer(session);
    if (session) void syncLocalCustomer(session);
    setIsLoading(false);
  }, []);

  const login = useCallback(async (phone: string, password: string, persistent = false) => {
    const loggedInCustomer = await loginCustomer(phone, password);
    saveSession(loggedInCustomer, persistent);
    setCustomer(loggedInCustomer);
    return loggedInCustomer;
  }, []);

  const register = useCallback(async (input: RegisterInput) => {
    const newCustomer = await registerCustomer(input);
    saveSession(newCustomer, true);
    setCustomer(newCustomer);
    return newCustomer;
  }, []);

  const logout = useCallback(() => {
    clearSession();
    setCustomer(null);
  }, []);

  const value = useMemo(() => ({ customer, isLoading, login, register, logout }), [customer, isLoading, login, register, logout]);
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) throw new Error("useAuth must be used inside AuthProvider");
  return context;
}
