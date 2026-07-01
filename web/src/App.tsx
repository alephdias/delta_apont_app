import { Routes, Route, Navigate } from "react-router-dom";
import { ProtectedRoute } from "./auth/ProtectedRoute";
import { AppLayout } from "./components/AppLayout";
import { LoginPage } from "./pages/LoginPage";
import { ChangePasswordPage } from "./pages/ChangePasswordPage";
import { DayViewPage } from "./pages/DayViewPage";
import { MonthClosePage } from "./pages/MonthClosePage";
import { EmpresasPage } from "./pages/EmpresasPage";
import { AplicativoPage } from "./pages/AplicativoPage";
import { SolicitacoesPage } from "./pages/SolicitacoesPage";
import { ConfiguracoesPage } from "./pages/ConfiguracoesPage";

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        path="/trocar-senha"
        element={
          <ProtectedRoute>
            <ChangePasswordPage />
          </ProtectedRoute>
        }
      />
      <Route
        element={
          <ProtectedRoute>
            <AppLayout />
          </ProtectedRoute>
        }
      >
        <Route path="/" element={<DayViewPage />} />
        <Route path="/fechamento" element={<MonthClosePage />} />
        <Route path="/solicitacoes" element={<SolicitacoesPage />} />
        <Route path="/empresas" element={<EmpresasPage />} />
        <Route path="/aplicativo" element={<AplicativoPage />} />
        <Route path="/configuracoes" element={<ConfiguracoesPage />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
