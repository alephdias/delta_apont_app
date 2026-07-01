import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { EvidenceApi } from "../api/client";

function messageOf(e: unknown): string {
  const data = (e as { response?: { data?: unknown } })?.response?.data;
  return typeof data === "string" && data ? data : "Não foi possível concluir.";
}

export function EvidencePanel({ solicitationId }: { solicitationId: number }) {
  const qc = useQueryClient();
  const key = ["evidence", solicitationId];
  const { data, isLoading } = useQuery({ queryKey: key, queryFn: () => EvidenceApi.list(solicitationId) });
  const [link, setLink] = useState("");
  const [busy, setBusy] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  const invalidate = () => qc.invalidateQueries({ queryKey: key });

  const addLink = useMutation({
    mutationFn: () => EvidenceApi.addLink(solicitationId, link.trim()),
    onSuccess: () => {
      setLink("");
      setErr(null);
      invalidate();
    },
    onError: (e) => setErr(messageOf(e)),
  });

  const del = useMutation({
    mutationFn: (id: number) => EvidenceApi.remove(id),
    onSuccess: invalidate,
    onError: (e) => setErr(messageOf(e)),
  });

  const uploadFile = async (file: File, caption?: string) => {
    setBusy(true);
    setErr(null);
    try {
      await EvidenceApi.upload(solicitationId, file, caption);
      invalidate();
    } catch (e) {
      setErr(messageOf(e));
    } finally {
      setBusy(false);
    }
  };

  const onPaste = (e: React.ClipboardEvent) => {
    const item = [...e.clipboardData.items].find((i) => i.type.startsWith("image/"));
    const file = item?.getAsFile();
    if (file) {
      e.preventDefault();
      void uploadFile(file, "colado da área de transferência");
    }
  };

  return (
    <div className="ev-panel" onPaste={onPaste} tabIndex={0}>
      {isLoading ? (
        <div className="ev-hint">carregando evidências…</div>
      ) : data && data.length > 0 ? (
        <div className="ev-list">
          {data.map((ev) => (
            <div className="ev-item" key={ev.id}>
              <span className="ev-kind">{ev.kind === "File" ? "arquivo" : "link"}</span>
              {ev.url ? (
                <a href={ev.url} target="_blank" rel="noreferrer">
                  {ev.caption || ev.value}
                </a>
              ) : (
                <span>{ev.caption || ev.value}</span>
              )}
              <button
                className="ev-del"
                title="Remover"
                onClick={() => del.mutate(ev.id)}
                aria-label="Remover evidência"
              >
                ✕
              </button>
            </div>
          ))}
        </div>
      ) : (
        <div className="ev-hint">nenhuma evidência ainda</div>
      )}

      <div className="ev-add">
        <input
          className="input"
          placeholder="Colar um link (chamado, caminho de rede)…"
          value={link}
          onChange={(e) => setLink(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === "Enter" && link.trim()) addLink.mutate();
          }}
        />
        <button className="btn btn-ghost btn-sm" disabled={!link.trim()} onClick={() => addLink.mutate()}>
          Adicionar link
        </button>
        <label className="btn btn-ghost btn-sm file-btn">
          {busy ? "Enviando…" : "Anexar arquivo"}
          <input
            type="file"
            hidden
            disabled={busy}
            onChange={(e) => {
              const f = e.target.files?.[0];
              if (f) void uploadFile(f);
              e.target.value = "";
            }}
          />
        </label>
      </div>
      <div className="ev-hint">dica: clique aqui e cole um print com Ctrl+V</div>
      {err && (
        <div className="alert-error" style={{ marginTop: "0.5rem" }}>
          {err}
        </div>
      )}
    </div>
  );
}
