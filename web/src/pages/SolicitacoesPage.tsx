import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ClientsApi, SolicitationsApi } from "../api/client";
import type { ClientItem, Solicitation } from "../api/client";

function messageOf(e: unknown): string {
  const data = (e as { response?: { data?: unknown } })?.response?.data;
  return typeof data === "string" && data ? data : "Não foi possível concluir a ação.";
}

type TypeFilter = "" | "SO" | "PA";

export function SolicitacoesPage() {
  const { data: all, isLoading } = useQuery({
    queryKey: ["solicitations-all"],
    queryFn: () => SolicitationsApi.list({ includeArchived: true }),
  });
  const { data: clients } = useQuery({ queryKey: ["clients"], queryFn: ClientsApi.list });

  const [q, setQ] = useState("");
  const [type, setType] = useState<TypeFilter>("");
  const [showArchived, setShowArchived] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const term = q.trim().toLowerCase();
  const list = (all ?? []).filter((s) => {
    if (!showArchived && s.isArchived) return false;
    if (type && s.type !== type) return false;
    if (
      term &&
      !s.number.toLowerCase().includes(term) &&
      !(s.title ?? "").toLowerCase().includes(term) &&
      !(s.clientName ?? "").toLowerCase().includes(term)
    )
      return false;
    return true;
  });

  return (
    <div>
      <div className="page-head">
        <div>
          <span className="eyebrow">Cadastro</span>
          <h2>Solicitações</h2>
        </div>
      </div>

      <div className="daytools">
        <div className="filters">
          <input
            className="input"
            style={{ width: 260 }}
            placeholder="Buscar por número, título ou empresa…"
            value={q}
            onChange={(e) => setQ(e.target.value)}
          />
          <div className="seg" role="group" aria-label="Tipo">
            {(["", "SO", "PA"] as TypeFilter[]).map((t) => (
              <button key={t || "all"} className={type === t ? "on" : ""} onClick={() => setType(t)}>
                {t === "" ? "Todos" : t}
              </button>
            ))}
          </div>
        </div>
        <label className="check">
          <input
            type="checkbox"
            checked={showArchived}
            onChange={(e) => setShowArchived(e.target.checked)}
          />
          mostrar arquivadas
        </label>
      </div>

      {error && (
        <div className="alert-error" style={{ marginBottom: "1rem" }}>
          {error}
        </div>
      )}

      {isLoading ? (
        <div className="placeholder">carregando…</div>
      ) : !list.length ? (
        <div className="placeholder">nenhuma solicitação encontrada</div>
      ) : (
        <div className="list">
          {list.map((s) => (
            <SolRow key={s.id} sol={s} clients={clients ?? []} onError={setError} />
          ))}
        </div>
      )}
    </div>
  );
}

function SolRow({
  sol,
  clients,
  onError,
}: {
  sol: Solicitation;
  clients: ClientItem[];
  onError: (m: string | null) => void;
}) {
  const qc = useQueryClient();
  const [editing, setEditing] = useState(false);
  const [title, setTitle] = useState(sol.title ?? "");
  const [clientId, setClientId] = useState<string>(sol.clientId ? String(sol.clientId) : "");
  const number = sol.number;

  const invalidate = () => {
    qc.invalidateQueries({ queryKey: ["solicitations-all"] });
    qc.invalidateQueries({ queryKey: ["clients"] });
  };

  const save = useMutation({
    mutationFn: () =>
      SolicitationsApi.update(sol.id, {
        clientId: clientId ? Number(clientId) : null,
        title: title.trim() || null,
        isArchived: sol.isArchived,
      }),
    onSuccess: () => {
      setEditing(false);
      onError(null);
      invalidate();
    },
    onError: (e) => onError(messageOf(e)),
  });

  const toggleArchive = useMutation({
    mutationFn: () =>
      SolicitationsApi.update(sol.id, {
        clientId: sol.clientId,
        title: sol.title,
        isArchived: !sol.isArchived,
      }),
    onSuccess: () => {
      onError(null);
      invalidate();
    },
    onError: (e) => onError(messageOf(e)),
  });

  const remove = useMutation({
    mutationFn: () => SolicitationsApi.remove(sol.id),
    onSuccess: () => {
      onError(null);
      invalidate();
    },
    onError: (e) => onError(messageOf(e)),
  });

  return (
    <div className={"sol-row" + (sol.isArchived ? " archived" : "")}>
      <div className="sol-main">
        <div className="sol-head">
          <span className={`chip chip-${sol.type.toLowerCase()}`}>{sol.type}</span>
          <span className="code">{number}</span>
          {sol.isArchived && <span className="arch-tag">arquivada</span>}
        </div>
        {editing ? (
          <div className="sol-edit">
            <input
              className="input"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              placeholder="Título / descrição"
            />
            <select className="select" value={clientId} onChange={(e) => setClientId(e.target.value)}>
              <option value="">— sem empresa —</option>
              {clients.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name}
                </option>
              ))}
            </select>
          </div>
        ) : (
          <div className="company-meta">
            {sol.clientName ?? "sem empresa"}
            {sol.title ? ` · ${sol.title}` : ""}
          </div>
        )}
      </div>

      <div className="company-actions">
        {editing ? (
          <>
            <button className="btn btn-ink btn-sm" disabled={save.isPending} onClick={() => save.mutate()}>
              Salvar
            </button>
            <button
              className="btn btn-ghost btn-sm"
              onClick={() => {
                setEditing(false);
                setTitle(sol.title ?? "");
                setClientId(sol.clientId ? String(sol.clientId) : "");
              }}
            >
              Cancelar
            </button>
          </>
        ) : (
          <>
            <button className="btn btn-ghost btn-sm" onClick={() => setEditing(true)}>
              Editar
            </button>
            <button className="btn btn-ghost btn-sm" onClick={() => toggleArchive.mutate()}>
              {sol.isArchived ? "Desarquivar" : "Arquivar"}
            </button>
            <button
              className="btn btn-danger btn-sm"
              onClick={() => {
                if (window.confirm(`Excluir ${sol.code}? (só é possível se não houver tempo lançado)`))
                  remove.mutate();
              }}
            >
              Excluir
            </button>
          </>
        )}
      </div>
    </div>
  );
}
