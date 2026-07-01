import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { AdminApi, ProfileApi } from "../api/client";
import type { CreatedUser } from "../api/client";

function messageOf(e: unknown): string {
  const data = (e as { response?: { data?: unknown } })?.response?.data;
  return typeof data === "string" && data ? data : "Não foi possível concluir a ação.";
}

function fmtDate(iso: string | null): string {
  if (!iso) return "—";
  const d = new Date(iso);
  return d.toLocaleDateString("pt-BR") + " " + d.toLocaleTimeString("pt-BR", { hour: "2-digit", minute: "2-digit" });
}

export function AdminPage() {
  const qc = useQueryClient();
  const { data: profile } = useQuery({ queryKey: ["profile"], queryFn: ProfileApi.get });
  const isAdmin = profile?.isAdmin ?? false;

  const { data: users } = useQuery({
    queryKey: ["admin-users"],
    queryFn: AdminApi.listUsers,
    enabled: isAdmin,
  });

  const [email, setEmail] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [created, setCreated] = useState<CreatedUser | null>(null);
  const [copied, setCopied] = useState(false);

  const create = useMutation({
    mutationFn: () => AdminApi.createUser(email.trim()),
    onSuccess: (u) => {
      setCreated(u);
      setEmail("");
      setError(null);
      qc.invalidateQueries({ queryKey: ["admin-users"] });
    },
    onError: (e) => setError(messageOf(e)),
  });

  if (profile && !isAdmin) {
    return (
      <div>
        <div className="page-head">
          <div>
            <span className="eyebrow">Administração</span>
            <h2>Acesso restrito</h2>
          </div>
        </div>
        <div className="placeholder">esta área é apenas para administradores</div>
      </div>
    );
  }

  return (
    <div>
      <div className="page-head">
        <div>
          <span className="eyebrow">Administração</span>
          <h2>Usuários</h2>
        </div>
      </div>

      <div className="config-card">
        <h3 className="config-title">Novo usuário</h3>
        <p className="config-hint">
          Uma senha temporária é gerada. No primeiro acesso, a pessoa define a própria senha.
        </p>
        <form
          className="company-add"
          onSubmit={(e) => {
            e.preventDefault();
            if (email.trim()) create.mutate();
          }}
        >
          <input
            className="input"
            type="email"
            placeholder="pessoa@deltadecisao.com.br"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
          />
          <button className="btn btn-ink" disabled={!email.trim() || create.isPending}>
            {create.isPending ? "Criando…" : "Criar usuário"}
          </button>
        </form>

        {error && <div className="alert-error">{error}</div>}

        {created && (
          <div className="cred-box">
            <div className="cred-row">
              <span className="eyebrow">E-mail</span>
              <span className="mono">{created.email}</span>
            </div>
            <div className="cred-row">
              <span className="eyebrow">Senha temporária</span>
              <span className="mono cred-pass">{created.password}</span>
              <button
                className="btn btn-ghost btn-sm"
                onClick={async () => {
                  await navigator.clipboard.writeText(created.password);
                  setCopied(true);
                  setTimeout(() => setCopied(false), 2000);
                }}
              >
                {copied ? "✓ copiado" : "Copiar"}
              </button>
            </div>
            <div className="ev-hint">anote agora — a senha não será mostrada novamente.</div>
          </div>
        )}
      </div>

      <div className="section-label">
        <span className="eyebrow">Contas</span>
        <span className="eyebrow">{users?.length ?? 0} usuário(s)</span>
      </div>
      {!users?.length ? (
        <div className="placeholder">carregando…</div>
      ) : (
        <div className="list">
          {users.map((u) => (
            <div className="company-row" key={u.email}>
              <div className="company-name" style={{ fontWeight: 500 }}>
                {u.email}
              </div>
              <div className="company-meta">criado {fmtDate(u.createdAt)}</div>
              <div className="company-meta">último acesso {fmtDate(u.lastSignInAt)}</div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
