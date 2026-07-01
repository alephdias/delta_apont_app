export function formatMinutes(min: number): string {
  const h = Math.floor(min / 60);
  const m = min % 60;
  if (h === 0) return `${m}min`;
  if (m === 0) return `${h}h`;
  return `${h}h${String(m).padStart(2, "0")}`;
}

/** Data local no formato YYYY-MM-DD. */
export function todayIso(): string {
  const now = new Date();
  return isoOf(now);
}

export function currentMonthIso(): string {
  const now = new Date();
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, "0")}`;
}

function isoOf(dt: Date): string {
  const y = dt.getFullYear();
  const mo = String(dt.getMonth() + 1).padStart(2, "0");
  const d = String(dt.getDate()).padStart(2, "0");
  return `${y}-${mo}-${d}`;
}

export function addDays(iso: string, n: number): string {
  const [y, m, d] = iso.split("-").map(Number);
  const dt = new Date(y, m - 1, d);
  dt.setDate(dt.getDate() + n);
  return isoOf(dt);
}

/** "15:32:00" -> "15:32". */
export function shortTime(t: string | null): string {
  if (!t) return "—";
  return t.slice(0, 5);
}

function dateFrom(iso: string): Date {
  const [y, m, d] = iso.split("-").map(Number);
  return new Date(y, m - 1, d);
}

export function longDate(iso: string): string {
  return dateFrom(iso).toLocaleDateString("pt-BR", {
    weekday: "long",
    day: "2-digit",
    month: "long",
  });
}

export function weekdayShort(iso: string): string {
  return dateFrom(iso)
    .toLocaleDateString("pt-BR", { weekday: "short" })
    .replace(".", "");
}

export function dayNum(iso: string): string {
  return iso.split("-")[2];
}

export function monthTitle(monthIso: string): string {
  const [y, m] = monthIso.split("-").map(Number);
  const s = new Date(y, m - 1, 1).toLocaleDateString("pt-BR", {
    month: "long",
    year: "numeric",
  });
  return s.charAt(0).toUpperCase() + s.slice(1);
}
