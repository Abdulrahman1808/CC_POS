import React from 'react';
import { LayoutDashboard, Users, BarChart3, Settings } from 'lucide-react';
import { Link, useLocation } from 'react-router-dom';
import { useI18n } from '../utils/I18nContext';

export function Sidebar() {
  const { t } = useI18n();
  const location = useLocation();
  const activePage = location.pathname.substring(1) || 'dashboard';

  const navItems = [
    { id: 'dashboard', path: '/dashboard', label: t('dashboard'), icon: LayoutDashboard },
    { id: 'tenants', path: '/tenants', label: t('tenants'), icon: Users },
    { id: 'analytics', path: '/analytics', label: t('analytics'), icon: BarChart3 },
    { id: 'settings', path: '/settings', label: t('settings'), icon: Settings }
  ];

  return (
    <div className="w-64 bg-sidebar border-r border-sidebar-border h-screen flex flex-col">
      <div className="p-6 border-b border-sidebar-border">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 bg-primary rounded-lg flex items-center justify-center text-white">
            E
          </div>
          <div>
            <div className="text-sidebar-foreground">Eagle POS</div>
            <div className="text-xs text-muted-foreground">Super Admin</div>
          </div>
        </div>
      </div>

      <nav className="flex-1 p-4">
        {navItems.map((item) => {
          const Icon = item.icon;
          const isActive = activePage === item.id || (activePage === '' && item.id === 'dashboard');

          return (
            <Link
              key={item.id}
              to={item.path}
              className={`w-full flex items-center gap-3 px-4 py-3 rounded-lg mb-2 transition-colors ${isActive
                  ? 'bg-sidebar-accent text-sidebar-accent-foreground'
                  : 'text-muted-foreground hover:bg-sidebar-accent/50 hover:text-sidebar-foreground'
                }`}
            >
              <Icon size={20} />
              <span>{item.label}</span>
            </Link>
          );
        })}
      </nav>
    </div>
  );
}
