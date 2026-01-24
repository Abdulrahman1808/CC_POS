import React, { useState } from 'react';
import { 
  Shield, 
  Bell, 
  Database, 
  Mail, 
  CreditCard, 
  Key,
  CheckCircle2,
  AlertCircle,
  Save,
  RefreshCw
} from 'lucide-react';
import { Button } from './Button';
import { Input } from './Input';
import { Tag } from './Tag';

export function Settings() {
  const [activeTab, setActiveTab] = useState('general');
  const [saving, setSaving] = useState(false);
  const [settings, setSettings] = useState({
    // General Settings
    platformName: 'Eagle POS',
    supportEmail: 'support@eaglepos.com',
    defaultLanguage: 'en',
    timezone: 'Africa/Cairo',
    
    // Security Settings
    sessionTimeout: '30',
    twoFactorEnabled: false,
    passwordExpiry: '90',
    
    // Email Notifications
    emailOnNewTenant: true,
    emailOnPaymentFailed: true,
    emailOnHighValue: true,
    dailyDigest: true,
    
    // Database Settings
    autoBackup: true,
    backupFrequency: 'daily',
    retentionDays: '30'
  });
  
  const handleSave = async () => {
    setSaving(true);
    // Simulate save
    await new Promise(resolve => setTimeout(resolve, 1500));
    setSaving(false);
    console.log('Settings saved:', settings);
  };
  
  const handleDatabaseBackup = () => {
    console.log('Triggering manual backup...');
    // In production, trigger Supabase backup
  };
  
  const tabs = [
    { id: 'general', label: 'General', icon: <Shield size={18} /> },
    { id: 'security', label: 'Security', icon: <Key size={18} /> },
    { id: 'notifications', label: 'Notifications', icon: <Bell size={18} /> },
    { id: 'billing', label: 'Billing', icon: <CreditCard size={18} /> },
    { id: 'database', label: 'Database', icon: <Database size={18} /> }
  ];
  
  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-foreground mb-1">Platform Settings</h1>
          <p className="text-muted-foreground">Manage your Eagle POS super-admin configuration</p>
        </div>
        <Button onClick={handleSave} disabled={saving}>
          {saving ? (
            <>
              <RefreshCw size={18} className="mr-2 animate-spin" />
              Saving...
            </>
          ) : (
            <>
              <Save size={18} className="mr-2" />
              Save Changes
            </>
          )}
        </Button>
      </div>
      
      {/* Tabs */}
      <div className="border-b border-border">
        <div className="flex gap-6 overflow-x-auto">
          {tabs.map((tab) => (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id)}
              className={`flex items-center gap-2 px-4 py-3 border-b-2 transition-colors whitespace-nowrap ${
                activeTab === tab.id
                  ? 'border-primary text-primary'
                  : 'border-transparent text-muted-foreground hover:text-foreground'
              }`}
            >
              {tab.icon}
              {tab.label}
            </button>
          ))}
        </div>
      </div>
      
      {/* Tab Content */}
      <div className="bg-card border border-border rounded-lg p-6">
        {/* General Settings */}
        {activeTab === 'general' && (
          <div className="space-y-6">
            <div>
              <h3 className="text-card-foreground mb-4">General Settings</h3>
              <div className="space-y-4">
                <Input
                  label="Platform Name"
                  value={settings.platformName}
                  onChange={(e) => setSettings({ ...settings, platformName: e.target.value })}
                />
                
                <Input
                  label="Support Email"
                  type="email"
                  value={settings.supportEmail}
                  onChange={(e) => setSettings({ ...settings, supportEmail: e.target.value })}
                />
                
                <div>
                  <label className="text-foreground mb-2 block">Default Language</label>
                  <select
                    value={settings.defaultLanguage}
                    onChange={(e) => setSettings({ ...settings, defaultLanguage: e.target.value })}
                    className="w-full bg-input border border-border rounded-lg px-4 py-2 text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
                  >
                    <option value="en">English</option>
                    <option value="ar">Arabic (العربية)</option>
                    <option value="fr">French (Français)</option>
                  </select>
                </div>
                
                <div>
                  <label className="text-foreground mb-2 block">Timezone</label>
                  <select
                    value={settings.timezone}
                    onChange={(e) => setSettings({ ...settings, timezone: e.target.value })}
                    className="w-full bg-input border border-border rounded-lg px-4 py-2 text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
                  >
                    <option value="Africa/Cairo">Africa/Cairo (GMT+2)</option>
                    <option value="Europe/London">Europe/London (GMT+0)</option>
                    <option value="America/New_York">America/New_York (GMT-5)</option>
                    <option value="Asia/Dubai">Asia/Dubai (GMT+4)</option>
                  </select>
                </div>
              </div>
            </div>
          </div>
        )}
        
        {/* Security Settings */}
        {activeTab === 'security' && (
          <div className="space-y-6">
            <div>
              <h3 className="text-card-foreground mb-4">Security & Authentication</h3>
              <div className="space-y-6">
                <div className="bg-warning/10 border border-warning/30 rounded-lg p-4 flex items-start gap-3">
                  <AlertCircle className="text-warning flex-shrink-0 mt-0.5" size={20} />
                  <div>
                    <p className="text-warning mb-1">Security Notice</p>
                    <p className="text-sm text-muted-foreground">
                      Changes to security settings will affect all tenant accounts. Proceed with caution.
                    </p>
                  </div>
                </div>
                
                <div>
                  <label className="text-foreground mb-2 block">Session Timeout (minutes)</label>
                  <input
                    type="number"
                    value={settings.sessionTimeout}
                    onChange={(e) => setSettings({ ...settings, sessionTimeout: e.target.value })}
                    className="w-full bg-input border border-border rounded-lg px-4 py-2 text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
                    min="5"
                    max="1440"
                  />
                  <p className="text-sm text-muted-foreground mt-1">
                    Users will be automatically logged out after this period of inactivity
                  </p>
                </div>
                
                <div className="flex items-center justify-between p-4 bg-accent/30 rounded-lg">
                  <div>
                    <div className="text-card-foreground mb-1">Two-Factor Authentication</div>
                    <div className="text-sm text-muted-foreground">Require 2FA for all admin accounts</div>
                  </div>
                  <label className="relative inline-flex items-center cursor-pointer">
                    <input
                      type="checkbox"
                      checked={settings.twoFactorEnabled}
                      onChange={(e) => setSettings({ ...settings, twoFactorEnabled: e.target.checked })}
                      className="sr-only peer"
                    />
                    <div className="w-11 h-6 bg-muted peer-focus:outline-none peer-focus:ring-2 peer-focus:ring-ring rounded-full peer peer-checked:after:translate-x-full rtl:peer-checked:after:-translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:start-[2px] after:bg-white after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-primary"></div>
                  </label>
                </div>
                
                <div>
                  <label className="text-foreground mb-2 block">Password Expiry (days)</label>
                  <input
                    type="number"
                    value={settings.passwordExpiry}
                    onChange={(e) => setSettings({ ...settings, passwordExpiry: e.target.value })}
                    className="w-full bg-input border border-border rounded-lg px-4 py-2 text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
                    min="30"
                    max="365"
                  />
                  <p className="text-sm text-muted-foreground mt-1">
                    Users will be prompted to change their password after this period
                  </p>
                </div>
              </div>
            </div>
          </div>
        )}
        
        {/* Notification Settings */}
        {activeTab === 'notifications' && (
          <div className="space-y-6">
            <div>
              <h3 className="text-card-foreground mb-4">Email Notifications</h3>
              <p className="text-sm text-muted-foreground mb-6">
                Configure which events trigger email notifications to super-admins
              </p>
              
              <div className="space-y-4">
                <div className="flex items-center justify-between p-4 bg-accent/30 rounded-lg">
                  <div className="flex items-center gap-3">
                    <Mail className="text-primary" size={20} />
                    <div>
                      <div className="text-card-foreground">New Tenant Registration</div>
                      <div className="text-sm text-muted-foreground">Notify when a new tenant signs up</div>
                    </div>
                  </div>
                  <label className="relative inline-flex items-center cursor-pointer">
                    <input
                      type="checkbox"
                      checked={settings.emailOnNewTenant}
                      onChange={(e) => setSettings({ ...settings, emailOnNewTenant: e.target.checked })}
                      className="sr-only peer"
                    />
                    <div className="w-11 h-6 bg-muted peer-focus:outline-none peer-focus:ring-2 peer-focus:ring-ring rounded-full peer peer-checked:after:translate-x-full rtl:peer-checked:after:-translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:start-[2px] after:bg-white after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-primary"></div>
                  </label>
                </div>
                
                <div className="flex items-center justify-between p-4 bg-accent/30 rounded-lg">
                  <div className="flex items-center gap-3">
                    <AlertCircle className="text-destructive" size={20} />
                    <div>
                      <div className="text-card-foreground">Payment Failures</div>
                      <div className="text-sm text-muted-foreground">Alert on failed subscription payments</div>
                    </div>
                  </div>
                  <label className="relative inline-flex items-center cursor-pointer">
                    <input
                      type="checkbox"
                      checked={settings.emailOnPaymentFailed}
                      onChange={(e) => setSettings({ ...settings, emailOnPaymentFailed: e.target.checked })}
                      className="sr-only peer"
                    />
                    <div className="w-11 h-6 bg-muted peer-focus:outline-none peer-focus:ring-2 peer-focus:ring-ring rounded-full peer peer-checked:after:translate-x-full rtl:peer-checked:after:-translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:start-[2px] after:bg-white after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-primary"></div>
                  </label>
                </div>
                
                <div className="flex items-center justify-between p-4 bg-accent/30 rounded-lg">
                  <div className="flex items-center gap-3">
                    <DollarSign className="text-success" size={20} />
                    <div>
                      <div className="text-card-foreground">High-Value Transactions</div>
                      <div className="text-sm text-muted-foreground">Notify for transactions over EGP 10,000</div>
                    </div>
                  </div>
                  <label className="relative inline-flex items-center cursor-pointer">
                    <input
                      type="checkbox"
                      checked={settings.emailOnHighValue}
                      onChange={(e) => setSettings({ ...settings, emailOnHighValue: e.target.checked })}
                      className="sr-only peer"
                    />
                    <div className="w-11 h-6 bg-muted peer-focus:outline-none peer-focus:ring-2 peer-focus:ring-ring rounded-full peer peer-checked:after:translate-x-full rtl:peer-checked:after:-translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:start-[2px] after:bg-white after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-primary"></div>
                  </label>
                </div>
                
                <div className="flex items-center justify-between p-4 bg-accent/30 rounded-lg">
                  <div className="flex items-center gap-3">
                    <Bell className="text-primary" size={20} />
                    <div>
                      <div className="text-card-foreground">Daily Digest</div>
                      <div className="text-sm text-muted-foreground">Receive daily summary at 9:00 AM</div>
                    </div>
                  </div>
                  <label className="relative inline-flex items-center cursor-pointer">
                    <input
                      type="checkbox"
                      checked={settings.dailyDigest}
                      onChange={(e) => setSettings({ ...settings, dailyDigest: e.target.checked })}
                      className="sr-only peer"
                    />
                    <div className="w-11 h-6 bg-muted peer-focus:outline-none peer-focus:ring-2 peer-focus:ring-ring rounded-full peer peer-checked:after:translate-x-full rtl:peer-checked:after:-translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:start-[2px] after:bg-white after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-primary"></div>
                  </label>
                </div>
              </div>
            </div>
          </div>
        )}
        
        {/* Billing Settings */}
        {activeTab === 'billing' && (
          <div className="space-y-6">
            <div>
              <h3 className="text-card-foreground mb-4">Billing & Subscription Plans</h3>
              
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
                <div className="bg-accent/30 border border-border rounded-lg p-6">
                  <div className="text-muted-foreground mb-2">Basic Plan</div>
                  <div className="text-2xl text-card-foreground mb-1">EGP 2,500</div>
                  <div className="text-sm text-muted-foreground mb-4">per month</div>
                  <div className="space-y-2 text-sm">
                    <div className="flex items-center gap-2">
                      <CheckCircle2 size={16} className="text-success" />
                      <span className="text-muted-foreground">Up to 100 transactions/day</span>
                    </div>
                    <div className="flex items-center gap-2">
                      <CheckCircle2 size={16} className="text-success" />
                      <span className="text-muted-foreground">1 location</span>
                    </div>
                    <div className="flex items-center gap-2">
                      <CheckCircle2 size={16} className="text-success" />
                      <span className="text-muted-foreground">Basic support</span>
                    </div>
                  </div>
                </div>
                
                <div className="bg-primary/10 border-2 border-primary rounded-lg p-6 relative">
                  <div className="absolute -top-3 left-1/2 -translate-x-1/2">
                    <Tag variant="success">Most Popular</Tag>
                  </div>
                  <div className="text-muted-foreground mb-2">Pro Plan</div>
                  <div className="text-2xl text-card-foreground mb-1">EGP 5,000</div>
                  <div className="text-sm text-muted-foreground mb-4">per month</div>
                  <div className="space-y-2 text-sm">
                    <div className="flex items-center gap-2">
                      <CheckCircle2 size={16} className="text-success" />
                      <span className="text-muted-foreground">Unlimited transactions</span>
                    </div>
                    <div className="flex items-center gap-2">
                      <CheckCircle2 size={16} className="text-success" />
                      <span className="text-muted-foreground">Up to 5 locations</span>
                    </div>
                    <div className="flex items-center gap-2">
                      <CheckCircle2 size={16} className="text-success" />
                      <span className="text-muted-foreground">Priority support</span>
                    </div>
                  </div>
                </div>
                
                <div className="bg-accent/30 border border-border rounded-lg p-6">
                  <div className="text-muted-foreground mb-2">Enterprise</div>
                  <div className="text-2xl text-card-foreground mb-1">EGP 25,000</div>
                  <div className="text-sm text-muted-foreground mb-4">per month</div>
                  <div className="space-y-2 text-sm">
                    <div className="flex items-center gap-2">
                      <CheckCircle2 size={16} className="text-success" />
                      <span className="text-muted-foreground">Everything in Pro</span>
                    </div>
                    <div className="flex items-center gap-2">
                      <CheckCircle2 size={16} className="text-success" />
                      <span className="text-muted-foreground">Unlimited locations</span>
                    </div>
                    <div className="flex items-center gap-2">
                      <CheckCircle2 size={16} className="text-success" />
                      <span className="text-muted-foreground">24/7 dedicated support</span>
                    </div>
                  </div>
                </div>
              </div>
              
              <div className="bg-success/10 border border-success/30 rounded-lg p-4 flex items-start gap-3">
                <CheckCircle2 className="text-success flex-shrink-0 mt-0.5" size={20} />
                <div>
                  <p className="text-success mb-1">Platform Revenue Summary</p>
                  <p className="text-sm text-muted-foreground">
                    58 Basic • 42 Pro • 15 Enterprise = <strong className="text-foreground">EGP 730,000/month</strong> in recurring revenue
                  </p>
                </div>
              </div>
            </div>
          </div>
        )}
        
        {/* Database Settings */}
        {activeTab === 'database' && (
          <div className="space-y-6">
            <div>
              <h3 className="text-card-foreground mb-4">Database Management</h3>
              
              <div className="bg-accent/30 border border-border rounded-lg p-6 mb-6">
                <div className="flex items-center justify-between mb-4">
                  <div>
                    <div className="text-card-foreground mb-1">Database Status</div>
                    <div className="text-sm text-muted-foreground">PostgreSQL on Supabase</div>
                  </div>
                  <Tag variant="success">Healthy</Tag>
                </div>
                
                <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-center">
                  <div>
                    <div className="text-2xl text-primary mb-1">1,420</div>
                    <div className="text-sm text-muted-foreground">Total Records</div>
                  </div>
                  <div>
                    <div className="text-2xl text-primary mb-1">2.4 GB</div>
                    <div className="text-sm text-muted-foreground">Database Size</div>
                  </div>
                  <div>
                    <div className="text-2xl text-primary mb-1">98.5%</div>
                    <div className="text-sm text-muted-foreground">Uptime</div>
                  </div>
                  <div>
                    <div className="text-2xl text-primary mb-1">12ms</div>
                    <div className="text-sm text-muted-foreground">Avg Query Time</div>
                  </div>
                </div>
              </div>
              
              <div className="space-y-4">
                <div className="flex items-center justify-between p-4 bg-accent/30 rounded-lg">
                  <div>
                    <div className="text-card-foreground mb-1">Automatic Backups</div>
                    <div className="text-sm text-muted-foreground">Enable scheduled database backups</div>
                  </div>
                  <label className="relative inline-flex items-center cursor-pointer">
                    <input
                      type="checkbox"
                      checked={settings.autoBackup}
                      onChange={(e) => setSettings({ ...settings, autoBackup: e.target.checked })}
                      className="sr-only peer"
                    />
                    <div className="w-11 h-6 bg-muted peer-focus:outline-none peer-focus:ring-2 peer-focus:ring-ring rounded-full peer peer-checked:after:translate-x-full rtl:peer-checked:after:-translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:start-[2px] after:bg-white after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-primary"></div>
                  </label>
                </div>
                
                {settings.autoBackup && (
                  <>
                    <div>
                      <label className="text-foreground mb-2 block">Backup Frequency</label>
                      <select
                        value={settings.backupFrequency}
                        onChange={(e) => setSettings({ ...settings, backupFrequency: e.target.value })}
                        className="w-full bg-input border border-border rounded-lg px-4 py-2 text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
                      >
                        <option value="hourly">Every Hour</option>
                        <option value="daily">Daily at 2:00 AM</option>
                        <option value="weekly">Weekly (Sunday)</option>
                      </select>
                    </div>
                    
                    <div>
                      <label className="text-foreground mb-2 block">Retention Period (days)</label>
                      <input
                        type="number"
                        value={settings.retentionDays}
                        onChange={(e) => setSettings({ ...settings, retentionDays: e.target.value })}
                        className="w-full bg-input border border-border rounded-lg px-4 py-2 text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
                        min="7"
                        max="365"
                      />
                      <p className="text-sm text-muted-foreground mt-1">
                        Backups older than this will be automatically deleted
                      </p>
                    </div>
                  </>
                )}
                
                <div className="pt-4">
                  <Button onClick={handleDatabaseBackup} variant="secondary" className="w-full">
                    <Database size={18} className="mr-2" />
                    Trigger Manual Backup Now
                  </Button>
                  <p className="text-xs text-muted-foreground text-center mt-2">
                    Last backup: Today at 2:00 AM
                  </p>
                </div>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
