-- =====================================================
-- COMPREHENSIVE SUPABASE CLEANUP
-- Fixes duplicate policies + performance issues
-- Run in Supabase SQL Editor
-- =====================================================

-- =====================================================
-- 1. DROP ALL OLD "Tenant isolation" POLICIES
-- =====================================================
DROP POLICY IF EXISTS "Tenant isolation" ON products;
DROP POLICY IF EXISTS "Tenant isolation" ON transactions;
DROP POLICY IF EXISTS "Tenant isolation" ON transaction_items;
DROP POLICY IF EXISTS "Tenant isolation" ON staff_members;

-- =====================================================
-- 2. DROP OUR NEW POLICIES (we'll recreate optimized versions)
-- =====================================================
-- Products
DROP POLICY IF EXISTS "products_select" ON products;
DROP POLICY IF EXISTS "products_insert" ON products;
DROP POLICY IF EXISTS "products_update" ON products;
DROP POLICY IF EXISTS "products_delete" ON products;

-- Transactions
DROP POLICY IF EXISTS "transactions_select" ON transactions;
DROP POLICY IF EXISTS "transactions_insert" ON transactions;
DROP POLICY IF EXISTS "transactions_update" ON transactions;

-- Transaction Items
DROP POLICY IF EXISTS "items_select" ON transaction_items;
DROP POLICY IF EXISTS "items_insert" ON transaction_items;

-- Staff Members
DROP POLICY IF EXISTS "staff_select" ON staff_members;
DROP POLICY IF EXISTS "staff_insert" ON staff_members;

-- Businesses
DROP POLICY IF EXISTS "businesses_select" ON businesses;
DROP POLICY IF EXISTS "businesses_insert" ON businesses;

-- =====================================================
-- 3. CREATE OPTIMIZED POLICIES
-- Using (SELECT current_setting(...)) for better performance
-- =====================================================

-- Helper function to get business ID from header
CREATE OR REPLACE FUNCTION get_tenant_id() RETURNS TEXT AS $$
BEGIN
    RETURN coalesce(
        nullif(current_setting('request.headers', true)::json->>'x-business-id', ''),
        'de000000-0000-0000-0000-000000000001'
    );
END;
$$ LANGUAGE plpgsql STABLE;

-- PRODUCTS
CREATE POLICY "products_all" ON products
    FOR ALL USING (
        business_id::text = (SELECT get_tenant_id())
        OR business_id IS NULL
    )
    WITH CHECK (true);

-- TRANSACTIONS
CREATE POLICY "transactions_all" ON transactions
    FOR ALL USING (
        business_id::text = (SELECT get_tenant_id())
        OR business_id IS NULL
    )
    WITH CHECK (true);

-- TRANSACTION ITEMS
CREATE POLICY "items_all" ON transaction_items
    FOR ALL USING (
        business_id::text = (SELECT get_tenant_id())
        OR business_id IS NULL
    )
    WITH CHECK (true);

-- STAFF MEMBERS
CREATE POLICY "staff_all" ON staff_members
    FOR ALL USING (
        business_id::text = (SELECT get_tenant_id())
        OR business_id IS NULL
    )
    WITH CHECK (true);

-- BUSINESSES
CREATE POLICY "businesses_all" ON businesses
    FOR ALL USING (
        id::text = (SELECT get_tenant_id())
    )
    WITH CHECK (true);

-- =====================================================
-- 4. CLEANUP DUPLICATE INDEXES
-- =====================================================
DROP INDEX IF EXISTS kv_store_917223f5_key_idx1;
DROP INDEX IF EXISTS kv_store_917223f5_key_idx2;
DROP INDEX IF EXISTS kv_store_917223f5_key_idx3;
DROP INDEX IF EXISTS kv_store_917223f5_key_idx4;
DROP INDEX IF EXISTS kv_store_917223f5_key_idx5;
DROP INDEX IF EXISTS kv_store_917223f5_key_idx6;
DROP INDEX IF EXISTS kv_store_917223f5_key_idx7;
DROP INDEX IF EXISTS kv_store_917223f5_key_idx8;

SELECT 'Cleanup complete! Policies optimized.' AS status;
