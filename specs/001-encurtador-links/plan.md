# Implementation Plan: Encurtador de Links

**Branch**: `001-encurtador-links` | **Date**: 2026-10-17 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/001-encurtador-links/spec.md`, plus the requested technical decisions: .NET 10, C#, Minimal APIs, one endpoint file per resource, EF Core with SQLite and a provider boundary for a later PostgreSQL migration, an application `LinkService` as the owner of business rules, xUnit, database-enforced code uniqueness in addition to service checks, atomic database-side click increments, no ambiguous symbols in generated codes, and no management key.

## Summary

Deliver a persistent short-link service that accepts an HTTP(S) destination, creates either a seven-character generated code or a four-to-twenty-character custom code, redirects requests, and reports the accumulated click count. Implement a .NET 10 Minimal API with resource-oriented endpoint mapping, an application-layer `LinkService`, and EF Core persistence through SQLite. Enforce code uniqueness with both service-level checks and a database unique index; increment clicks with a database-side atomic update. Generated codes use a cryptographically random, unambiguous 30-character alphabet. No management key or other account-like credential is included; statistics remain available to anyone who knows the code.

## Technical Context

**Language/Version**: C# / .NET 10

**Primary Dependencies**: ASP.NET Core Minimal APIs; EF Core 10; EF Core SQLite provider; EF Core design-time tooling for repeatable migrations; xUnit and the .NET test SDK. PostgreSQL support is a future provider substitution, not part of this delivery.

**Storage**: SQLite in a persistent file, accessed through EF Core. Persistence configuration and data access remain behind Infrastructure so a later PostgreSQL provider can replace SQLite without moving business rules.

**Testing**: xUnit unit tests for application rules and integration tests using a temporary SQLite database for persistence, HTTP contracts, collision behavior, restart persistence, and concurrent click increments.

**Target Platform**: .NET 10 server deployment with durable storage for the SQLite file.

**Project Type**: HTTP web service

**Performance Goals**: No numeric latency or throughput target was specified. Correct redirects, durable data, unique codes, and lossless concurrent increments take priority; record baseline test timings without creating an SLA.

**Constraints**: Validation failures use HTTP 400 with ProblemDetails. Database constraints are authoritative for uniqueness. Click increments must be atomic in the database and must not use an application read-modify-write. The full short URL is formed from an explicitly configured public base URL, not an untrusted request Host header. Apply schema migrations as a deployment step before serving requests. No management key, login, per-IP rate limit, or CAPTCHA is in scope. SQLite requires durable file storage; concurrent database writes may be serialized by the provider.

**Scale/Scope**: No traffic or data-volume target was specified. Initial delivery targets one SQLite-backed deployment with a durable database file; horizontal scaling and the PostgreSQL migration are future work.

## Constitution Check

| Principle / gate | Status | Plan alignment |
|---|---|---|
| I. Separação entre API e Aplicação | PASS | Minimal API endpoint mappings call `LinkService`; validation and business rules stay in the application layer. |
| II. Persistência Independente de Provedor | PASS | EF Core and SQLite are isolated in Infrastructure behind application-facing persistence contracts; no SQLite-specific behavior is exposed to endpoints or business rules. |
| III. Testes de Regras de Negócio e Respostas de Validação | PASS | Business rules receive xUnit coverage; invalid request data returns HTTP 400 ProblemDetails. A duplicate custom code is a conflict (HTTP 409), not a malformed-input validation error. |
| IV. Dependências Justificadas | PASS | Dependencies are limited to the explicitly requested web stack, EF Core/SQLite persistence, xUnit, and EF Core design-time tooling for repeatable schema migrations. |
| V. Planos e Commits Vinculados a Requisitos | PASS | Implementation tasks and commits should reference the functional requirement IDs; this plan preserves requirement traceability. |

**Pre-design gate**: PASS. The requested architecture is compatible with the constitution. There is no new external service or unjustified dependency.

## Project Structure

### Documentation (this feature)

```text
specs/001-encurtador-links/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
└── contracts/
    └── http-api.md
```

### Source Code (repository root)

```text
src/
├── Encurtador.Api/
│   ├── Endpoints/
│   │   └── LinksEndpoints.cs
│   └── Program.cs
├── Encurtador.Application/
│   ├── Links/
│   │   ├── LinkService.cs
│   │   ├── ILinkRepository.cs
│   │   ├── Link.cs
│   │   └── LinkCodeGenerator.cs
│   └── Common/
│       └── Result types for expected domain outcomes
└── Encurtador.Infrastructure/
    └── Persistence/
        ├── LinkDbContext.cs
        ├── LinkRepository.cs
        └── Migrations/

