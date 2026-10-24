# GatewayActivity – Ocelot API Gateway with JWT

A Products service and an Orders service placed behind a single Ocelot API gateway.
The gateway hides the service ports, enforces JWT authentication (public reads, protected
writes and orders), and rate-limits the login route.

## Projects and ports

| Project | Port | Purpose |
|---|---|---|
| Gateway | 5000 | Ocelot gateway – the only address the client uses |
| ProductsService | 5001 | Products CRUD API (`/api/products`) |
| OrdersService | 5002 | In-memory orders API (`/api/orders`) |
| AuthService | 5003 | Issues JWTs at `POST /api/auth/login` |

The AuthService was built for this activity. The Gateway and AuthService share the
same `Jwt` Key, Issuer, and Audience in their `appsettings.json`, so tokens issued by
AuthService validate at the gateway.

Demo login: `{"username":"admin","password":"Password123!"}`

## How to run

```powershell
dotnet build
# start each project (four terminals), or:
foreach ($p in "ProductsService","OrdersService","AuthService","Gateway") {
    Start-Process powershell -ArgumentList "-NoExit","-Command","Set-Location '$PWD\$p'; dotnet run"
}
```

## Gateway routes (ocelot.json)

| Upstream (client calls) | Methods | Downstream | Protection |
|---|---|---|---|
| /gateway/products | GET | :5001 /api/products | Public |
| /gateway/products/{id} | GET | :5001 /api/products/{id} | Public |
| /gateway/products | POST, PUT, DELETE | :5001 /api/products | JWT (GatewayKey) |
| /gateway/products/{id} | PUT, DELETE | :5001 /api/products/{id} | JWT (GatewayKey) |
| /gateway/orders | GET | :5002 /api/orders | JWT (GatewayKey) |
| /gateway/auth/login | POST | :5003 /api/auth/login | Rate limited: 5 requests / 10 s |

Routes with the same path but different methods do not overlap, so no Priority values
were needed. The extra `/gateway/products/{id}` PUT/DELETE route was added so updates
and deletes by id are also forwarded and protected.

Additional feature configured: **rate limiting** on the login route.

## Test matrix (Part C) – all requests sent to http://localhost:5000 only

| Request (via the gateway) | Token | Expected | Observed |
|---|---|---|---|
| GET /gateway/products | none | 200 | 200 |
| GET /gateway/products/1 | none | 200 | 200 |
| POST /gateway/auth/login (correct password) | none | 200 + token | 200 + token |
| POST /gateway/products | none | 401 | 401 |
| POST /gateway/products | valid | 201 | 201 |
| GET /gateway/orders | none | 401 | 401 |
| GET /gateway/orders | valid | 200 | 200 |
| POST /gateway/auth/login x6 quickly | none | 6th returns 429 | attempts 1–5: 200, attempt 6: 429 |

Raw results are saved in `test-results.csv`.

## Which requests does the gateway reject before they reach a service, and why?

1. **Unauthenticated writes to products (POST/PUT/DELETE) → 401.** These routes have
   `AuthenticationOptions` with the `GatewayKey` JWT scheme. Ocelot validates the token's
   signature, issuer, audience, and expiry before forwarding. With no valid token, the request
   is rejected at the gateway and never reaches ProductsService on port 5001.

2. **Unauthenticated `GET /gateway/orders` → 401.** Orders are protected the same way, so
   requests without a valid token never reach OrdersService on port 5002.

3. **Login attempts beyond 5 within 10 seconds → 429.** The login route has
   `RateLimitOptions` (Limit 5, Period 10s). The 6th request is throttled by the gateway and
   never reaches AuthService on port 5003. This protects the login endpoint from brute-force
   password guessing.

Public `GET` requests for products have no authentication options, so the gateway forwards
them freely. In every case the client only knows `http://localhost:5000`; ports 5001–5003
are never called directly.
'@
