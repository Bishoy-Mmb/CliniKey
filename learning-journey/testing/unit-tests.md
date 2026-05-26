# Unit Tests in CliniKey

Open [OnboardTenantCommandHandlerTests.cs](../../tests/CliniKey.Tests/Application/OnboardTenantCommandHandlerTests.cs)
alongside this document.

---

## What a Unit Test Is Doing

A unit test for a command handler is answering the question:
**"Given these inputs and these conditions, does the handler do the right thing?"**

It does not care about databases, HTTP, or Docker. It replaces every dependency with
a fake and controls exactly what those fakes return.

---

## The Setup Pattern

Every unit test class in CliniKey follows the same structure:

```csharp
public class OnboardTenantCommandHandlerTests
{
    // 1. Declare all dependencies as fields
    private readonly IClinicRepository _clinicRepository;
    private readonly ITenantProvisioningService _tenantProvisioningService;
    private readonly FakeTimeProvider _clock;
    private readonly OnboardTenantCommandHandler _handler;

    // 2. Constructor runs before EVERY test
    public OnboardTenantCommandHandlerTests()
    {
        // Create fakes for every interface
        _clinicRepository = Substitute.For<IClinicRepository>();
        _tenantProvisioningService = Substitute.For<ITenantProvisioningService>();

        // Control time exactly
        _clock = new FakeTimeProvider(new DateTimeOffset(2026, 5, 23, 10, 0, 0, TimeSpan.Zero));

        // Build the real handler with fake dependencies
        _handler = new OnboardTenantCommandHandler(
            _clinicRepository,
            _tenantProvisioningService,
            ...
            _clock);
    }
}
```

**Key insight**: The constructor runs fresh for every single test. Each test gets its
own clean set of fakes. Tests cannot pollute each other.

---

## NSubstitute — Faking Interfaces

`Substitute.For<IClinicRepository>()` creates an object that implements
`IClinicRepository` but does nothing by default. You then configure it:

```csharp
// Make ExistsByPhoneAsync return false (no duplicate)
_clinicRepository
    .ExistsByPhoneAsync(Arg.Any<PhoneNumber>(), null, Arg.Any<CancellationToken>())
    .Returns(false);
```

`Arg.Any<T>()` means "match any value of this type". You use it when you don't care
about the exact argument, only the return value.

You can also **verify** that a method was or was not called:

```csharp
// Assert the repository Add method was called exactly once
_clinicRepository.Received(1).Add(Arg.Any<Clinic>());

// Assert provisioning was never called (because phone was duplicate)
await _tenantProvisioningService
    .DidNotReceive()
    .ProvisionAsync(Arg.Any<Tenant>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
```

This is powerful. You are not just testing the return value — you are testing the
**side effects** (what the handler called and what it did not call).

---

## FakeTimeProvider — Controlling Time

The real system uses `TimeProvider` (injected) instead of `DateTime.UtcNow` directly.
This is what makes tests possible:

```csharp
// In tests: time is frozen at this exact moment
_clock = new FakeTimeProvider(new DateTimeOffset(2026, 5, 23, 10, 0, 0, TimeSpan.Zero));

// In production code: uses the injected TimeProvider
user.InitializeCreatedAt(_clock.GetUtcNow().UtcDateTime);
```

If you had used `DateTime.UtcNow` directly in production code, tests would get a
different timestamp every time they ran — making assertions on dates impossible.
`FakeTimeProvider` freezes the clock so tests are deterministic.

---

## The Three Test Cases — Reading the Handler Tests

### Test 1: Happy path

```csharp
[Fact]
public async Task Handle_NoConflictAndProvisioningSucceeds_ReturnsProvisionedClinic()
```

Setup:
- Phone does not exist → `ExistsByPhoneAsync` returns `false`
- Provisioning succeeds → `ProvisionAsync` returns `Result.Success(...)`

Asserts:
- Result is success
- Response has correct name, phone, schema prefix
- `TenantRepository.Add` was called once
- `ClinicRepository.Add` was called once
- `UnitOfWork.SaveChangesAsync` was called twice (once before provisioning, once after)

This test proves the happy path orchestration is correct.

---

### Test 2: Duplicate phone — fail fast

```csharp
[Fact]
public async Task Handle_DuplicatePhone_ReturnsConflictWithoutProvisioning()
```

Setup:
- Phone exists → `ExistsByPhoneAsync` returns `true`

Asserts:
- Result is failure with `TenantErrors.DuplicatePhone`
- `ClinicRepository.Add` was **never** called
- `TenantRepository.Add` was **never** called
- `ProvisionAsync` was **never** called

This test proves the handler short-circuits correctly on duplicate phone
without touching the database or provisioning infrastructure.

---

### Test 3: Provisioning failure — compensation

```csharp
[Fact]
public async Task Handle_ProvisioningFails_RemovesClinicAndReturnsFailure()
```

This is the most interesting test. It uses a NSubstitute capture pattern:

```csharp
Clinic? addedClinic = null;
_clinicRepository
    .When(x => x.Add(Arg.Any<Clinic>()))
    .Do(call => addedClinic = call.Arg<Clinic>()); // capture what was added
```

Setup:
- Phone does not exist
- `ProvisionAsync` returns `Result.Failure`

Asserts:
- Result is failure with `TenantErrors.ProvisioningFailed`
- The exact clinic that was added was then **removed** (`ClinicRepository.Remove(addedClinic)`)
- The exact tenant that was added was then **removed** (`TenantRepository.Remove(addedTenant)`)
- `SaveChangesAsync` was still called twice (add, then compensate)

This test proves the rollback compensation logic fires correctly when provisioning
fails. Without this test, you would not know if partial state is left in the database.

---

## FluentAssertions — Reading Assertions

```csharp
result.IsSuccess.Should().BeTrue();
result.Value.SchemaName.Should().StartWith("tenant_");
result.Value.SchemaName.Should().HaveLength(39);
result.Error.Should().Be(TenantErrors.DuplicatePhone);
patientsInB.Should().BeEmpty("tenant B must not see tenant A rows");
counts.Should().OnlyContain(count => count == 1);
```

The string in `BeEmpty("...")` is the failure message — if the assertion fails,
xUnit prints that message so you know instantly why.

---

## What Unit Tests Cannot Tell You

Unit tests for the handler above **cannot** verify:
- That the SQL actually executes correctly in PostgreSQL
- That the schema name is unique across concurrent provisioning calls
- That the search path isolation actually works under connection pooling

Those questions require real infrastructure. That is why integration tests exist.
