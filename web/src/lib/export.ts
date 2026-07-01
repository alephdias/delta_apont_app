import type { DayEntry } from "../api/client";

function hhmm(min: number): string {
  const h = Math.floor(min / 60);
  const m = min % 60;
  return `${h}:${String(m).padStart(2, "0")}`;
}

const HEADERS = [
  "Código",
  "Cliente",
  "Início",
  "Fim",
  "Apontado (min)",
  "Apontado (h:mm)",
  "Observações",
];

function cells(e: DayEntry): (string | number)[] {
  return [
    e.code,
    e.clientName ?? "",
    e.firstStart ? e.firstStart.slice(0, 5) : "",
    e.isRunning ? "em curso" : e.lastEnd ? e.lastEnd.slice(0, 5) : "",
    e.adjustedMinutes,
    hhmm(e.adjustedMinutes),
    e.notes ?? "",
  ];
}

/** Texto separado por tabs — cola direto no Excel/planilha/sistema. */
export function toTsv(entries: DayEntry[]): string {
  const lines = [HEADERS.join("\t"), ...entries.map((e) => cells(e).join("\t"))];
  return lines.join("\n");
}

/** CSV com ";" (padrão do Excel pt-BR) e BOM para acentos. */
export function toCsv(entries: DayEntry[]): string {
  const esc = (v: string | number) => {
    const s = String(v);
    return /[",;\n]/.test(s) ? `"${s.replace(/"/g, '""')}"` : s;
  };
  const lines = [
    HEADERS.map(esc).join(";"),
    ...entries.map((e) => cells(e).map(esc).join(";")),
  ];
  return lines.join("\r\n");
}

export function downloadCsv(filename: string, csv: string) {
  const blob = new Blob(["﻿" + csv], { type: "text/csv;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = filename;
  a.click();
  URL.revokeObjectURL(url);
}

export async function copyText(text: string): Promise<void> {
  await navigator.clipboard.writeText(text);
}
