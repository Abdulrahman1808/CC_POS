import React from 'react';
import { Outlet } from 'react-router-dom';
import { Sidebar } from './Sidebar';
import { Header } from './Header';
import { useAuth } from '../utils/AuthContext';

export function Layout() {
    const { user, logout } = useAuth();

    return (
        <div className="dark min-h-screen bg-background flex">
            <Sidebar />
            <div className="flex-1 flex flex-col min-h-screen">
                <Header
                    onLogout={logout}
                    userName={user?.fullName || "Admin"}
                    userRole={user?.role || "admin"}
                />
                <main className="flex-1 p-6 overflow-y-auto">
                    <Outlet />
                </main>
            </div>
        </div>
    );
}
