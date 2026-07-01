import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ProfileApi } from "../api/client";
import { formatMinutes } from "../lib/format";

const presets = [
  { label: "Estagiário", hours: "6h", minutes: 360 },
  { label: "Analista", hours: "8h", minutes: 480 },
];

export function ConfiguracoesPage() {
  const qc = useQueryClient();
  const { data: profile, isLoading } = useQuery({ queryKey: ["profile"], queryFn: ProfileApi.get });

  const [name, setName] = useState("");
  const [minutes, setMinutes] = useState(360);
  const [customHours, setCustomHours] = useState("6");
  const [saved, setSaved] = useState(false);

  useEffect(() => {
    if (profile) {
      setName(profile.displayName ?? "");
      setMinutes(profile.dailyTargetMinutes);
      setCustomHours((profile.dailyTargetMinutes / 60).toString());
    }
  }, [profile]);

  const save = useMutation({
    mutationFn: () =>
      ProfileApi.update({ displayName: name.trim() || null, dailyTargetMinutes: minutes }),
    onSuccess: () => {
      setSaved(true);
      setTimeout(() => setSaved(false), 2000);
      qc.invalidateQueries({ queryKey: ["profile"] });
      qc.invalidateQueries({ queryKey: ["month"] });
    },
  });

  if (isLoading) return <div className="placeholder">carregando…</div>;

  return (
    <div>
      <div className="page-head">
        <div>
          <span className="eyebrow">Preferências</span>
          <h2>Configurações</h2>
        </div>
      </div>

      <div className="config-card">
        <h3 className="config-title">Meta diária</h3>
        <p className="config-hint">
          Quanto tempo você precisa fechar por dia. Usada nas metas do dia e do mês.
        </p>
        <div className="preset-row">
          {presets.map((p) => (
            <button
              key={p.minutes}
              className={"preset" + (minutes === p.minutes ? " on" : "")}
              onClick={() => {
                setMinutes(p.minutes);
                setCustomHours((p.minutes / 60).toString());
              }}
            >
              <span className="preset-h">{p.hours}</span>
              <span className="preset-l">{p.label}</span>
            </button>
          ))}
          <div className="preset preset-custom">
            <span className="preset-l">Outro (horas)</span>
            <input
              className="input"
              type="number"
              min={0}
              step={0.25}
              value={customHours}
              onChange={(e) => {
                setCustomHours(e.target.value);
                const h = Number(e.target.value);
                if (!Number.isNaN(h) && h > 0) setMinutes(Math.round(h * 60));
              }}
            />
          </div>
        </div>
        <div className="config-current">
          Meta atual: <b>{formatMinutes(minutes)}</b>
        </div>
      </div>

      <div className="config-card">
        <h3 className="config-title">Nome de exibição</h3>
        <p className="config-hint">Opcional. Como você quer ser identificado.</p>
        <input
          className="input"
          style={{ maxWidth: 320 }}
          value={name}
          onChange={(e) => setName(e.target.value)}
          placeholder={profile?.email ?? "Seu nome"}
        />
      </div>

      <div className="config-save">
        <button className="btn btn-ink" disabled={save.isPending} onClick={() => save.mutate()}>
          {save.isPending ? "Salvando…" : "Salvar alterações"}
        </button>
        {saved && <span className="saved">✓ salvo</span>}
      </div>
    </div>
  );
}
