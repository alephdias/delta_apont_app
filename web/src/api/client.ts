import axios from "axios";

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? "https://localhost:7291/api",
  headers: { "Content-Type": "application/json" },
});

export interface Item {
  id: number;
  name: string;
  description?: string | null;
  createdAt: string;
}

export const ItemsApi = {
  list: () => api.get<Item[]>("/items").then((r) => r.data),
  get: (id: number) => api.get<Item>(`/items/${id}`).then((r) => r.data),
  create: (data: Pick<Item, "name" | "description">) =>
    api.post<Item>("/items", data).then((r) => r.data),
  update: (id: number, data: Pick<Item, "name" | "description">) =>
    api.put(`/items/${id}`, data),
  remove: (id: number) => api.delete(`/items/${id}`),
};
