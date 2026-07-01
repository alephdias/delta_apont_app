import type { ReactNode } from "react";
import { Navigate, useLocation } from "react-router-dom";
import { useAuth } from "./AuthContext";

export function ProtectedRoute({ children }: { children: ReactNode }) {
  const { session, loading, mustChangePassword } = useAuth();
  const location = useLocation();

  if (loading) return <div className="center-screen">Carregando…</div>;
  if (!session) return <Navigate to="/login" replace />;
  if (mustChangePassword && location.pathname !== "/trocar-senha")
    return <Navigate to="/trocar-senha" replace />;

  return <>{children}</>;
}
