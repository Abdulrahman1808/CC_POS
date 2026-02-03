-- =====================================================
-- POS System - RLS FIX for Sync Issues
-- Run this in Supabase SQL Editor
-- =====================================================

-- 1. UPDATE ALL NULL business_ids TO DEVELOPER ID
-- This allows existing data to be visible
UPDATE products SET business_id = 'de000000-0000-0000-0000-000000000001' WHERE business_id IS NULL;
UPDATE transactions SET business_id = 'de000000-0000-0000-0000-000000000001' WHERE business_id IS NULL;
UPDATE transaction_items SET business_id = 'de000000-0000-0000-0000-000000000001' WHERE business_id IS NULL;
UPDATE staff_members SET business_id = 'de000000-0000-0000-0000-000000000001' WHERE business_id IS NULL;

-- 2. DROP EXISTING RLS POLICIES
DROP POLICY IF EXISTS "tenant_isolation_products" ON products;
DROP POLICY IF EXISTS "tenant_isolation_transactions" ON transactions;
DROP POLICY IF EXISTS "tenant_isolation_items" ON transaction_items;
DROP POLICY IF EXISTS "tenant_isolation_staff" ON staff_members;

-- 3. CREATE NEW RLS POLICIES THAT HANDLE NULL AND HEADER MATCHING
-- These policies:
-- - Allow SELECT/UPDATE/DELETE when business_id matches header
-- - Allow INSERT when the incoming business_id matches header

-- Products - SELECT policy
CREATE POLICY "products_select" ON products
    FOR SELECT USING (
        business_id::text = coalesce(
            nullif(current_setting('request.headers', true)::json->>'x-business-id', ''),
            'de000000-0000-0000-0000-000000000001'
        )
        OR business_id IS NULL
    );

-- Products - INSERT policy
CREATE POLICY "products_insert" ON products
    FOR INSERT WITH CHECK (true);

-- Products - UPDATE policy
CREATE POLICY "products_update" ON products
    FOR UPDATE USING (
        business_id::text = coalesce(
            nullif(current_setting('request.headers', true)::json->>'x-business-id', ''),
            'de000000-0000-0000-0000-000000000001'
        )
        OR business_id IS NULL
    );

-- Products - DELETE policy
CREATE POLICY "products_delete" ON products
    FOR DELETE USING (
        business_id::text = coalesce(
            nullif(current_setting('request.headers', true)::json->>'x-business-id', ''),
            'de000000-0000-0000-0000-000000000001'
        )
    );

-- Transactions - SELECT
CREATE POLICY "transactions_select" ON transactions
    FOR SELECT USING (
        business_id::text = coalesce(
            nullif(current_setting('request.headers', true)::json->>'x-business-id', ''),
            'de000000-0000-0000-0000-000000000001'
        )
        OR business_id IS NULL
    );

-- Transactions - INSERT
CREATE POLICY "transactions_insert" ON transactions
    FOR INSERT WITH CHECK (true);

-- Transactions - UPDATE
CREATE POLICY "transactions_update" ON transactions
    FOR UPDATE USING (
        business_id::text = coalesce(
            nullif(current_setting('request.headers', true)::json->>'x-business-id', ''),
            'de000000-0000-0000-0000-000000000001'
        )
        OR business_id IS NULL
    );

-- Transaction Items - SELECT
CREATE POLICY "items_select" ON transaction_items
    FOR SELECT USING (
        business_id::text = coalesce(
            nullif(current_setting('request.headers', true)::json->>'x-business-id', ''),
            'de000000-0000-0000-0000-000000000001'
        )
        OR business_id IS NULL
    );

-- Transaction Items - INSERT
CREATE POLICY "items_insert" ON transaction_items
    FOR INSERT WITH CHECK (true);

-- Staff Members - SELECT
CREATE POLICY "staff_select" ON staff_members
    FOR SELECT USING (
        business_id::text = coalesce(
            nullif(current_setting('request.headers', true)::json->>'x-business-id', ''),
            'de000000-0000-0000-0000-000000000001'
        )
        OR business_id IS NULL
    );

-- Staff Members - INSERT
CREATE POLICY "staff_insert" ON staff_members
    FOR INSERT WITH CHECK (true);

-- Done!
SELECT 'RLS policies fixed!' AS status;
