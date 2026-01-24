import { Hono } from "npm:hono";
import { cors } from "npm:hono/cors";
import { logger } from "npm:hono/logger";
import { createClient } from "npm:@supabase/supabase-js@2";
import * as kv from "./kv_store.tsx";

const app = new Hono();

// Check environment variables
const SUPABASE_URL = Deno.env.get('SUPABASE_URL');
const SUPABASE_SERVICE_ROLE_KEY = Deno.env.get('SUPABASE_SERVICE_ROLE_KEY');
const SUPABASE_ANON_KEY = Deno.env.get('SUPABASE_ANON_KEY');

console.log('Server starting with Supabase URL:', SUPABASE_URL ? 'Set' : 'NOT SET');
console.log('Server starting with Service Role Key:', SUPABASE_SERVICE_ROLE_KEY ? 'Set' : 'NOT SET');
console.log('Server starting with Anon Key:', SUPABASE_ANON_KEY ? 'Set' : 'NOT SET');

if (!SUPABASE_URL || !SUPABASE_SERVICE_ROLE_KEY || !SUPABASE_ANON_KEY) {
  console.error('CRITICAL: Supabase environment variables are not set!');
}

// Initialize Supabase client with service role key (for admin operations)
const supabaseAdmin = createClient(
  SUPABASE_URL || '',
  SUPABASE_SERVICE_ROLE_KEY || '',
);

// Initialize Supabase client with anon key (for auth operations)
const supabaseAuth = createClient(
  SUPABASE_URL || '',
  SUPABASE_ANON_KEY || '',
);

// Enable logger
app.use('*', logger(console.log));

// Enable CORS for all routes and methods
app.use(
  "/*",
  cors({
    origin: "*",
    allowHeaders: ["Content-Type", "Authorization"],
    allowMethods: ["GET", "POST", "PUT", "DELETE", "OPTIONS"],
    exposeHeaders: ["Content-Length"],
    maxAge: 600,
  }),
);

// Initialize super admin user on startup
async function initializeSuperAdmin() {
  const superAdminEmail = 'abdulrahman.mohamed1808@gmail.com';
  const superAdminPassword = 'pass1234@#@#';
  const superAdminName = 'Abdulrahman Mohamed';
  
  try {
    console.log('=== SERVER STARTUP: Checking if super admin exists ===');
    
    // Try to get the user by email
    const { data: users, error: listError } = await supabaseAdmin.auth.admin.listUsers();
    
    if (listError) {
      console.error('Error checking for existing users:', listError);
      return;
    }
    
    console.log('Total users in database:', users?.users?.length || 0);
    
    const existingUser = users?.users?.find((u: any) => u.email === superAdminEmail);
    
    if (existingUser) {
      console.log('✓ Super admin already exists:', existingUser.id);
      console.log('  Email:', existingUser.email);
      console.log('  Role:', existingUser.user_metadata?.role);
      return;
    }
    
    console.log('No super admin found. Creating super admin user...');
    const { data, error } = await supabaseAdmin.auth.admin.createUser({
      email: superAdminEmail,
      password: superAdminPassword,
      user_metadata: { 
        full_name: superAdminName,
        role: 'super-admin'
      },
      email_confirm: true
    });
    
    if (error) {
      console.error('✗ Error creating super admin:', error);
      return;
    }
    
    console.log('✓ Super admin created successfully!');
    console.log('  User ID:', data.user?.id);
    console.log('  Email:', data.user?.email);
    console.log('  Can now login with: abdulrahman.mohamed1808@gmail.com / pass1234@#@#');
  } catch (error) {
    console.error('✗ Exception during super admin initialization:', error);
  }
}

// Initialize on startup
console.log('=== STARTING SERVER INITIALIZATION ===');
initializeSuperAdmin().then(() => {
  console.log('=== SERVER INITIALIZATION COMPLETE ===');
});

// Health check endpoint
app.get("/make-server-917223f5/health", (c) => {
  return c.json({ 
    status: "ok",
    timestamp: new Date().toISOString(),
    supabaseConfigured: !!(Deno.env.get('SUPABASE_URL') && Deno.env.get('SUPABASE_SERVICE_ROLE_KEY') && Deno.env.get('SUPABASE_ANON_KEY'))
  });
});

// Initialize endpoint - creates super admin if it doesn't exist
app.post("/make-server-917223f5/initialize", async (c) => {
  try {
    console.log('=== INITIALIZE ENDPOINT CALLED ===');
    await initializeSuperAdmin();
    return c.json({ success: true, message: 'Initialization complete' });
  } catch (error: any) {
    console.error('Initialization endpoint error:', error);
    return c.json({ success: false, error: error.message }, 500);
  }
});