tests/
├── Encurtador.UnitTests/
│   └── Links/
└── Encurtador.IntegrationTests/
    └── Links/
```

**Structure Decision**: Use three production projects to keep HTTP transport, business rules, and provider-specific EF Core code separated, plus focused unit and integration test projects. All link operations belong to one link resource and are mapped in `LinksEndpoints.cs`; do not split endpoint handlers across files for create, redirect, and statistics. The persistent entity and generated-code policy live in separate application files. No frontend or authentication project is needed.

## Design Decisions

- **Generated code alphabet**: Generate exactly seven characters using `23456789ABCDEFGHJKMNPQRSTVWXYZ` (30 symbols). This omits `0`, `1`, `I`, `L`, `O`, and `U`; the first five are common visual confusions, and `U` follows the Crockford-style alphabet convention. Use a cryptographically secure random source. This remains alphanumeric and gives 30^7 possible generated codes. The user confirmed this restricted alphabet applies only to automatically generated codes; custom codes retain the spec's separate rule: 4–20 ASCII letters or digits, case-sensitive.
- **Uniqueness and collision handling**: Check code availability in `LinkService` for a helpful early outcome, then rely on a named unique database index as the final authority, including concurrent creations. Translate only a violation of that named code constraint into a collision outcome; let unrelated persistence errors surface. For generated-code collisions, retry with a fresh candidate up to five total candidates. For a custom-code collision, return a conflict and never substitute a different code.
- **Atomic click count**: Resolve the destination without reading the count, then increment the persisted count with a single EF Core relational `ExecuteUpdate` expression (`ClickCount = ClickCount + 1`). Never load a count and later write it back. Return the redirect only after the increment succeeds. Disable caching on the redirect response so a cached redirect does not bypass future counting.
- **Management and statistics**: Do not create, return, store, or require a management key. This explicitly follows the user's plan clarification; with no accounts or key, the stats query is public to anyone who knows the link code, as already stated in the spec assumptions.
- **Provider boundary**: Keep provider setup, unique-constraint classification, schema, and migrations in Infrastructure. Keep `LinkService` and its contracts provider-neutral. Apply migrations explicitly before serving requests rather than racing API instances at startup. Before a future PostgreSQL migration, verify an EF Core 10-compatible Npgsql provider release and test the migration against PostgreSQL; provider major versions must match EF Core.
- **Persistence lifetime**: Configure SQLite to use a durable file location supplied by deployment configuration. Do not use an in-memory database for normal execution. Integration tests may use isolated temporary databases.
- **Short URL origin**: Require a configured public base URL and use it when returning `shortUrl`; do not trust an arbitrary inbound Host header to construct shareable links.
- **HTTP behavior**: Use 400 ProblemDetails for malformed input, 409 ProblemDetails when a chosen code is already used, and 404 for an unknown code. Use a non-permanent redirect with `Cache-Control: no-store` to support counting each service request.

## Constitution Check (post-design)

| Principle / gate | Status | Design evidence |
|---|---|---|
| I. Separação entre API e Aplicação | PASS | `LinksEndpoints.cs` delegates to `LinkService`; endpoint handlers do not use EF Core. |
| II. Persistência Independente de Provedor | PASS | `ILinkRepository` is implemented in Infrastructure; EF Core models and provider error classification remain there. |
| III. Testes de Regras de Negócio e Respostas de Validação | PASS | Unit tests cover service rules, integration tests cover SQLite behavior, and API input validation uses 400 ProblemDetails. |
| IV. Dependências Justificadas | PASS | SQLite and xUnit are user-requested. EF Core provides the provider-switch boundary; its design-time tooling is limited to repeatable migrations. |
| V. Planos e Commits Vinculados a Requisitos | PASS | Downstream tasks should reference FR identifiers from `spec.md`; no plan-versus-constitution conflict remains. |

**Post-design gate**: PASS. No constitution violations or unresolved technical-context decisions remain for this delivery. Explicitly unspecified performance and scale targets are recorded as constraints, not hidden assumptions.

## Complexity Tracking

No constitution violations. Three production projects are the minimum split needed to keep the API, application rules, and provider-specific persistence responsibilities separate while meeting the provider-substitution requirement.
