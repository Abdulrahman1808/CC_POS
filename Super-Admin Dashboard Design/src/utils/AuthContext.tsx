import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import {
    signIn,
    signUp as supabaseSignUp,
    signOut as supabaseSignOut,
    getSession,
    initializeSuperAdmin,
} from './supabase/client';

interface User {
    email: string;
    role: string;
    fullName: string;
}

interface AuthContextType {
    user: User | null;
    isAuthenticated: boolean;
    loading: boolean;
    authLoading: boolean;
    authError: string;
    initError: string | null;
    login: (email: string, password: string) => Promise<void>;
    signup: (email: string, password: string, fullName: string) => Promise<void>;
    logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

const SUPER_ADMIN_EMAIL = "abdulrahman.mohamed1808@gmail.com";

export function AuthProvider({ children }: { children: ReactNode }) {
    const [user, setUser] = useState<User | null>(null);
    const [isAuthenticated, setIsAuthenticated] = useState(false);
    const [loading, setLoading] = useState(true);
    const [authLoading, setAuthLoading] = useState(false);
    const [authError, setAuthError] = useState("");
    const [initError, setInitError] = useState<string | null>(null);

    useEffect(() => {
        const init = async () => {
            try {
                const cachedEmail = localStorage.getItem("superadmin_email");
                if (cachedEmail && cachedEmail !== SUPER_ADMIN_EMAIL) {
                    localStorage.removeItem("superadmin_exists");
                    localStorage.removeItem("superadmin_needs_confirmation");
                    localStorage.removeItem("superadmin_last_check");
                }
                localStorage.setItem("superadmin_email", SUPER_ADMIN_EMAIL);

                const superAdminExists = localStorage.getItem("superadmin_exists");
                const needsConfirmation = localStorage.getItem("superadmin_needs_confirmation");

                if (needsConfirmation === "true") {
                    setInitError("Email not confirmed. Please disable email confirmation in Supabase and manually confirm the user.");
                } else if (superAdminExists !== "true") {
                    const result = await initializeSuperAdmin();
                    if (result.needsManualConfirmation) {
                        setInitError(result.error || "Email not confirmed. Please disable email confirmation in Supabase and manually confirm the user.");
                    } else if (result.rateLimited) {
                        setInitError("Rate limited. Please wait 60 seconds and refresh the page.");
                    } else if (result.error) {
                        setInitError(result.error);
                    }
                }
            } catch (error: any) {
                setInitError(error?.message || "Failed to connect to Supabase");
            }

            await checkSession();
        };

        init();
    }, []);

    const checkSession = async () => {
        try {
            const session = await getSession();
            if (session?.access_token && session.user) {
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

    const login = async (email: string, password: string) => {
        setAuthLoading(true);
        setAuthError("");
        try {
            const { session, user: supabaseUser } = await signIn(email, password);
            if (session?.access_token && supabaseUser) {
                const userData = {
                    email: supabaseUser.email || email,
                    role: supabaseUser.user_metadata?.role || "admin",
                    fullName: supabaseUser.user_metadata?.full_name || "",
                };
                setUser(userData);
                setIsAuthenticated(true);
            } else {
                throw new Error("Login failed: No session token received");
            }
        } catch (error: any) {
            console.error("Login error:", error);
            let errorMessage = error.message || "Login failed. Please try again.";
            if (error.message?.includes("Invalid login credentials")) {
                errorMessage = "Invalid credentials. If you just created this account, you may need to disable email confirmation in Supabase.";
            } else if (error.message?.includes("Email not confirmed")) {
                errorMessage = "Email not confirmed. Please confirm the user in Supabase.";
            }
            setAuthError(errorMessage);
            throw error;
        } finally {
            setAuthLoading(false);
        }
    };

    const signup = async (email: string, password: string, fullName: string) => {
        setAuthLoading(true);
        setAuthError("");
        try {
            const result = await supabaseSignUp(email, password, fullName, SUPER_ADMIN_EMAIL);
            if (result.success) {
                await new Promise((resolve) => setTimeout(resolve, 1000));
                const actualPassword = email.toLowerCase() === SUPER_ADMIN_EMAIL.toLowerCase() ? "pass1234@#@#" : password;
                await login(email, actualPassword);
            } else {
                throw new Error("Signup failed: No success confirmation from server");
            }
        } catch (error: any) {
            setAuthError(error?.message || "Failed to create account. Please try again.");
            throw error;
        } finally {
            setAuthLoading(false);
        }
    };

    const logout = async () => {
        try {
            await supabaseSignOut();
            setIsAuthenticated(false);
            setUser(null);
        } catch (error) {
            console.error("Logout error:", error);
        }
    };

    return (
        <AuthContext.Provider value={{ user, isAuthenticated, loading, authLoading, authError, initError, login, signup, logout }}>
            {children}
        </AuthContext.Provider>
    );
}

export function useAuth() {
    const context = useContext(AuthContext);
    if (context === undefined) {
        throw new Error('useAuth must be used within an AuthProvider');
    }
    return context;
}
