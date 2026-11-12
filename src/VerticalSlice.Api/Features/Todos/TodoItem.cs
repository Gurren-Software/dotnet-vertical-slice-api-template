using VerticalSlice.Api.Common;

namespace VerticalSlice.Api.Features.Todos;

public sealed class TodoItem : Entity
{
    private TodoItem()
    {
    }

    public Guid Id { get; private init; }
    public Guid UserId { get; private init; }
    public string Description { get; private set; }
    public DateTime? DueDate { get; private set; }
    public List<string> Labels { get; private set; } = [];
    public bool IsCompleted { get; private set; }
    public DateTime CreatedAt { get; private init; }
    public DateTime? CompletedAt { get; private set; }
    public Priority Priority { get; private set; }

    public static TodoItem Create(
        Guid userId,
        string description,
        Priority priority,
        DateTime? dueDate = null,
        List<string>? labels = null,
        DateTime? createdAt = null)
    {
        var todoItem = new TodoItem
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Description = description,
            Priority = priority,
            DueDate = dueDate,
            Labels = labels is null ? [] : [.. labels],
            IsCompleted = false,
            CreatedAt = createdAt ?? DateTime.UtcNow
        };

        todoItem.Raise(new TodoItemCreatedDomainEvent(todoItem.Id));

        return todoItem;
    }

    public Result Complete(DateTime? completedAt = null)
    {
        if (IsCompleted)
        {
            return Result.Failure(TodoItemErrors.AlreadyCompleted(Id));
        }

        IsCompleted = true;
        CompletedAt = completedAt ?? DateTime.UtcNow;

        Raise(new TodoItemCompletedDomainEvent(Id));

        return Result.Success();
    }

    public void UpdateDescription(string description)
    {
        Description = description;

        Raise(new TodoItemUpdatedDomainEvent(Id));
    }
}
