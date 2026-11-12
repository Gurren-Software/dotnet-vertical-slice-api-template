using VerticalSlice.Api.Common;

namespace VerticalSlice.Api.Features.Todos;

public sealed record TodoItemCreatedDomainEvent(Guid TodoItemId) : IDomainEvent;
