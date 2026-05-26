# Integration Tests and Testcontainers in CliniKey

Open these files alongside this document:
- [TenantProvisioningIntegrationTests.cs](../../tests/CliniKey.Tests/Infrastructure/TenantProvisioningIntegrationTests.cs)
- [TenantSchemaSwitchingTests.cs](../../tests/CliniKey.Tests/Infrastructure/TenantSchemaSwitchingTests.cs)
- [TenantConcurrentIsolationTests.cs](../../tests/CliniKey.Tests/Infrastructure/TenantConcurrentIsolationTests.cs)

---

## Why Integration Tests Exist

Unit tests proved the logic. Integration tests prove the infrastructure.

The question they answer is: **"When this code runs against a real PostgreSQL database,
does it actually work?"**

This matters because:
- SQL syntax errors only appear at runtime against a real database
- Schema isolation only works if `search_path` is set correctly on every connection
- Advisory locks only behave correctly under real concurrent connections
- EF Core migrations only apply cleanly to a real PostgreSQL schema

You cannot mock your way to confidence about any of these things.

---

## What Is Testcontainers?

Testcontainers is a library that starts a real Docker container programmatically,
from inside your test code, using the Docker daemon on your machine.

For PostgreSQL it works like this:

```csharp
// Declare the container — this does NOT start it yet
private readonly PostgreSqlContainer _postgres =
    new PostgreSqlBuilder("postgres:16-alpine").Build();
```

`postgres:16-alpine` is the Docker image name. Alpine is a minimal Linux image.
This produces a tiny, fast container (~50MB).

---

## IAsyncLifetime — The Lifecycle Contract

Every integration test class implements `IAsyncLifetime`:

```csharp
public sealed class TenantProvisioningIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres =
        new PostgreSqlBuilder("postgres:16-alpine").Build();

    // xUnit calls this BEFORE any test in this class runs
    public async Task InitializeAsync()
    {
        await _postgres.StartAsync(); // Docker container starts here
    }

    // xUnit calls this AFTER all tests in this class finish
    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync(); // Container is destroyed here
    }
}
```

**One container per test class**. All tests inside the class share the same container.
This is faster than one container per test but still gives isolation between classes.

After `StartAsync()`, you get a real connection string:

```csharp
_postgres.GetConnectionString()
// → "Host=localhost;Port=49153;Database=postgres;Username=postgres;Password=..."
```

The port is random (Docker assigns it). Testcontainers handles this transparently.

---

## What The Provisioning Test Proves

### Test 1: Schema creation and baseline migration

```csharp
[Fact]
public async Task ProvisionAsync_CreatesSchemaAndAppliesBaselineMigration()
```

Step by step:

```
1. Create a SharedDbContext pointing at the Testcontainers PostgreSQL
2. EnsureCreated() — builds the shared schema tables in the real DB
3. Create a Tenant and Clinic entity, save to shared DB
4. Create a real NpgsqlDataSource
5. Create the real TenantMigrationService (no mocks)
6. Create the real TenantProvisioningService (no mocks)
7. Call ProvisionAsync(tenant, null)
8. Assert result is success
9. Query information_schema — does the schema physically exist?
10. Query __EFMigrationsHistory in that schema — was the migration recorded?
```

The assertions at the end query PostgreSQL's own system tables:

```csharp
private async Task<bool> SchemaExistsAsync(string schemaName)
{
    // Queries information_schema.schemata — PostgreSQL's own catalog
    var command = new NpgsqlCommand(
        "SELECT EXISTS (SELECT 1 FROM information_schema.schemata WHERE schema_name = @schema_name)",
        connection);
}
```

This is proof. The schema physically exists in the database.

---

### Test 2: Rollback compensation

```csharp
[Fact]
public async Task ProvisionAsync_WhenMigrationFails_DropsCreatedSchema()
```

This uses a `FailingTenantMigrationService` — a private fake class defined inside
the test file itself:

```csharp
private sealed class FailingTenantMigrationService : ITenantMigrationService
{
    public Task<Result<string?>> ApplyMigrationsAsync(string schemaName, ...)
    {
        return Task.FromResult(Result.Failure<string?>(TenantErrors.MigrationFailed));
    }
}
```

This is not NSubstitute. It is a hand-written fake implementing the interface.
The test uses this fake to simulate a migration failure, then asserts:

```csharp
result.IsFailure.Should().BeTrue();
(await SchemaExistsAsync(tenant.SchemaName)).Should().BeFalse();
```

The schema must not exist after a failed provision. If the provisioning service
forgot to drop the schema on failure, this test catches it.

This is a compensation test — it proves the rollback logic works against
a real database, not just against a mock.

---

## What The Schema Switching Test Proves

