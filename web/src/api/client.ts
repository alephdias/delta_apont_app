import axios from "axios";
import { supabase } from "../lib/supabase";

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? "http://localhost:5213/api",
});

// Anexa o access_token do Supabase em toda requisição.
api.interceptors.request.use(async (config) => {
  const { data } = await supabase.auth.getSession();
  const token = data.session?.access_token;
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

// Em 401, encerra a sessão (o ProtectedRoute redireciona para o login).
api.interceptors.response.use(
  (r) => r,
  async (error) => {
    if (error.response?.status === 401) await supabase.auth.signOut();
    return Promise.reject(error);
  }
);

// ----- Tipos (espelham os DTOs da API) -----
export interface ClientItem {
  id: number;
  name: string;
  createdAt: string;
  solicitationCount: number;
}

export interface Solicitation {
  id: number;
  type: "SO" | "PA";
  number: string;
  code: string;
  clientId: number | null;
  clientName: string | null;
  title: string | null;
  description: string | null;
  isArchived: boolean;
  createdAt: string;
}

export interface DayEntry {
  id: number;
  solicitationId: number;
  code: string;
  type: string;
  clientName: string | null;
  title: string | null;
  workDate: string;
  realMinutes: number;
  adjustedMinutes: number;
  suggestedMinutes: number;
  firstStart: string | null;
  lastEnd: string | null;
  isRunning: boolean;
  notes: string | null;
}

export interface MonthDaySummary {
  workDate: string;
  totalAdjustedMinutes: number;
  targetMinutes: number;
  metTarget: boolean;
}

export interface MonthSummary {
  month: string;
  targetMinutes: number;
  totalAdjustedMinutes: number;
  days: MonthDaySummary[];
}

export interface Profile {
  userId: string;
  email: string;
  displayName: string | null;
  dailyTargetMinutes: number;
  isAdmin: boolean;
}

export interface AdminUser {
  email: string;
  createdAt: string | null;
  lastSignInAt: string | null;
}

export interface CreatedUser {
  email: string;
  password: string;
}

// ----- Endpoints -----
export const ClientsApi = {
  list: () => api.get<ClientItem[]>("/clients").then((r) => r.data),
  create: (name: string) =>
    api.post<ClientItem>("/clients", { name }).then((r) => r.data),
  update: (id: number, name: string) =>
    api.put<ClientItem>(`/clients/${id}`, { name }).then((r) => r.data),
  remove: (id: number) => api.delete(`/clients/${id}`),
};

export interface UpdateSolicitationBody {
  clientId?: number | null;
  clientName?: string | null;
  title?: string | null;
  description?: string | null;
  isArchived: boolean;
}

export const SolicitationsApi = {
  list: (params?: {
    q?: string;
    clientId?: number;
    type?: "SO" | "PA";
    date?: string;
    includeArchived?: boolean;
  }) => api.get<Solicitation[]>("/solicitations", { params }).then((r) => r.data),
  update: (id: number, body: UpdateSolicitationBody) =>
    api.put<Solicitation>(`/solicitations/${id}`, body).then((r) => r.data),
  remove: (id: number) => api.delete(`/solicitations/${id}`),
};

export const DayEntriesApi = {
  byDate: (date: string) =>
    api.get<DayEntry[]>("/dayentries", { params: { date } }).then((r) => r.data),
  month: (month: string) =>
    api.get<MonthSummary>("/dayentries/month", { params: { month } }).then((r) => r.data),
  upsert: (body: {
    solicitationId: number;
    workDate: string;
    adjustedMinutes: number;
    notes?: string | null;
  }) => api.put<DayEntry>("/dayentries", body).then((r) => r.data),
};

export const ProfileApi = {
  get: () => api.get<Profile>("/profile").then((r) => r.data),
  update: (body: { displayName?: string | null; dailyTargetMinutes: number }) =>
    api.put<Profile>("/profile", body).then((r) => r.data),
};

export interface Evidence {
  id: number;
  solicitationId: number;
  kind: "Link" | "File";
  value: string;
  caption: string | null;
  createdAt: string;
  url: string | null;
}

export const AdminApi = {
  listUsers: () => api.get<AdminUser[]>("/admin/users").then((r) => r.data),
  createUser: (email: string) =>
    api.post<CreatedUser>("/admin/users", { email }).then((r) => r.data),
};

export const EvidenceApi = {
  list: (solicitationId: number) =>
    api.get<Evidence[]>("/evidence", { params: { solicitationId } }).then((r) => r.data),
  addLink: (solicitationId: number, value: string, caption?: string | null) =>
    api
      .post<Evidence>("/evidence", { solicitationId, kind: "Link", value, caption })
      .then((r) => r.data),
  upload: (solicitationId: number, file: File, caption?: string | null) => {
    const fd = new FormData();
    fd.append("solicitationId", String(solicitationId));
    if (caption) fd.append("caption", caption);
    fd.append("file", file);
    return api.post<Evidence>("/evidence/upload", fd).then((r) => r.data);
  },
  remove: (id: number) => api.delete(`/evidence/${id}`),
};
