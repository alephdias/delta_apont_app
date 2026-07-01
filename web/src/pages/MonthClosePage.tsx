import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { DayEntriesApi } from "../api/client";
import { QuarterMeter } from "../components/QuarterMeter";
import { dayNum, formatMinutes, monthTitle, weekdayShort } from "../lib/format";

export function MonthClosePage() {
  const [month, setMonth] = useState(currentMonth());

  const { data, isLoading } = useQuery({
    queryKey: ["month", month],
    queryFn: () => DayEntriesApi.month(month),
  });

  const target = data?.targetMinutes ?? 360;
  const metDays = data?.days.filter((d) => d.metTarget).length ?? 0;

  return (
    <div>
      <div className="page-head">
        <div>
          <span className="eyebrow">Fechamento</span>
          <h2>{monthTitle(month)}</h2>
        </div>
        <input
          type="month"
          className="date-input"
          value={month}
          onChange={(e) => setMonth(e.target.value)}
        />
      </div>

      <div className="gauge">
        <div className="gauge-top">
          <div>
            <span className="eyebrow" style={{ display: "block", marginBottom: "0.5rem" }}>
              Total apontado no mês
            </span>
            <div className="gauge-figure">
              {formatMinutes(data?.totalAdjustedMinutes ?? 0)}
            </div>
          </div>
          <div className="gauge-right">
            <span className="eyebrow">Dias na meta</span>
            <div className="gauge-target">
              {metDays} / {data?.days.length ?? 0}
            </div>
          </div>
        </div>
        <div className="gauge-legend">
          <span>
            meta diária <b>{formatMinutes(target)}</b>
          </span>
          <span>
            dias com lançamento <b>{data?.days.length ?? 0}</b>
          </span>
        </div>
      </div>

      <div className="section-label">
        <span className="eyebrow">Dia a dia</span>
        <span className="eyebrow">cada tick = 15 min</span>
      </div>

      {isLoading ? (
        <div className="placeholder">carregando…</div>
      ) : !data?.days.length ? (
        <div className="placeholder">nenhum lançamento neste mês</div>
      ) : (
        <div className="list">
          {data.days.map((d) => (
            <div key={d.workDate} className={"month-day" + (d.metTarget ? " met" : "")}>
              <div className="md-date">
                {dayNum(d.workDate)}
                <span className="dow">{weekdayShort(d.workDate)}</span>
              </div>
              <QuarterMeter
                adjustedMinutes={d.totalAdjustedMinutes}
                targetMinutes={d.targetMinutes}
                size="sm"
              />
              <div className="md-value">
                {formatMinutes(d.totalAdjustedMinutes)}
                <br />
                {d.metTarget ? (
                  <span className="met-tag">meta ✓</span>
                ) : (
                  <span className="pct">
                    {Math.round((d.totalAdjustedMinutes / d.targetMinutes) * 100)}%
                  </span>
                )}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

function currentMonth(): string {
  const now = new Date();
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, "0")}`;
}
