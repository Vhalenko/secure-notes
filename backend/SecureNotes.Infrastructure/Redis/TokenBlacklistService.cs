using StackExchange.Redis;
using SecureNotes.Core.Interfaces;

namespace SecureNotes.Infrastructure.Redis;

public class TokenBlacklistService : ITokenBlacklistService
{
    private readonly IConnectionMultiplexer _redis;
    private const string Prefix = "blacklist:";

    public TokenBlacklistService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task AddAsync(string token, TimeSpan expiry)
    {
        var db = _redis.GetDatabase();
        await db.StringSetAsync(Prefix + token, "1", expiry);
    }

    public async Task<bool> IsBlacklistedAsync(string token)
    {
        var db = _redis.GetDatabase();
        return await db.KeyExistsAsync(Prefix + token);
    }
}