using FluentValidation.TestHelper;
using Web.Api.Features.Todos;

namespace Web.Api.UnitTests.Todos;

public sealed class TodoValidatorsTests
{
    private readonly CreateTodo.Validator _createValidator = new();
    private readonly UpdateTodo.Validator _updateValidator = new();

    [Fact]
    public void CreateValidator_Should_HaveError_WhenDescriptionIsEmpty()
    {
        var command = new CreateTodo.Command
        {
            UserId = Guid.NewGuid(),
            Description = string.Empty,
            Priority = Priority.Low
        };

        TestValidationResult<CreateTodo.Command> result = _createValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Description);
    }

    [Theory]
    [InlineData(DateTimeKind.Unspecified)]
    [InlineData(DateTimeKind.Local)]
    public void CreateValidator_Should_HaveError_WhenDueDateIsNotUtc(DateTimeKind kind)
    {
        var command = new CreateTodo.Command
        {
            UserId = Guid.NewGuid(),
            Description = "Buy groceries",
            Priority = Priority.Low,
            DueDate = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(1), kind)
        };

        TestValidationResult<CreateTodo.Command> result = _createValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.DueDate);
    }

    [Fact]
    public void CreateValidator_Should_NotHaveError_WhenDueDateIsUtcInTheFuture()
    {
        var command = new CreateTodo.Command
        {
            UserId = Guid.NewGuid(),
            Description = "Buy groceries",
            Priority = Priority.Low,
            DueDate = DateTime.UtcNow.AddDays(1)
        };

        TestValidationResult<CreateTodo.Command> result = _createValidator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.DueDate);
    }

    [Fact]
    public void CreateValidator_Should_NotHaveErrors_WhenCommandIsValid()
    {
        var command = new CreateTodo.Command
        {
            UserId = Guid.NewGuid(),
            Description = "Buy groceries",
            Priority = Priority.Medium,
            Labels = ["home"]
        };

        TestValidationResult<CreateTodo.Command> result = _createValidator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UpdateValidator_Should_HaveError_WhenDescriptionExceedsMaxLength()
    {
        var command = new UpdateTodo.Command(Guid.NewGuid(), new string('a', 501));

        TestValidationResult<UpdateTodo.Command> result = _updateValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Description);
    }

    [Fact]
    public void UpdateValidator_Should_NotHaveErrors_WhenCommandIsValid()
    {
        var command = new UpdateTodo.Command(Guid.NewGuid(), "Updated description");

        TestValidationResult<UpdateTodo.Command> result = _updateValidator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
