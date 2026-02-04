/**
 * Supabase Data Queries for React Dashboard
 * Fetches data filtered by business_id for multi-tenant support
 */

import { createClient } from '@supabase/supabase-js';

const supabaseUrl = import.meta.env.VITE_SUPABASE_URL;
const supabaseKey = import.meta.env.VITE_SUPABASE_ANON_KEY;

export const supabase = createClient(supabaseUrl, supabaseKey);

// =====================================================
// Business Profile Queries
// =====================================================

export async function getBusinessProfile() {
  const { data: { user } } = await supabase.auth.getUser();
  if (!user) return null;

  const { data, error } = await supabase
    .from('business_profiles')
    .select('*')
    .eq('owner_id', user.id)
    .single();

  if (error) console.error('Error fetching business profile:', error);
  return data;
}

export async function getBusinessId(): Promise<string | null> {
  const profile = await getBusinessProfile();
  return profile?.id || null;
}

// =====================================================
// Products Queries
// =====================================================

export async function getProducts() {
  const businessId = await getBusinessId();
  if (!businessId) return [];

  const { data, error } = await supabase
    .from('products')
    .select('*')
    .eq('business_id', businessId)
    .eq('is_deleted', false)
    .order('name');

  if (error) console.error('Error fetching products:', error);
  return data || [];
}

export async function updateProduct(id: string, updates: Partial<Product>) {
  const { data, error } = await supabase
    .from('products')
    .update({ 
      ...updates, 
      last_updated_by: 'WebDashboard',
      updated_at: new Date().toISOString()
    })
    .eq('id', id)
    .select()
    .single();

  if (error) throw error;
  return data;
}

// =====================================================
// Transactions Queries
// =====================================================

export async function getTransactions(options?: {
  startDate?: Date;
  endDate?: Date;
  limit?: number;
}) {
  const businessId = await getBusinessId();
  if (!businessId) return [];

  let query = supabase
    .from('transactions')
    .select('*, transaction_items(*)')
    .eq('business_id', businessId)
    .order('created_at', { ascending: false });

  if (options?.startDate) {
    query = query.gte('created_at', options.startDate.toISOString());
  }
  if (options?.endDate) {
    query = query.lte('created_at', options.endDate.toISOString());
  }
  if (options?.limit) {
    query = query.limit(options.limit);
  }

  const { data, error } = await query;
  if (error) console.error('Error fetching transactions:', error);
  return data || [];
}

export async function getTodaysSales() {
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  
  const transactions = await getTransactions({ startDate: today });
  return transactions.reduce((sum, t) => sum + (t.total || 0), 0);
}

export async function getSalesAnalytics(days: number = 7) {
  const businessId = await getBusinessId();
  if (!businessId) return [];

  const startDate = new Date();
  startDate.setDate(startDate.getDate() - days);

  const { data, error } = await supabase
    .from('transactions')
    .select('total, created_at')
    .eq('business_id', businessId)
    .gte('created_at', startDate.toISOString())
    .order('created_at');

  if (error) console.error('Error fetching analytics:', error);
  
  // Group by day
  const groupedByDay = (data || []).reduce((acc, t) => {
    const day = new Date(t.created_at).toLocaleDateString();
    acc[day] = (acc[day] || 0) + t.total;
    return acc;
  }, {} as Record<string, number>);

  return Object.entries(groupedByDay).map(([date, total]) => ({
    date,
    total
  }));
}

// =====================================================
// Staff Members Queries
// =====================================================

export async function getStaffMembers() {
  const businessId = await getBusinessId();
  if (!businessId) return [];

  const { data, error } = await supabase
    .from('staff_members')
    .select('*')
    .eq('business_id', businessId)
    .eq('is_active', true)
    .order('name');

  if (error) console.error('Error fetching staff:', error);
  return data || [];
}

export async function updateStaffPermissions(id: string, permissions: Partial<StaffMember>) {
  const { data, error } = await supabase
    .from('staff_members')
    .update(permissions)
    .eq('id', id)
    .select()
    .single();

  if (error) throw error;
  return data;
}

// =====================================================
// Realtime Subscriptions
// =====================================================

export function subscribeToTransactions(
  businessId: string,
  callback: (payload: any) => void
) {
  return supabase
    .channel('transactions_changes')
    .on(
      'postgres_changes',
      {
        event: 'INSERT',
        schema: 'public',
        table: 'transactions',
        filter: `business_id=eq.${businessId}`
      },
      callback
    )
    .subscribe();
}

// =====================================================
// Types
// =====================================================

interface Product {
  id: string;
  name: string;
  price: number;
  category?: string;
  is_active: boolean;
  last_updated_by: string;
}

interface StaffMember {
  id: string;
  name: string;
  level: number;
  can_delete_transactions: boolean;
  can_change_prices: boolean;
  can_view_reports: boolean;
  can_manage_staff: boolean;
  can_void_items: boolean;
  can_apply_discounts: boolean;
}
