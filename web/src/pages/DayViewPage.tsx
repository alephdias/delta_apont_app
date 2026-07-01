import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { DayEntriesApi, ProfileApi } from "../api/client";
import { DayEntryRow } from "../components/DayEntryRow";
import { QuarterMeter } from "../components/QuarterMeter";
import { addDays, formatMinutes, longDate, todayIso } from "../lib/format";

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
  const delta = totalAdj - totalReal;
  const remaining = Math.max(0, target - totalAdj);

  return (
    <div>
      <div className="page-head">
        <div>
          <span className="eyebrow">Apontamentos</span>
          <h2>Meu dia</h2>
        </div>
        <div className="date-nav">
          <button className="icon-btn" onClick={() => setDate(addDays(date, -1))} aria-label="dia anterior">
            ‹
          </button>
          <input
            type="date"
            className="date-input"
            value={date}
            onChange={(e) => setDate(e.target.value)}
          />
          <button className="icon-btn" onClick={() => setDate(addDays(date, 1))} aria-label="próximo dia">
            ›
          </button>
        </div>
      </div>

      {/* Herói: o medidor de quartos de hora */}
      <div className="gauge">
        <div className="gauge-top">
          <div>
            <span className="eyebrow" style={{ display: "block", marginBottom: "0.5rem" }}>
              Apontado · {longDate(date)}
            </span>
            <div className="gauge-figure">
              {formatMinutes(totalAdj)} <span className="of">/ {formatMinutes(target)}</span>
            </div>
          </div>
          <div className="gauge-right">
            <span className="eyebrow">Meta do dia</span>
            <div className="gauge-target">{formatMinutes(target)}</div>
          </div>
        </div>

        <QuarterMeter key={date} adjustedMinutes={totalAdj} targetMinutes={target} animate />

        <div className="gauge-legend">
          <span>
            tempo real <b>{formatMinutes(totalReal)}</b>
          </span>
          <span>
            apontado <b>{formatMinutes(totalAdj)}</b>{" "}
            <span className="delta-chip">{delta > 0 ? `Δ +${delta}m` : "Δ ±0"}</span>
          </span>
          <span>
            {remaining > 0 ? (
              <>
                falta <b>{formatMinutes(remaining)}</b>
              </>
            ) : (
              <b className="delta-chip">meta batida</b>
            )}
          </span>
        </div>
      </div>

      <div className="section-label">
        <span className="eyebrow">Solicitações do dia</span>
        <span className="eyebrow">{entries?.length ?? 0} registro(s)</span>
      </div>

      {isLoading ? (
        <div className="placeholder">carregando…</div>
      ) : !entries?.length ? (
        <div className="placeholder">nenhum apontamento neste dia</div>
      ) : (
        <div className="list">
          {entries.map((e) => (
            <DayEntryRow key={e.solicitationId} entry={e} date={date} />
          ))}
        </div>
      )}
    </div>
  );
}
