import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { DayEntriesApi, ProfileApi } from "../api/client";
import { DayEntryRow } from "../components/DayEntryRow";
import { QuarterMeter } from "../components/QuarterMeter";
import { addDays, formatMinutes, longDate, todayIso } from "../lib/format";
import { copyText, downloadCsv, toCsv, toTsv } from "../lib/export";

type TypeFilter = "" | "SO" | "PA";

export function DayViewPage() {
  const [date, setDate] = useState(todayIso());
  const [clientFilter, setClientFilter] = useState("");
  const [typeFilter, setTypeFilter] = useState<TypeFilter>("");
  const [copied, setCopied] = useState(false);

  const { data: entries, isLoading } = useQuery({
    queryKey: ["dayentries", date],
    queryFn: () => DayEntriesApi.byDate(date),
  });
  const { data: profile } = useQuery({ queryKey: ["profile"], queryFn: ProfileApi.get });

  const all = entries ?? [];
  const clientsInDay = [...new Set(all.map((e) => e.clientName).filter(Boolean))].sort() as string[];
  const filtered = all.filter(
    (e) =>
      (!clientFilter || e.clientName === clientFilter) &&
      (!typeFilter || e.type === typeFilter)
  );

  // O medidor reflete o dia inteiro (a meta é do dia), não o filtro.
  const totalReal = all.reduce((s, e) => s + e.realMinutes, 0);
  const totalAdj = all.reduce((s, e) => s + e.adjustedMinutes, 0);
  const target = profile?.dailyTargetMinutes ?? 360;
  const delta = totalAdj - totalReal;
  const remaining = Math.max(0, target - totalAdj);

  const copyDay = async () => {
    await copyText(toTsv(filtered));
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };
  const csvDay = () => downloadCsv(`apontamentos-${date}.csv`, toCsv(filtered));

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
          <input type="date" className="date-input" value={date} onChange={(e) => setDate(e.target.value)} />
          <button className="icon-btn" onClick={() => setDate(addDays(date, 1))} aria-label="próximo dia">
            ›
          </button>
        </div>
      </div>

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

      {all.length > 0 && (
        <div className="daytools">
          <div className="filters">
            <select
              className="select"
              value={clientFilter}
              onChange={(e) => setClientFilter(e.target.value)}
              aria-label="Filtrar por empresa"
            >
              <option value="">Todas as empresas</option>
              {clientsInDay.map((c) => (
                <option key={c} value={c}>
                  {c}
                </option>
              ))}
            </select>
            <div className="seg" role="group" aria-label="Filtrar por tipo">
              {(["", "SO", "PA"] as TypeFilter[]).map((t) => (
                <button
                  key={t || "all"}
                  className={typeFilter === t ? "on" : ""}
                  onClick={() => setTypeFilter(t)}
                >
                  {t === "" ? "Todos" : t}
                </button>
              ))}
            </div>
          </div>
          <div className="actions">
            <button className="btn btn-ghost btn-sm" onClick={copyDay}>
              {copied ? "✓ copiado" : "Copiar"}
            </button>
            <button className="btn btn-ghost btn-sm" onClick={csvDay}>
              CSV
            </button>
          </div>
        </div>
      )}

      <div className="section-label">
        <span className="eyebrow">Solicitações do dia</span>
        <span className="eyebrow">
          {filtered.length}
          {filtered.length !== all.length ? ` de ${all.length}` : ""} registro
          {all.length === 1 && filtered.length === 1 ? "" : "s"}
        </span>
      </div>

      {isLoading ? (
        <div className="placeholder">carregando…</div>
      ) : !all.length ? (
        <div className="placeholder">nenhum apontamento neste dia</div>
      ) : !filtered.length ? (
        <div className="placeholder">nenhum registro com esse filtro</div>
      ) : (
        <div className="list">
          {filtered.map((e) => (
            <DayEntryRow key={e.solicitationId} entry={e} date={date} />
          ))}
        </div>
      )}
    </div>
  );
}
