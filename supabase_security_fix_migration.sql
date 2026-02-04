-- ============================================================
-- SUPABASE SECURITY FIX MIGRATION
-- Generated: 2026-02-04
-- Purpose: Fix RLS policy security warnings and function search paths
-- ============================================================
-- 
-- IMPORTANT: Review this file carefully before running!
-- Run this in your Supabase SQL Editor (Dashboard > SQL Editor)
--
-- This migration fixes:
-- 1. Function search_path security (2 functions)
-- 2. RLS policies with overly permissive WITH CHECK (true) (14 policies)
-- ============================================================

-- ============================================================
-- PART 1: FIX FUNCTION SEARCH_PATH (Security Hardening)
-- ============================================================
-- These functions need a fixed search_path to prevent search path injection attacks

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

-- Drop the overly permissive policy
DROP POLICY IF EXISTS businesses_all ON public.businesses;

-- Create proper tenant-isolated policies
CREATE POLICY businesses_select ON public.businesses
    FOR SELECT USING (
        (id)::text = (SELECT public.get_tenant_id())
    );

CREATE POLICY businesses_insert ON public.businesses
    FOR INSERT WITH CHECK (
        (id)::text = (SELECT public.get_tenant_id())
        OR (SELECT public.get_tenant_id()) IS NULL  -- Allow initial registration
    );

CREATE POLICY businesses_update ON public.businesses
    FOR UPDATE USING (
        (id)::text = (SELECT public.get_tenant_id())
    ) WITH CHECK (
        (id)::text = (SELECT public.get_tenant_id())
    );

CREATE POLICY businesses_delete ON public.businesses
    FOR DELETE USING (
        (id)::text = (SELECT public.get_tenant_id())
    );

-- ============================================================
-- PART 3: FIX RLS POLICIES - USERS TABLE
-- ============================================================

-- Drop overly permissive policies
DROP POLICY IF EXISTS users_insert ON public.users;
DROP POLICY IF EXISTS users_update ON public.users;

-- Create proper policies
CREATE POLICY users_insert ON public.users
    FOR INSERT WITH CHECK (
        (business_id)::text = (SELECT public.get_tenant_id())
        OR (SELECT public.get_tenant_id()) IS NULL  -- Allow initial registration
    );

CREATE POLICY users_update ON public.users
    FOR UPDATE USING (
        (business_id)::text = (SELECT public.get_tenant_id())
    ) WITH CHECK (
        (business_id)::text = (SELECT public.get_tenant_id())
    );

-- ============================================================
-- PART 4: FIX RLS POLICIES - CUSTOMERS TABLE
-- ============================================================

-- Drop overly permissive policies
DROP POLICY IF EXISTS customers_insert ON public.customers;
DROP POLICY IF EXISTS customers_update ON public.customers;

-- Create proper tenant-isolated policies
CREATE POLICY customers_insert ON public.customers
    FOR INSERT WITH CHECK (
        (business_id)::text = (SELECT public.get_tenant_id())
    );

CREATE POLICY customers_update ON public.customers
    FOR UPDATE USING (
        (business_id)::text = (SELECT public.get_tenant_id())
    ) WITH CHECK (
        (business_id)::text = (SELECT public.get_tenant_id())
    );

-- ============================================================
-- PART 5: FIX RLS POLICIES - POS_CUSTOMERS TABLE
-- ============================================================

-- Drop overly permissive policies
DROP POLICY IF EXISTS pos_customers_insert ON public.pos_customers;
DROP POLICY IF EXISTS pos_customers_update ON public.pos_customers;

-- Create proper tenant-isolated policies
CREATE POLICY pos_customers_insert ON public.pos_customers
    FOR INSERT WITH CHECK (
        (business_id)::text = (SELECT public.get_tenant_id())
    );

CREATE POLICY pos_customers_update ON public.pos_customers
    FOR UPDATE USING (
        (business_id)::text = (SELECT public.get_tenant_id())
    ) WITH CHECK (
        (business_id)::text = (SELECT public.get_tenant_id())
    );

