import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { DayEntriesApi, ProfileApi } from "../api/client";
import { DayEntryRow } from "../components/DayEntryRow";
import { formatMinutes, todayIso } from "../lib/format";

function Stat({
  label,
  value,
  accent,
}: {
  label: string;
  value: string;
  accent?: boolean;
}) {
  return (
    <div className={`stat${accent ? " stat-accent" : ""}`}>
      <span className="stat-label">{label}</span>
      <span className="stat-value">{value}</span>
    </div>
  );
}

export function DayViewPage() {
  const [date, setDate] = useState(todayIso());

  const { data: entries, isLoading } = useQuery({
    queryKey: ["dayentries", date],
    queryFn: () => DayEntriesApi.byDate(date),
  });
  const { data: profile } = useQuery({
    queryKey: ["profile"],
    queryFn: ProfileApi.get,
  });

  const totalReal = entries?.reduce((s, e) => s + e.realMinutes, 0) ?? 0;
  const totalAdj = entries?.reduce((s, e) => s + e.adjustedMinutes, 0) ?? 0;
  const target = profile?.dailyTargetMinutes ?? 360;

  return (
    <div className="page">
      <div className="page-head">
        <div>
          <h2>Meu dia</h2>
          <p className="muted">Solicitações que você atendeu no dia.</p>
        </div>
        <input
          type="date"
          className="date-input"
          value={date}
          onChange={(e) => setDate(e.target.value)}
        />
      </div>

      <div className="stat-row">
        <Stat label="Tempo real" value={formatMinutes(totalReal)} />
        <Stat label="Apontado" value={formatMinutes(totalAdj)} accent />
        <Stat label="Meta do dia" value={formatMinutes(target)} />
        <Stat
          label="Falta p/ meta"
          value={formatMinutes(Math.max(0, target - totalAdj))}
        />
      </div>

      <div className="card">
        {isLoading ? (
          <div className="empty">Carregando…</div>
        ) : !entries?.length ? (
          <div className="empty">Nenhum apontamento neste dia.</div>
        ) : (
          <table className="table">
            <thead>
              <tr>
                <th>Solicitação</th>
                <th>Cliente</th>
                <th className="num">Início</th>
                <th className="num">Fim</th>
                <th className="num">Real</th>
                <th className="num">Apontado</th>
                <th>Observações</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {entries.map((e) => (
                <DayEntryRow key={e.solicitationId} entry={e} date={date} />
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}
