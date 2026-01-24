import React, { useState } from 'react';
import { Search, Bell, User, LogOut, Crown } from 'lucide-react';

interface HeaderProps {
  onLogout: () => void;
  userName?: string;
  userRole?: string;
}

export function Header({ onLogout, userName = 'Admin', userRole = 'admin' }: HeaderProps) {
  const [showDropdown, setShowDropdown] = useState(false);
  const isSuperAdmin = userRole === 'super-admin';
  
  return (
    <div className="bg-card border-b border-border px-6 py-4 flex items-center justify-between">
      <div className="flex items-center gap-4 flex-1 max-w-xl">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" size={20} />
          <input
            type="text"
            placeholder="Search tenants..."
            className="w-full bg-input border border-border rounded-lg pl-10 pr-4 py-2 text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
          />
        </div>
      </div>
      
      <div className="flex items-center gap-4">
        <button className="relative p-2 text-muted-foreground hover:text-foreground transition-colors">
          <Bell size={20} />
          <span className="absolute top-1 right-1 w-2 h-2 bg-destructive rounded-full"></span>
        </button>
        
        <div className="relative">
          <button 
            onClick={() => setShowDropdown(!showDropdown)}
            className="flex items-center gap-2 p-2 rounded-lg hover:bg-accent transition-colors"
          >
            <div className="w-8 h-8 bg-primary rounded-full flex items-center justify-center text-white relative">
              <User size={16} />
              {isSuperAdmin && (
                <Crown size={12} className="absolute -top-1 -right-1 text-warning" />
              )}
            </div>
            <div className="text-left">
              <div className="text-foreground">{userName}</div>
              <div className="text-xs text-muted-foreground">
                {isSuperAdmin ? 'Super Admin' : 'Admin'}
              </div>
            </div>
          </button>
          
          {showDropdown && (
            <div className="absolute right-0 mt-2 w-48 bg-popover border border-border rounded-lg shadow-lg py-1 z-50">
              <button
                onClick={() => {
                  setShowDropdown(false);
                  onLogout();
                }}
                className="w-full flex items-center gap-2 px-4 py-2 text-popover-foreground hover:bg-accent transition-colors"
              >
                <LogOut size={16} />
                <span>Logout</span>
              </button>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
