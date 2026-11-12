using VerticalSlice.Api.Common;

namespace VerticalSlice.Api.Database;

public interface IDomainEventsDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
