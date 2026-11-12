using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using VerticalSlice.Api.Authentication;
using VerticalSlice.Api.Common;
using VerticalSlice.Api.Database;
using VerticalSlice.Api.Features.Todos;
using VerticalSlice.Api.UnitTests.Abstractions;

namespace VerticalSlice.Api.UnitTests.Todos;

public sealed class UpdateTodoHandlerTests : BaseHandlerTest
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenTodoBelongsToAnotherUser()
    {
        // Arrange
        await using ApplicationDbContext context = CreateDbContext();
        Guid todoItemId = await SeedTodoAsync(context, ownerId: Guid.NewGuid());

        HybridCache cache = CreateCache();
        IUserContext userContext = Substitute.For<IUserContext>();
        userContext.UserId.Returns(UserId);

        var handler = new UpdateTodo.Handler(context, userContext, cache);
        var command = new UpdateTodo.Command(todoItemId, "Updated description");

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("TodoItems.NotFound");
    }

    [Fact]
    public async Task Handle_Should_UpdateDescriptionAndDispatchDomainEvent_WhenValid()
    {
        // Arrange
        IDomainEventsDispatcher dispatcher = Substitute.For<IDomainEventsDispatcher>();
        await using ApplicationDbContext context = CreateDbContext(dispatcher);
        Guid todoItemId = await SeedTodoAsync(context, ownerId: UserId);

        HybridCache cache = CreateCache();
        IUserContext userContext = Substitute.For<IUserContext>();
        userContext.UserId.Returns(UserId);

        var handler = new UpdateTodo.Handler(context, userContext, cache);
        var command = new UpdateTodo.Command(todoItemId, "Updated description");

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        TodoItem todoItem = await context.TodoItems.SingleAsync(t => t.Id == todoItemId);
        todoItem.Description.ShouldBe("Updated description");
        await dispatcher.Received().DispatchAsync(
            Arg.Is<IEnumerable<IDomainEvent>>(events => events.Any(e => e is TodoItemUpdatedDomainEvent)),
            Arg.Any<CancellationToken>());
    }

    private static async Task<Guid> SeedTodoAsync(ApplicationDbContext context, Guid ownerId)
    {
        var todoItem = TodoItem.Create(ownerId, "Original description", Priority.Low);

        context.TodoItems.Add(todoItem);
        await context.SaveChangesAsync();

        return todoItem.Id;
    }
}