Open [TenantSchemaSwitchingTests.cs](../../tests/CliniKey.Tests/Infrastructure/TenantSchemaSwitchingTests.cs).

```csharp
[Fact]
public async Task EfCore_ResolvedTenantSearchPath_IsolatesPatientsBySchema()
```

This test:
1. Creates two tenant schemas: `tenant_ef_a` and `tenant_ef_b`
2. Applies real operational migrations to both (so patients table exists)
3. Creates an `AppDbContext` for `tenant_ef_a` with the `TenantConnectionInterceptor`
4. Saves a patient into `tenant_ef_a`
5. Creates a separate `AppDbContext` for `tenant_ef_b`
6. Queries patients from `tenant_ef_b`
7. Asserts the list is empty

```csharp
patientsInB.Should().BeEmpty(
    "tenant B must not see tenant A rows through a pooled EF connection");
```

The assertion message explains exactly why it matters: **pooled connections**.
When you use connection pooling, the same physical connection might be reused
for different tenants. The `TenantConnectionInterceptor` must set `search_path`
on every connection open, not just the first time. This test proves it does.

How the interceptor is wired into the test context:

```csharp
private AppDbContext CreateContext(string schemaName)
{
    var tenantContext = new TenantContext();
    tenantContext.Resolve(Guid.NewGuid(), schemaName, TenantStatus.Active, ...);

    var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseNpgsql(_postgres.GetConnectionString())
        .AddInterceptors(new TenantConnectionInterceptor(tenantContext)) // ← key line
        .Options;

    return new AppDbContext(options);
}
```

`TenantContext` is the scoped object that holds the resolved tenant for a request.
`TenantConnectionInterceptor` reads from it and issues `SET search_path = tenant_ef_a`
every time EF Core opens a database connection.

---

## What The Concurrent Isolation Test Proves

Open [TenantConcurrentIsolationTests.cs](../../tests/CliniKey.Tests/Infrastructure/TenantConcurrentIsolationTests.cs).

```csharp
[Fact]
public async Task EfCore_ConcurrentRequestsAcrossTenTenants_DoNotShareSearchPathState()
```

This is the hardest test to understand but the most important:

```csharp
// Create 10 schemas: tenant_00, tenant_01, ... tenant_09
var schemas = Enumerable.Range(0, 10).Select(i => $"tenant_{i:00}").ToArray();

// Apply migrations to all 10
foreach (var schema in schemas) { ... }

// Insert one patient into each schema — all 10 running concurrently
await Task.WhenAll(schemas.Select((schema, index) => InsertPatientAsync(schema, index)));

// Count patients in each schema — all 10 running concurrently
var counts = await Task.WhenAll(schemas.Select(CountPatientsAsync));

// Every schema must have exactly 1 patient — no cross-contamination
counts.Should().OnlyContain(count => count == 1);
```

`Task.WhenAll` runs all 10 inserts simultaneously. If `search_path` state leaks
between concurrent connections — which it can if pooling is not handled correctly —
one tenant might write into another tenant's schema. The count assertion catches this.

This test runs against a real database with real concurrency. No mock can simulate this.

---

## The `[Trait]` Attribute

```csharp
[Trait("Category", "Integration")]
public sealed class TenantProvisioningIntegrationTests : IAsyncLifetime
```

This tag does nothing by default but lets you filter tests:

```bash
# Run only integration tests (slower, requires Docker)
dotnet test --filter "Category=Integration"

# Run everything except integration tests (fast feedback loop)
dotnet test --filter "Category!=Integration"
```

This is useful in CI pipelines or when you want fast unit test feedback while
working on logic, and only run integration tests before committing.

---

## Why Tests Take 51 Seconds

The 51 seconds you saw in the test run is almost entirely Docker startup time.

- Docker pulls `postgres:16-alpine` on first run (once, then cached)
- Each test class starts its own container
- Starting PostgreSQL inside Docker takes ~2-5 seconds per class
- With ~8 integration test classes, that is 16-40 seconds of startup
- The actual test SQL runs in milliseconds

On subsequent runs the image is cached. The startup cost is just the container
boot time, not image download.

---

## Common Questions

**Q: Do tests share the same container?**

No. Each test class gets its own container via `IAsyncLifetime`. Tests inside
the same class share one container, but classes are isolated.

**Q: What if Docker is not running?**

Integration tests will fail with a connection error during `InitializeAsync`.
You need Docker Desktop (or Docker Engine on Linux) running.

**Q: Can I run integration tests without Docker?**

Not with Testcontainers. If you need to run tests without Docker, use the
`--filter "Category!=Integration"` flag to skip them.

**Q: Does each test class get a fresh database?**

Yes. A new container means a new PostgreSQL instance with no schemas,
no tables, and no data. The test sets up exactly what it needs.
