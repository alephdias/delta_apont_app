import { NavLink, Outlet } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";
import { APP_NAME, Logo } from "./Brand";
import { ThemeToggle } from "./ThemeToggle";

export function AppLayout() {
  const { session, signOut } = useAuth();
  const email = session?.user?.email ?? "";

  return (
    <div className="shell">
      <header className="topbar">
        <div className="brand">
          <Logo />
          <span className="brand-name">{APP_NAME}</span>
        </div>
        <nav className="nav">
          <NavLink to="/" end>
            Meu dia
          </NavLink>
          <NavLink to="/fechamento">Fechamento</NavLink>
          <NavLink to="/empresas">Empresas</NavLink>
        </nav>
        <div className="topbar-end">
          <span className="topbar-user">{email}</span>
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
