import type { Metadata } from "next";
import type { ReactNode } from "react";
import Link from "next/link";
import "./globals.css";

export const metadata: Metadata = {
  title: "BD Commerce Storefront",
  description: "Browse products, place an order, and track its status.",
};

export default function RootLayout({ children }: { children: ReactNode }) {
  return (
    <html lang="en">
      <body>
        <header className="site-header">
          <div className="container header-inner">
            <Link href="/" className="brand">
              BD Commerce
            </Link>
            <nav aria-label="Main navigation">
              <Link href="/">Products</Link>
              <Link href="/checkout">Checkout</Link>
            </nav>
          </div>
        </header>
        <main className="container page">{children}</main>
        <footer className="site-footer">
          <div className="container">BD Commerce storefront</div>
        </footer>
      </body>
    </html>
  );
}
