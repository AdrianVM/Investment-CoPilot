using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using InvestmentCoPilot.SharedKernel.Persistence;
using Npgsql;

namespace InvestmentCoPilot.Host.Health;

internal static class HealthEndpoints
{
    private const string ReadyTag = "ready";

    public static IServiceCollection AddApiHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .Add(new HealthCheckRegistration(
                "postgres",
                sp => new PostgresHealthCheck(ModuleDbContextRegistration.GetConnectionString(sp.GetRequiredService<IConfiguration>())),
                HealthStatus.Unhealthy,
                [ReadyTag],
                TimeSpan.FromSeconds(5)));
        return services;
    }

    /// <summary>
    /// <c>/health/live</c>: the process is up (runs no checks). <c>/health/ready</c>: the API can serve requests,
    /// including reaching Postgres. Both are the only endpoints that do not require authentication.
    /// </summary>
    public static void MapApiHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains(ReadyTag) })
            .AllowAnonymous();
    }

    private sealed class PostgresHealthCheck(string connectionString) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync(cancellationToken);
                await using var command = new NpgsqlCommand("SELECT 1", connection);
                await command.ExecuteScalarAsync(cancellationToken);
                return HealthCheckResult.Healthy();
            }
            catch (Exception ex) when (ex is NpgsqlException or TimeoutException or OperationCanceledException)
            {
                return HealthCheckResult.Unhealthy("Postgres is not reachable.", ex);
            }
        }
    }
}
