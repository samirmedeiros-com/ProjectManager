using Microsoft.Extensions.Caching.Memory;

namespace ProjectManagerWebAPI.Services;

public record OraConsoleCredentials(string Username, string Password);

public interface IOraConsoleSessionStore
{
    string CreateSession(string username, string password);
    bool TryGetCredentials(string sessionId, out OraConsoleCredentials credentials);
    void RemoveSession(string sessionId);
}

public class OraConsoleSessionStore : IOraConsoleSessionStore
{
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _slidingExpiration;
    private readonly TimeSpan _absoluteExpiration;

    public OraConsoleSessionStore(IMemoryCache cache, IConfiguration configuration)
    {
        _cache = cache;
        var section = configuration.GetSection("OraConsole");
        _slidingExpiration = TimeSpan.FromMinutes(section.GetValue("SessionSlidingMinutes", 30));
        _absoluteExpiration = TimeSpan.FromMinutes(section.GetValue("SessionAbsoluteMinutes", 240));
    }

    public string CreateSession(string username, string password)
    {
        var sessionId = Guid.NewGuid().ToString("N");
        var options = new MemoryCacheEntryOptions
        {
            SlidingExpiration = _slidingExpiration,
            AbsoluteExpirationRelativeToNow = _absoluteExpiration
        };
        _cache.Set(sessionId, new OraConsoleCredentials(username, password), options);
        return sessionId;
    }

    public bool TryGetCredentials(string sessionId, out OraConsoleCredentials credentials)
    {
        if (_cache.TryGetValue(sessionId, out OraConsoleCredentials? found) && found != null)
        {
            credentials = found;
            return true;
        }
        credentials = null!;
        return false;
    }

    public void RemoveSession(string sessionId) => _cache.Remove(sessionId);
}
