import { createClient as createSupabaseClient } from '@supabase/supabase-js';
import { projectId, publicAnonKey } from './info';

const supabaseUrl = `https://${projectId}.supabase.co`;

// Create a singleton Supabase client for the frontend with proper configuration
export const supabase = createSupabaseClient(supabaseUrl, publicAnonKey, {
  auth: {
    persistSession: true,
    autoRefreshToken: true,
    detectSessionInUrl: false
  }
});

// Initialize super admin account (frontend-only approach)
export async function initializeSuperAdmin() {
  const superAdminEmail = 'abdulrahman.mohamed1808@gmail.com';
  const superAdminPassword = 'pass1234@#@#';
  const superAdminName = 'Abdulrahman Mohamed';
  
  // Check localStorage to avoid rate limiting
  const lastCheck = localStorage.getItem('superadmin_last_check');
  const now = Date.now();
  
  // Only check once every 5 minutes to avoid rate limiting
  if (lastCheck && (now - parseInt(lastCheck)) < 5 * 60 * 1000) {
    console.log('Skipping super admin check (checked recently)');
    return { skipped: true };
  }
  
  try {
    console.log('Checking if super admin exists...');
    
    // Try to sign in with super admin credentials
    const { data: signInData, error: signInError } = await supabase.auth.signInWithPassword({
      email: superAdminEmail,
      password: superAdminPassword
    });
    
    if (signInData?.user) {
      console.log('✓ Super admin already exists');
      // Sign out immediately since we're just checking
      await supabase.auth.signOut();
      
      // Store the check time
      localStorage.setItem('superadmin_last_check', now.toString());
      localStorage.setItem('superadmin_exists', 'true');
      
      return { alreadyExists: true };
    }
    
    // If we get a rate limit error, just skip for now
    if (signInError && signInError.message.includes('request this after')) {
      console.warn('Rate limited. Will try again later.');
      return { rateLimited: true };
    }
    
    // If we get an "Email not confirmed" error, the account exists but needs confirmation
    if (signInError && signInError.message.includes('Email not confirmed')) {
      console.error('⚠ Super admin account exists but email is not confirmed!');
      console.log('');
      console.log('CRITICAL: You must disable email confirmation in Supabase:');
      console.log('1. Go to: https://supabase.com/dashboard');
      console.log('2. Navigate to: Authentication → Providers → Email');
      console.log('3. DISABLE "Confirm email" toggle');
      console.log('4. Save changes');
      console.log('5. Then manually confirm the user in the dashboard:');
      console.log('   - Go to: Authentication → Users');
      console.log('   - Find: abdulrahman.mohamed1808@gmail.com');
      console.log('   - Click the three dots → \"Confirm email\"');
      console.log('');
      
      localStorage.setItem('superadmin_last_check', now.toString());
      localStorage.setItem('superadmin_exists', 'true');
      localStorage.setItem('superadmin_needs_confirmation', 'true');
      
      return { 
        needsManualConfirmation: true, 
        error: 'Email not confirmed. Please disable email confirmation in Supabase and manually confirm the user.'
      };
    }
    
    // If we get an "Invalid login credentials" error, the user might not exist
    if (signInError && signInError.message !== 'Invalid login credentials') {
      // This is a different error (network, config, etc.) - log it but don't block
      console.warn('Could not check super admin status:', signInError.message);
      return { error: signInError.message };
    }
    
    // Store the check time before attempting to create
    localStorage.setItem('superadmin_last_check', now.toString());
    
    // If sign in failed with invalid credentials, the user doesn't exist - create them
    console.log('Super admin does not exist, creating account...');
    console.log('NOTE: If email confirmation is enabled in Supabase, you may need to disable it.');
    console.log('Go to: Supabase Dashboard > Authentication > Providers > Email > Disable "Confirm email"');
    
    const { data: signUpData, error: signUpError } = await supabase.auth.signUp({
      email: superAdminEmail,
      password: superAdminPassword,
      options: {
        data: {
          full_name: superAdminName,
          role: 'super-admin'
        },
        emailRedirectTo: undefined // Don't redirect anywhere
      }
    });
    
    if (signUpError) {
      // Check for rate limit error
      if (signUpError.message.includes('request this after')) {
        console.warn('Rate limited during signup. Please wait 60 seconds and try again.');
        return { rateLimited: true, message: signUpError.message };
      }
      
      console.error('Error creating super admin:', signUpError.message);
      console.log('');
      console.log('TROUBLESHOOTING:');
      console.log('1. Check that your Supabase project is active and not paused');
      console.log('2. Disable email confirmation: Dashboard > Authentication > Providers > Email');
      console.log('3. Verify your internet connection');
      return { error: signUpError.message };
    }
    
    if (signUpData?.user) {
      console.log('✓ Super admin account created successfully!');
      
      localStorage.setItem('superadmin_exists', 'true');
      
      // Check if email confirmation is required
      if (signUpData.user.confirmed_at === null) {
        console.warn('⚠ Email confirmation is REQUIRED but not configured!');
        console.log('');
        console.log('IMPORTANT: To fix this, go to your Supabase Dashboard:');
        console.log('1. Navigate to: Authentication > Providers > Email');
        console.log('2. Disable "Confirm email"');
        console.log('3. Then try signing up again');
        console.log('');
        console.log('Alternatively, check your email for a confirmation link.');
      }
      
      // Sign out the newly created account
      await supabase.auth.signOut();
      
      return { created: true, needsConfirmation: signUpData.user.confirmed_at === null };
    }
    
    return { error: 'Unknown error during signup' };
  } catch (error: any) {
    console.error('Super admin creation error:', error);
    
    // Check for specific error types
    if (error?.message?.includes('Email logins are disabled')) {
      console.error('');
      console.error('═══════════════════════════════════════════════════════════════════════════════');
      console.error('⚠️  EMAIL AUTHENTICATION IS DISABLED IN SUPABASE');
      console.error('═══════════════════════════════════════════════════════════════════════════════');
      console.error('');
      console.error('REQUIRED SETUP (2 minutes):');
      console.error('');
      console.error('1. Go to: https://supabase.com/dashboard');
      console.error('2. Select your project');
      console.error('3. Click: Authentication → Providers → Email');
      console.error('4. ENABLE the "Email" provider toggle (turn it ON)');
      console.error('5. DISABLE the "Confirm email" toggle (turn it OFF)');
      console.error('6. Click "Save"');
      console.error('7. Refresh this page');
      console.error('');
      console.error('═══════════════════════════════════════════════════════════════════════════════');
      console.error('');
      
      return {
        error: 'Email authentication is disabled. Please enable it in Supabase (see console for instructions).',
        needsManualConfirmation: false,
      };
    }
    
    if (error?.message?.includes('Email not confirmed')) {
      console.log('4. User needs email confirmation');
    }
    
    return { error: error.message };
  }
}

