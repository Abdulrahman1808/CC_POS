-- =====================================================
-- Supabase Schema for POS System Web Dashboard
-- MIGRATION-SAFE: Handles existing tables
-- =====================================================

-- =====================================================
-- STEP 1: Create tables if they don't exist
-- =====================================================

CREATE TABLE IF NOT EXISTS business_profiles (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  owner_id UUID REFERENCES auth.users(id) ON DELETE CASCADE,
  machine_id TEXT UNIQUE NOT NULL,
  business_name TEXT NOT NULL DEFAULT 'My Business',
  plan_name TEXT DEFAULT 'Basic',
  max_employees INT DEFAULT 3,
  cloud_sync_enabled BOOLEAN DEFAULT true,
  created_at TIMESTAMPTZ DEFAULT NOW(),
  updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS products (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  name TEXT NOT NULL,
  description TEXT,
  price DECIMAL(18,2) NOT NULL,
  barcode TEXT,
  sku TEXT,
  category TEXT,
  stock_quantity INT DEFAULT 0,
  image_path TEXT,
  is_active BOOLEAN DEFAULT true,
  is_deleted BOOLEAN DEFAULT false,
  created_at TIMESTAMPTZ DEFAULT NOW(),
  updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS transactions (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  transaction_number TEXT UNIQUE NOT NULL,
  sub_total DECIMAL(18,2) NOT NULL,
  tax_rate DECIMAL(5,4) DEFAULT 0.14,
  tax_amount DECIMAL(18,2) NOT NULL,
  total DECIMAL(18,2) NOT NULL,
  payment_method TEXT DEFAULT 'Cash',
  customer_id UUID,
  customer_name TEXT,
  created_at TIMESTAMPTZ DEFAULT NOW(),
  is_synced BOOLEAN DEFAULT true
);

CREATE TABLE IF NOT EXISTS transaction_items (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  transaction_id UUID REFERENCES transactions(id) ON DELETE CASCADE,
  product_id UUID,
  product_name TEXT NOT NULL,
  unit_price DECIMAL(18,2) NOT NULL,
  quantity INT NOT NULL,
  created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS staff_members (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  name TEXT NOT NULL,
  pin TEXT NOT NULL,
  email TEXT,
  level INT DEFAULT 0,
  is_active BOOLEAN DEFAULT true,
  can_delete_transactions BOOLEAN DEFAULT false,
  can_change_prices BOOLEAN DEFAULT false,
  can_view_reports BOOLEAN DEFAULT false,
  can_manage_staff BOOLEAN DEFAULT false,
  can_void_items BOOLEAN DEFAULT false,
  can_apply_discounts BOOLEAN DEFAULT false,
  can_access_settings BOOLEAN DEFAULT false,
  can_reconcile_cash_drawer BOOLEAN DEFAULT false,
  last_login_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ DEFAULT NOW(),
  updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- =====================================================
-- STEP 2: Add missing columns to existing tables
-- =====================================================

-- Add business_id to products
ALTER TABLE products ADD COLUMN IF NOT EXISTS business_id UUID REFERENCES business_profiles(id) ON DELETE CASCADE;
ALTER TABLE products ADD COLUMN IF NOT EXISTS last_updated_by TEXT DEFAULT 'Desktop';

-- Add business_id to transactions
ALTER TABLE transactions ADD COLUMN IF NOT EXISTS business_id UUID REFERENCES business_profiles(id) ON DELETE CASCADE;
ALTER TABLE transactions ADD COLUMN IF NOT EXISTS staff_member_id UUID;
ALTER TABLE transactions ADD COLUMN IF NOT EXISTS last_updated_by TEXT DEFAULT 'Desktop';

-- Add business_id to staff_members
ALTER TABLE staff_members ADD COLUMN IF NOT EXISTS business_id UUID REFERENCES business_profiles(id) ON DELETE CASCADE;

-- =====================================================
-- STEP 3: Enable RLS
-- =====================================================

ALTER TABLE business_profiles ENABLE ROW LEVEL SECURITY;
ALTER TABLE products ENABLE ROW LEVEL SECURITY;
ALTER TABLE transactions ENABLE ROW LEVEL SECURITY;
ALTER TABLE transaction_items ENABLE ROW LEVEL SECURITY;
ALTER TABLE staff_members ENABLE ROW LEVEL SECURITY;

-- =====================================================
-- STEP 4: Drop old policies (if any) and create new ones
-- =====================================================

DROP POLICY IF EXISTS "Service role full access" ON business_profiles;
DROP POLICY IF EXISTS "Service role full access" ON products;
DROP POLICY IF EXISTS "Service role full access" ON transactions;
DROP POLICY IF EXISTS "Service role full access" ON transaction_items;
DROP POLICY IF EXISTS "Service role full access" ON staff_members;
DROP POLICY IF EXISTS "Owners view own profile" ON business_profiles;
DROP POLICY IF EXISTS "Owners update own profile" ON business_profiles;
DROP POLICY IF EXISTS "Web users view products" ON products;
DROP POLICY IF EXISTS "Web users update products" ON products;
DROP POLICY IF EXISTS "Web users view transactions" ON transactions;
DROP POLICY IF EXISTS "Web users manage staff" ON staff_members;

-- Service role (desktop) full access
CREATE POLICY "Service role full access" ON business_profiles FOR ALL TO service_role USING (true);
CREATE POLICY "Service role full access" ON products FOR ALL TO service_role USING (true);
CREATE POLICY "Service role full access" ON transactions FOR ALL TO service_role USING (true);
CREATE POLICY "Service role full access" ON transaction_items FOR ALL TO service_role USING (true);
CREATE POLICY "Service role full access" ON staff_members FOR ALL TO service_role USING (true);

-- Web users (authenticated)
CREATE POLICY "Owners view own profile" ON business_profiles
  FOR SELECT TO authenticated USING (owner_id = auth.uid());

CREATE POLICY "Owners update own profile" ON business_profiles
  FOR UPDATE TO authenticated USING (owner_id = auth.uid());

CREATE POLICY "Web users view products" ON products
  FOR SELECT TO authenticated
  USING (business_id IN (SELECT id FROM business_profiles WHERE owner_id = auth.uid()));

CREATE POLICY "Web users update products" ON products
  FOR UPDATE TO authenticated
  USING (business_id IN (SELECT id FROM business_profiles WHERE owner_id = auth.uid()));

CREATE POLICY "Web users view transactions" ON transactions
  FOR SELECT TO authenticated
  USING (business_id IN (SELECT id FROM business_profiles WHERE owner_id = auth.uid()));

CREATE POLICY "Web users manage staff" ON staff_members
  FOR ALL TO authenticated
  USING (business_id IN (SELECT id FROM business_profiles WHERE owner_id = auth.uid()));

-- =====================================================
-- STEP 5: Enable Realtime
-- =====================================================

DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM pg_publication_tables 
    WHERE pubname = 'supabase_realtime' AND tablename = 'products'
  ) THEN
    ALTER PUBLICATION supabase_realtime ADD TABLE products;
  END IF;
END $$;

-- =====================================================
-- STEP 6: Triggers
-- =====================================================

CREATE OR REPLACE FUNCTION update_updated_at()
RETURNS TRIGGER AS $$
BEGIN
  NEW.updated_at = NOW();
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS products_updated_at ON products;
CREATE TRIGGER products_updated_at
  BEFORE UPDATE ON products FOR EACH ROW EXECUTE FUNCTION update_updated_at();

DROP TRIGGER IF EXISTS business_profiles_updated_at ON business_profiles;
CREATE TRIGGER business_profiles_updated_at
  BEFORE UPDATE ON business_profiles FOR EACH ROW EXECUTE FUNCTION update_updated_at();

-- =====================================================
-- STEP 7: Link Machine to Owner Function
-- Called by Desktop app after Supabase Auth login
-- =====================================================

CREATE OR REPLACE FUNCTION link_machine_to_owner(p_machine_id TEXT)
RETURNS UUID AS $$
DECLARE
  v_business_id UUID;
  v_user_id UUID;
BEGIN
  v_user_id := auth.uid();
  
  -- Check if machine already linked to a business
  SELECT id INTO v_business_id 
  FROM business_profiles 
  WHERE machine_id = p_machine_id;
  
  IF v_business_id IS NOT NULL THEN
    -- Update owner if not set (first-time web pairing)
    UPDATE business_profiles 
    SET owner_id = v_user_id, updated_at = NOW()
    WHERE id = v_business_id AND owner_id IS NULL;
    
    RETURN v_business_id;
  ELSE
    -- Create new business profile for this machine
    INSERT INTO business_profiles (machine_id, owner_id, business_name)
    VALUES (p_machine_id, v_user_id, 'My Business')
    RETURNING id INTO v_business_id;
    
    RETURN v_business_id;
  END IF;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- =====================================================
-- DONE! Schema is now web-ready.
-- =====================================================

-- =====================================================
-- DONE! Schema is now web-ready.
-- =====================================================
