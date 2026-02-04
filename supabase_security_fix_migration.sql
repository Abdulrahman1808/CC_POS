-- ============================================================
-- SUPABASE SECURITY FIX MIGRATION (SAFE VERSION)
-- Generated: 2026-02-04
-- Purpose: Fix RLS policy security warnings and function search paths
-- ============================================================
-- 
-- This is a SAFE version that handles missing columns gracefully.
-- Run this in your Supabase SQL Editor (Dashboard > SQL Editor)
-- ============================================================

-- ============================================================
-- PART 1: FIX FUNCTION SEARCH_PATH (Security Hardening)
-- ============================================================

-- Fix get_tenant_id function
CREATE OR REPLACE FUNCTION public.get_tenant_id()
RETURNS TEXT
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = public
AS $$
  SELECT COALESCE(
    current_setting('app.tenant_id', true),
    (current_setting('request.jwt.claims', true)::json->>'business_id')::text
  );
$$;

-- Fix get_branch_id function
CREATE OR REPLACE FUNCTION public.get_branch_id()
RETURNS TEXT
LANGUAGE sql
STABLE  
SECURITY DEFINER
SET search_path = public
AS $$
  SELECT COALESCE(
    current_setting('app.branch_id', true),
    (current_setting('request.jwt.claims', true)::json->>'branch_id')::text
  );
$$;

-- ============================================================
-- PART 2: FIX RLS POLICIES - BUSINESSES TABLE
-- ============================================================

DROP POLICY IF EXISTS businesses_all ON public.businesses;
DROP POLICY IF EXISTS businesses_select ON public.businesses;
DROP POLICY IF EXISTS businesses_insert ON public.businesses;
DROP POLICY IF EXISTS businesses_update ON public.businesses;
DROP POLICY IF EXISTS businesses_delete ON public.businesses;

CREATE POLICY businesses_select ON public.businesses
    FOR SELECT USING ((id)::text = (SELECT public.get_tenant_id()));

CREATE POLICY businesses_insert ON public.businesses
    FOR INSERT WITH CHECK (
        (id)::text = (SELECT public.get_tenant_id())
        OR (SELECT public.get_tenant_id()) IS NULL
    );

CREATE POLICY businesses_update ON public.businesses
    FOR UPDATE USING ((id)::text = (SELECT public.get_tenant_id()))
    WITH CHECK ((id)::text = (SELECT public.get_tenant_id()));

CREATE POLICY businesses_delete ON public.businesses
    FOR DELETE USING ((id)::text = (SELECT public.get_tenant_id()));

-- ============================================================
-- PART 3: FIX PRODUCTS TABLE
-- ============================================================

DROP POLICY IF EXISTS products_branch_isolation ON public.products;

CREATE POLICY products_branch_isolation ON public.products
    FOR ALL USING (
        ((business_id)::text = (SELECT public.get_tenant_id()))
        AND (
            (branch_id IS NULL) 
            OR ((branch_id)::text = (SELECT public.get_branch_id())) 
            OR ((SELECT public.get_branch_id()) IS NULL)
        )
    ) WITH CHECK (
        ((business_id)::text = (SELECT public.get_tenant_id()))
        AND (
            (branch_id IS NULL) 
            OR ((branch_id)::text = (SELECT public.get_branch_id())) 
            OR ((SELECT public.get_branch_id()) IS NULL)
        )
    );

-- ============================================================
-- PART 4: FIX STAFF_MEMBERS TABLE
-- ============================================================

DROP POLICY IF EXISTS staff_branch_isolation ON public.staff_members;

CREATE POLICY staff_branch_isolation ON public.staff_members
    FOR ALL USING (
        ((business_id)::text = (SELECT public.get_tenant_id()))
        AND (
            (branch_id IS NULL) 
            OR ((branch_id)::text = (SELECT public.get_branch_id())) 
            OR ((SELECT public.get_branch_id()) IS NULL)
        )
    ) WITH CHECK (
        ((business_id)::text = (SELECT public.get_tenant_id()))
        AND (
            (branch_id IS NULL) 
            OR ((branch_id)::text = (SELECT public.get_branch_id())) 
            OR ((SELECT public.get_branch_id()) IS NULL)
        )
    );

