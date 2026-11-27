# Load results — canonical k6 scenario (REAL measurements)

All numbers below were observed, not estimated. Machine-readable export: `results/k6-summary.json`.

## Exact command

From `projects/bd-commerce-api` (isolated fork stack; host 8080/4317/4318/16686 were occupied):

```sh
COMPOSE_PROJECT_NAME=cv2bf API_PORT=18081 OTEL_GRPC_PORT=14317 OTEL_HTTP_PORT=14318 JAEGER_UI_PORT=16687 \
  docker compose build api && \
COMPOSE_PROJECT_NAME=cv2bf API_PORT=18081 OTEL_GRPC_PORT=14317 OTEL_HTTP_PORT=14318 JAEGER_UI_PORT=16687 \
  docker compose up -d

docker run --rm --user "$(id -u):$(id -g)" --network=host -v "$PWD":/work -w /work \
  -e BASE_URL=http://127.0.0.1:18081 \
  grafana/k6:latest run --summary-export=results/k6-summary.json load/k6.js
```

Exit code: **0**. Script: `load/k6.js` (canonical ramping-vus). k6 v2.4.0 (`grafana/k6:latest`,
commit `dd9395906d`, go1.27.3, linux/amd64), Docker 29.9.1.

## Hardware / context

- CPU: AMD Ryzen 7 6800HS Creator Edition, 16 logical CPUs (`nproc`=16), x86_64.
- Memory: 14 GiB total, 7.7 GiB available at measurement time (`free -h`).
- OS: Linux 7.2.11-1-cachyos x86_64.
- Stack: local Docker Compose only (`cv2bf-api`, `cv2bf-postgres`, `cv2bf-otel-collector`,
  `cv2bf-jaeger`), all healthy. No external endpoint.
- API image `bd-commerce-api:local` rebuilt from fork source immediately before the run.

## Concurrency and duration

- Executor `ramping-vus`: 0 -> 10 VUs over 10s, hold 10 VUs for 20s, ramp down over 5s
  (2s graceful stop). Max VUs observed: 10.
- Wall-clock: **35.4s**, 1300 complete iterations, 0 interrupted.

## Measured results

| Metric | Observed |
|---|---|
| HTTP requests | **6502** (183.93 req/s) |
| Error rate (`http_req_failed`) | **0.00%** — 0 of 6502 |
| Checks | **6500 / 6500 passed (100%)** |
| `http_req_duration` avg / median | 1.95 ms / 1.03 ms |
| `http_req_duration` p90 / **p95** | 5.40 ms / **6.19 ms** |
| `http_req_duration` max | 211.36 ms |
| p99 | **not emitted** by k6 v2.4.0 in console or summary JSON (only p90/p95 are exported); no value invented |
| `iteration_duration` avg / p95 | 210.50 ms / 214.37 ms (includes 200 ms `sleep`) |
| Data received / sent | 2.2 MB / 940 kB |

All five endpoint checks passed (health 200, products 200, get order 200, create order 201,
initiate payment 200). Thresholds `rate<0.01`, `p(95)<2000`, `checks>0.99` all satisfied.

## Reconciliation / provenance

- **05:23:22-05:24:04Z earlier run** produced real numbers (6067 requests, 1213 iterations,
  max 10 VUs, 35.5s, p95 16.46 ms, 0 failures) and exited 0, but `load/k6.js` was overwritten
  by an external writer at 11:23:39 local during that run, making its provenance ambiguous;
  its `--summary-export` also failed with a container-uid permission error, so it wrote no
  JSON. It is **superseded** by the fork run above.
- External writer's 5-VU/30s script preserved verbatim (plus one provenance comment) as
  `load/k6.external-baseline.js`; their summary JSON preserved as
  `results/k6-summary.external-baseline.json`; their `results/k6-run.log`,
  `results/compose-state-at-run.json`, `results/jaeger-traces.json` and
  `results/tracing-evidence.json` are untouched.
- `results/k6-summary.json` now holds the canonical fork run; the external summary was
  copied aside first. No figures in this file are invented.

## Observability wire-up confirmation

After reconciliation, `GET http://127.0.0.1:16687/api/services` returned
`{"data":["bd-commerce-api"]}`; a trace query returned 20 traces whose spans include both
automatic (`GET /health`, `GET /api/v1/products`) and named (`products.list`) operations.
API -> OTLP/gRPC collector -> `otlp/jaeger` exporter -> Jaeger all-in-one is live and local-only.

## For the docs lane (README lines needing update — README intentionally not edited here)

- ~45 and ~111-143: contract-test description still names the removed
  `tests/BdCommerce.Api.ContractTests/` project, its `ContractFixture`, the golden path under
  that project, and "21 contract tests / 31 integration tests". Authoritative layout is now
  `tests/BdCommerce.Api.Tests/OpenApiContractTests.cs` (27 contract cases + 9 existing = 36 total),
  golden at `tests/BdCommerce.Api.Tests/Golden/openapi-v1.json`.
- ~131-143: k6 section still describes the external 5 VU/30s script and its numbers; replace
  with the canonical ramping-vus figures above.
