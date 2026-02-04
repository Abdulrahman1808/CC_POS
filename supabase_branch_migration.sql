-- =====================================================
-- MULTI-BRANCH SUPABASE MIGRATION (IDEMPOTENT)
-- Run in Supabase SQL Editor
-- Safe to run multiple times
-- =====================================================

-- =====================================================
-- 1. CREATE BRANCHES TABLE
-- =====================================================
CREATE TABLE IF NOT EXISTS branches (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    business_id UUID REFERENCES businesses(id) ON DELETE CASCADE,
    name TEXT NOT NULL,
    name_ar TEXT,  -- Arabic name for RTL display
    address TEXT,
    address_ar TEXT,  -- Arabic address
    phone TEXT,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Create index for business lookups
CREATE INDEX IF NOT EXISTS idx_branches_business_id ON branches(business_id);
CREATE INDEX IF NOT EXISTS idx_branches_active ON branches(business_id, is_active);

-- =====================================================
-- 2. ADD branch_id TO EXISTING TABLES
-- =====================================================
ALTER TABLE products ADD COLUMN IF NOT EXISTS branch_id UUID REFERENCES branches(id);
ALTER TABLE transactions ADD COLUMN IF NOT EXISTS branch_id UUID REFERENCES branches(id);
ALTER TABLE staff_members ADD COLUMN IF NOT EXISTS branch_id UUID REFERENCES branches(id);

-- Create indexes for branch lookups
CREATE INDEX IF NOT EXISTS idx_products_branch ON products(branch_id);
CREATE INDEX IF NOT EXISTS idx_transactions_branch ON transactions(branch_id);
CREATE INDEX IF NOT EXISTS idx_staff_branch ON staff_members(branch_id);

-- =====================================================
-- 3. ENABLE RLS ON BRANCHES TABLE
-- =====================================================
ALTER TABLE branches ENABLE ROW LEVEL SECURITY;

-- =====================================================
-- 4. CREATE OPTIMIZED RLS POLICIES
-- Drop ALL possible policy names first for idempotency
-- =====================================================

-- Helper function to get branch ID from header with caching
CREATE OR REPLACE FUNCTION get_branch_id() RETURNS TEXT AS $$
BEGIN
    RETURN coalesce(
        nullif(current_setting('request.headers', true)::json->>'x-branch-id', ''),
        NULL
    );
END;
$$ LANGUAGE plpgsql STABLE;

-- BRANCHES policies
DROP POLICY IF EXISTS "branches_select" ON branches;
DROP POLICY IF EXISTS "branches_insert" ON branches;
CREATE POLICY "branches_select" ON branches
    FOR SELECT USING (
        business_id::text = (SELECT get_tenant_id())
    );
CREATE POLICY "branches_insert" ON branches
    FOR INSERT WITH CHECK (
        business_id::text = (SELECT get_tenant_id())
    );

-- PRODUCTS policies (drop old and new names)
DROP POLICY IF EXISTS "products_all" ON products;
DROP POLICY IF EXISTS "products_branch_isolation" ON products;
CREATE POLICY "products_branch_isolation" ON products
    FOR ALL USING (
        business_id::text = (SELECT get_tenant_id())
        AND (
            branch_id IS NULL 
            OR branch_id::text = (SELECT get_branch_id())
            OR (SELECT get_branch_id()) IS NULL
        )
    )
    WITH CHECK (true);

-- TRANSACTIONS policies (drop old and new names)
DROP POLICY IF EXISTS "transactions_all" ON transactions;
DROP POLICY IF EXISTS "transactions_branch_isolation" ON transactions;
CREATE POLICY "transactions_branch_isolation" ON transactions
    FOR ALL USING (
        business_id::text = (SELECT get_tenant_id())
        AND (
            branch_id IS NULL 
            OR branch_id::text = (SELECT get_branch_id())
            OR (SELECT get_branch_id()) IS NULL
        )
    )
    WITH CHECK (true);

-- STAFF policies (drop old and new names)
DROP POLICY IF EXISTS "staff_all" ON staff_members;
DROP POLICY IF EXISTS "staff_branch_isolation" ON staff_members;
CREATE POLICY "staff_branch_isolation" ON staff_members
    FOR ALL USING (
        business_id::text = (SELECT get_tenant_id())
        AND (
            branch_id IS NULL 
            OR branch_id::text = (SELECT get_branch_id())
            OR (SELECT get_branch_id()) IS NULL
        )
    )
    WITH CHECK (true);

-- =====================================================
-- 5. INSERT DEVELOPER BRANCH FOR TESTING
-- =====================================================
INSERT INTO branches (id, business_id, name, name_ar, address, is_active)
VALUES (
    'de000000-0000-0000-0000-000000000002',
    'de000000-0000-0000-0000-000000000001',
    'Main Branch (Dev)',
    'الفرع الرئيسي',
    '123 Developer Street',
    true
)
ON CONFLICT (id) DO NOTHING;

SELECT 'Multi-branch migration complete!' AS status;
