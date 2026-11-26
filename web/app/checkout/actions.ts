"use server";

import { redirect } from "next/navigation";
import { createOrder, initiatePayment } from "@/lib/api";

export type CheckoutState = {
  error: string | null;
};

export async function submitOrder(
  _previousState: CheckoutState,
  formData: FormData
): Promise<CheckoutState> {
  const customerName = String(formData.get("customerName") ?? "").trim();
  const customerEmail = String(formData.get("customerEmail") ?? "").trim();
  const productId = String(formData.get("productId") ?? "").trim();
  const quantity = Number(String(formData.get("quantity") ?? "1").trim());

  if (!customerName) {
    return { error: "Please enter your name." };
  }
  if (!customerEmail) {
    return { error: "Please enter your email." };
  }
  if (!productId) {
    return { error: "Please enter a product id." };
  }
  if (!Number.isInteger(quantity) || quantity < 1) {
    return { error: "Quantity must be a whole number of at least 1." };
  }

  let checkoutUrl: string;
  try {
    const order = await createOrder({
      customerName,
      customerEmail,
      items: [{ productId, quantity }],
    });
    const payment = await initiatePayment({
      orderId: order.id,
      amount: order.total,
      idempotencyKey: crypto.randomUUID(),
    });
    checkoutUrl = payment.checkoutUrl;
  } catch (cause) {
    const detail = cause instanceof Error ? cause.message : "unknown error";
    return { error: `Checkout could not be completed: ${detail}` };
  }

  redirect(checkoutUrl);
}
