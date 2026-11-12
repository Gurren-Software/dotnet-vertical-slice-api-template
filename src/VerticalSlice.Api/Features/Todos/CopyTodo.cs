using FluentValidation;
using Microsoft.EntityFrameworkCore;
using VerticalSlice.Api.Authentication;
using VerticalSlice.Api.Common;
using VerticalSlice.Api.Common.Endpoints;
using VerticalSlice.Api.Common.Extensions;
using VerticalSlice.Api.Common.Messaging;
using VerticalSlice.Api.Database;
using VerticalSlice.Api.Features.Users;

namespace VerticalSlice.Api.Features.Todos;

public static class CopyTodo
{
    public sealed class Command : ICommand<Guid>
    {
        public Guid UserId { get; set; }
        public Guid TodoId { get; set; }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.UserId).NotEmpty();
            RuleFor(c => c.TodoId).NotEmpty();
        }
    }

    internal sealed class Handler(
        ApplicationDbContext context,
        IDateTimeProvider dateTimeProvider,
        IUserContext userContext)
        : ICommandHandler<Command, Guid>
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

            TodoItem? existingTodo = await context.TodoItems.AsNoTracking()
                .SingleOrDefaultAsync(t => t.Id == command.TodoId && t.UserId == command.UserId, cancellationToken);

            if (existingTodo is null)
            {
                return Result.Failure<Guid>(TodoItemErrors.NotFound(command.TodoId));
            }

            var copiedTodoItem = TodoItem.Create(
                user.Id,
                existingTodo.Description,
                existingTodo.Priority,
                existingTodo.DueDate,
                existingTodo.Labels,
                dateTimeProvider.UtcNow);

            context.TodoItems.Add(copiedTodoItem);

            await context.SaveChangesAsync(cancellationToken);

            return copiedTodoItem.Id;
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public sealed class Request
        {
            public Guid UserId { get; set; }
            public Guid TodoId { get; set; }
        }

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("todos/{todoId}/copy", async (
                Guid todoId,
                Request request,
                ICommandHandler<Command, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new Command
                {
                    UserId = request.UserId,
                    TodoId = todoId
                };

                Result<Guid> result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
            .WithTags(Tags.Todos)
            .RequireAuthorization();
        }
    }
}