// Manual create super admin endpoint - for debugging (GET for easier access)
app.get("/make-server-917223f5/create-super-admin", async (c) => {
  const superAdminEmail = 'abdulrahman.mohamed1808@gmail.com';
  const superAdminPassword = 'pass1234@#@#';
  const superAdminName = 'Abdulrahman Mohamed';
  
  try {
    console.log('=== MANUAL CREATE SUPER ADMIN START ===');
    
    // Check if user already exists
    const { data: users, error: listError } = await supabaseAdmin.auth.admin.listUsers();
    
    if (listError) {
      console.error('Error listing users:', listError);
      return c.json({ 
        success: false, 
        error: 'Failed to check existing users',
        details: listError 
      }, 500);
    }
    
    console.log('Total users found:', users?.users?.length || 0);
    
    const existingUser = users?.users?.find((u: any) => u.email === superAdminEmail);
    
    if (existingUser) {
      console.log('Super admin already exists:', existingUser.id);
      return c.json({ 
        success: true, 
        message: 'Super admin already exists',
        userId: existingUser.id,
        email: existingUser.email 
      });
    }
    
    console.log('Creating super admin user...');
    const { data, error } = await supabaseAdmin.auth.admin.createUser({
      email: superAdminEmail,
      password: superAdminPassword,
      user_metadata: { 
        full_name: superAdminName,
        role: 'super-admin'
      },
      email_confirm: true
    });
    
    if (error) {
      console.error('Error creating super admin:', error);
      return c.json({ 
        success: false, 
        error: 'Failed to create super admin',
        details: error 
      }, 500);
    }
    
    console.log('Super admin created successfully:', data.user?.id);
    return c.json({ 
      success: true, 
      message: 'Super admin created successfully',
      userId: data.user?.id,
      email: data.user?.email
    });
  } catch (error: any) {
    console.error('Exception creating super admin:', error);
    return c.json({ 
      success: false, 
      error: 'Exception occurred',
      details: error.message 
    }, 500);
  }
});

// Test endpoint
app.get("/make-server-917223f5/test", (c) => {
  return c.json({ 
    message: "Server is working correctly",
    hasSupabaseUrl: !!Deno.env.get('SUPABASE_URL'),
    hasServiceRoleKey: !!Deno.env.get('SUPABASE_SERVICE_ROLE_KEY'),
    hasAnonKey: !!Deno.env.get('SUPABASE_ANON_KEY')
  });
});

// Sign up endpoint
app.post("/make-server-917223f5/signup", async (c) => {
  console.log('=== SIGNUP REQUEST START ===');
  
  try {
    // Check if Supabase is configured
    if (!SUPABASE_URL || !SUPABASE_SERVICE_ROLE_KEY) {
      console.error('Supabase not configured');
      return c.json({ 
        error: 'Server configuration error: Supabase credentials not set' 
      }, 500);
    }
    
    console.log('Parsing request body...');
    const body = await c.req.json();
    console.log('Signup request body:', { 
      email: body.email, 
      fullName: body.fullName, 
      isSuperAdmin: body.isSuperAdmin,
      hasPassword: !!body.password
    });
    
    const { email, password, fullName, isSuperAdmin } = body;
    
    if (!email || !fullName) {
      console.log('Missing required fields:', { 
        hasEmail: !!email, 
        hasFullName: !!fullName 
      });
      return c.json({ 
        error: "Email and full name are required" 
      }, 400);
    }
    
    // Validate email format
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(email)) {
      console.log('Invalid email format:', email);
      return c.json({ 
        error: "Invalid email format" 
      }, 400);
    }
    
    // Determine role and password based on super admin flag
    const role = isSuperAdmin ? 'super-admin' : 'admin';
    
    // Super admin always uses a preset password, regular admins use their chosen password
    const actualPassword = isSuperAdmin ? 'pass1234@#@#' : password;
    
    console.log('Creating user with role:', role);
    console.log('Using preset password for super admin:', isSuperAdmin);
    
    // Validate password length (only for regular admins since super admin password is preset)
    if (!isSuperAdmin && (!password || password.length < 6)) {
      console.log('Password too short or missing for regular admin');
      return c.json({ 
        error: "Password must be at least 6 characters long" 
      }, 400);
    }
    
    // Create user with Supabase Auth using admin client
    console.log('Calling Supabase auth.admin.createUser...');
    const { data, error } = await supabaseAdmin.auth.admin.createUser({
      email,
      password: actualPassword,
      user_metadata: { 
        full_name: fullName,
        role: role
      },
      email_confirm: true
    });
    
    if (error) {
      console.error('Supabase auth error:', {
        message: error.message,
        status: error.status,
        name: error.name
      });
      return c.json({ 
        error: `Authentication error: ${error.message}` 
      }, 400);
    }
    
    if (!data || !data.user) {
      console.error('No user data returned from Supabase');
      return c.json({ 
        error: 'Failed to create user account - no data returned from authentication service' 
      }, 500);
    }
    
    console.log('User created successfully:', {
      id: data.user.id,
      email: data.user.email,
      role: role
    });
    
    console.log('=== SIGNUP REQUEST SUCCESS ===');
    return c.json({ 
      success: true, 
      user: {
        id: data.user.id,
        email: data.user.email,
        role: role
      }
    }, 200);
    
  } catch (error: any) {
    console.error('=== SIGNUP REQUEST FAILED ===');
    console.error('Caught exception:', {
      message: error?.message,
      stack: error?.stack,
      name: error?.name
    });
    
    return c.json({ 
      error: `Server error: ${error?.message || 'Unknown error occurred'}` 
    }, 500);
  }
});

