using VerticalSlice.Api.Common;

namespace VerticalSlice.Api.Features.Todos;

public sealed record TodoItemUpdatedDomainEvent(Guid TodoItemId) : IDomainEvent;
