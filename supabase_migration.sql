-- =====================================================
-- POS System - Supabase Schema Migration (v2)
-- Compatible with existing tables using BIGINT IDs
-- Run this ENTIRE script in Supabase SQL Editor
-- =====================================================

-- 1. CREATE BUSINESSES TABLE (for tenant management)
CREATE TABLE IF NOT EXISTS businesses (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name TEXT NOT NULL,
    email TEXT UNIQUE,
    license_key TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    settings JSONB DEFAULT '{}'
);

-- 2. ADD business_id COLUMN TO EXISTING PRODUCTS TABLE
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'products' AND column_name = 'business_id'
    ) THEN
        ALTER TABLE products ADD COLUMN business_id UUID;
    END IF;
END $$;

-- 3. CREATE STAFF MEMBERS TABLE (if not exists)
CREATE TABLE IF NOT EXISTS staff_members (
    id BIGSERIAL PRIMARY KEY,
    name TEXT NOT NULL,
    email TEXT,
    phone TEXT,
    role TEXT DEFAULT 'Cashier',
    pin_hash TEXT,
    is_active BOOLEAN DEFAULT true,
    business_id UUID,
    last_updated_by TEXT DEFAULT 'Desktop',
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- 4. CREATE OR UPDATE TRANSACTIONS TABLE
CREATE TABLE IF NOT EXISTS transactions (
    id BIGSERIAL PRIMARY KEY,
    transaction_number TEXT NOT NULL,
    sub_total DECIMAL(10,2) NOT NULL DEFAULT 0,
    tax_rate DECIMAL(5,4) DEFAULT 0.14,
    tax_amount DECIMAL(10,2) NOT NULL DEFAULT 0,
    total DECIMAL(10,2) NOT NULL DEFAULT 0,
    payment_method TEXT DEFAULT 'Cash',
    payment_reference TEXT,
    customer_id BIGINT,
    customer_name TEXT,
    customer_phone TEXT,
    is_synced BOOLEAN DEFAULT false,
    business_id UUID,
    staff_member_id BIGINT,
    last_updated_by TEXT DEFAULT 'Desktop',
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- 5. CREATE TRANSACTION ITEMS TABLE
CREATE TABLE IF NOT EXISTS transaction_items (
    id BIGSERIAL PRIMARY KEY,
    transaction_id BIGINT REFERENCES transactions(id) ON DELETE CASCADE,
    product_id BIGINT,
    product_name TEXT NOT NULL,
    quantity INTEGER NOT NULL DEFAULT 1,
    unit_price DECIMAL(10,2) NOT NULL DEFAULT 0,
    tax_rate DECIMAL(5,4) DEFAULT 0.14,
    line_total DECIMAL(10,2) NOT NULL DEFAULT 0,
    business_id UUID,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- 6. ADD MISSING COLUMNS TO EXISTING TABLES
DO $$ 
BEGIN
    -- Add business_id to transactions if missing
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'transactions' AND column_name = 'business_id'
    ) THEN
        ALTER TABLE transactions ADD COLUMN business_id UUID;
    END IF;
    
    -- Add business_id to transaction_items if missing
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'transaction_items' AND column_name = 'business_id'
    ) THEN
        ALTER TABLE transaction_items ADD COLUMN business_id UUID;
    END IF;
    
    -- Add business_id to staff_members if missing
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'staff_members' AND column_name = 'business_id'
    ) THEN
        ALTER TABLE staff_members ADD COLUMN business_id UUID;
    END IF;
END $$;

-- 7. CREATE INDEXES FOR PERFORMANCE
CREATE INDEX IF NOT EXISTS idx_products_business ON products(business_id);
CREATE INDEX IF NOT EXISTS idx_transactions_business ON transactions(business_id);
CREATE INDEX IF NOT EXISTS idx_transaction_items_business ON transaction_items(business_id);
CREATE INDEX IF NOT EXISTS idx_staff_members_business ON staff_members(business_id);

-- =====================================================
-- 8. ENABLE ROW LEVEL SECURITY
-- =====================================================
ALTER TABLE products ENABLE ROW LEVEL SECURITY;
ALTER TABLE transactions ENABLE ROW LEVEL SECURITY;
ALTER TABLE transaction_items ENABLE ROW LEVEL SECURITY;
ALTER TABLE staff_members ENABLE ROW LEVEL SECURITY;

-- =====================================================
-- 9. DROP EXISTING POLICIES (if any) AND CREATE NEW ONES
-- =====================================================

-- Products
DROP POLICY IF EXISTS "tenant_isolation_products" ON products;
CREATE POLICY "tenant_isolation_products" ON products
    FOR ALL USING (
        business_id::text = coalesce(
            current_setting('request.headers', true)::json->>'x-business-id',
            '00000000-0000-0000-0000-000000000000'
        )
    );

-- Transactions
DROP POLICY IF EXISTS "tenant_isolation_transactions" ON transactions;
CREATE POLICY "tenant_isolation_transactions" ON transactions
    FOR ALL USING (
        business_id::text = coalesce(
            current_setting('request.headers', true)::json->>'x-business-id',
            '00000000-0000-0000-0000-000000000000'
        )
    );

-- Transaction items
DROP POLICY IF EXISTS "tenant_isolation_items" ON transaction_items;
CREATE POLICY "tenant_isolation_items" ON transaction_items
    FOR ALL USING (
        business_id::text = coalesce(
            current_setting('request.headers', true)::json->>'x-business-id',
            '00000000-0000-0000-0000-000000000000'
        )
    );

-- Staff members
DROP POLICY IF EXISTS "tenant_isolation_staff" ON staff_members;
CREATE POLICY "tenant_isolation_staff" ON staff_members
    FOR ALL USING (
        business_id::text = coalesce(
            current_setting('request.headers', true)::json->>'x-business-id',
            '00000000-0000-0000-0000-000000000000'
        )
    );

-- =====================================================
-- 10. INSERT TEST BUSINESS (Developer Mode)
-- =====================================================
INSERT INTO businesses (id, name, email, license_key)
VALUES (
    'de000000-0000-0000-0000-000000000001'::uuid,
    'Developer Test Business',
    'dev@test.local',
    'DevSecret2026'
) ON CONFLICT (id) DO NOTHING;

-- Done! Schema updated and RLS enabled.
SELECT 'Migration complete!' AS status;
