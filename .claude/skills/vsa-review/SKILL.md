---
name: vsa-review
description: Review pending changes against the Vertical Slice Architecture template's conventions — one-file slices, slice isolation, Result-based error handling, direct DbContext access, validation, endpoints, and test coverage. Use when the user asks to review changes, check conventions, or audit a feature before committing.
argument-hint: [optional: specific files or feature to review; defaults to the working-tree diff]
---

# Vertical Slice Architecture Convention Review

Review the given scope (default: `git diff` + untracked files) against this template's conventions. Report findings with file:line references, ordered by severity. Do not fix anything unless asked.

## Checklist

### Slice structure (violations are blockers)
- One use case = one file: `src/VerticalSlice.Api/Features/{Entity}/{UseCase}.cs`, namespace `VerticalSlice.Api.Features.{Entity}`, containing a `public static class {UseCase}`.
- The slice is fully nested: `Command`/`Query`, `Validator` (commands only), `Handler`, and `Endpoint` are all nested inside that static class. No separate Application/Domain/Infrastructure project, no separate `Endpoints/` folder.
- `Handler` is `internal sealed` with a primary constructor, implementing the custom `ICommandHandler<>` / `IQueryHandler<>` (from `VerticalSlice.Api.Common.Messaging`) — no MediatR.
- Response DTOs are the slice's nested `Response`; queries project with `.Select(...)` and never return domain entities.

### Slice isolation (blockers)
- **No cross-feature slice references.** A feature may reference the shared `Common`/`Database`/`Authentication`/`Authorization` code and another feature's **domain types** (entity, `{Entity}Errors`, domain events). It must **never** reference another feature's `Command`/`Query`/`Handler`/`Validator`/`Endpoint`.
- No manual DI registration for handlers/validators/endpoints — they are discovered by Scrutor scanning, `AddValidatorsFromAssembly(includeInternalTypes: true)`, and `AddEndpoints`. Flag anything wiring a slice up by hand.

### Data access
- Handlers inject the concrete `ApplicationDbContext` (from `VerticalSlice.Api.Database`) directly. There is **no `IApplicationDbContext`** — flag any reference to it.
- Persistence details (keys, conversions, relationships) live in `IEntityTypeConfiguration<>` classes under `Database/Configurations/`, not on entities. Entities are plain domain types in `Features/{Entity}/`.

### Entities
- Created only via a `public static {Entity} Create(...)` factory (with a `private` parameterless ctor for EF) — flag any `new {Entity} { ... }` in slices or tests.
- No public setters: `private init` for creation-time values (`Id`, FK ids, `CreatedAt`), `private set` for mutable state; mutations go through entity methods.
- `Id = Guid.CreateVersion7()` inside the factory — flag `Guid.NewGuid()` for entity ids.
- Creation date is an optional `DateTime? createdAt = null` factory parameter defaulting to `DateTime.UtcNow`; handlers pass `dateTimeProvider.UtcNow`.

### Error handling
- Expected failures return `Result` / `Result<T>` (from `VerticalSlice.Api.Common`) — no exceptions for control flow, no try/catch around business rules.
- Errors come from static `{Entity}Errors` factories with `"{Feature}.{Reason}"` codes and the semantically correct type (`NotFound` / `Conflict` / `Problem` / `Failure`).
- Endpoints translate failures only via `result.Match(Results.Ok|NoContent, CustomResults.Problem)`.

### Validation & security
- Every command has a nested FluentValidation `Validator`; handlers don't re-check input shape (but do enforce business rules). Queries have no validator.
- Handlers acting on user-owned data enforce ownership: filter by `IUserContext.UserId` or return `UserErrors.Unauthorized()`.
- Endpoints implement `IEndpoint`, call `.RequireAuthorization()` (or `.HasPermission(...)`) and `.WithTags(Tags.X)`, and contain only request→command mapping plus result matching — no business logic.
- No `DateTime.UtcNow` / `DateTime.Now` in handlers — use `IDateTimeProvider`.
- Dates are UTC everywhere: flag any `DateTime.Now`, `DateTime.Today`, `ToLocalTime()`, `ToUniversalTime()` outside endpoint request mapping, or an EF `HasConversion` added to paper over non-UTC dates. Endpoints convert `Kind == Local` request dates to UTC; every `DateTime` on a `Command` must have a validator rule requiring `Kind == DateTimeKind.Utc`.

### State changes & caching
- Commands that mutate state raise a domain event before `SaveChangesAsync` — from the entity's `Create` factory or mutation method (e.g. `todoItem.Complete(...)`), not by setting properties in the handler.
- Any `HybridCache`-cached read has matching invalidation (`cache.RemoveAsync`) in every command that mutates that data; keys come from the `{Feature}CacheKeys` class.

### Tests
- New/changed handlers have unit tests covering every `Result.Failure` path plus the happy path (persisted state + domain events), using the nested types (`new {UseCase}.Handler(...)`).
- New/changed validators have `TestValidate` tests per rule.
- New/changed endpoints have integration tests over real HTTP.
- Test naming and Arrange/Act/Assert structure match existing tests.

## Output format

Group findings as **Blockers** (cross-feature slice references, `IApplicationDbContext` usage, missing auth, thrown exceptions for expected failures), **Convention violations** (file/nesting structure, naming, error codes, missing events/invalidation), and **Test gaps**. For each: `file:line`, what's wrong, and the one-line fix. Close with a verdict: ready to commit, or what must change first. If everything passes, say so and run `dotnet build` + `dotnet test` to confirm.
