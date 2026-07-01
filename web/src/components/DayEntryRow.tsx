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
  const invalid = adjusted % 15 !== 0 || adjusted < 0;

  return (
    <tr>
      <td>
        <span className={`pill pill-${entry.type.toLowerCase()}`}>{entry.type}</span>
        <span className="code">{entry.code}</span>
        {entry.isRunning && <span className="running-dot" title="Em andamento" />}
        {entry.title && <div className="row-sub">{entry.title}</div>}
      </td>
      <td>{entry.clientName ?? "—"}</td>
      <td className="num">{shortTime(entry.firstStart)}</td>
      <td className="num">{shortTime(entry.lastEnd)}</td>
      <td className="num muted">{formatMinutes(entry.realMinutes)}</td>
      <td className="num">
        <input
          className={`mins-input${invalid ? " input-invalid" : ""}`}
          type="number"
          step={15}
          min={0}
          value={adjusted}
          onChange={(e) => {
            setAdjusted(Number(e.target.value));
            setSaved(false);
          }}
        />
        {invalid && <div className="hint-error">múltiplo de 15</div>}
      </td>
      <td>
        <input
          className="notes-input"
          value={notes}
          onChange={(e) => {
            setNotes(e.target.value);
            setSaved(false);
          }}
          placeholder="observações…"
        />
      </td>
      <td className="col-action">
        {dirty ? (
          <button
            className="btn btn-sm btn-primary"
            disabled={invalid || mutation.isPending}
            onClick={() => mutation.mutate()}
          >
            {mutation.isPending ? "…" : "Salvar"}
          </button>
        ) : saved ? (
          <span className="saved-msg">✓ salvo</span>
        ) : null}
      </td>
    </tr>
  );
}
