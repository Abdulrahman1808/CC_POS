# Eagle POS Super-Admin Setup Instructions

## Current Issue: "Invalid login credentials" Error

This error means your super admin account exists but **email confirmation is enabled** in Supabase. You need to disable it.

## Quick Fix (5 minutes)

### Step 1: Disable Email Confirmation in Supabase

1. Go to [Supabase Dashboard](https://supabase.com/dashboard)
2. Select your project
3. Click **Authentication** in the left sidebar
4. Click **Providers**
5. Click **Email** provider
6. Find the **"Confirm email"** toggle
7. **Turn it OFF** (disable it)
8. Click **Save**

### Step 2: Manually Confirm Existing Users (Important!)

Even after disabling email confirmation, existing users still need to be confirmed:

1. In your Supabase Dashboard, go to **Authentication > Users**
2. Find the user `abdulrahman.mohamed@gmail.com`
3. Click the **three dots** (⋮) next to the user
4. Select **"Confirm email"** from the dropdown
5. The user status should now show as "Confirmed"

### Step 3: Test Login

1. **Refresh this page**
2. Try logging in again with:
   - Email: `abdulrahman.mohamed@gmail.com`
   - Password: `pass1234@#@#`

## Why This Happens

By default, Supabase requires email confirmation for new users. However, since this is a development/internal dashboard and you haven't configured an email service (SMTP), users can't receive confirmation emails. This causes all login attempts to fail.

## Alternative: Configure Email Service (Advanced)

If you prefer to keep email confirmation enabled, you'll need to:

1. Configure an SMTP server in Supabase
2. Go to: **Authentication > Email Templates**
3. Set up your SMTP credentials
4. Users will receive confirmation emails when they sign up

**Note:** For internal dashboards, disabling email confirmation is recommended.

## Troubleshooting

### Still getting "Invalid login credentials"?

- Make sure you clicked **Save** after disabling email confirmation
- Make sure you manually confirmed the user in the Users tab
- Try waiting 30 seconds and refreshing the page (Supabase caching)
- Clear your browser's localStorage and refresh

### Getting "Rate limited" error?

- Supabase limits authentication attempts to prevent abuse
- Wait 60 seconds and try again
- The app caches the super admin check to avoid this in the future

### Can't access Supabase Dashboard?

- Make sure you're logged in to the correct Supabase account
- Make sure your project isn't paused (free tier projects pause after inactivity)
- Check that your internet connection is working

## Support

If you continue to have issues, check the browser console for detailed error messages. The console will provide specific guidance based on the error type.
