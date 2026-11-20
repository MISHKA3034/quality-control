import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { CssBaseline, ThemeProvider } from '@mui/material';
import { AppLayout } from './components/Layout/AppLayout';
import { ProtectedRoute } from './components/ProtectedRoute';
import { AuthProvider } from './contexts/AuthContext';
import { NotificationProvider } from './contexts/NotificationContext';
import { AuditPage } from './pages/AuditPage';
import { BatchesPage } from './pages/BatchesPage';
import { CorrectiveActionDetailPage } from './pages/CorrectiveActionDetailPage';
import { CorrectiveActionsPage } from './pages/CorrectiveActionsPage';
import { DashboardPage } from './pages/DashboardPage';
import { DefectDetailPage } from './pages/DefectDetailPage';
import { DefectsPage } from './pages/DefectsPage';
import { ForbiddenPage, NotFoundPage } from './pages/ErrorPages';
import { ForgotPasswordPage } from './pages/ForgotPasswordPage';
import { InspectionCreatePage } from './pages/InspectionCreatePage';
import { InspectionDetailPage } from './pages/InspectionDetailPage';
import { InspectionsPage } from './pages/InspectionsPage';
import { LoginPage } from './pages/LoginPage';
import { ProductsPage } from './pages/ProductsPage';
import { ReportsPage } from './pages/ReportsPage';
import { ResetPasswordPage } from './pages/ResetPasswordPage';
import { SettingsPage } from './pages/SettingsPage';
import { UsersPage } from './pages/UsersPage';
import { theme } from './theme';

export default function App() {
  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />

      <BrowserRouter>
        {/* NotificationProvider sits outside AuthProvider so a session-expiry
            message can still be shown while auth state is being torn down. */}
        <NotificationProvider>
          <AuthProvider>
            <Routes>
              <Route path="/login" element={<LoginPage />} />
              <Route path="/forgot-password" element={<ForgotPasswordPage />} />
              <Route path="/reset-password" element={<ResetPasswordPage />} />

              <Route element={<ProtectedRoute />}>
                <Route element={<AppLayout />}>
                  <Route index element={<DashboardPage />} />
                  <Route path="products" element={<ProductsPage />} />
                  <Route path="batches" element={<BatchesPage />} />

                  <Route path="inspections" element={<InspectionsPage />} />
                  {/* Before the :id route, or "new" would be parsed as an id. */}
                  <Route path="inspections/new" element={<InspectionCreatePage />} />
                  <Route path="inspections/:id" element={<InspectionDetailPage />} />

                  <Route path="defects" element={<DefectsPage />} />
                  <Route path="defects/:id" element={<DefectDetailPage />} />

                  <Route path="corrective-actions" element={<CorrectiveActionsPage />} />
                  <Route path="corrective-actions/:id" element={<CorrectiveActionDetailPage />} />

                  <Route path="reports" element={<ReportsPage />} />
                  <Route path="settings" element={<SettingsPage />} />
                  <Route path="403" element={<ForbiddenPage />} />

                  <Route element={<ProtectedRoute roles={['Administrator', 'ProductionManager']} />}>
                    <Route path="audit" element={<AuditPage />} />
                  </Route>

                  <Route element={<ProtectedRoute roles={['Administrator']} />}>
                    <Route path="users" element={<UsersPage />} />
                  </Route>

                  <Route path="404" element={<NotFoundPage />} />
                  <Route path="*" element={<Navigate to="/404" replace />} />
                </Route>
              </Route>
            </Routes>
          </AuthProvider>
        </NotificationProvider>
      </BrowserRouter>
    </ThemeProvider>
  );
}
