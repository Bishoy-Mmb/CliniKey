# E2E Thinking in CliniKey

## What "End-to-End" Actually Means

E2E testing means testing the full stack from the HTTP request through to the
database and back, as close to production as possible.

In a web API context, a true E2E test does this:

```
HTTP POST /api/v1/tenants
    ↓
Authentication middleware (JWT validation)
    ↓
TenantResolutionMiddleware
    ↓
TenantsController.OnboardTenant()
    ↓
MediatR → OnboardTenantCommandHandler
    ↓
TenantProvisioningService → real PostgreSQL
    ↓
201 Created { tenantId, clinicId }
```

Every layer in the real application runs. No mocking.

---

## What CliniKey Has Today vs True E2E

### What exists today

| Layer | Test Type | File |
|-------|-----------|------|
| Domain logic | Unit | `Domain/ClinicTests.cs` |
| Handler orchestration | Unit | `Application/OnboardTenantCommandHandlerTests.cs` |
| Schema creation / rollback | Integration (Testcontainers) | `Infrastructure/TenantProvisioningIntegrationTests.cs` |
| Tenant isolation under concurrency | Integration (Testcontainers) | `Infrastructure/TenantConcurrentIsolationTests.cs` |
| Controller routing / response shape | Unit (mocked sender) | `API/TenantsControllerTests.cs` |
| Middleware auth bypass logic | Unit (mocked registry) | `API/TenantResolutionMiddlewareTests.cs` |

### What is NOT tested yet

| Gap | What it would catch |
|-----|---------------------|
| Full HTTP request cycle | Middleware + routing + handler + DB in one shot |
| JWT auth enforcement on endpoints | A request without a valid token returns 401 |
| Policy enforcement on endpoints | A ClinicAdmin JWT gets 403 on `/api/v1/tenants` |
| Real login → use token → onboard clinic | The entire operator onboarding flow as HTTP |

---

## How True E2E Tests Would Be Built in ASP.NET Core

ASP.NET Core provides `WebApplicationFactory<T>` — it boots your entire application
in memory, including all middleware, routing, and DI, but lets you swap the real
database for a test one.

```csharp
// A shared test application factory
public class CliniKeyWebAppFactory : WebApplicationFactory<Program>
{
    private readonly PostgreSqlContainer _postgres =
        new PostgreSqlBuilder("postgres:16-alpine").Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace the real DB connection string with the test container one
            services.Configure<ConnectionStrings>(opts =>
                opts.Database = _postgres.GetConnectionString());
        });
    }
}
```

A test then uses an `HttpClient` to hit real endpoints:

```csharp
[Fact]
public async Task OnboardTenant_WithPlatformOperatorToken_Returns201()
{
    var client = _factory.CreateClient();

    // 1. Login to get a real JWT
    var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
    {
        email = "operator@clinikey.local",
        password = "CliniKeyDev#12345"
    });
    var tokens = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();

    // 2. Use the JWT to onboard a tenant
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);

    var onboardResponse = await client.PostAsJsonAsync("/api/v1/tenants", new
    {
        name = "E2E Test Clinic",
        phone = "01199999999",
        address = "E2E Test Address"
    });

    onboardResponse.StatusCode.Should().Be(HttpStatusCode.Created);
    var body = await onboardResponse.Content.ReadFromJsonAsync<OnboardTenantResponse>();
    body!.TenantId.Should().NotBe(Guid.Empty);
}
```

This test:
- Boots the entire ASP.NET Core pipeline
- Runs real JWT validation
- Runs real middleware
- Hits a real PostgreSQL database (via Testcontainers)
- Returns a real HTTP response

---

## Why CliniKey Does Not Have This Yet

E2E tests with `WebApplicationFactory` are powerful but have a setup cost:

1. The app has multiple DbContexts (`AuthDbContext`, `SharedDbContext`, `AppDbContext`)
   — each needs a test-aware connection string
2. The dev seed (PlatformOperator user, dev tenant) needs to run inside the test factory
3. JWT keys, issuer, and audience must be configured correctly in the test environment
4. The test factory needs to be shared across test classes efficiently

None of this is hard. But it is a 1-2 day investment to do cleanly. For Spec 003,
the existing unit + integration test coverage is sufficient because:

- The HTTP layer is thin (controllers just call MediatR and map results)
- The provisioning logic is fully covered by infrastructure integration tests
- Manual testing through Scalar validated the full flow

---

## When to Add E2E Tests

E2E tests become high-value when:

- You have complex authorization rules you want to enforce by HTTP test
  (e.g., "a ClinicAdmin calling `/api/v1/tenants` must get 403")
- You add a multi-step workflow where the order of HTTP calls matters
- You approach production and want a regression safety net for the full stack

A good target for the next spec: add one `WebApplicationFactory` setup, one login
test, and one "happy path onboarding" E2E test. Once the factory exists, adding
more E2E tests is cheap.

---

## The Testing Confidence Map for Spec 003

```
Domain rules              ████████████  Unit tests
Handler orchestration     ████████████  Unit tests
Provisioning / rollback   ████████████  Integration tests (Testcontainers)
Schema isolation          ████████████  Integration tests (Testcontainers)
Concurrent isolation      ████████████  Integration tests (Testcontainers)
Migration service         ████████████  Integration tests (Testcontainers)
Controller routing        ████████████  Unit tests (mocked sender)
Middleware bypass logic   ████████████  Unit tests (mocked registry)
─────────────────────────────────────────────────────
Full HTTP request cycle   ░░░░░░░░░░░░  Not yet (covered manually via Scalar)
JWT auth enforcement      ░░░░░░░░░░░░  Not yet
Policy enforcement        ░░░░░░░░░░░░  Not yet
```

The gaps are real but low risk for V1. They are the right next investment for Spec 004.
