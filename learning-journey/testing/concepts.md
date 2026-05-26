# Concepts: The Three Testing Layers

## Why Tests Exist at All

A test is a question you never want to answer manually again.

When you write `ProvisionAsync_WhenMigrationFails_DropsCreatedSchema`, you are writing
down the question: "If migrations blow up mid-provisioning, does the schema get cleaned
up?" Once it is a test, you never have to manually check this again. Every `dotnet test`
run re-answers it in milliseconds.

Tests also act as living documentation. Reading a test file tells you exactly what the
code is supposed to do, including failure cases, which the main code does not always
make obvious.

---

## The Three Layers

### Layer 1 — Unit Tests

**Question**: Does this logic make the right decision?

A unit test runs entirely in memory. No database. No HTTP. No file system.
All dependencies are replaced with fakes (mocks/stubs).

```
Test → Handler → (fake repository, fake provisioning service, fake clock)
                           ↓
                  Returns a fake result
                           ↓
              Assert the handler made the right decision
```

**Speed**: Milliseconds.
**What it proves**: The logic (branching, orchestration, error handling) is correct.
**What it does NOT prove**: That the code works with a real database.

**In CliniKey**, unit tests live in `Application/` and `Domain/` and `API/`.

---

### Layer 2 — Integration Tests

**Question**: Does this code work with a real database?

An integration test runs real infrastructure. In CliniKey that means a real
PostgreSQL database running inside a Docker container, managed by Testcontainers.

```
Test → Real service → Real NpgsqlDataSource → Real PostgreSQL (Docker)
                                ↓
                     Actual SQL executes
                                ↓
              Assert real rows exist / do not exist
```

**Speed**: Seconds (Docker startup + real queries).
**What it proves**: The SQL is correct, schemas are created properly, isolation works.
**What it does NOT prove**: That the HTTP layer routes correctly.

**In CliniKey**, integration tests live in `Infrastructure/`.

---

### Layer 3 — API / Controller Tests

**Question**: Does this HTTP request produce the right response shape?

An API test in CliniKey is a unit test of the controller. The MediatR `ISender`
is mocked, so no handler runs. The test only checks that:

- The controller calls the right command/query
- It maps the result to the right HTTP status code
- It returns the right response body shape

```
Test → Controller → (fake ISender returns fake Result)
                           ↓
              Assert HTTP 200 / 201 / 204 / 500
```

**Speed**: Milliseconds.
**What it proves**: The controller wiring and response mapping are correct.
**What it does NOT prove**: That the actual handler or database works.

**In CliniKey**, API tests live in `API/`.

---

## The Pyramid

```
         /\
        /  \         API Tests (fast, test HTTP shape only)
       /----\
      /      \       Unit Tests (fast, test logic only)
     /--------\
    /          \     Integration Tests (slow, test real DB behavior)
   /____________\
```

You want more unit tests than integration tests because unit tests are faster.
But for infrastructure concerns — schema creation, search path isolation, migrations —
only integration tests give you real confidence.

---

## The Key Packages Used in CliniKey

| Package | What it does |
|---------|-------------|
| `xUnit` | The test framework. Discovers and runs test methods marked `[Fact]` or `[Theory]` |
| `FluentAssertions` | Makes assertions read like English: `.Should().BeTrue()`, `.Should().BeEmpty()` |
| `NSubstitute` | Creates fake implementations of interfaces for unit tests |
| `Testcontainers.PostgreSql` | Starts and stops a real PostgreSQL Docker container per test class |
| `Microsoft.Extensions.Time.Testing` | Provides `FakeTimeProvider` so tests can control what "now" is |

---

## What Each Package Replaces

| Without the package | With the package |
|---------------------|-----------------|
| `Assert.True(result)` | `result.Should().BeTrue("because provisioning succeeded")` |
| Manual mock classes | `Substitute.For<IClinicRepository>()` |
| Real clock (`DateTime.UtcNow`) in tests | `FakeTimeProvider` set to a fixed date |
| Shared dev database (fragile) | Testcontainers spins up a fresh DB per test class |
