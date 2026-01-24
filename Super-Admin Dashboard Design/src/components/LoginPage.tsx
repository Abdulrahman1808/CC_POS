import React, { useState, useEffect } from 'react';
import { Eye, EyeOff, AlertCircle } from 'lucide-react';
import { Input } from './Input';
import { Button } from './Button';

interface LoginPageProps {
  onLogin: (email: string, password: string) => Promise<void>;
  onSignup: (email: string, password: string, fullName: string) => Promise<void>;
  error?: string;
  loading?: boolean;
}

export function LoginPage({ onLogin, onSignup, error, loading }: LoginPageProps) {
  const [mode, setMode] = useState<'login' | 'signup'>('login');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [fullName, setFullName] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [showSetupHelp, setShowSetupHelp] = useState(false);
  const [isEmailConfirmationError, setIsEmailConfirmationError] = useState(false);
  
  // Check if error is a fetch/network error or email confirmation error
  useEffect(() => {
    if (error && error.includes('Email not confirmed')) {
      setIsEmailConfirmationError(true);
      setShowSetupHelp(true);
    } else if (error && (error.includes('fetch') || error.includes('Failed to fetch') || error.includes('network'))) {
      setIsEmailConfirmationError(false);
      setShowSetupHelp(true);
    } else if (error && error.includes('Rate limited')) {
      setIsEmailConfirmationError(false);
      setShowSetupHelp(true);
    } else if (error && error.includes('Invalid email or password')) {
      // This could mean the account needs confirmation
      setIsEmailConfirmationError(false);
      setShowSetupHelp(true);
    } else {
      setIsEmailConfirmationError(false);
      setShowSetupHelp(false);
    }
  }, [error]);
  
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (mode === 'login') {
      await onLogin(email, password);
    } else {
      await onSignup(email, password, fullName);
    }
  };
  
  return (
    <div className="min-h-screen bg-background flex items-center justify-center p-4">
      <div className="w-full max-w-md">
        <div className="text-center mb-8">
          <div className="inline-flex items-center justify-center w-16 h-16 bg-primary rounded-lg mb-4">
            <span className="text-white text-2xl">E</span>
          </div>
          <h1 className="text-foreground mb-2">Eagle POS</h1>
          <h2 className="text-muted-foreground">Super-Admin Cockpit</h2>
        </div>
        
        <div className="bg-card border border-border rounded-lg p-8">
          {/* Tab Switcher */}
          <div className="flex gap-2 mb-6 bg-muted p-1 rounded-lg">
            <button
              type="button"
              onClick={() => setMode('login')}
              className={`flex-1 py-2 rounded-md transition-colors ${
                mode === 'login' 
                  ? 'bg-primary text-white' 
                  : 'text-muted-foreground hover:text-foreground'
              }`}
            >
              Sign In
            </button>
            <button
              type="button"
              onClick={() => setMode('signup')}
              className={`flex-1 py-2 rounded-md transition-colors ${
                mode === 'signup' 
                  ? 'bg-primary text-white' 
                  : 'text-muted-foreground hover:text-foreground'
              }`}
            >
              Sign Up
            </button>
          </div>
          
          {error && (
            <div className="mb-4 p-3 bg-destructive/20 border border-destructive/30 text-destructive rounded-lg text-sm">
              {error}
            </div>
          )}
          
          {showSetupHelp && (
            <div className="mb-4 p-4 bg-warning/20 border border-warning/30 text-warning rounded-lg text-sm space-y-2">
              <div className="flex items-start gap-2">
                <AlertCircle size={20} className="flex-shrink-0 mt-0.5" />
                <div className="flex-1">
                  <strong className="block mb-2">🔧 Setup Required</strong>
                  <p className="mb-3">Your account exists but needs email confirmation to be disabled. Follow these steps:</p>
                  <ol className="list-decimal list-inside space-y-2 ml-2">
                    <li className="mb-3">
                      <strong>Enable Email Authentication:</strong>
                      <ul className="list-disc list-inside ml-4 mt-1 text-xs opacity-90">
                        <li>Go to: <strong>Authentication → Providers → Email</strong></li>
                        <li>ENABLE the <strong>"Email"</strong> provider toggle</li>
                        <li>DISABLE the <strong>"Confirm email"</strong> toggle</li>
                        <li>Click <strong>Save</strong></li>
                      </ul>
                    </li>
                    <li className="mb-3">
                      <strong>Manually Confirm User:</strong>
                      <ul className="list-disc list-inside ml-4 mt-1 text-xs opacity-90">
                        <li>Go to: <strong>Authentication → Users</strong></li>
                        <li>Find: <code>abdulrahman.mohamed1808@gmail.com</code></li>
                        <li>Click the three dots (⋮) → <strong>"Confirm email"</strong></li>
                      </ul>
                    </li>
                    <li className="mt-2">
                      <strong>Refresh this page</strong> and try logging in again
                    </li>
                  </ol>
                  <p className="mt-3 text-xs opacity-80">⏱ This should take about 2-3 minutes</p>
                </div>
              </div>
            </div>
          )}
          
          <form onSubmit={handleSubmit} className="space-y-6">
            {mode === 'signup' && (
              <Input
                label="Full Name"
                type="text"
                placeholder="John Smith"
                value={fullName}
                onChange={(e) => setFullName(e.target.value)}
                required
              />
            )}
            
            <Input
              label="Email Address"
              type="email"
              placeholder="admin@eaglepos.com"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
            />
            
            <div>
              <div className="flex flex-col gap-2">
                <label className="text-foreground">Password</label>
                <div className="relative">
                  <input
                    type={showPassword ? 'text' : 'password'}
                    placeholder="Enter your password"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    required
                    className="w-full bg-input border border-border text-foreground px-4 py-2 rounded-lg focus:outline-none focus:ring-2 focus:ring-ring pr-10"
                  />
                  <button
                    type="button"
                    onClick={() => setShowPassword(!showPassword)}
                    className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground transition-colors"
                  >
                    {showPassword ? <EyeOff size={20} /> : <Eye size={20} />}
                  </button>
                </div>
              </div>
            </div>
            
            <Button type="submit" className="w-full" disabled={loading}>
              {loading ? 'Processing...' : mode === 'login' ? 'Sign In' : 'Sign Up'}
            </Button>
            
            {mode === 'login' && (
              <div className="text-center">
                <a href="#" className="text-primary hover:underline">
                  Forgot Password?
                </a>
              </div>
            )}
          </form>
        </div>
      </div>
    </div>
  );
}