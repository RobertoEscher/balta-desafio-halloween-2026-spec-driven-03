# Research: Encurtador de Links

**Date**: 2026-10-17  
**Scope**: Resolve the persistence, concurrency, generated-code alphabet, and credential-scope decisions for the implementation plan.

## 1. Provider-portable persistence with EF Core

**Decision**: Use standard EF Core relational mappings and a SQLite provider in the initial deployment. Keep the `DbContext`, migrations, provider setup, and provider-specific error classification in Infrastructure. Expose persistence operations through an application-facing repository contract. PostgreSQL is a future provider replacement, not part of this implementation.

**Rationale**: EF Core's relational model and unique-index mapping work across relational providers. Keeping provider-specific code at the persistence boundary allows the application service and endpoint contracts to remain independent of SQLite. An eventual PostgreSQL move still requires compatible EF Core/Npgsql versions and provider-specific migration testing.

Apply schema migrations as an explicit deployment step before serving traffic rather than having every API instance migrate on startup. Use a configured persistent SQLite file path and a configured public base URL for generated share links.

**Alternatives considered**:
- Put EF Core in the API or application project: rejected because persistence details would cross the intended application boundary.
- Use provider-specific SQL for SQLite: rejected because it would make future provider replacement harder.
- Implement PostgreSQL now: rejected because the requested initial provider is SQLite and PostgreSQL is explicitly future work.

**Sources**:
- [EF Core indexes](https://learn.microsoft.com/en-us/ef/core/modeling/indexes)
- [SQLite provider limitations](https://learn.microsoft.com/en-us/ef/core/providers/sqlite/limitations)
- [EF Core providers and version compatibility](https://learn.microsoft.com/en-us/ef/core/providers/)
- [Npgsql EF Core provider](https://www.npgsql.org/efcore/)

## 2. Unique short codes and concurrent creation

**Decision**: Perform an early availability check in `LinkService`, but enforce uniqueness with a database unique index on the exact code value. Infrastructure translates only violations of that named index into a code-collision result. Generated-code collisions cause a fresh random candidate to be tried a bounded number of times; duplicate custom codes produce a conflict and are never changed.

**Rationale**: A service check alone has a race between checking and inserting. The database constraint remains authoritative when requests run concurrently. Classifying the specific constraint avoids converting unrelated database failures into success-shaped collision handling.

**Alternatives considered**:
- Rely only on a service-level existence check: rejected because concurrent requests can both observe an unused code.
- Catch every persistence exception as a collision: rejected because it would hide database faults.
- Use provider-specific upsert SQL: rejected because it is unnecessary for this workload and reduces portability.

**Sources**:
- [EF Core indexes](https://learn.microsoft.com/en-us/ef/core/modeling/indexes)
- [PostgreSQL constraints](https://www.postgresql.org/docs/current/ddl-constraints.html)
- [PostgreSQL error codes](https://www.postgresql.org/docs/current/errcodes-appendix.html)
- [SQLite result codes](https://www.sqlite.org/rescode.html)
- [Microsoft.Data.Sqlite database errors](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/database-errors)

## 3. Atomic click increments

**Decision**: Increment with a single database-side EF Core `ExecuteUpdate` operation that assigns `ClickCount + 1`; never load the count into application memory and write a replacement value. Resolve the destination separately and only return the redirect after the update reports a matching row.

**Rationale**: The database evaluates the addition against the row's current value, preventing lost increments from competing requests. EF Core's relational update API avoids hand-written provider-specific SQL. It executes immediately and bypasses EF's change tracker, so the same entity must not later be saved from stale tracked state.

**Alternatives considered**:
- Read, increment in C#, then save: rejected because concurrent requests can overwrite each other's increments.
- Provider-specific SQL: rejected while provider portability is a goal.
- Asynchronous aggregation: rejected because the specified observable count is exact immediately after successful redirection.

**Sources**:
- [EF Core execute insert, update, and delete](https://learn.microsoft.com/en-us/ef/core/saving/execute-insert-update-delete)
- [PostgreSQL transaction isolation](https://www.postgresql.org/docs/current/transaction-iso.html)
- [SQLite transactions](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/transactions)

## 4. Unambiguous generated alphabet

**Decision**: Generate seven characters from `23456789ABCDEFGHJKMNPQRSTVWXYZ`, using a cryptographically secure random generator. This 30-symbol uppercase alphabet excludes `0`, `1`, `I`, `L`, and `O` as visual look-alikes, and `U` following the Crockford-style human-readable alphabet. The user confirmed this restricted alphabet applies only to automatically generated codes. Keep user-chosen codes under their existing validation rule (4–20 ASCII letters or digits), preserve exact case, and enforce uniqueness on the exact stored string.

**Rationale**: Restricting only generated codes satisfies the 7-character generated-code requirement while improving manual readability; it does not silently narrow the separately specified custom-code input rule. The 30-symbol alphabet provides 30^7 possible generated codes, and a database unique index handles collisions regardless of probability.

**Alternatives considered**:
- Use every alphanumeric character: rejected for generated codes because of visual ambiguity (for example, `0/O` and `1/I`).
- Use standard RFC 4648 Base32: rejected because it still includes visually confusable letters.
- Normalize custom codes to uppercase: rejected because the specification defines exact, case-sensitive uniqueness.

**Sources**:
- [RFC 4648, alphabet and rejection of non-alphabet characters](https://www.rfc-editor.org/rfc/rfc4648.html#section-3.3)
- [Crockford Base32 proposal](https://www.crockford.com/base32.html)
- [Cryptographically secure random numbers in .NET](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.randomnumbergenerator)

## 5. Management-key scope

**Decision**: Do not implement a management key, hash, credential field, or key-protected endpoint. Keep link statistics public to anyone who knows the code.

**Rationale**: The user explicitly removed the management-key item during planning. The feature specification already documents that, without user accounts, anyone who knows a short code may query its count.

**Alternatives considered**:
- Add a key for statistics: rejected by the user's explicit instruction.
- Hash and store a key for future management operations: rejected because no such operation is in scope.
