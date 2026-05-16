# CliniKey

<p align="center">
  <img src="docs/clinikey-banner.png" alt="CliniKey Banner" width="100%" />
</p>

<p align="center">
  <strong>Production-Grade Dental Clinic Management SaaS</strong><br/>
  Architected with Domain-Driven Design (DDD), CQRS, and Clean Architecture on .NET 10.
</p>

<p align="center">
  <em>Built with discipline, not deadlines. Every pattern is intentional. Every error is handled.</em>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet" alt=".NET 10"/>
  <img src="https://img.shields.io/badge/C%23-13-239120?style=flat-square&logo=csharp" alt="C# 13"/>
  <img src="https://img.shields.io/badge/PostgreSQL-Schema--per--Tenant-4169E1?style=flat-square&logo=postgresql" alt="PostgreSQL"/>
  <img src="https://img.shields.io/badge/Architecture-DDD%20%2B%20CQRS-blue?style=flat-square" alt="Architecture"/>
  <img src="https://img.shields.io/badge/Tests-94%20Passing-2ea043?style=flat-square" alt="Tests"/>
  <img src="https://img.shields.io/badge/Build-0%20Warnings-2ea043?style=flat-square" alt="Build"/>
</p>

---

## Overview

CliniKey is a dental clinic management platform built for the Egyptian market. It enforces real DB-level multi-tenancy via PostgreSQL schema-per-tenant, validates FDI tooth codes (ISO 3950), tracks split payments across Cash, Visa, InstaPay, and Fawry with per-line 14% VAT snapshots, and drives clinical workflows through strict state machines — all without a single anemic model or unhandled exception path. Every design choice, from the `Result<T>` pattern to the MediatR pipeline, is optimized for **maintainability**, **auditability**, and **explicit domain logic**.

---

## Architecture

```mermaid
%%{init: {
  "theme": "base",
  "themeVariables": {
    "fontSize": "14px",
    "fontFamily": "ui-sans-serif, system-ui, sans-serif"
  },
  "flowchart": {
    "nodeSpacing": 20,
    "rankSpacing": 60,
    "padding": 24,
    "curve": "basis"
  }
}}%%
flowchart TD
    subgraph API["API Layer"]
        A1(["Controllers"]) ~~~ A2(["Middleware"]) ~~~ A3(["Result → ProblemDetails"])
    end

    subgraph APP["Application Layer"]
        B1(["CQRS Commands/Queries"]) ~~~ B2(["MediatR Pipeline"]) ~~~ B3(["FluentValidation"])
    end

    subgraph DOM["Domain Layer"]
        C1(["Aggregate Roots"]) ~~~ C2(["Value Objects"]) ~~~ C3(["Domain Events"]) ~~~ C4(["Result(T) Pattern"])
    end

    subgraph INF["Infrastructure Layer"]
        D1(["EF Core"]) ~~~ D2(["PostgreSQL"]) ~~~ D3(["Repositories"]) ~~~ D4(["UnitOfWork"])
    end

    subgraph SK["SharedKernel"]
        E1(["Entity(T)"]) ~~~ E2(["AggregateRoot(T)"]) ~~~ E3(["ValueObject"]) ~~~ E4(["Error"])
    end

    API --> APP
    APP --> DOM
    DOM --> INF
    INF --> SK

    style API fill:#0D5C47,stroke:#0D5C47,color:#A8DECE,rx:16
    style APP fill:#2B5A10,stroke:#2B5A10,color:#B8D98A,rx:16
    style DOM fill:#6B3D08,stroke:#6B3D08,color:#F5C87A,rx:16
    style INF fill:#2D2472,stroke:#2D2472,color:#C4C0F8,rx:16
    style SK  fill:#7A2E14,stroke:#7A2E14,color:#F5BBA8,rx:16

    style A1 fill:#0F6E56,stroke:#5DCAA5,color:#E1F5EE,rx:20
    style A2 fill:#0F6E56,stroke:#5DCAA5,color:#E1F5EE,rx:20
    style A3 fill:#0F6E56,stroke:#5DCAA5,color:#E1F5EE,rx:20

    style B1 fill:#3B6D11,stroke:#97C459,color:#EAF3DE,rx:20
    style B2 fill:#3B6D11,stroke:#97C459,color:#EAF3DE,rx:20
    style B3 fill:#3B6D11,stroke:#97C459,color:#EAF3DE,rx:20

    style C1 fill:#854F0B,stroke:#EF9F27,color:#FAEEDA,rx:20
    style C2 fill:#854F0B,stroke:#EF9F27,color:#FAEEDA,rx:20
    style C3 fill:#854F0B,stroke:#EF9F27,color:#FAEEDA,rx:20
    style C4 fill:#854F0B,stroke:#EF9F27,color:#FAEEDA,rx:20

    style D1 fill:#3C3489,stroke:#AFA9EC,color:#EEEDFE,rx:20
    style D2 fill:#3C3489,stroke:#AFA9EC,color:#EEEDFE,rx:20
    style D3 fill:#3C3489,stroke:#AFA9EC,color:#EEEDFE,rx:20
    style D4 fill:#3C3489,stroke:#AFA9EC,color:#EEEDFE,rx:20

    style E1 fill:#993C1D,stroke:#F0997B,color:#FAECE7,rx:20
    style E2 fill:#993C1D,stroke:#F0997B,color:#FAECE7,rx:20
    style E3 fill:#993C1D,stroke:#F0997B,color:#FAECE7,rx:20
    style E4 fill:#993C1D,stroke:#F0997B,color:#FAECE7,rx:20
```

