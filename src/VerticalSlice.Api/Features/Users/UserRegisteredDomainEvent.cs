using VerticalSlice.Api.Common;

namespace VerticalSlice.Api.Features.Users;

public sealed record UserRegisteredDomainEvent(Guid UserId, string Email) : IDomainEvent;