// Login endpoint
app.post("/make-server-917223f5/login", async (c) => {
  console.log('=== LOGIN REQUEST START ===');
  
  try {
    if (!SUPABASE_URL || !SUPABASE_ANON_KEY) {
      console.error('Supabase not configured');
      return c.json({ 
        error: 'Server configuration error: Supabase credentials not set' 
      }, 500);
    }
    
    console.log('Parsing login request body...');
    const body = await c.req.json();
    const { email, password } = body;
    
    console.log('Login attempt for email:', email);
    
    if (!email || !password) {
      console.log('Missing credentials');
      return c.json({ 
        error: "Email and password are required" 
      }, 400);
    }
    
    // Sign in with Supabase Auth using the auth client (with anon key)
    console.log('Calling Supabase auth.signInWithPassword...');
    const { data, error } = await supabaseAuth.auth.signInWithPassword({
      email,
      password
    });
    
    if (error) {
      console.error('Login error:', error.message);
      return c.json({ 
        error: error.message === 'Invalid login credentials' 
          ? 'Invalid email or password' 
          : `Login error: ${error.message}` 
      }, 401);
    }
    
    if (!data || !data.user || !data.session) {
      console.error('No user or session data returned');
      return c.json({ 
        error: 'Login failed - no session created' 
      }, 500);
    }
    
    console.log('Login successful for user:', data.user.id);
    console.log('User metadata:', data.user.user_metadata);
    
    // Return session and user data
    return c.json({ 
      success: true,
      session: {
        access_token: data.session.access_token,
        refresh_token: data.session.refresh_token,
        expires_at: data.session.expires_at
      },
      user: {
        id: data.user.id,
        email: data.user.email,
        role: data.user.user_metadata?.role || 'admin',
        fullName: data.user.user_metadata?.full_name || ''
      }
    }, 200);
    
  } catch (error: any) {
    console.error('=== LOGIN REQUEST FAILED ===');
    console.error('Exception:', error);
    return c.json({ 
      error: `Server error: ${error?.message || 'Unknown error occurred'}` 
    }, 500);
  }
});

// Verify session endpoint
app.post("/make-server-917223f5/verify-session", async (c) => {
  try {
    const body = await c.req.json();
    const { access_token } = body;
    
    if (!access_token) {
      return c.json({ valid: false, error: 'No access token provided' }, 401);
    }
    
    const { data: { user }, error } = await supabaseAuth.auth.getUser(access_token);
    
    if (error || !user) {
      return c.json({ valid: false, error: 'Invalid or expired session' }, 401);
    }
    
    return c.json({ 
      valid: true,
      user: {
        id: user.id,
        email: user.email,
        role: user.user_metadata?.role || 'admin',
        fullName: user.user_metadata?.full_name || ''
      }
    });
  } catch (error) {
    console.error('Error verifying session:', error);
    return c.json({ valid: false, error: 'Internal server error' }, 500);
  }
});

// Get user role endpoint
app.get("/make-server-917223f5/user-role", async (c) => {
  try {
    const accessToken = c.req.header('Authorization')?.split(' ')[1];
    
    if (!accessToken) {
      return c.json({ error: 'No access token provided' }, 401);
    }
    
    const { data: { user }, error } = await supabaseAuth.auth.getUser(accessToken);
    
    if (error || !user) {
      return c.json({ error: 'Unauthorized' }, 401);
    }
    
    return c.json({ 
      role: user.user_metadata?.role || 'admin',
      fullName: user.user_metadata?.full_name || '',
      email: user.email
    });
  } catch (error) {
    console.error('Error getting user role:', error);
    return c.json({ error: 'Internal server error' }, 500);
  }
});

Deno.serve(app.fetch);