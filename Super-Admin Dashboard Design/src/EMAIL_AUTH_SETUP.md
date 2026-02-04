# Email Authentication Setup Guide

## Error: "Email logins are disabled"

This error means that email authentication is currently disabled in your Supabase project. Here's how to fix it:

---

## Quick Fix (2 minutes)

### Step 1: Enable Email Provider

1. Go to [Supabase Dashboard](https://supabase.com/dashboard)
2. Select your **Eagle POS** project
3. Navigate to: **Authentication** → **Providers** → **Email**
4. **ENABLE** the "Email" provider toggle (turn it **ON**)
5. **DISABLE** the "Confirm email" toggle (turn it **OFF**)
6. Click **"Save"**

### Step 2: Confirm the Super Admin User (if already created)

1. Go to: **Authentication** → **Users**
2. Look for: `abdulrahman.mohamed1808@gmail.com`
3. Click the **three dots (⋮)** on the right
4. Select **"Confirm email"**

### Step 3: Test Login

1. **Refresh** this page
2. Try logging in with:
   - **Email**: `abdulrahman.mohamed1808@gmail.com`
   - **Password**: `pass1234@#@#`

---

## Troubleshooting

### If you still can't login:

1. **Clear your browser cache** and refresh
2. **Delete the user** in Supabase (Authentication → Users → Delete)
3. **Refresh this page** - it will automatically recreate the super admin
4. Try logging in again

### If you see "Invalid credentials":

- The user might not exist yet. Try clicking **"Sign Up"** first, then use **"Sign In"**

### Still having issues?

Check the browser console (press **F12**) for detailed error messages and setup instructions.

---

## Important Notes

- **Email confirmation MUST be disabled** for development/testing
- The super admin account uses a fixed password: `pass1234@#@#`
- All other accounts will be regular admins (not super admins)
- You can change these settings in `/App.tsx` (search for `SUPER_ADMIN_EMAIL`)