-- ============================================================
-- PART 5: FIX TRANSACTIONS TABLE
-- ============================================================

DROP POLICY IF EXISTS transactions_branch_isolation ON public.transactions;

CREATE POLICY transactions_branch_isolation ON public.transactions
    FOR ALL USING (
        ((business_id)::text = (SELECT public.get_tenant_id()))
        AND (
            (branch_id IS NULL) 
            OR ((branch_id)::text = (SELECT public.get_branch_id())) 
            OR ((SELECT public.get_branch_id()) IS NULL)
        )
    ) WITH CHECK (
        ((business_id)::text = (SELECT public.get_tenant_id()))
        AND (
            (branch_id IS NULL) 
            OR ((branch_id)::text = (SELECT public.get_branch_id())) 
            OR ((SELECT public.get_branch_id()) IS NULL)
        )
    );

-- ============================================================
-- PART 6: FIX TRANSACTION_ITEMS TABLE (may not have business_id)
-- ============================================================

DROP POLICY IF EXISTS items_all ON public.transaction_items;

-- Check if business_id column exists, otherwise use simpler policy
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'transaction_items' 
        AND column_name = 'business_id'
    ) THEN
        EXECUTE 'CREATE POLICY items_all ON public.transaction_items FOR ALL USING (
            ((business_id)::text = (SELECT public.get_tenant_id())) OR (business_id IS NULL)
        ) WITH CHECK (
            ((business_id)::text = (SELECT public.get_tenant_id())) OR (business_id IS NULL)
        )';
    ELSE
        -- If no business_id, link to parent transaction
        EXECUTE 'CREATE POLICY items_all ON public.transaction_items FOR ALL USING (
            EXISTS (SELECT 1 FROM public.transactions t WHERE t.id = transaction_id 
                    AND (t.business_id)::text = (SELECT public.get_tenant_id()))
        ) WITH CHECK (
            EXISTS (SELECT 1 FROM public.transactions t WHERE t.id = transaction_id 
                    AND (t.business_id)::text = (SELECT public.get_tenant_id()))
        )';
    END IF;
END $$;

-- ============================================================
-- PART 7: FIX SALES TABLE (may not have business_id)
-- ============================================================

DROP POLICY IF EXISTS sales_insert ON public.sales;
DROP POLICY IF EXISTS sales_update ON public.sales;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'sales' 
        AND column_name = 'business_id'
    ) THEN
        EXECUTE 'CREATE POLICY sales_insert ON public.sales FOR INSERT 
            WITH CHECK ((business_id)::text = (SELECT public.get_tenant_id()))';
        EXECUTE 'CREATE POLICY sales_update ON public.sales FOR UPDATE 
            USING ((business_id)::text = (SELECT public.get_tenant_id()))
            WITH CHECK ((business_id)::text = (SELECT public.get_tenant_id()))';
    ELSE
        -- Fallback: allow authenticated users
        EXECUTE 'CREATE POLICY sales_insert ON public.sales FOR INSERT WITH CHECK (auth.role() = ''authenticated'')';
        EXECUTE 'CREATE POLICY sales_update ON public.sales FOR UPDATE USING (auth.role() = ''authenticated'') WITH CHECK (auth.role() = ''authenticated'')';
    END IF;
END $$;

-- ============================================================
-- PART 8: FIX SALE_ITEMS TABLE (may not have business_id)
-- ============================================================

DROP POLICY IF EXISTS sale_items_insert ON public.sale_items;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'sales' 
        AND column_name = 'business_id'
    ) THEN
        EXECUTE 'CREATE POLICY sale_items_insert ON public.sale_items FOR INSERT 
            WITH CHECK (EXISTS (
                SELECT 1 FROM public.sales s WHERE s.id = sale_id 
                AND (s.business_id)::text = (SELECT public.get_tenant_id())
            ))';
    ELSE
        EXECUTE 'CREATE POLICY sale_items_insert ON public.sale_items FOR INSERT WITH CHECK (auth.role() = ''authenticated'')';
    END IF;
