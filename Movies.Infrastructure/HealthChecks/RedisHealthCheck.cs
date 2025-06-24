using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Movies.Infrastructure.HealthChecks;

public class RedisHealthCheck(IConnectionMultiplexer connectionMultiplexer) : IHealthCheck
{
    private const string HealthCheckName = "RedisHealthCheck";
    private readonly IConnectionMultiplexer _connectionMultiplexer = connectionMultiplexer;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var database = _connectionMultiplexer.GetDatabase();
            await database.PingAsync();
            
            return HealthCheckResult.Healthy("Redis is accessible");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis is not accessible", ex);
        }
    }
}