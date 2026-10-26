using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace ImageServer.Authorization;

public sealed class AuthorizationCheckService : IAuthorizationCheckService
{
    private const string CacheKeyPrefix = "SGAuthorize:";

    private readonly SgAuthorizeOptions _options;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AuthorizationCheckService> _logger;

    public AuthorizationCheckService(
        IOptions<SgAuthorizeOptions> options,
        IMemoryCache cache,
        ILogger<AuthorizationCheckService> logger)
    {
        _options = options.Value;
        _cache = cache;
        _logger = logger;
    }

    public async Task<bool> IsAuthorizedAsync(
        string url,
        int userId,
        string filePath,
        string rawUrl,
        CancellationToken cancellationToken)
    {
        if (_options.CacheSeconds > 0
            && _cache.TryGetValue(BuildCacheKey(userId, rawUrl), out bool cached))
        {
            _logger.LogDebug("Authorization cache hit for user {UserId}", userId);
            return cached;
        }

        var authorized = await ExecuteWithRetryAsync(url, userId, filePath, rawUrl, cancellationToken)
            .ConfigureAwait(false);

        if (_options.CacheSeconds > 0)
        {
            _cache.Set(
                BuildCacheKey(userId, rawUrl),
                authorized,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_options.CacheSeconds),
                    Priority = CacheItemPriority.Low
                });
        }

        return authorized;
    }

    private async Task<bool> ExecuteWithRetryAsync(
        string url,
        int userId,
        string filePath,
        string rawUrl,
        CancellationToken cancellationToken)
    {
        var attempts = Math.Max(1, _options.ConnectionAttempts);
        var sleepMs = Math.Max(0, _options.ConnectionAttemptsSleep);
        SqlException? lastEx = null;

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                await using var connection = new SqlConnection(_options.Connection);
                var parameters = new
                {
                    Url = url,
                    User = userId.ToString(),
                    FilePath = filePath,
                    RawUrl = rawUrl
                };

                _logger.LogDebug("Checking authorization for user {UserId} via {Procedure}", userId, _options.Procedure);

                var result = await connection.ExecuteScalarAsync<object>(
                    new CommandDefinition(
                        _options.Procedure,
                        parameters,
                        commandType: CommandType.StoredProcedure,
                        commandTimeout: 5,
                        cancellationToken: cancellationToken)).ConfigureAwait(false);

                return Convert.ToBoolean(result);
            }
            catch (SqlException ex)
            {
                lastEx = ex;
                _logger.LogWarning(
                    ex,
                    "Unable to connect to datastore (attempt {Attempt}/{Attempts})",
                    attempt,
                    attempts);

                if (attempt < attempts && sleepMs > 0)
                {
                    await Task.Delay(sleepMs, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        _logger.LogError("Unable to connect to database after {Attempts} attempts.", attempts);
        throw new InvalidOperationException("Unable to connect to database", lastEx);
    }

    private static string BuildCacheKey(int userId, string rawUrl) =>
        CacheKeyPrefix + userId + ":" + rawUrl;
}
