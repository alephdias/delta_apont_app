import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ClientsApi, SolicitationsApi } from "../api/client";
import type { ClientItem, Solicitation, SolStatus } from "../api/client";
import { EvidencePanel } from "../components/EvidencePanel";

function messageOf(e: unknown): string {
  const data = (e as { response?: { data?: unknown } })?.response?.data;
  return typeof data === "string" && data ? data : "Não foi possível concluir a ação.";
}

type TypeFilter = "" | "SO" | "PA";

const STATUS_LABEL: Record<SolStatus, string> = {
  FilaDeEspera: "Na fila de espera",
  EmAtendimento: "Em atendimento",
  Pausada: "Pausada",
  Finalizado: "Finalizado",
};

const STATUS_OPTIONS: SolStatus[] = ["FilaDeEspera", "EmAtendimento", "Pausada", "Finalizado"];

export function SolicitacoesPage() {
  const { data: all, isLoading } = useQuery({
    queryKey: ["solicitations-all"],
    queryFn: () => SolicitationsApi.list({ includeArchived: true }),
  });
  const { data: clients } = useQuery({ queryKey: ["clients"], queryFn: ClientsApi.list });

  const [q, setQ] = useState("");
  const [type, setType] = useState<TypeFilter>("");
  const [status, setStatus] = useState<"" | SolStatus>("");
  const [tag, setTag] = useState("");
  const [showArchived, setShowArchived] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const term = q.trim().toLowerCase();
  const tagTerm = tag.trim().toLowerCase();
  const list = (all ?? []).filter((s) => {
    if (!showArchived && s.isArchived) return false;
    if (type && s.type !== type) return false;
    if (status && s.status !== status) return false;
    if (tagTerm && !(s.tags ?? "").toLowerCase().includes(tagTerm)) return false;
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
            style={{ width: 230 }}
            placeholder="Buscar número, título ou empresa…"
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
          <select
            className="select"
            value={status}
            onChange={(e) => setStatus(e.target.value as "" | SolStatus)}
            aria-label="Status"
          >
            <option value="">Todos status</option>
            {STATUS_OPTIONS.map((s) => (
              <option key={s} value={s}>
                {STATUS_LABEL[s]}
              </option>
            ))}
          </select>
          <input
            className="input"
            style={{ width: 140 }}
            placeholder="etiqueta…"
            value={tag}
            onChange={(e) => setTag(e.target.value)}
          />
        </div>
        <label className="check">
          <input type="checkbox" checked={showArchived} onChange={(e) => setShowArchived(e.target.checked)} />
          arquivadas
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
  const [showEv, setShowEv] = useState(false);
  const [title, setTitle] = useState(sol.title ?? "");
  const [clientId, setClientId] = useState<string>(sol.clientId ? String(sol.clientId) : "");
  const [status, setStatus] = useState<SolStatus>(sol.status);
  const [tags, setTags] = useState(sol.tags ?? "");

  const invalidate = () => {
    qc.invalidateQueries({ queryKey: ["solicitations-all"] });
    qc.invalidateQueries({ queryKey: ["clients"] });
  };

  const save = useMutation({
    mutationFn: () =>
      SolicitationsApi.update(sol.id, {
        clientId: clientId ? Number(clientId) : null,
        title: title.trim() || null,
        status,
        tags: tags.trim() || null,
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
        status: sol.status,
        tags: sol.tags,
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

  const duplicate = useMutation({
    mutationFn: (num: string) =>
      SolicitationsApi.create({
        type: sol.type,
        number: num,
        clientId: sol.clientId,
        title: sol.title,
        tags: sol.tags,
      }),
    onSuccess: () => {
      onError(null);
      invalidate();
    },
    onError: (e) => onError(messageOf(e)),
  });

  const onDuplicate = () => {
    const n = window.prompt(`Duplicar ${sol.code} — informe o número da nova ${sol.type}:`, "");
    if (n && n.trim()) duplicate.mutate(n.trim());
  };

  return (
    <div className={"sol-row" + (sol.isArchived ? " archived" : "")}>
      <div className="sol-main">
        <div className="sol-head">
          <span className={`chip chip-${sol.type.toLowerCase()}`}>{sol.type}</span>
          <span className="code">{sol.number}</span>
          <span className={`status-badge st-${sol.status.toLowerCase()}`}>{STATUS_LABEL[sol.status]}</span>
          {sol.isArchived && <span className="arch-tag">arquivada</span>}
        </div>
        {editing ? (
          <div className="sol-edit">
            <input
              className="input"
              value={title}
              placeholder="Título / descrição"
              onChange={(e) => setTitle(e.target.value)}
            />
            <select className="select" value={clientId} onChange={(e) => setClientId(e.target.value)}>
              <option value="">— sem empresa —</option>
              {clients.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name}
                </option>
              ))}
            </select>
            <select className="select" value={status} onChange={(e) => setStatus(e.target.value as SolStatus)}>
              {STATUS_OPTIONS.map((s) => (
                <option key={s} value={s}>
                  {STATUS_LABEL[s]}
                </option>
              ))}
            </select>
            <input
              className="input"
              value={tags}
              placeholder="etiquetas (vírgula)"
              onChange={(e) => setTags(e.target.value)}
            />
          </div>
        ) : (
          <div className="company-meta">
            {sol.clientName ?? "sem empresa"}
            {sol.title ? ` · ${sol.title}` : ""}
            {sol.tags && (
              <span className="tag-chips">
                {sol.tags.split(",").map((t) => (
                  <span key={t} className="tag-chip">
                    #{t}
                  </span>
                ))}
              </span>
            )}
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
                setStatus(sol.status);
                setTags(sol.tags ?? "");
              }}
            >
              Cancelar
            </button>
          </>
        ) : (
          <>
            <button
              className={"btn btn-ghost btn-sm" + (showEv ? " on" : "")}
              onClick={() => setShowEv((v) => !v)}
            >
              Evidências
            </button>
            <button className="btn btn-ghost btn-sm" onClick={() => setEditing(true)}>
              Editar
            </button>
            <button className="btn btn-ghost btn-sm" onClick={onDuplicate}>
              Duplicar
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
      {showEv && <EvidencePanel solicitationId={sol.id} />}
    </div>
  );
}
