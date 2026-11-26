import Link from "next/link";
import { getProducts, type Product } from "@/lib/api";

export const dynamic = "force-dynamic";

function formatPrice(product: Product): string {
  const amount = Number.isFinite(product.price) ? product.price : 0;
  const currency = product.currency || "";
  return `${currency} ${amount.toFixed(2)}`.trim();
}

export default async function HomePage() {
  let products: Product[] = [];
  let error: string | null = null;

  try {
    products = await getProducts();
  } catch (cause) {
    error = cause instanceof Error ? cause.message : "Could not load products.";
  }

  return (
    <section>
      <h1>Products</h1>
      <p className="lede">Browse the catalog and start a checkout for any item.</p>

      {error && (
        <div className="alert alert-error" role="alert">
          <strong>Products are unavailable right now.</strong> {error}
        </div>
      )}

      {!error && products.length === 0 && (
        <div className="alert alert-info">There are no products to show yet.</div>
      )}

      {!error && products.length > 0 && (
        <div className="product-grid">
          {products.map((product) => (
            <article className="card" key={product.id}>
              <h2>{product.name}</h2>
              <p>{product.description}</p>
              <p className="price">{formatPrice(product)}</p>
              <Link
                className="button"
                href={`/checkout?product=${encodeURIComponent(product.id)}`}
              >
                Buy now
              </Link>
            </article>
          ))}
        </div>
      )}
    </section>
  );
}
