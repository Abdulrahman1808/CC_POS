# Supabase Setup Instructions for Eagle POS

## Quick Setup (5 minutes)

### Step 1: Verify Your Supabase Project

1. Go to [https://supabase.com/dashboard](https://supabase.com/dashboard)
2. Make sure your project `fmmvliedejizyauwpfea` is **active** (not paused)
3. If it's paused, click "Restore" to activate it

### Step 2: Disable Email Confirmation (Required!)

This is the most important step to fix the "Failed to fetch" errors.

1. In your Supabase Dashboard, navigate to:
   **Authentication** → **Providers** → **Email**

2. Find the setting **"Confirm email"** and **DISABLE** it
   - Toggle it OFF (it should be gray/disabled)
   - This allows users to sign up without needing to verify their email

3. Click **Save** at the bottom

### Step 3: Verify Your Credentials

Your credentials are already configured in `/utils/supabase/info.tsx`:
- **Project ID**: `fmmvliedejizyauwpfea`
- **Anon Key**: Already set (starts with `eyJhbGciOi...`)

### Step 4: Test the Connection

1. Refresh your application
2. Check the browser console (F12) for any error messages
3. Try to sign in with:
   - **Email**: `abdulrahman.mohamed@gmail.com`
   - **Password**: `pass1234@#@#`

---

## Troubleshooting

### Error: "Email not confirmed"

**Cause**: The super admin account was created but email confirmation is still enabled in Supabase.

**Solution** (Choose ONE option):

**Option 1: Disable email confirmation + Manually confirm the existing user** (RECOMMENDED)
1. Go to [Supabase Dashboard](https://supabase.com/dashboard) → **Authentication** → **Providers** → **Email**
2. **Disable** the "Confirm email" toggle
3. Click **Save**
4. Go to **Authentication** → **Users**
5. Find the user `abdulrahman.mohamed@gmail.com`
6. Click the **three dots (•••)** on the right
7. Select **"Send magic link"** or click on the user and manually set the `email_confirmed_at` field
8. Refresh your app and try to sign in

**Option 2: Delete and recreate the user**
1. Go to [Supabase Dashboard](https://supabase.com/dashboard) → **Authentication** → **Providers** → **Email**
2. **Disable** the "Confirm email" toggle
3. Click **Save**
4. Go to **Authentication** → **Users**
5. Find `abdulrahman.mohamed@gmail.com` and **delete** it
6. Refresh your app - it will automatically recreate the user
7. Try to sign in

### Error: "Rate limited" or "For security purposes, you can only request this after 59 seconds"

**Cause**: Supabase has rate limiting to prevent abuse. The app tried to sign in/up too many times.

**Solution**:
1. **Wait 60 seconds** - This is Supabase's security feature
2. Clear your browser cache and localStorage:
   - Open Developer Console (F12)
   - Go to Application/Storage tab
   - Clear localStorage
   - Refresh the page
3. After 60 seconds, try logging in again
4. The app now caches the super admin check, so this shouldn't happen again

### Error: "Failed to fetch" or "AuthRetryableFetchError"

**Cause**: Email confirmation is still enabled in Supabase

**Solution**:
1. Go to Supabase Dashboard → Authentication → Providers → Email
2. **Disable** "Confirm email"
3. Save and refresh your app

### Error: "Invalid login credentials"

**Cause**: The super admin account hasn't been created yet

**Solution**:
1. Click the **"Sign Up"** tab
2. Create an account with:
   - Email: `abdulrahman.mohamed@gmail.com`
   - Full Name: Your name
   - Password: Any password (the system will use `pass1234@#@#` automatically)
3. Then switch to **"Sign In"** and login

### Error: Network or connection issues

**Solutions**:
1. Check your internet connection
2. Verify the Supabase project is not paused
3. Try accessing `https://fmmvliedejizyauwpfea.supabase.co` in your browser - it should show a Supabase page

---

## Alternative: Manual Account Creation

If automatic creation doesn't work, create the account manually:

1. Go to Supabase Dashboard → **Authentication** → **Users**
2. Click **"Add user"** → **"Create new user"**
3. Fill in:
   - **Email**: `abdulrahman.mohamed@gmail.com`
   - **Password**: `pass1234@#@#`
   - **Auto Confirm User**: ✅ **ENABLE THIS** (very important!)
4. Click **"Create user"**
5. Click on the newly created user
6. Scroll to **"User Metadata"** section
7. Click **"Edit"** and add this JSON:
   ```json
   {
     "full_name": "Abdulrahman Mohamed",
     "role": "super-admin"
   }
   ```
8. Save and try logging in

---

## Need More Help?

Check the browser console (F12) for detailed error messages and share them for more specific troubleshooting.