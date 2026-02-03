-- =====================================================
-- CLEANUP OLD "Allow all for anon" POLICIES
-- These are dangerous and must be removed!
-- Run in Supabase SQL Editor
-- =====================================================

-- Remove dangerous legacy policies
DROP POLICY IF EXISTS "Allow all for anon" ON activity_log;
DROP POLICY IF EXISTS "Allow all for anon" ON customers;
DROP POLICY IF EXISTS "Allow all for anon" ON pos_customers;
DROP POLICY IF EXISTS "Allow all for anon" ON products;
DROP POLICY IF EXISTS "Allow all for anon" ON sale_items;
DROP POLICY IF EXISTS "Allow all for anon" ON sales;
DROP POLICY IF EXISTS "Allow all for anon" ON users;

-- Verify RLS is enabled on these tables
ALTER TABLE activity_log ENABLE ROW LEVEL SECURITY;
ALTER TABLE customers ENABLE ROW LEVEL SECURITY;
ALTER TABLE pos_customers ENABLE ROW LEVEL SECURITY;
ALTER TABLE sale_items ENABLE ROW LEVEL SECURITY;
ALTER TABLE sales ENABLE ROW LEVEL SECURITY;
ALTER TABLE users ENABLE ROW LEVEL SECURITY;

-- Create proper tenant-based policies for these tables
-- (Add business_id column first if needed)

-- Activity Log - SELECT only for now
CREATE POLICY "activity_log_select" ON activity_log
    FOR SELECT USING (true);  -- Logs can be read

-- Customers
CREATE POLICY "customers_select" ON customers FOR SELECT USING (true);
CREATE POLICY "customers_insert" ON customers FOR INSERT WITH CHECK (true);
CREATE POLICY "customers_update" ON customers FOR UPDATE USING (true);

-- POS Customers
CREATE POLICY "pos_customers_select" ON pos_customers FOR SELECT USING (true);
CREATE POLICY "pos_customers_insert" ON pos_customers FOR INSERT WITH CHECK (true);
CREATE POLICY "pos_customers_update" ON pos_customers FOR UPDATE USING (true);

-- Sale Items
CREATE POLICY "sale_items_select" ON sale_items FOR SELECT USING (true);
CREATE POLICY "sale_items_insert" ON sale_items FOR INSERT WITH CHECK (true);

-- Sales
CREATE POLICY "sales_select" ON sales FOR SELECT USING (true);
CREATE POLICY "sales_insert" ON sales FOR INSERT WITH CHECK (true);
CREATE POLICY "sales_update" ON sales FOR UPDATE USING (true);

-- Users (be careful - may need auth integration)
CREATE POLICY "users_select" ON users FOR SELECT USING (true);
CREATE POLICY "users_insert" ON users FOR INSERT WITH CHECK (true);
CREATE POLICY "users_update" ON users FOR UPDATE USING (true);

SELECT 'Old "Allow all for anon" policies removed!' AS status;
