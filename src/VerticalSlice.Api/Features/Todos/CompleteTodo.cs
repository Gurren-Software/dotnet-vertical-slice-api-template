using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using VerticalSlice.Api.Authentication;
using VerticalSlice.Api.Common;
using VerticalSlice.Api.Common.Endpoints;
using VerticalSlice.Api.Common.Extensions;
using VerticalSlice.Api.Common.Messaging;
using VerticalSlice.Api.Database;

namespace VerticalSlice.Api.Features.Todos;

public static class CompleteTodo
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
        IUserContext userContext,
        HybridCache cache)
        : ICommandHandler<Command>
    {
        public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
        {
            TodoItem? todoItem = await context.TodoItems
                .SingleOrDefaultAsync(t => t.Id == command.TodoItemId && t.UserId == userContext.UserId, cancellationToken);

            if (todoItem is null)
            {
                return Result.Failure(TodoItemErrors.NotFound(command.TodoItemId));
            }

            Result completeResult = todoItem.Complete(dateTimeProvider.UtcNow);

            if (completeResult.IsFailure)
            {
                return completeResult;
            }

            await context.SaveChangesAsync(cancellationToken);

            await cache.RemoveAsync(TodoCacheKeys.ById(todoItem.UserId, todoItem.Id), cancellationToken);

            return Result.Success();
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("todos/{id:guid}/complete", async (
                Guid id,
                ICommandHandler<Command> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new Command(id);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.NoContent, CustomResults.Problem);
            })
            .WithTags(Tags.Todos)
            .RequireAuthorization();
        }
    }
}
