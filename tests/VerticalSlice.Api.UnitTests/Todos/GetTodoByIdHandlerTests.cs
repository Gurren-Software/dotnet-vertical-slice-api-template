using Microsoft.Extensions.Caching.Hybrid;
using VerticalSlice.Api.Authentication;
using VerticalSlice.Api.Common;
using VerticalSlice.Api.Database;
using VerticalSlice.Api.Features.Todos;
using VerticalSlice.Api.UnitTests.Abstractions;

namespace VerticalSlice.Api.UnitTests.Todos;

public sealed class GetTodoByIdHandlerTests : BaseHandlerTest
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenTodoDoesNotExist()
    {
        // Arrange
        await using ApplicationDbContext context = CreateDbContext();
        HybridCache cache = CreateCache();
        IUserContext userContext = Substitute.For<IUserContext>();
        userContext.UserId.Returns(UserId);

        var handler = new GetTodoById.Handler(context, userContext, cache);
        var query = new GetTodoById.Query(Guid.NewGuid());

        // Act
        Result<GetTodoById.Response> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("TodoItems.NotFound");
    }

    [Fact]
    public async Task Handle_Should_ReturnTodo_WhenItExistsForTheCurrentUser()
    {
        // Arrange
        await using ApplicationDbContext context = CreateDbContext();
        var todoItem = TodoItem.Create(UserId, "Cached todo", Priority.High, labels: ["important"]);
        context.TodoItems.Add(todoItem);
        await context.SaveChangesAsync();

        HybridCache cache = CreateCache();
        IUserContext userContext = Substitute.For<IUserContext>();
        userContext.UserId.Returns(UserId);

        var handler = new GetTodoById.Handler(context, userContext, cache);
        var query = new GetTodoById.Query(todoItem.Id);

        // Act
        Result<GetTodoById.Response> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(todoItem.Id);
        result.Value.Description.ShouldBe("Cached todo");
    }
}
