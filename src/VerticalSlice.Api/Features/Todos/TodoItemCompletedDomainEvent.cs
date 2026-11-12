using VerticalSlice.Api.Common;

namespace VerticalSlice.Api.Features.Todos;

public sealed record TodoItemCompletedDomainEvent(Guid TodoItemId) : IDomainEvent;