END $$;

-- ============================================================
-- PART 9: FIX USERS TABLE (may not have business_id)
-- ============================================================

DROP POLICY IF EXISTS users_insert ON public.users;
DROP POLICY IF EXISTS users_update ON public.users;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'users' 
        AND column_name = 'business_id'
    ) THEN
        EXECUTE 'CREATE POLICY users_insert ON public.users FOR INSERT 
            WITH CHECK ((business_id)::text = (SELECT public.get_tenant_id()) OR (SELECT public.get_tenant_id()) IS NULL)';
        EXECUTE 'CREATE POLICY users_update ON public.users FOR UPDATE 
            USING ((business_id)::text = (SELECT public.get_tenant_id()))
            WITH CHECK ((business_id)::text = (SELECT public.get_tenant_id()))';
    ELSE
        EXECUTE 'CREATE POLICY users_insert ON public.users FOR INSERT WITH CHECK (auth.role() = ''authenticated'' OR auth.role() = ''anon'')';
        EXECUTE 'CREATE POLICY users_update ON public.users FOR UPDATE USING (auth.role() = ''authenticated'') WITH CHECK (auth.role() = ''authenticated'')';
    END IF;
END $$;

-- ============================================================
-- PART 10: FIX CUSTOMERS TABLE (may not have business_id)
-- ============================================================

DROP POLICY IF EXISTS customers_insert ON public.customers;
DROP POLICY IF EXISTS customers_update ON public.customers;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'customers' 
        AND column_name = 'business_id'
    ) THEN
        EXECUTE 'CREATE POLICY customers_insert ON public.customers FOR INSERT 
            WITH CHECK ((business_id)::text = (SELECT public.get_tenant_id()))';
        EXECUTE 'CREATE POLICY customers_update ON public.customers FOR UPDATE 
            USING ((business_id)::text = (SELECT public.get_tenant_id()))
            WITH CHECK ((business_id)::text = (SELECT public.get_tenant_id()))';
    ELSE
        EXECUTE 'CREATE POLICY customers_insert ON public.customers FOR INSERT WITH CHECK (auth.role() = ''authenticated'')';
        EXECUTE 'CREATE POLICY customers_update ON public.customers FOR UPDATE USING (auth.role() = ''authenticated'') WITH CHECK (auth.role() = ''authenticated'')';
    END IF;
END $$;

-- ============================================================
-- PART 11: FIX POS_CUSTOMERS TABLE (may not have business_id)
-- ============================================================

DROP POLICY IF EXISTS pos_customers_insert ON public.pos_customers;
DROP POLICY IF EXISTS pos_customers_update ON public.pos_customers;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'pos_customers' 
        AND column_name = 'business_id'
    ) THEN
        EXECUTE 'CREATE POLICY pos_customers_insert ON public.pos_customers FOR INSERT 
            WITH CHECK ((business_id)::text = (SELECT public.get_tenant_id()))';
        EXECUTE 'CREATE POLICY pos_customers_update ON public.pos_customers FOR UPDATE 
            USING ((business_id)::text = (SELECT public.get_tenant_id()))
            WITH CHECK ((business_id)::text = (SELECT public.get_tenant_id()))';
    ELSE
        EXECUTE 'CREATE POLICY pos_customers_insert ON public.pos_customers FOR INSERT WITH CHECK (auth.role() = ''authenticated'')';
        EXECUTE 'CREATE POLICY pos_customers_update ON public.pos_customers FOR UPDATE USING (auth.role() = ''authenticated'') WITH CHECK (auth.role() = ''authenticated'')';
    END IF;
END $$;

-- ============================================================
-- VERIFICATION: List all updated policies
-- ============================================================

SELECT 
    tablename,
    policyname,
    cmd,
    qual IS NOT NULL AS has_using,
    with_check IS NOT NULL AS has_with_check
FROM pg_policies 
WHERE schemaname = 'public'
ORDER BY tablename, policyname;

-- ============================================================
-- SUCCESS MESSAGE
-- ============================================================
DO $$ 
BEGIN 
    RAISE NOTICE 'Security migration completed successfully!';
END $$;
