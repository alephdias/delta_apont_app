import { NavLink, Outlet } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { useAuth } from "../auth/AuthContext";
import { ProfileApi } from "../api/client";
import { Logo } from "./Brand";
import { ThemeToggle } from "./ThemeToggle";

export function AppLayout() {
  const { session, signOut } = useAuth();
  const email = session?.user?.email ?? "";
  const { data: profile } = useQuery({ queryKey: ["profile"], queryFn: ProfileApi.get });

  return (
    <div className="shell">
      <header className="topbar">
        <div className="brand">
          <Logo />
        </div>
        <nav className="nav">
          <NavLink to="/" end>
            Meu dia
          </NavLink>
          <NavLink to="/fechamento">Fechamento</NavLink>
          <NavLink to="/solicitacoes">Solicitações</NavLink>
          <NavLink to="/empresas">Empresas</NavLink>
          <NavLink to="/aplicativo">Aplicativo</NavLink>
          {profile?.isAdmin && <NavLink to="/admin">Admin</NavLink>}
        </nav>
        <div className="topbar-end">
          <span className="topbar-user">{email}</span>
          <NavLink to="/configuracoes" className="icon-link" title="Configurações" aria-label="Configurações">
            ⚙
          </NavLink>
          <ThemeToggle />
          <button className="btn btn-ghost btn-sm" onClick={() => signOut()}>
            Sair
          </button>
        </div>
      </header>
      <main className="main">
        <Outlet />
      </main>
    </div>
  );
}
