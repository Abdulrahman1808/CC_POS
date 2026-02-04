/// <reference types="node" />

/**
 * Paymob/Fawry Webhook Handler
 * 
 * This file handles payment callbacks from Paymob and Fawry.
 * Deploy as a Supabase Edge Function or Express.js endpoint.
 * 
 * Endpoint: POST /api/webhooks/paymob
 */

import { createClient } from '@supabase/supabase-js';

// Environment variables (set in your deployment platform)
const SUPABASE_URL = process.env.VITE_SUPABASE_URL || '';
const SUPABASE_SERVICE_KEY = process.env.SUPABASE_SERVICE_KEY || '';
const PAYMOB_HMAC_SECRET = process.env.PAYMOB_HMAC_SECRET || '';

const supabase = createClient(SUPABASE_URL, SUPABASE_SERVICE_KEY);

/**
 * Paymob Transaction Callback Data
 */
interface PaymobCallback {
    obj: {
        id: number;
        order: {
            id: number;
            merchant_order_id: string; // Our transaction ID
        };
        success: boolean;
        amount_cents: number;
        source_data: {
            type: string; // 'card', 'wallet', 'fawry', 'meeza'
            sub_type: string;
            pan: string;
        };
        pending: boolean;
        created_at: string;
        currency: string;
        integration_id: number;
    };
    type: string; // 'TRANSACTION', 'TOKEN'
    hmac: string;
}

/**
 * Verify Paymob HMAC signature
 */
function verifyHmac(data: PaymobCallback, receivedHmac: string): boolean {
    // In production, implement HMAC-SHA512 verification
    // using the PAYMOB_HMAC_SECRET
    // For now, return true for development
    if (!PAYMOB_HMAC_SECRET) {
        console.warn('⚠️ PAYMOB_HMAC_SECRET not set, skipping verification');
        return true;
    }

    // TODO: Implement proper HMAC verification
    // const crypto = require('crypto');
    // const hmac = crypto.createHmac('sha512', PAYMOB_HMAC_SECRET);
    // const calculatedHmac = hmac.update(concatenatedString).digest('hex');
    // return calculatedHmac === receivedHmac;

    return true;
}

/**
 * Handle Paymob webhook callback
 */
export async function handlePaymobWebhook(body: PaymobCallback) {
    const { obj } = body;

    console.log(`[Paymob Webhook] Received: ${body.type} for order ${obj.order.merchant_order_id}`);

    // Verify HMAC
    if (!verifyHmac(body, body.hmac)) {
        throw new Error('Invalid HMAC signature');
    }

    // Get transaction ID from merchant_order_id
    const transactionId = obj.order.merchant_order_id;

    // Determine payment status
    let status: 'paid' | 'pending' | 'failed';
    if (obj.success) {
        status = 'paid';
    } else if (obj.pending) {
        status = 'pending';
    } else {
        status = 'failed';
    }

    // Map payment method
    const paymentMethod = mapPaymentMethod(obj.source_data.type);

    // Update transaction in Supabase
    const { error } = await supabase
        .from('transactions')
        .update({
            status: status,
            payment_method: paymentMethod,
            payment_reference: obj.id.toString(),
            paid_at: obj.success ? new Date().toISOString() : null,
            updated_at: new Date().toISOString(),
        })
        .eq('id', transactionId);

    if (error) {
        console.error('[Paymob Webhook] Supabase update error:', error);
        throw error;
    }

    console.log(`[Paymob Webhook] Transaction ${transactionId} updated to ${status}`);

    return { success: true, status };
}

/**
 * Map Paymob payment type to our payment methods
 */
function mapPaymentMethod(type: string): string {
    switch (type.toLowerCase()) {
        case 'card':
            return 'card';
        case 'meeza':
            return 'meeza';
        case 'wallet':
            return 'vodafone_cash';
        case 'fawry':
            return 'fawry';
        default:
            return 'other';
    }
}

/**
 * Fawry Reference Callback Handler
 * Called when a Fawry reference code is paid at a kiosk
 */
export async function handleFawryCallback(body: {
    referenceNumber: string;
    merchantRefNum: string; // Our transaction ID
    paymentStatus: 'PAID' | 'EXPIRED' | 'REFUNDED';
    paymentAmount: number;
    paymentTime: string;
}) {
    console.log(`[Fawry Webhook] Received: ${body.paymentStatus} for ref ${body.referenceNumber}`);

    const transactionId = body.merchantRefNum;

    const status = body.paymentStatus === 'PAID' ? 'paid' :
        body.paymentStatus === 'EXPIRED' ? 'expired' : 'refunded';

    const { error } = await supabase
        .from('transactions')
        .update({
            status: status,
            payment_method: 'fawry',
            payment_reference: body.referenceNumber,
            paid_at: body.paymentStatus === 'PAID' ? body.paymentTime : null,
            updated_at: new Date().toISOString(),
        })
        .eq('id', transactionId);

    if (error) {
        console.error('[Fawry Webhook] Supabase update error:', error);
        throw error;
    }

    console.log(`[Fawry Webhook] Transaction ${transactionId} updated to ${status}`);

    return { success: true, status };
}

/**
 * Express.js route handler example
 * Use this in your backend service
 */
export const paymobWebhookHandler = async (req: any, res: any) => {
    try {
        const result = await handlePaymobWebhook(req.body);
        res.json(result);
    } catch (error: any) {
        console.error('[Paymob Webhook Error]', error);
        res.status(400).json({ error: error.message });
    }
};

export const fawryWebhookHandler = async (req: any, res: any) => {
    try {
        const result = await handleFawryCallback(req.body);
        res.json(result);
    } catch (error: any) {
        console.error('[Fawry Webhook Error]', error);
        res.status(400).json({ error: error.message });
    }
};
