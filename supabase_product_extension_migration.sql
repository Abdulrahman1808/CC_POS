-- =====================================================
-- PRODUCT INVENTORY EXTENSION MIGRATION (IDEMPOTENT)
-- Run in Supabase SQL Editor
-- Adds wholesale/retail inventory fields to products table
-- Safe to run multiple times
-- =====================================================

-- =====================================================
-- 1. ADD NEW COLUMNS TO PRODUCTS TABLE
-- =====================================================

-- Retail quantity (individual units)
ALTER TABLE products ADD COLUMN IF NOT EXISTS retail_quantity INTEGER DEFAULT 0;

-- Storage location
ALTER TABLE products ADD COLUMN IF NOT EXISTS location VARCHAR(100);

-- Product type (Wholesale/Retail)
ALTER TABLE products ADD COLUMN IF NOT EXISTS product_type VARCHAR(50);

-- Weight specification
ALTER TABLE products ADD COLUMN IF NOT EXISTS weight VARCHAR(50);

-- Carton tracking
ALTER TABLE products ADD COLUMN IF NOT EXISTS carton_count INTEGER DEFAULT 0;
ALTER TABLE products ADD COLUMN IF NOT EXISTS units_per_carton INTEGER DEFAULT 0;
ALTER TABLE products ADD COLUMN IF NOT EXISTS carton_fraction DECIMAL(10,2) DEFAULT 0;

-- Flavor/variant
ALTER TABLE products ADD COLUMN IF NOT EXISTS flavor VARCHAR(100);

-- Unit type for selling
ALTER TABLE products ADD COLUMN IF NOT EXISTS unit_type VARCHAR(50);

-- Wholesale/Retail pricing
ALTER TABLE products ADD COLUMN IF NOT EXISTS wholesale_supplier_price DECIMAL(18,2) DEFAULT 0;
ALTER TABLE products ADD COLUMN IF NOT EXISTS wholesale_sale_price DECIMAL(18,2) DEFAULT 0;
ALTER TABLE products ADD COLUMN IF NOT EXISTS retail_sale_price DECIMAL(18,2) DEFAULT 0;

-- Extra retail quantity buffer
ALTER TABLE products ADD COLUMN IF NOT EXISTS extra_retail_quantity INTEGER DEFAULT 0;

-- =====================================================
-- 2. UPDATE COMMENTS FOR DOCUMENTATION
-- =====================================================

COMMENT ON COLUMN products.retail_quantity IS 'Retail stock quantity (individual units)';
COMMENT ON COLUMN products.location IS 'Storage location (e.g., Store 2 / مخزن 2)';
COMMENT ON COLUMN products.product_type IS 'Product type (Wholesale / جملة or Retail / قطاعى)';
COMMENT ON COLUMN products.weight IS 'Weight specification (e.g., 1kg / 1 كجم)';
COMMENT ON COLUMN products.carton_count IS 'Number of cartons in stock';
COMMENT ON COLUMN products.units_per_carton IS 'Units per carton for wholesale calculations';
COMMENT ON COLUMN products.carton_fraction IS 'Partial carton count (e.g., 0.5 for half carton)';
COMMENT ON COLUMN products.flavor IS 'Product flavor/variant (e.g., تفاحتين, عنب نعناع)';
COMMENT ON COLUMN products.unit_type IS 'Unit type for selling';
COMMENT ON COLUMN products.wholesale_supplier_price IS 'Supplier wholesale purchase price';
COMMENT ON COLUMN products.wholesale_sale_price IS 'Wholesale selling price';
COMMENT ON COLUMN products.retail_sale_price IS 'Retail selling price';
COMMENT ON COLUMN products.extra_retail_quantity IS 'Extra retail quantity buffer';

-- =====================================================
-- 3. CREATE INDEXES FOR COMMON QUERIES
-- =====================================================

-- Index for filtering by product type
CREATE INDEX IF NOT EXISTS idx_products_product_type ON products(product_type);

-- Index for filtering by location
CREATE INDEX IF NOT EXISTS idx_products_location ON products(location);

-- Index for filtering by flavor
CREATE INDEX IF NOT EXISTS idx_products_flavor ON products(flavor);

-- =====================================================
-- 4. VERIFY MIGRATION
-- =====================================================

-- List all columns to verify
SELECT column_name, data_type, column_default
FROM information_schema.columns 
WHERE table_name = 'products' 
  AND table_schema = 'public'
ORDER BY ordinal_position;

-- =====================================================
-- DONE! 
-- New columns added:
-- - retail_quantity
-- - location
-- - product_type
-- - weight
-- - carton_count
-- - units_per_carton
-- - carton_fraction
-- - flavor
-- - unit_type
-- - wholesale_supplier_price
-- - wholesale_sale_price
-- - retail_sale_price
-- - extra_retail_quantity
-- =====================================================
