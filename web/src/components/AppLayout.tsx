import { NavLink, Outlet } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";

export function AppLayout() {
  const { session, signOut } = useAuth();
  const email = session?.user?.email ?? "";

  return (
    <div className="shell">
      <header className="topbar">
        <div className="brand">
          <span className="delta-mark">Δ</span>
          <span className="brand-name">Delta Apont</span>
        </div>
        <nav className="nav">
          <NavLink to="/" end>
            Meu dia
          </NavLink>
          <NavLink to="/fechamento">Fechamento</NavLink>
        </nav>
        <div className="topbar-end">
          <span className="topbar-user">{email}</span>
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
