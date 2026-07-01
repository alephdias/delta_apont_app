import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ClientsApi } from "../api/client";
import type { ClientItem } from "../api/client";

function messageOf(e: unknown): string {
  const data = (e as { response?: { data?: unknown } })?.response?.data;
  return typeof data === "string" && data ? data : "Não foi possível concluir a ação.";
}

export function EmpresasPage() {
  const qc = useQueryClient();
  const { data, isLoading } = useQuery({ queryKey: ["clients"], queryFn: ClientsApi.list });
  const [name, setName] = useState("");
  const [error, setError] = useState<string | null>(null);

  const create = useMutation({
    mutationFn: () => ClientsApi.create(name.trim()),
    onSuccess: () => {
      setName("");
      setError(null);
      qc.invalidateQueries({ queryKey: ["clients"] });
    },
    onError: (e) => setError(messageOf(e)),
  });

  return (
    <div>
      <div className="page-head">
        <div>
          <span className="eyebrow">Cadastro</span>
          <h2>Empresas</h2>
        </div>
      </div>

      <form
        className="company-add"
        onSubmit={(e) => {
          e.preventDefault();
          if (name.trim()) create.mutate();
        }}
      >
        <input
          className="input"
          placeholder="Nome da empresa…"
          value={name}
          onChange={(e) => setName(e.target.value)}
        />
        <button className="btn btn-ink" disabled={!name.trim() || create.isPending}>
          {create.isPending ? "…" : "Adicionar"}
        </button>
      </form>

      {error && (
        <div className="alert-error" style={{ marginBottom: "1rem" }}>
          {error}
        </div>
      )}

      {isLoading ? (
        <div className="placeholder">carregando…</div>
      ) : !data?.length ? (
        <div className="placeholder">nenhuma empresa cadastrada</div>
      ) : (
        <div className="list">
          {data.map((c) => (
            <CompanyRow key={c.id} company={c} onError={setError} />
          ))}
        </div>
      )}
    </div>
  );
}

function CompanyRow({
  company,
  onError,
}: {
  company: ClientItem;
  onError: (m: string | null) => void;
}) {
  const qc = useQueryClient();
  const [editing, setEditing] = useState(false);
  const [name, setName] = useState(company.name);

  const rename = useMutation({
    mutationFn: () => ClientsApi.update(company.id, name.trim()),
    onSuccess: () => {
      setEditing(false);
      onError(null);
      qc.invalidateQueries({ queryKey: ["clients"] });
    },
    onError: (e) => onError(messageOf(e)),
  });

  const remove = useMutation({
    mutationFn: () => ClientsApi.remove(company.id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["clients"] }),
    onError: (e) => onError(messageOf(e)),
  });

  return (
    <div className="company-row">
      <div>
        {editing ? (
          <input
            className="company-name-input"
            value={name}
            autoFocus
            onChange={(e) => setName(e.target.value)}
          />
        ) : (
          <div className="company-name">{company.name}</div>
        )}
      </div>
      <div className="company-meta">
        {company.solicitationCount} solicitação
        {company.solicitationCount === 1 ? "" : "s"}
      </div>
      <div className="company-actions">
        {editing ? (
          <>
            <button
              className="btn btn-ink btn-sm"
              disabled={!name.trim() || rename.isPending}
              onClick={() => rename.mutate()}
            >
              Salvar
            </button>
            <button
              className="btn btn-ghost btn-sm"
              onClick={() => {
                setEditing(false);
                setName(company.name);
              }}
            >
              Cancelar
            </button>
          </>
        ) : (
          <>
            <button
              className="btn btn-ghost btn-sm"
              onClick={() => {
                onError(null);
                setEditing(true);
              }}
            >
              Renomear
            </button>
            <button
              className="btn btn-danger btn-sm"
              onClick={() => {
                if (window.confirm(`Excluir a empresa "${company.name}"?`)) remove.mutate();
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
