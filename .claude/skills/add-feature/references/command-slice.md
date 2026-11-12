# Command Slice Templates

A command use case is **one file**: `src/VerticalSlice.Api/Features/{Entity}/{UseCase}.cs`, namespace `VerticalSlice.Api.Features.{Entity}`. Everything below is nested inside `public static class {UseCase}`. Replace `{Entity}` (feature folder, plural, e.g. `Todos`), `{UseCase}` (e.g. `ArchiveTodo`), and the domain type names throughout.

## Full slice skeleton

```csharp
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using VerticalSlice.Api.Authentication;
using VerticalSlice.Api.Common;
using VerticalSlice.Api.Common.Endpoints;
using VerticalSlice.Api.Common.Extensions;
using VerticalSlice.Api.Common.Messaging;
using VerticalSlice.Api.Database;

namespace VerticalSlice.Api.Features.Todos;

public static class ArchiveTodo
{
    public sealed record Command(Guid TodoItemId) : ICommand;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.TodoItemId).NotEmpty();
        }
    }

    internal sealed class Handler(
        ApplicationDbContext context,
        IDateTimeProvider dateTimeProvider,
        IUserContext userContext) : ICommandHandler<Command>
    {
        public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
        {
            TodoItem? todoItem = await context.TodoItems
                .SingleOrDefaultAsync(
                    t => t.Id == command.TodoItemId && t.UserId == userContext.UserId,
                    cancellationToken);

            if (todoItem is null)
            {
                return Result.Failure(TodoItemErrors.NotFound(command.TodoItemId));
            }

            // The entity enforces the rule, mutates its private state, and raises the event.
            Result archiveResult = todoItem.Archive(dateTimeProvider.UtcNow);

            if (archiveResult.IsFailure)
            {
                return archiveResult;
            }

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }

    // The endpoint is nested in this same file — see endpoint.md.
    public sealed class Endpoint : IEndpoint { /* ... */ }
}
```

## Command

Positional record for few parameters (nested):

```csharp
public sealed record Command(Guid TodoItemId) : ICommand;
```

Class with settable properties when there are many parameters (matches `CreateTodo.Command`):

```csharp
public sealed class Command : ICommand<Guid>
{
    public Guid UserId { get; set; }
    public string Description { get; set; }
    public DateTime? DueDate { get; set; }
    public List<string> Labels { get; set; } = [];
    public Priority Priority { get; set; }
}
```

- `ICommand` → handler returns `Result` (endpoint responds `204 NoContent`).
- `ICommand<TResponse>` → handler returns `Result<TResponse>` (endpoint responds `200 Ok`).
- `ICommand`/`ICommand<T>` come from `VerticalSlice.Api.Common.Messaging`.

## Validator

Nested `public sealed class Validator : AbstractValidator<Command>`. Auto-registered and executed by `ValidationDecorator` before the handler runs. Because `Command` is nested, `AbstractValidator<Command>` resolves without qualification inside the slice class.

```csharp
public sealed class Validator : AbstractValidator<Command>
{
    public Validator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.Priority).IsInEnum();
        RuleFor(c => c.Description).NotEmpty().MaximumLength(255);
        RuleFor(c => c.DueDate)
            .Cascade(CascadeMode.Stop)
            .Must(dueDate => dueDate!.Value.Kind == DateTimeKind.Utc)
            .WithMessage("'Due Date' must include a timezone (ISO 8601 with 'Z' or an offset such as '+02:00').")
            .GreaterThanOrEqualTo(_ => DateTime.UtcNow.Date)
            .When(x => x.DueDate.HasValue);
    }
}
```

## Handler

`internal sealed class Handler`, primary constructor, `ApplicationDbContext` for data access — **injected directly**, there is no `IApplicationDbContext`. Guard clauses return `Result.Failure` with feature errors; the happy path creates the entity via its `{Entity}.Create(...)` factory or calls an entity method (which mutates private state and raises the domain event), then saves and returns. Handlers never assign entity properties or call `Raise` for state the entity owns.

