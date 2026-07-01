import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";

export function ChangePasswordPage() {
  const { updatePassword, signOut } = useAuth();
  const navigate = useNavigate();
  const [password, setPassword] = useState("");
  const [confirm, setConfirm] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    if (password.length < 6) {
      setError("A senha deve ter pelo menos 6 caracteres.");
      return;
    }
    if (password !== confirm) {
      setError("As senhas não conferem.");
      return;
    }
    setLoading(true);
    try {
      await updatePassword(password);
      navigate("/", { replace: true });
    } catch {
      setError("Não foi possível atualizar a senha. Tente novamente.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="auth">
      <form className="auth-card" onSubmit={onSubmit}>
        <div className="auth-head">
          <span className="delta-mark">Δ</span>
          <h1>Delta Apont</h1>
        </div>
        <p className="eyebrow">Primeiro acesso</p>
        <h2 className="auth-title">Defina sua senha</h2>
        <p className="auth-sub">Escolha uma senha de sua preferência para continuar.</p>

        <label className="field">
          <span>Nova senha</span>
          <input
            className="input"
            type="password"
            autoComplete="new-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
          />
        </label>

        <label className="field">
          <span>Confirmar senha</span>
          <input
            className="input"
            type="password"
            autoComplete="new-password"
            value={confirm}
            onChange={(e) => setConfirm(e.target.value)}
            required
          />
        </label>

        {error && <div className="alert-error">{error}</div>}

        <button className="btn btn-ink btn-block" disabled={loading}>
          {loading ? "Salvando…" : "Salvar e continuar"}
        </button>
        <button
          type="button"
          className="btn btn-ghost btn-block"
          style={{ marginTop: "0.6rem" }}
          onClick={() => signOut()}
        >
          Sair
        </button>
      </form>
    </div>
  );
}
