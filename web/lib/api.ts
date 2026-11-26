/**
 * Single gateway for every call to the BD Commerce API.
 * The base URL is read from the public environment variable. If it is
 * missing we fail loudly instead of falling back to a local default.
 */

export type Product = {
  id: string;
  name: string;
  description: string;
  price: number;
  currency: string;
};

export type OrderItemInput = {
  productId: string;
  quantity: number;
};

export type CreateOrderInput = {
  customerName: string;
  customerEmail: string;
  items: OrderItemInput[];
};

export type Order = {
  id: string;
  status: string;
  total: number;
  createdAt: string;
};

export type InitiatePaymentInput = {
  orderId: string;
  amount: number;
  idempotencyKey: string;
};

export type PaymentInitiation = {
  paymentIntentId: string;
  status: string;
  checkoutUrl: string;
};

export function getApiBaseUrl(): string {
  const raw = process.env.NEXT_PUBLIC_API_BASE_URL;
  if (typeof raw !== "string" || raw.trim() === "") {
    throw new Error(
      "NEXT_PUBLIC_API_BASE_URL is not set. Set it to the API origin, for example https://api.example.com, and rebuild the storefront."
    );
  }
  return raw.trim().replace(/\/+$/, "");
}

async function requestJson<T>(path: string, init?: RequestInit): Promise<T> {
  const url = `${getApiBaseUrl()}${path}`;
  let res: Response;
  try {
    res = await fetch(url, {
      ...init,
      cache: "no-store",
      headers: {
        "Content-Type": "application/json",
        ...(init?.headers ?? {}),
      },
    });
  } catch (cause) {
    const detail = cause instanceof Error ? cause.message : "network error";
    throw new Error(`Could not reach the API: ${detail}`);
  }
  if (!res.ok) {
    throw new Error(`API request failed (${res.status}) for ${path}`);
  }
  return (await res.json()) as T;
}

export async function getHealth(): Promise<string> {
  const res = await fetch(`${getApiBaseUrl()}/health`, { cache: "no-store" });
  if (!res.ok) {
    throw new Error(`Health check failed (${res.status})`);
  }
  return res.text();
}

export async function getProducts(): Promise<Product[]> {
  return requestJson<Product[]>("/api/v1/products");
}

export async function createOrder(input: CreateOrderInput): Promise<Order> {
  return requestJson<Order>("/api/v1/orders", {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export async function getOrder(id: string): Promise<Order | null> {
  const url = `${getApiBaseUrl()}/api/v1/orders/${encodeURIComponent(id)}`;
  const res = await fetch(url, { cache: "no-store" });
  if (res.status === 404) {
    return null;
  }
  if (!res.ok) {
    throw new Error(`Failed to load order (${res.status})`);
  }
  return (await res.json()) as Order;
}

export async function initiatePayment(
  input: InitiatePaymentInput
): Promise<PaymentInitiation> {
  return requestJson<PaymentInitiation>("/api/v1/payments/initiate", {
    method: "POST",
    body: JSON.stringify(input),
  });
}
