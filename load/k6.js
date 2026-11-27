// Canonical local-only k6 load scenario for bd-commerce-api (ramping VUs, 35s).
// The external writer's 5-VU/30s smoke variant is preserved at k6.external-baseline.js.
// Run via Docker against the Compose stack (see results/LOAD-RESULTS.md):
//   docker run --rm --network=host -v "$PWD":/work -w /work \
//     -e BASE_URL=http://127.0.0.1:18080 \
//     grafana/k6:latest run --summary-export=results/k6-summary.json load/k6.js
//
// No external endpoint is contacted: BASE_URL always points at the local Compose API.

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter } from 'k6/metrics';

const BASE_URL = __ENV.BASE_URL || 'http://127.0.0.1:8080';
const JSON_HEADERS = { 'Content-Type': 'application/json' };
const IDEMPOTENCY_KEY = 'load-test-idempotency-key';

// Counts requests whose status violated the documented API contract.
export const contractFailures = new Counter('contract_failures');

export const options = {
  scenarios: {
    mixed_api: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '10s', target: 10 },
        { duration: '20s', target: 10 },
        { duration: '5s', target: 0 }
      ],
      gracefulRampDown: '2s'
    }
  },
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<2000'],
    checks: ['rate>0.99']
  }
};

function record(ok) {
  if (!ok) {
    contractFailures.add(1);
  }
}

export function setup() {
  const products = http.get(`${BASE_URL}/api/v1/products`, { tags: { endpoint: 'products' } });
  if (products.status !== 200) {
    throw new Error(`setup: GET /api/v1/products returned ${products.status}`);
  }

  const productsBody = JSON.parse(products.body);
  if (!Array.isArray(productsBody) || productsBody.length === 0) {
    throw new Error('setup: no seeded products available');
  }
  const productId = productsBody[0].id;

  const created = http.post(
    `${BASE_URL}/api/v1/orders`,
    JSON.stringify({
      customerName: 'Load Test Customer',
      customerEmail: 'load-test@example.com',
      items: [{ productId, quantity: 1 }]
    }),
    { headers: JSON_HEADERS, tags: { endpoint: 'orders.create' } }
  );
  if (created.status !== 201) {
    throw new Error(`setup: POST /api/v1/orders returned ${created.status}: ${created.body}`);
  }

  const order = JSON.parse(created.body);
  return { productId, orderId: order.id, amount: order.total };
}

export default function (data) {
  let response = http.get(`${BASE_URL}/health`, { tags: { endpoint: 'health' } });
  record(check(response, { 'health returns 200': (res) => res.status === 200 }));

  response = http.get(`${BASE_URL}/api/v1/products`, { tags: { endpoint: 'products' } });
  record(check(response, { 'products returns 200': (res) => res.status === 200 }));

  response = http.get(`${BASE_URL}/api/v1/orders/${data.orderId}`, { tags: { endpoint: 'orders.get' } });
  record(check(response, { 'get order returns 200': (res) => res.status === 200 }));

  response = http.post(
    `${BASE_URL}/api/v1/orders`,
    JSON.stringify({
      customerName: 'Load Test Customer',
      customerEmail: 'load-test@example.com',
      items: [{ productId: data.productId, quantity: 1 }]
    }),
    { headers: JSON_HEADERS, tags: { endpoint: 'orders.create' } }
  );
  record(check(response, { 'create order returns 201': (res) => res.status === 201 }));

  // Same key on every iteration: exercises the idempotent payment-initiation path.
  response = http.post(
    `${BASE_URL}/api/v1/payments/initiate`,
    JSON.stringify({
      orderId: data.orderId,
      amount: data.amount,
      idempotencyKey: IDEMPOTENCY_KEY
    }),
    { headers: JSON_HEADERS, tags: { endpoint: 'payments.initiate' } }
  );
  record(check(response, { 'initiate payment returns 200': (res) => res.status === 200 }));

  sleep(0.2);
}