### Layer Responsibilities

```text
src/
├── CliniKey.SharedKernel/     # DDD primitives: Entity<T>, AggregateRoot<T>, Result<T>, Error
├── CliniKey.Domain/           # Pure C# — Business rules, Aggregates, and Value Objects
├── CliniKey.Application/      # CQRS Handlers, Pipeline Behaviors, and DTOs
├── CliniKey.Infrastructure/   # EF Core, PostgreSQL Repositories, and Multi-tenancy
└── CliniKey.API/              # Thin HTTP Adapter — Controllers delegating to MediatR
```

### Core Principles

- **Inward Dependency Flow**: Domain never references Infrastructure. Enforced by project structure.
- **CQRS**: Clean separation of write-side transactions (commands) and read-side performance (Dapper queries).
- **Explicit Aggregate Boundaries**: Encapsulated state transitions with zero "anemic" models.
- **Multi-tenant Isolation**: Every database operation is scoped to the active tenant's PostgreSQL schema. There is no application-level filtering — the wrong schema is structurally unreachable. This is the most security-critical guarantee in the system: a misconfigured query cannot leak data across tenants because the schema boundary makes it architecturally impossible, not just unlikely.

---

## Core Domain Logic

The system models the complete dental clinic lifecycle across five aggregates:

```mermaid
%%{init: {
  "theme": "base",
  "themeVariables": {
    "fontSize": "13px",
    "fontFamily": "ui-sans-serif, system-ui, sans-serif"
  },
  "flowchart": {
    "nodeSpacing": 8,
    "rankSpacing": 32,
    "padding": 16,
    "curve": "basis"
  }
}}%%
flowchart LR
    P(["Patient"])
    --> A(["Appointment"])
    --> T(["Treatment Plan"])
    --> I(["Invoice"])
    --> PY(["Payment"])

    style P  fill:#0D5C47,stroke:#5DCAA5,color:#E1F5EE,rx:20
    style A  fill:#2B5A10,stroke:#97C459,color:#EAF3DE,rx:20
    style T  fill:#6B3D08,stroke:#EF9F27,color:#FAEEDA,rx:20
    style I  fill:#2D2472,stroke:#AFA9EC,color:#EEEDFE,rx:20
    style PY fill:#7A2E14,stroke:#F0997B,color:#FAECE7,rx:20
```

### Domain-Driven Payment Handling

```csharp
public Result RecordPayment(Money amount, PaymentMethod method)
{
    if (Status == InvoiceStatus.Paid)
        return Result.Failure(InvoiceErrors.AlreadyPaid);

    var remaining = CalculateTotal().Value.Amount - CalculatePaidAmount().Value.Amount;

    if (amount.Amount > remaining)
        return Result.Failure(InvoiceErrors.Overpayment);

    _payments.Add(new Payment(amount, method, DateTime.UtcNow));

    Status = CalculatePaidAmount().Value.Amount >= CalculateTotal().Value.Amount
        ? InvoiceStatus.Paid
        : InvoiceStatus.PartiallyPaid;

    MarkUpdated();
    return Result.Success();
}
```

---

## Tech Stack

