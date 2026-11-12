using VerticalSlice.Api.Features.Users;

namespace VerticalSlice.Api.Authentication;

public interface ITokenProvider
{
    string Create(User user);

    string GenerateRefreshToken();
}
