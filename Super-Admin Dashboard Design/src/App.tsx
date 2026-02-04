import React, { useState, useEffect } from "react";
import { LoginPage } from "./components/LoginPage";
import { Sidebar } from "./components/Sidebar";
import { Header } from "./components/Header";
import { Dashboard } from "./components/Dashboard";
import { TenantManagement } from "./components/TenantManagement";
import { Analytics } from "./components/Analytics";
import { Settings } from "./components/Settings";
import {
  signIn,
  signUp,
  signOut,
  getSession,
  getUserRole,
  initializeSuperAdmin,
} from "./utils/supabase/client";

import { I18nProvider } from "./utils/I18nContext";

// IMPORTANT: Set your super admin email here
// Replace 'your-email@example.com' with your actual email address
// The first account created with this email will be the Super Admin
// All other emails will create regular Admin accounts
const SUPER_ADMIN_EMAIL = "abdulrahman.mohamed1808@gmail.com"; // Change this to your email

export default function App() {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [activePage, setActivePage] = useState("dashboard");
  const [user, setUser] = useState<{
    email: string;
    role: string;
    fullName: string;
  } | null>(null);
  const [loading, setLoading] = useState(true);
  const [authLoading, setAuthLoading] = useState(false);
  const [authError, setAuthError] = useState("");
  const [initError, setInitError] = useState<string | null>(null);

  // Check for existing session on mount and initialize super admin
  useEffect(() => {
    initializeApp();
  }, []);

  const initializeApp = async () => {
    try {
      // IMPORTANT: Clear cache if super admin email has changed
      const cachedEmail = localStorage.getItem("superadmin_email");
      if (cachedEmail && cachedEmail !== SUPER_ADMIN_EMAIL) {
        console.log("Super admin email changed, clearing cache...");
        localStorage.removeItem("superadmin_exists");
        localStorage.removeItem("superadmin_needs_confirmation");
        localStorage.removeItem("superadmin_last_check");
      }
      // Store current email for future checks
      localStorage.setItem("superadmin_email", SUPER_ADMIN_EMAIL);

      // Check if we already know the super admin exists
      const superAdminExists = localStorage.getItem("superadmin_exists");
      const needsConfirmation = localStorage.getItem("superadmin_needs_confirmation");

      if (needsConfirmation === "true") {
        console.warn("\n" + "=".repeat(80));
        console.warn("⚠️  SUPER ADMIN ACCOUNT NEEDS SETUP");
        console.warn("=".repeat(80));
        console.warn("");
        console.warn("Your super admin account exists but needs email confirmation.");
        console.warn("");
        console.warn("QUICK FIX (5 minutes):");
        console.warn("");
        console.warn("1. Go to: https://supabase.com/dashboard");
        console.warn("2. Select your project");
        console.warn("3. Click: Authentication → Providers → Email");
        console.warn("4. ENABLE the 'Email' provider (turn it ON)");
        console.warn("5. DISABLE the 'Confirm email' toggle (turn it OFF)");
        console.warn("6. Click Save");
        console.warn("");
        console.warn("7. Then go to: Authentication → Users");
        console.warn("8. Find: abdulrahman.mohamed1808@gmail.com");
        console.warn("9. Click the three dots (⋮) → 'Confirm email'");
        console.warn("");
        console.warn("10. Refresh this page and try logging in");
        console.warn("");
        console.warn("See SETUP_INSTRUCTIONS.md for detailed help.");
        console.warn("=".repeat(80) + "\n");

        setInitError(
          "Email not confirmed. Please disable email confirmation in Supabase and manually confirm the user."
        );
      } else if (superAdminExists === "true") {
        console.log("Super admin already verified (cached)");
      } else {
        // Initialize super admin if needed (frontend-only approach)
        console.log("Initializing super admin...");
        const result = await initializeSuperAdmin();

        if (result.needsManualConfirmation) {
          console.error("\n" + "=".repeat(80));
          console.error("⚠️  SUPER ADMIN ACCOUNT NEEDS SETUP");
          console.error("=".repeat(80));
          console.error("");
          console.error("Your super admin account exists but needs email confirmation.");
          console.error("");
          console.error("QUICK FIX (5 minutes):");
          console.error("");
          console.error("1. Go to: https://supabase.com/dashboard");
          console.error("2. Select your project");
          console.error("3. Click: Authentication → Providers → Email");
          console.error("4. DISABLE the 'Confirm email' toggle");
          console.error("5. Click Save");
          console.error("");
          console.error("6. Then go to: Authentication → Users");
          console.error("7. Find: abdulrahman.mohamed1808@gmail.com");
          console.error("8. Click the three dots (⋮) → 'Confirm email'");
          console.error("");
          console.error("9. Refresh this page and try logging in");
          console.error("");
          console.error("See SETUP_INSTRUCTIONS.md for detailed help.");
          console.error("=".repeat(80) + "\n");

          setInitError(
            result.error ||
            "Email not confirmed. Please disable email confirmation in Supabase and manually confirm the user."
          );
        } else if (result.rateLimited) {
          console.warn(
            "Rate limited by Supabase. Please wait 60 seconds before trying again."
          );
          setInitError(
            "Rate limited. Please wait 60 seconds and refresh the page."
          );
        } else if (result.error) {
          console.error("Super admin initialization error:", result.error);
          setInitError(result.error);
        } else if (result.created) {
          console.log("✓ Super admin account created! You can now sign in.");
        } else if (result.alreadyExists) {
          console.log("✓ Super admin account already exists.");
        } else if (result.skipped) {
          console.log("✓ Skipped initialization (checked recently).");
        }
      }
    } catch (error: any) {
      console.error("Initialization error:", error);
      setInitError(error?.message || "Failed to connect to Supabase");
    }

    // Check for existing session
    await checkSession();
  };

  const checkSession = async () => {
    try {
      const session = await getSession();
      if (session?.access_token && session.user) {
        // Get user metadata from the session
        const userData = {
          email: session.user.email || "",
          role: session.user.user_metadata?.role || "admin",
          fullName: session.user.user_metadata?.full_name || "",
        };

        setUser(userData);
        setIsAuthenticated(true);
      }
    } catch (error) {
      console.error("Error checking session:", error);
    } finally {
      setLoading(false);
    }
  };

  const handleLogin = async (
    email: string,
    password: string,
  ) => {
    setAuthLoading(true);
    setAuthError("");

    try {
      console.log("Attempting login for:", email);
      const { session, user } = await signIn(email, password);
      console.log(
        "Login response received, has session:",
        !!session,
      );
      console.log("Has access token:", !!session?.access_token);
      console.log("User data:", user);

      if (session?.access_token && user) {
        // Get user metadata directly from the returned user object
        const userData = {
          email: user.email || email,
          role: user.user_metadata?.role || "admin",
          fullName: user.user_metadata?.full_name || "",
        };

        console.log("User data extracted:", userData);

        setUser(userData);
        setIsAuthenticated(true);
        console.log("Login successful!");
      } else {
        throw new Error(
          "Login failed: No session token received",
        );
      }
    } catch (error: any) {
      console.error("Login error:", error);
      let errorMessage = "Login failed. Please try again.";

      if (error.message?.includes("Invalid login credentials")) {
        errorMessage =
          "Invalid credentials. If you just created this account, you may need to disable email confirmation in Supabase. Go to: Dashboard > Authentication > Providers > Email > Disable 'Confirm email'";
      } else if (error.message?.includes("Email not confirmed")) {
        errorMessage =
          "Email not confirmed. Please go to Supabase Dashboard > Authentication > Providers > Email and disable 'Confirm email', then manually confirm the user in the Users tab.";
      } else if (error.message) {
        errorMessage = error.message;
      }

      setAuthError(errorMessage);
    } finally {
      setAuthLoading(false);
    }
  };

  const handleSignup = async (
    email: string,
    password: string,
    fullName: string,
  ) => {
    setAuthLoading(true);
    setAuthError("");

    try {
      console.log("Starting signup process...");
      console.log("Email:", email);
      console.log("Full Name:", fullName);
      console.log("Super Admin Email:", SUPER_ADMIN_EMAIL);
      console.log(
        "Is Super Admin:",
        email.toLowerCase() === SUPER_ADMIN_EMAIL.toLowerCase(),
      );

      // Call backend signup endpoint
      const result = await signUp(
        email,
        password,
        fullName,
        SUPER_ADMIN_EMAIL,
      );
      console.log("Signup successful, result:", result);

      if (result.success) {
        // Automatically sign in after signup
        console.log(
          "Signup succeeded, attempting auto-login...",
        );

        // Add a small delay to ensure the user is fully created in the database
        await new Promise((resolve) =>
          setTimeout(resolve, 1000),
        );

        // Use the actual password that was set (super admin has preset password)
        const actualPassword = email.toLowerCase() === SUPER_ADMIN_EMAIL.toLowerCase()
          ? "pass1234@#@#"
          : password;
        await handleLogin(email, actualPassword);
      } else {
        throw new Error(
          "Signup failed: No success confirmation from server",
        );
      }
    } catch (error: any) {
      console.error("Signup error caught in App:", error);
      const errorMessage =
        error?.message ||
        "Failed to create account. Please try again.";
      console.error("Setting error message:", errorMessage);
      setAuthError(errorMessage);
      setAuthLoading(false);
    }
  };

  const handleLogout = async () => {
    try {
      await signOut();
      setIsAuthenticated(false);
      setUser(null);
      setActivePage("dashboard");
    } catch (error) {
      console.error("Logout error:", error);
    }
  };

  // Show loading state while checking session
  if (loading) {
    return (
      <div className="dark min-h-screen bg-background flex items-center justify-center">
        <div className="text-foreground">Loading...</div>
      </div>
    );
  }

  if (!isAuthenticated) {
    return (
      <I18nProvider>
        <LoginPage
          onLogin={handleLogin}
          onSignup={handleSignup}
          error={authError}
          loading={authLoading}
        />
      </I18nProvider>
    );
  }

  return (
    <I18nProvider>
      <div className="dark min-h-screen bg-background flex">
        <Sidebar
          activePage={activePage}
          onNavigate={setActivePage}
        />

        <div className="flex-1 flex flex-col min-h-screen">
          <Header
            onLogout={handleLogout}
            userName={user?.fullName || "Admin"}
            userRole={user?.role || "admin"}
          />

          <main className="flex-1 p-6 overflow-y-auto">
            {activePage === "dashboard" && <Dashboard />}
            {activePage === "tenants" && <TenantManagement />}
            {activePage === "analytics" && <Analytics />}
            {activePage === "settings" && <Settings />}
          </main>
        </div>
      </div>
    </I18nProvider>
  );
}