import React from "react";
import { Routes, Route, Navigate } from "react-router-dom";
import { LoginPage } from "./components/LoginPage";
import { Dashboard } from "./components/Dashboard";
import { TenantManagement } from "./components/TenantManagement";
import { Analytics } from "./components/Analytics";
import { Settings } from "./components/Settings";
import { Layout } from "./components/Layout";
import { AuthProvider, useAuth } from "./utils/AuthContext";
import { ProtectedRoute } from "./utils/ProtectedRoute";
import { I18nProvider } from "./utils/I18nContext";

function AppRoutes() {
  const { login, signup, authError, authLoading } = useAuth();

  return (
    <Routes>
      <Route path="/login" element={
        <LoginPage
          onLogin={login}
          onSignup={signup}
          error={authError}
          loading={authLoading}
        />
      } />

      <Route element={
        <ProtectedRoute>
          <Layout />
        </ProtectedRoute>
      }>
        <Route path="/dashboard" element={<Dashboard />} />
        <Route path="/tenants" element={<TenantManagement />} />
        <Route path="/analytics" element={<Analytics />} />
        <Route path="/settings" element={<Settings />} />
        {/* Redirect root to dashboard */}
        <Route path="/" element={<Navigate to="/dashboard" replace />} />
      </Route>

      {/* Catch all redirect to dashboard */}
      <Route path="*" element={<Navigate to="/dashboard" replace />} />
    </Routes>
  );
}

export default function App() {
  return (
    <I18nProvider>
      <AuthProvider>
        <AppRoutes />
      </AuthProvider>
    </I18nProvider>
  );
}