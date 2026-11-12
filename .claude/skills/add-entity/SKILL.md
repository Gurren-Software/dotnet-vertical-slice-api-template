---
name: add-entity
description: Add a new domain entity to the Vertical Slice Architecture template — entity class, error catalog, domain events, EF Core configuration, DbContext wiring, and migration. Use when the user asks to add an entity, aggregate, domain model, or table.
argument-hint: <entity description, e.g. "Project with a name, owner, and list of todos">
---

# Add a Domain Entity

Create a new entity and wire it through the single `VerticalSlice.Api` project, following the `TodoItem` pattern. The entity and its domain types live with their feature; only the EF configuration and the `DbSet` live in `Database/`.

## Files to create/modify

1. **Entity** — `src/VerticalSlice.Api/Features/{Entity}/{Entity}.cs`

```csharp
using VerticalSlice.Api.Common;

namespace VerticalSlice.Api.Features.Projects;

public sealed class Project : Entity
{
    private Project()
    {
    }

    public Guid Id { get; private init; }
    public Guid OwnerId { get; private init; }
    public string Name { get; private set; }
    public DateTime CreatedAt { get; private init; }

    public static Project Create(Guid ownerId, string name, DateTime? createdAt = null)
    {
        var project = new Project
        {
            Id = Guid.CreateVersion7(),
            OwnerId = ownerId,
            Name = name,
            CreatedAt = createdAt ?? DateTime.UtcNow
        };

        project.Raise(new ProjectCreatedDomainEvent(project.Id));

        return project;
    }

    public void Rename(string name)
    {
        Name = name;

        Raise(new ProjectRenamedDomainEvent(Id));
    }
}
```

- `sealed class`, inherits `Entity` (from `VerticalSlice.Api.Common` — gives it `DomainEvents` + `Raise(...)`).
- **Construction only through a `public static {Entity} Create(...)` factory** plus a `private` parameterless constructor (for EF Core). Never `new {Entity} { ... }` outside the entity.
- **Encapsulated state**: `private init` for values fixed at creation (`Id`, owner/FK ids, `CreatedAt`), `private set` for state that changes later. No public setters.
- **`Id = Guid.CreateVersion7()`** in the factory (time-ordered, index-friendly) — never `Guid.NewGuid()`, never passed in.
- **Creation date is an optional last parameter** `DateTime? createdAt = null`, defaulting to `DateTime.UtcNow`. Handlers pass `dateTimeProvider.UtcNow` so it stays testable.
- **Dates are always UTC.** Entities assign dates as-is and never convert them: callers pass UTC (`dateTimeProvider.UtcNow`), and dates from requests are converted to UTC by the endpoint (offset dates) or rejected by the slice's validator (no timezone). No EF value converter — Npgsql rejects non-UTC values for `timestamptz`, so a slip fails loudly.
- The factory raises the `{Entity}CreatedDomainEvent`; state changes go through intention-revealing methods (`Rename`, `Complete`, …) that mutate and raise their event. A method guarding a business rule returns `Result` (e.g. `TodoItem.Complete` → `TodoItemErrors.AlreadyCompleted`).
- Copy incoming collections (`Labels = labels is null ? [] : [.. labels]`); collections default to `= [];`.
- Navigations (rare — prefer FK ids) are getter-only (`public User User { get; }`); EF fills the backing field.

2. **Error catalog** — `src/VerticalSlice.Api/Features/{Entity}/{Entity}Errors.cs`

```csharp
using VerticalSlice.Api.Common;

namespace VerticalSlice.Api.Features.Projects;

public static class ProjectErrors
{
    public static Error NotFound(Guid projectId) => Error.NotFound(
        "Projects.NotFound",
        $"The project with the Id = '{projectId}' was not found");
}
```

Codes are `"{FeaturePlural}.{Reason}"`. Pick the factory by semantics: `Error.NotFound` (404), `Error.Conflict` (409), `Error.Problem` (400), `Error.Failure` (500). `Error`/`ErrorType` are in `VerticalSlice.Api.Common`.

3. **Domain events** — one record per file, `src/VerticalSlice.Api/Features/{Entity}/{Entity}{PastTenseVerb}DomainEvent.cs`

```csharp
using VerticalSlice.Api.Common;

namespace VerticalSlice.Api.Features.Projects;

public sealed record ProjectCreatedDomainEvent(Guid ProjectId) : IDomainEvent;
```

Create at minimum the `Created` event; add others as commands need them. Events carry ids, not entities. `IDomainEvent` is in `VerticalSlice.Api.Common`.

4. **EF configuration** — `src/VerticalSlice.Api/Database/Configurations/{Entity}Configuration.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VerticalSlice.Api.Features.Projects;
using VerticalSlice.Api.Features.Users;

namespace VerticalSlice.Api.Database.Configurations;

internal sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.HasKey(p => p.Id);

        builder.HasOne<User>().WithMany().HasForeignKey(p => p.OwnerId);
    }
}
```

Relationships are configured shadow-style (`HasOne<User>().WithMany()`) — entities hold foreign-key ids, not navigation properties. Configurations are picked up automatically by `ApplyConfigurationsFromAssembly` in `ApplicationDbContext`. Referencing another feature's entity here (e.g. `User`) is fine — it's a domain-type reference, not a cross-slice handler dependency.

5. **DbContext wiring** — add `DbSet<{Entity}> {Plural}` to `src/VerticalSlice.Api/Database/ApplicationDbContext.cs`. This is the **only** place a `DbSet` is declared — there is no `IApplicationDbContext` in this template. Handlers and unit tests use the concrete `ApplicationDbContext`, so the new set is available everywhere automatically.

6. **Migration** — from the repo root (single project, so it's both `--project` and `--startup-project`):

```
dotnet ef migrations add Add_{Plural} --project src/VerticalSlice.Api --startup-project src/VerticalSlice.Api
```

Migration names are `PascalCase_With_Underscores` (see `Add_RefreshTokens`).

## Rules

- The entity is a plain domain type in its feature folder — no EF attributes, no persistence concerns on it. Keys, conversions, and relationships live exclusively in the `Database/Configurations/` configuration.
- Entity, errors, and domain events go in `Features/{Entity}/` (namespace `VerticalSlice.Api.Features.{Entity}`); the EF configuration goes in `Database/Configurations/`; the `DbSet` on `ApplicationDbContext`.
- Domain types are shareable across features (a slice in another feature may reference this entity), but never reference another feature's `Command`/`Handler`.
- Run `dotnet build` and `dotnet test` when done.
- If the user also wants use cases for the entity, continue with the `add-feature` skill.