// Helper function to get the current session
export async function getSession() {
  try {
    const { data: { session }, error } = await supabase.auth.getSession();
    if (error) {
      console.error('Error getting session:', error);
      return null;
    }
    return session;
  } catch (error) {
    console.error('Exception getting session:', error);
    return null;
  }
}

// Helper function to get user role from backend
export async function getUserRole(accessToken: string) {
  try {
    const response = await fetch(
      `https://${projectId}.supabase.co/functions/v1/make-server-917223f5/user-role`,
      {
        headers: {
          'Authorization': `Bearer ${accessToken}`
        }
      }
    );
    
    if (!response.ok) {
      throw new Error('Failed to get user role');
    }
    
    const data = await response.json();
    return data;
  } catch (error) {
    console.error('Error fetching user role:', error);
    return null;
  }
}

// Helper function to sign up
export async function signUp(email: string, password: string, fullName: string, superAdminEmail: string) {
  const isSuperAdmin = email.toLowerCase() === superAdminEmail.toLowerCase();
  
  try {
    console.log('Attempting signup with:', { email, fullName, isSuperAdmin });
    
    // For super admin, use preset password
    const actualPassword = isSuperAdmin ? 'pass1234@#@#' : password;
    const role = isSuperAdmin ? 'super-admin' : 'admin';
    
    // Use Supabase client directly for signup
    const { data, error } = await supabase.auth.signUp({
      email,
      password: actualPassword,
      options: {
        data: {
          full_name: fullName,
          role: role
        }
      }
    });
    
    if (error) {
      console.error('Supabase signup error:', error);
      throw error;
    }
    
    if (!data || !data.user) {
      throw new Error('No user data returned from signup');
    }
    
    console.log('Signup successful:', { userId: data.user.id, email: data.user.email });
    
    return {
      success: true,
      user: {
        id: data.user.id,
        email: data.user.email,
        role: role
      }
    };
  } catch (error: any) {
    console.error('Signup error details:', error);
    // Re-throw the original error message
    throw error;
  }
}

// Helper function to sign in - uses Supabase client directly
export async function signIn(email: string, password: string) {
  try {
    console.log('Attempting to sign in with email:', email);
    
    const { data, error } = await supabase.auth.signInWithPassword({
      email,
      password
    });
    
    if (error) {
      console.error('Supabase sign in error:', error);
      throw error;
    }
    
    if (!data || !data.session) {
      throw new Error('No session returned from sign in');
    }
    
    console.log('Sign in successful, session created');
    
    return {
      session: data.session,
      user: data.user
    };
  } catch (error: any) {
    console.error('Sign in error:', error);
    throw error;
  }
}

// Helper function to sign out
export async function signOut() {
  const { error } = await supabase.auth.signOut();
  if (error) {
    throw error;
  }
}