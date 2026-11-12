using Microsoft.EntityFrameworkCore;
using VerticalSlice.Api.Authentication;
using VerticalSlice.Api.Common;
using VerticalSlice.Api.Common.Messaging;
using VerticalSlice.Api.Database;

namespace VerticalSlice.Api.Features.Users;

public static class GetUserByEmail
{
    public sealed record Query(string Email) : IQuery<Response>;

    public sealed record Response
    {
        public Guid Id { get; init; }

        public string Email { get; init; }

        public string FirstName { get; init; }

        public string LastName { get; init; }
    }

    internal sealed class Handler(ApplicationDbContext context, IUserContext userContext)
        : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken cancellationToken)
        {
            Response? user = await context.Users
                .Where(u => u.Email == query.Email)
                .Select(u => new Response
                {
                    Id = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Email = u.Email
                })
                .SingleOrDefaultAsync(cancellationToken);

            if (user is null)
            {
                return Result.Failure<Response>(UserErrors.NotFoundByEmail);
            }

            if (user.Id != userContext.UserId)
            {
                return Result.Failure<Response>(UserErrors.Unauthorized());
            }

            return user;
        }
    }
}
