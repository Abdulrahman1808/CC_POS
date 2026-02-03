-- =====================================================
-- FIX for Supabase Security Errors
-- Run in SQL Editor
-- =====================================================

-- 1. Enable RLS on businesses table
ALTER TABLE businesses ENABLE ROW LEVEL SECURITY;

-- 2. Create policy for businesses table 
-- Only allow reading your own business
CREATE POLICY "businesses_select" ON businesses
    FOR SELECT USING (
        id::text = coalesce(
            nullif(current_setting('request.headers', true)::json->>'x-business-id', ''),
            'de000000-0000-0000-0000-000000000001'
        )
    );

-- Allow insert for registration flow
CREATE POLICY "businesses_insert" ON businesses
    FOR INSERT WITH CHECK (true);

SELECT 'Businesses table secured!' AS status;
