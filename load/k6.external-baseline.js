// PROVENANCE: external concurrent writer's 5-VU/30s smoke script, captured 11:23:39 local; superseded by load/k6.js.
// Local-only smoke load against the Compose stack.
// Run: BASE_URL=http://127.0.0.1:18080 k6 run load/k6.js --summary-export=results/k6-summary.json
import http from 'k6/http';
import { check, sleep } from 'k6';

const BASE_URL = __ENV.BASE_URL || 'http://127.0.0.1:18080';

export const options = {
  vus: 5,
  duration: '30s',
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<2000'],
    checks: ['rate>0.99'],
  },
};

export default function () {
  const health = http.get(`${BASE_URL}/health`);
  check(health, { 'health returns 200': (r) => r.status === 200 });

  const products = http.get(`${BASE_URL}/api/v1/products`);
  check(products, {
    'products returns 200': (r) => r.status === 200,
    'products returns a JSON array': (r) => Array.isArray(r.json()),
  });

  sleep(1);
}