```csharp
internal sealed class Handler(
    ApplicationDbContext context,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : ICommandHandler<Command, Guid>
{
    public async Task<Result<Guid>> Handle(Command command, CancellationToken cancellationToken)
    {
        if (userContext.UserId != command.UserId)
        {
            return Result.Failure<Guid>(UserErrors.Unauthorized());
        }

        User? user = await context.Users.AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<Guid>(UserErrors.NotFound(command.UserId));
        }

        // Factory generates Id (Guid.CreateVersion7) and raises TodoItemCreatedDomainEvent.
        var todoItem = TodoItem.Create(
            user.Id,
            command.Description,
            command.Priority,
            command.DueDate,
            command.Labels,
            dateTimeProvider.UtcNow);

        context.TodoItems.Add(todoItem);

        await context.SaveChangesAsync(cancellationToken);

        return todoItem.Id;
    }
}
```

Notes:
- `Handler` is `internal`, but the assembly scan finds it (Scrutor `AssignableTo(ICommandHandler<,>)` over `DefinedTypes`, which includes nested types).
- Ownership: either filter by `userContext.UserId` in the query (preferred) or compare explicitly and return `Result.Failure(UserErrors.Unauthorized())`. `IUserContext` is in `VerticalSlice.Api.Authentication`; `UserErrors`/`User` in `VerticalSlice.Api.Features.Users` (add `using VerticalSlice.Api.Features.Users;` — a domain-type reference, which is allowed across features).
- `IDateTimeProvider.UtcNow` (from `VerticalSlice.Api.Common`) for timestamps — never `DateTime.Now`/`DateTime.Today`, and not `DateTime.UtcNow` directly in handlers. All dates are UTC. Dates from requests: the endpoint converts offset dates (`Kind == Local`) with `ToUniversalTime()` when mapping `Request` → `Command`, and every `DateTime` on a `Command` gets a validator rule requiring `Kind == DateTimeKind.Utc`, so dates sent without a timezone (`Unspecified`) are rejected (400).
- If the command invalidates cached query data, inject `HybridCache` and call `cache.RemoveAsync({Feature}CacheKeys.X(...), cancellationToken)` after saving.
- For a returning command (`ICommand<Guid>`), return the value directly — `Result<T>` has an implicit conversion: `return todoItem.Id;`.

## Domain additions (if needed)

These are domain types and live in the feature folder (`Features/{Entity}/`), namespace `VerticalSlice.Api.Features.{Entity}` — see the `add-entity` skill.

Entity method on the existing entity (properties are `private set`, so the slice cannot mutate them directly):

```csharp
public Result Archive(DateTime? archivedAt = null)
{
    if (IsArchived)
    {
        return Result.Failure(TodoItemErrors.AlreadyArchived(Id));
    }

    IsArchived = true;
    ArchivedAt = archivedAt ?? DateTime.UtcNow;

    Raise(new TodoItemArchivedDomainEvent(Id));

    return Result.Success();
}
```

Error factory on the existing `{Entity}Errors` class:

```csharp
public static Error AlreadyArchived(Guid todoItemId) => Error.Problem(
    "TodoItems.AlreadyArchived",
    $"The todo item with Id = '{todoItemId}' is already archived.");
```

Error type → HTTP status (via `CustomResults.Problem`): `NotFound` → 404, `Conflict` → 409, `Problem`/`Validation` → 400, `Failure` → 500.

Domain event, one file each in `Features/{Entity}/`:

```csharp
using VerticalSlice.Api.Common;

namespace VerticalSlice.Api.Features.Todos;

public sealed record TodoItemArchivedDomainEvent(Guid TodoItemId) : IDomainEvent;
```

Optional event handler — a small standalone type (it isn't a use case, so it need not be nested):

```csharp
using VerticalSlice.Api.Common;

namespace VerticalSlice.Api.Features.Todos;

internal sealed class TodoItemArchivedDomainEventHandler : IDomainEventHandler<TodoItemArchivedDomainEvent>
{
    public Task Handle(TodoItemArchivedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        // Side effects here (notifications, projections, ...)
        return Task.CompletedTask;
    }
}
```