| Area | Technology |
|------|------------|
| **Runtime** | .NET 10 / C# 13 |
| **Messaging** | MediatR 14.x |
| **Validation** | FluentValidation |
| **ORM / Data** | EF Core + Dapper |
| **Database** | PostgreSQL |
| **Testing** | xUnit / Testcontainers |

---

## Request Pipeline

Every command and query passes through a rigorous, ordered pipeline. Queries bypass the transaction behavior automatically via the `IBaseCommand` marker interface.

```mermaid
%%{init: {
  "theme": "base",
  "themeVariables": {
    "fontSize": "13px",
    "fontFamily": "ui-sans-serif, system-ui, sans-serif"
  },
  "flowchart": {
    "nodeSpacing": 8,
    "rankSpacing": 32,
    "padding": 16,
    "curve": "basis"
  }
}}%%
flowchart LR
    R(["Request"])
    --> L(["Logging"])
    --> V(["Validation"])
    --> T(["Transaction"])
    --> H(["Handler"])
    --> P(["Response"])

    style R fill:#1e293b,stroke:#475569,color:#94a3b8,rx:20
    style L fill:#1e293b,stroke:#475569,color:#94a3b8,rx:20
    style V fill:#1e293b,stroke:#475569,color:#94a3b8,rx:20
    style T fill:#1e293b,stroke:#475569,color:#94a3b8,rx:20
    style H fill:#0D5C47,stroke:#5DCAA5,color:#E1F5EE,rx:20
    style P fill:#1e293b,stroke:#475569,color:#94a3b8,rx:20
```

---

## Testing

```text
94 Tests • 0 Failures • 0 Build Warnings
```

Tests are organized around three concerns, each with a distinct strategy:

| Concern | Strategy | Tool |
|---------|----------|------|
| **Domain Invariants** | Exhaustive state transition and value object validation | xUnit |
| **Tenant Isolation** | Real PostgreSQL instances verify Tenant A cannot read Tenant B data | Testcontainers |
| **CQRS Handlers** | Unit tests with mocked repositories for use-case orchestration | NSubstitute |

All tests follow the `Method_Scenario_ExpectedResult` naming convention for immediate legibility at a glance:

- `RecordPayment_Overpayment_ReturnsFailure`
- `CheckIn_FromCompleted_ReturnsInvalidTransition`
- `Create_ValidFDICode_ReturnsSuccess`

---

## Development Process

CliniKey follows **Spec-Driven Development (SDD)**. No code is shipped without a specification. Every phase is documented in an **Execution Summary Record (ESR)** — a traceable audit trail of architectural decisions, trade-offs, and build status for each feature shipped.

```mermaid
%%{init: {
  "theme": "base",
  "themeVariables": {
    "fontSize": "13px",
    "fontFamily": "ui-sans-serif, system-ui, sans-serif"
  },
  "flowchart": {
    "nodeSpacing": 8,
    "rankSpacing": 32,
    "padding": 16,
    "curve": "basis"
  }
}}%%
flowchart LR
    S(["Specification"])
    --> PL(["Planning"])
    --> T(["Tasks"])
    --> I(["Implementation"])
    --> R(["Review"])
    --> E(["ESR"])

    style S  fill:#1e293b,stroke:#475569,color:#94a3b8,rx:20
    style PL fill:#1e293b,stroke:#475569,color:#94a3b8,rx:20
    style T  fill:#1e293b,stroke:#475569,color:#94a3b8,rx:20
    style I  fill:#0D5C47,stroke:#5DCAA5,color:#E1F5EE,rx:20
    style R  fill:#1e293b,stroke:#475569,color:#94a3b8,rx:20
    style E  fill:#1e293b,stroke:#475569,color:#94a3b8,rx:20
```

---

## Project Metrics

| Metric | Value |
|--------|-------|
| **Lines of Code** | ~4,300 |
| **API Endpoints** | 15 |
| **Unit Tests** | 94 |
| **Build Warnings** | 0 |

---

## Getting Started

```bash
# Clone
git clone https://github.com/Bishoy-Mmb/CliniKey.git
cd CliniKey

# Build (expect 0 warnings)
dotnet build CliniKey.slnx

# Run unit tests
dotnet test CliniKey.slnx --filter "Category!=Integration"

# Run all tests (requires Docker)
dotnet test CliniKey.slnx
```

---

## License

This project is for portfolio and educational purposes. All rights reserved.

---

<p align="center">
  <sub>Built with discipline, not deadlines. Every pattern is intentional. Every error is handled.</sub>
</p>