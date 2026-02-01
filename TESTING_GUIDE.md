# POS System - Testing Guide

## Overview
This guide provides test scenarios for validating the POS system before production deployment.

---

## Pre-Requisites

### For Tester (Self-Contained Build)
- Windows 10/11 (64-bit)
- **NO .NET installation required** (self-contained)

### Test Data
The app seeds sample products on first run (Coffee, Pastries, Drinks categories).

---

## Test Scenarios

### 1. Zero-Configuration Test (CRITICAL)

**Purpose:** Confirm the app runs on a clean PC without .NET 8 installed.

| Step | Action | Expected |
|------|--------|----------|
| 1 | Copy `POSSystem.exe` to a test PC with NO .NET installed | File copies successfully |
| 2 | Double-click `POSSystem.exe` | App launches without error |
| 3 | Navigate to Inventory | Products display correctly |
| 4 | Complete a test sale | Transaction saves to local DB |

**✓ Pass Criteria:** App runs standalone without .NET dependency errors.

---

### 2. Egypt VAT Test (14% Tax)

**Purpose:** Verify VAT calculates correctly per product.

| Step | Action | Expected |
|------|--------|----------|
| 1 | Add "Espresso" ($3.50) to cart | Subtotal: $3.50 |
| 2 | Check Tax display | Tax: $0.49 (14% of $3.50) |
| 3 | Check Total | Total: $3.99 |
| 4 | Add "Latte" ($4.50) | Subtotal: $8.00, Tax: $1.12, Total: $9.12 |

**✓ Pass Criteria:** Tax calculates as 14% of each item (not flat rate).

---

### 3. Fawry Reference Test (Mobile Payment)

**Purpose:** Verify Fawry reference code generation for mobile payments.

| Step | Action | Expected |
|------|--------|----------|
| 1 | Add products to cart | Items appear in cart |
| 2 | Click "Checkout" | Checkout panel opens |
| 3 | Select "Mobile" payment method | Mobile selected |
| 4 | Click "Complete Sale" | Fawry reference generated |
| 5 | Check reference format | Format: `FWY-XXXX-XXXX` |

**✓ Pass Criteria:** Reference code in `FWY-ABCD-1234` format displayed.

---

### 4. Cash Payment Test

**Purpose:** Verify cash payment with change calculation.

| Step | Action | Expected |
|------|--------|----------|
| 1 | Add items totaling ~$10 | Total displays |
| 2 | Select "Cash" payment | Cash selected |
| 3 | Enter $20 in "Amount Tendered" | Amount accepted |
| 4 | Click "Complete Sale" | Change Due: $10.00 displayed |
| 5 | Wait 2 seconds | Sale completes, cart clears |

**✓ Pass Criteria:** Change calculates correctly, sale completes.

---

### 5. Card Payment Test

**Purpose:** Verify card authorization simulation.

| Step | Action | Expected |
|------|--------|----------|
| 1 | Add products to cart | Items appear |
| 2 | Select "Card" payment | Card selected |
| 3 | Click "Complete Sale" | "Authorizing..." delay (2-3 sec) |
| 4 | Wait for completion | Sale completes after delay |

**✓ Pass Criteria:** 2-3 second authorization delay observed.

---

### 6. Offline Sync Test

**Purpose:** Verify offline-first functionality.

| Step | Action | Expected |
|------|--------|----------|
| 1 | Disconnect from internet | "Offline" indicator shows |
| 2 | Complete 3 sales | Sales save locally |
| 3 | Check "Pending" count | Shows 3 pending syncs |
| 4 | Reconnect to internet | "Online" indicator shows |
| 5 | Click "Sync Now" | Pending count goes to 0 |

**✓ Pass Criteria:** Sales persist offline, sync when online.

---

### 7. Product Management Test

**Purpose:** Verify inventory CRUD operations.

| Step | Action | Expected |
|------|--------|----------|
| 1 | Go to Inventory | Products list displays |
| 2 | Click "Add Product" | Add form opens |
| 3 | Fill in product details | Fields accept input |
| 4 | Click "Save" | Product appears in list |
| 5 | Select product, click Delete | Confirmation dialog |
| 6 | Confirm delete | Product removed |
| 7 | Restart app | Deleted product stays deleted |

**✓ Pass Criteria:** Product changes persist after restart.

---

## Known Issues

| Issue | Workaround |
|-------|------------|
| First launch slow | Wait 5-10 seconds for DB initialization |
| QR Code rendering | Requires valid Tax ID in Settings |

---

## Support

For bugs or issues, check the Debug output window (if running in development mode) or create an issue in the repository.

---

*Last Updated: December 2024*
