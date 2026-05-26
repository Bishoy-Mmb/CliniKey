# Learning Journey: Testing in CliniKey

This folder teaches the testing strategy used in CliniKey from first principles,
grounded in the actual test files in the codebase.

## Reading Order

1. [concepts.md](./concepts.md) — What each test type is, why it exists, and when to use it
2. [unit-tests.md](./unit-tests.md) — How unit tests work in CliniKey using NSubstitute and FakeTimeProvider
3. [integration-tests.md](./integration-tests.md) — How Testcontainers spins up real PostgreSQL and what it proves
4. [e2e-thinking.md](./e2e-thinking.md) — What E2E means in this codebase and what comes next

## The Feature In One Sentence

CliniKey uses three test layers — unit, integration, and API — each testing a different
level of trust, and Testcontainers makes the integration layer run against a real
database without any shared state.

## Why Testing Here Feels Different From Tutorials

Most tutorials test a simple function that adds two numbers. CliniKey tests things like:

- "Does provisioning a tenant schema roll back cleanly if migrations fail?"
- "If 10 tenants write patients concurrently, does each one see only its own rows?"
- "Does a deactivated clinic block requests at the middleware layer?"

These questions can't be answered with mocks alone. They require a real database,
a real schema, and real SQL executing in the right order.

## The Mental Model

Think of the three layers as three different questions:

```
Unit Test          → "Does this logic make the right decision?"
Integration Test   → "Does this code work with a real database?"
E2E / API Test     → "Does this HTTP request produce the right response?"
```

Each layer has a different speed, cost, and level of confidence.

## What the Test Suite Contains (174 tests, all passing)

| Folder | Count | Type | What It Covers |
|--------|-------|------|----------------|
| `Domain/` | ~20 | Unit | Entity rules, value object validation |
| `Application/` | ~60 | Unit | Command/query handler orchestration |
| `Infrastructure/` | ~80 | Integration | Real DB, schema creation, isolation, migrations |
| `API/` | ~14 | Unit | Controller routing, response shape |

## Suggested Study Rhythm

1. Read [concepts.md](./concepts.md) to orient
2. Open [unit-tests.md](./unit-tests.md) side by side with [OnboardTenantCommandHandlerTests.cs](../../tests/CliniKey.Tests/Application/OnboardTenantCommandHandlerTests.cs)
3. Open [integration-tests.md](./integration-tests.md) side by side with [TenantProvisioningIntegrationTests.cs](../../tests/CliniKey.Tests/Infrastructure/TenantProvisioningIntegrationTests.cs)
4. Run `dotnet test` and watch the 51-second wall clock time — that's Docker starting
