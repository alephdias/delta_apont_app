import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { DayEntriesApi } from "../api/client";
import { formatMinutes, currentMonthIso, formatDateBr } from "../lib/format";

export function MonthClosePage() {
  const [month, setMonth] = useState(currentMonthIso());

  const { data, isLoading } = useQuery({
    queryKey: ["month", month],
    queryFn: () => DayEntriesApi.month(month),
  });

  const target = data?.targetMinutes ?? 360;

  return (
    <div className="page">
      <div className="page-head">
        <div>
          <h2>Fechamento mensal</h2>
          <p className="muted">Total apontado por dia versus a meta diária.</p>
        </div>
        <input
          type="month"
          className="date-input"
          value={month}
          onChange={(e) => setMonth(e.target.value)}
        />
      </div>

      <div className="stat-row">
        <div className="stat stat-accent">
          <span className="stat-label">Total apontado no mês</span>
          <span className="stat-value">
            {formatMinutes(data?.totalAdjustedMinutes ?? 0)}
          </span>
        </div>
        <div className="stat">
          <span className="stat-label">Meta diária</span>
          <span className="stat-value">{formatMinutes(target)}</span>
        </div>
        <div className="stat">
          <span className="stat-label">Dias com lançamento</span>
          <span className="stat-value">{data?.days.length ?? 0}</span>
        </div>
      </div>

      <div className="card">
        {isLoading ? (
          <div className="empty">Carregando…</div>
        ) : !data?.days.length ? (
          <div className="empty">Nenhum lançamento neste mês.</div>
        ) : (
          <table className="table">
            <thead>
              <tr>
                <th>Dia</th>
                <th className="num">Apontado</th>
                <th className="num">Meta</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {data.days.map((d) => {
                const pct = Math.min(
                  100,
                  Math.round((d.totalAdjustedMinutes / d.targetMinutes) * 100)
                );
                return (
                  <tr key={d.workDate}>
                    <td>{formatDateBr(d.workDate)}</td>
                    <td className="num">{formatMinutes(d.totalAdjustedMinutes)}</td>
                    <td className="num muted">{formatMinutes(d.targetMinutes)}</td>
                    <td>
                      <div className="progress">
                        <div
                          className={`progress-bar${d.metTarget ? " ok" : ""}`}
                          style={{ width: `${pct}%` }}
                        />
                      </div>
                      <span className={`status-tag${d.metTarget ? " ok" : ""}`}>
                        {d.metTarget ? "Meta batida" : `${pct}%`}
                      </span>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}
