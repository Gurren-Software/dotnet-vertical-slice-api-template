using VerticalSlice.Api.Common;

namespace VerticalSlice.Api.Features.Todos;

public sealed record TodoItemDeletedDomainEvent(Guid TodoItemId) : IDomainEvent;
