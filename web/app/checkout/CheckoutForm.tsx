"use client";

import { useActionState } from "react";
import { submitOrder, type CheckoutState } from "./actions";

const initialState: CheckoutState = { error: null };

export function CheckoutForm({
  initialProductId = "",
}: {
  initialProductId?: string;
}) {
  const [state, formAction, pending] = useActionState(submitOrder, initialState);

  return (
    <form action={formAction} className="card form-card">
      {state.error && (
        <div className="alert alert-error" role="alert">
          {state.error}
        </div>
      )}

      <div className="field">
        <label htmlFor="customerName">Full name</label>
        <input
          id="customerName"
          name="customerName"
          type="text"
          autoComplete="name"
          required
        />
      </div>

      <div className="field">
        <label htmlFor="customerEmail">Email address</label>
        <input
          id="customerEmail"
          name="customerEmail"
          type="email"
          autoComplete="email"
          required
        />
      </div>

      <div className="field">
        <label htmlFor="productId">Product id</label>
        <input
          id="productId"
          name="productId"
          type="text"
          required
          defaultValue={initialProductId}
          placeholder="For example, prod_123"
        />
        <p className="hint">Copy the id from a product card on the products page.</p>
      </div>

      <div className="field">
        <label htmlFor="quantity">Quantity</label>
        <input
          id="quantity"
          name="quantity"
          type="number"
          min="1"
          step="1"
          defaultValue="1"
          required
        />
      </div>

      <button type="submit" disabled={pending}>
        {pending ? "Submitting..." : "Place order"}
      </button>
    </form>
  );
}
