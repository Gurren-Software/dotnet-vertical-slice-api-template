namespace Web.Api.Features.Users;

public sealed class RefreshToken
{
    private RefreshToken()
    {
    }

    public Guid Id { get; private init; }
    public string Token { get; private set; }
    public Guid UserId { get; private init; }
    public DateTime ExpiresOnUtc { get; private set; }
    public User User { get; }

    public static RefreshToken Create(Guid userId, string token, DateTime expiresOnUtc) => new()
    {
        Id = Guid.CreateVersion7(),
        UserId = userId,
        Token = token,
        ExpiresOnUtc = expiresOnUtc
    };

    public void Rotate(string token, DateTime expiresOnUtc)
    {
        Token = token;
        ExpiresOnUtc = expiresOnUtc;
    }
}
