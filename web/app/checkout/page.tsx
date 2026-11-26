import Link from "next/link";
import { CheckoutForm } from "./CheckoutForm";

export default async function CheckoutPage({
  searchParams,
}: {
  searchParams: Promise<{ product?: string | string[] }>;
}) {
  const params = await searchParams;
  const raw = params.product;
  const initialProductId = Array.isArray(raw) ? raw[0] ?? "" : raw ?? "";

  return (
    <section>
      <h1>Checkout</h1>
      <p className="lede">
        Fill in the details below to place your order and continue to payment.
      </p>
      <CheckoutForm initialProductId={initialProductId} />
      <p style={{ marginTop: 20 }}>
        <Link href="/">Back to products</Link>
      </p>
    </section>
  );
}
