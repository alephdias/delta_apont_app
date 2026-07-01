import { NavLink, Outlet } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";

export function AppLayout() {
  const { session, signOut } = useAuth();
  const email = session?.user?.email ?? "";

  return (
    <div className="app-shell">
      <header className="topbar">
        <div className="brand">
          <span className="brand-mark">Δ</span>
          <span className="brand-name">Delta Apont</span>
        </div>
        <nav className="topnav">
          <NavLink to="/" end>
            Meu dia
          </NavLink>
          <NavLink to="/fechamento">Fechamento</NavLink>
        </nav>
        <div className="topbar-user">
          <span className="user-email">{email}</span>
          <button className="btn btn-ghost" onClick={() => signOut()}>
            Sair
          </button>
        </div>
      </header>
      <main className="app-main">
        <Outlet />
      </main>
    </div>
  );
}