-- ============================================================
-- PART 6: FIX RLS POLICIES - PRODUCTS TABLE
-- ============================================================

-- Drop overly permissive policy
DROP POLICY IF EXISTS products_branch_isolation ON public.products;

-- Create proper branch-isolated policy with proper WITH CHECK
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
-- PART 7: FIX RLS POLICIES - SALES TABLE
-- ============================================================

-- Drop overly permissive policies
DROP POLICY IF EXISTS sales_insert ON public.sales;
DROP POLICY IF EXISTS sales_update ON public.sales;

-- Create proper tenant-isolated policies
CREATE POLICY sales_insert ON public.sales
    FOR INSERT WITH CHECK (
        (business_id)::text = (SELECT public.get_tenant_id())
    );

CREATE POLICY sales_update ON public.sales
    FOR UPDATE USING (
        (business_id)::text = (SELECT public.get_tenant_id())
    ) WITH CHECK (
        (business_id)::text = (SELECT public.get_tenant_id())
    );

-- ============================================================
-- PART 8: FIX RLS POLICIES - SALE_ITEMS TABLE
-- ============================================================

-- Drop overly permissive policy
DROP POLICY IF EXISTS sale_items_insert ON public.sale_items;

-- Create proper tenant-isolated policy
CREATE POLICY sale_items_insert ON public.sale_items
    FOR INSERT WITH CHECK (
        EXISTS (
            SELECT 1 FROM public.sales s 
            WHERE s.id = sale_id 
            AND (s.business_id)::text = (SELECT public.get_tenant_id())
        )
    );

-- ============================================================
-- PART 9: FIX RLS POLICIES - TRANSACTIONS TABLE
-- ============================================================

-- Drop overly permissive policy
DROP POLICY IF EXISTS transactions_branch_isolation ON public.transactions;

-- Create proper branch-isolated policy
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
-- PART 10: FIX RLS POLICIES - TRANSACTION_ITEMS TABLE
-- ============================================================

-- Drop overly permissive policy
DROP POLICY IF EXISTS items_all ON public.transaction_items;

-- Create proper tenant-isolated policy
CREATE POLICY items_all ON public.transaction_items
    FOR ALL USING (
        ((business_id)::text = (SELECT public.get_tenant_id()))
        OR (business_id IS NULL)
    ) WITH CHECK (
        ((business_id)::text = (SELECT public.get_tenant_id()))
        OR (business_id IS NULL AND (SELECT public.get_tenant_id()) IS NOT NULL)
    );

-- ============================================================
-- PART 11: FIX RLS POLICIES - STAFF_MEMBERS TABLE
-- ============================================================

-- Drop overly permissive policy
DROP POLICY IF EXISTS staff_branch_isolation ON public.staff_members;

-- Create proper branch-isolated policy
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
-- VERIFICATION: Check that all policies are properly configured
-- ============================================================

-- List all policies to verify changes
SELECT 
    schemaname,
    tablename,
    policyname,
    permissive,
    roles,
    cmd,
    qual IS NOT NULL AS has_using,
    with_check IS NOT NULL AS has_with_check
FROM pg_policies 
WHERE schemaname = 'public'
ORDER BY tablename, policyname;

-- ============================================================
-- NOTES FOR MANUAL AUTH CONFIGURATION:
-- ============================================================
-- 
-- The following settings need to be configured in Supabase Dashboard:
-- 
-- 1. LEAKED PASSWORD PROTECTION:
--    Dashboard > Authentication > Settings > Security
--    Enable "Leaked Password Protection"
--
-- 2. MULTI-FACTOR AUTHENTICATION (MFA):
--    Dashboard > Authentication > Settings > Multi Factor Auth
--    Enable TOTP (Authenticator app) and/or SMS
--
-- These cannot be configured via SQL - they are Auth service settings.
-- ============================================================
