import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { DayEntriesApi } from "../api/client";
import type { DayEntry } from "../api/client";
import { formatMinutes, shortTime } from "../lib/format";

export function DayEntryRow({ entry, date }: { entry: DayEntry; date: string }) {
  const qc = useQueryClient();
  const [adjusted, setAdjusted] = useState(entry.adjustedMinutes);
  const [notes, setNotes] = useState(entry.notes ?? "");
  const [saved, setSaved] = useState(false);

  const mutation = useMutation({
    mutationFn: () =>
      DayEntriesApi.upsert({
        solicitationId: entry.solicitationId,
        workDate: date,
        adjustedMinutes: adjusted,
        notes,
      }),
    onSuccess: () => {
      setSaved(true);
      qc.invalidateQueries({ queryKey: ["dayentries", date] });
      qc.invalidateQueries({ queryKey: ["month"] });
    },
  });

  const dirty = adjusted !== entry.adjustedMinutes || notes !== (entry.notes ?? "");
  const delta = adjusted - entry.realMinutes;
  const number = entry.code.replace(`${entry.type}-`, "");

  const step = (d: number) => {
    setAdjusted((v) => Math.max(0, v + d));
    setSaved(false);
  };

  return (
    <div className={"readout" + (entry.isRunning ? " running" : "")}>
      <div className="r-id">
        <div className="r-code">
          <span className={`chip chip-${entry.type.toLowerCase()}`}>{entry.type}</span>
          <span className="code">{number}</span>
          {entry.isRunning && <span className="running-dot" title="Em andamento" />}
        </div>
        <div className="r-client">
          {entry.clientName ?? "sem cliente"}
          {entry.title ? ` · ${entry.title}` : ""}
        </div>
      </div>

      <div className="r-time">
        {shortTime(entry.firstStart)}
        <span className="arrow">→</span>
        {entry.isRunning ? "agora" : shortTime(entry.lastEnd)}
        <div className="r-real">real {formatMinutes(entry.realMinutes)}</div>
      </div>

      <div className="r-adjust">
        <div className="stepper">
          <button type="button" onClick={() => step(-15)} aria-label="menos 15 min">
            −
          </button>
          <span className="val">{formatMinutes(adjusted)}</span>
          <button type="button" onClick={() => step(15)} aria-label="mais 15 min">
            +
          </button>
        </div>
        <span className={"delta-badge" + (delta === 0 ? " zero" : "")}>
          {delta > 0 ? `+${delta}m` : delta < 0 ? `${delta}m` : "±0"}
        </span>
      </div>

      <div className="r-save">
        {dirty ? (
          <button
            className="btn btn-ink btn-sm"
            disabled={mutation.isPending}
            onClick={() => mutation.mutate()}
          >
            {mutation.isPending ? "…" : "Salvar"}
          </button>
        ) : saved ? (
          <span className="saved">✓ salvo</span>
        ) : null}
      </div>

      <div className="notes-line">
        <input
          className="notes-input"
          value={notes}
          onChange={(e) => {
            setNotes(e.target.value);
            setSaved(false);
          }}
          placeholder="observações sobre o atendimento…"
        />
      </div>
    </div>
  );
}
