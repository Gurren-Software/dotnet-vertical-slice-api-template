using Microsoft.EntityFrameworkCore;
using Web.Api.Authentication;
using Web.Api.Common;
using Web.Api.Database;
using Web.Api.Features.Todos;
using Web.Api.Features.Users;
using Web.Api.UnitTests.Abstractions;

namespace Web.Api.UnitTests.Todos;

public sealed class CreateTodoHandlerTests : BaseHandlerTest
{
    private static readonly Guid UserId = Guid.NewGuid();

    private static CreateTodo.Command CreateCommand(Guid userId) => new()
    {
        UserId = userId,
        Description = "Write unit tests",
        Priority = Priority.Medium,
        Labels = ["work"]
    };

    [Fact]
    public async Task Handle_Should_ReturnUnauthorized_WhenUserIdDoesNotMatchContext()
    {
        await using ApplicationDbContext context = CreateDbContext();
        IUserContext userContext = Substitute.For<IUserContext>();
        userContext.UserId.Returns(Guid.NewGuid());
        IDateTimeProvider dateTimeProvider = Substitute.For<IDateTimeProvider>();

        var handler = new CreateTodo.Handler(context, dateTimeProvider, userContext);

        Result<Guid> result = await handler.Handle(CreateCommand(UserId), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(UserErrors.Unauthorized());
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenUserDoesNotExist()
    {
        await using ApplicationDbContext context = CreateDbContext();
        IUserContext userContext = Substitute.For<IUserContext>();
        userContext.UserId.Returns(UserId);
        IDateTimeProvider dateTimeProvider = Substitute.For<IDateTimeProvider>();

        var handler = new CreateTodo.Handler(context, dateTimeProvider, userContext);

        Result<Guid> result = await handler.Handle(CreateCommand(UserId), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(UserErrors.NotFound(UserId));
    }

    [Fact]
    public async Task Handle_Should_PersistTodoAndDispatchDomainEvent_WhenValid()
    {
        IDomainEventsDispatcher dispatcher = Substitute.For<IDomainEventsDispatcher>();
        await using ApplicationDbContext context = CreateDbContext(dispatcher);
        var user = User.Create("test@example.com", "Test", "User", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        IUserContext userContext = Substitute.For<IUserContext>();
        userContext.UserId.Returns(user.Id);
        IDateTimeProvider dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);

        var handler = new CreateTodo.Handler(context, dateTimeProvider, userContext);

        Result<Guid> result = await handler.Handle(CreateCommand(user.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        TodoItem todoItem = await context.TodoItems.SingleAsync(t => t.Id == result.Value);
        todoItem.Description.ShouldBe("Write unit tests");
        todoItem.UserId.ShouldBe(user.Id);
        await dispatcher.Received().DispatchAsync(
            Arg.Is<IEnumerable<IDomainEvent>>(events => events.Any(e => e is TodoItemCreatedDomainEvent)),
            Arg.Any<CancellationToken>());
    }
}
