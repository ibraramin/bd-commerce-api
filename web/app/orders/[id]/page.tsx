import Link from "next/link";
import { getOrder, type Order } from "@/lib/api";

export const dynamic = "force-dynamic";

function formatTotal(order: Order): string {
  const amount = Number.isFinite(order.total) ? order.total : 0;
  return `$${amount.toFixed(2)}`;
}

export default async function OrderPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;

  let order: Order | null = null;
  let error: string | null = null;
  try {
    order = await getOrder(id);
  } catch (cause) {
    error = cause instanceof Error ? cause.message : "Could not load the order.";
  }

  return (
    <section>
      <h1>Order status</h1>
      <p className="lede">Order id: {id}</p>

      {error && (
        <div className="alert alert-error" role="alert">
          <strong>The order could not be loaded.</strong> {error}
        </div>
      )}

      {!error && order === null && (
        <div className="alert alert-info">No order was found for this id.</div>
      )}

      {!error && order !== null && (
        <dl className="details">
          <dt>Status</dt>
          <dd>{order.status}</dd>
          <dt>Total</dt>
          <dd>{formatTotal(order)}</dd>
          <dt>Created</dt>
          <dd>{order.createdAt}</dd>
        </dl>
      )}

      <p style={{ marginTop: 24 }}>
        <Link href="/">Back to products</Link>
      </p>
    </section>
  );
}
