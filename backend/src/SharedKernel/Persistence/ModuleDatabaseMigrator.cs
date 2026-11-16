using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InvestmentCoPilot.SharedKernel.Persistence;

/// <summary>Applies one module's pending migrations. Registered automatically by <c>AddModuleDbContext</c>.</summary>
public interface IModuleDatabaseMigrator
{
    string Schema { get; }

    Task MigrateAsync(IServiceProvider scopedServices, CancellationToken cancellationToken);
}

internal sealed class ModuleDatabaseMigrator<TContext> : IModuleDatabaseMigrator
    where TContext : ModuleDbContext<TContext>, IModuleDbContext
{
    public string Schema => TContext.Schema;

    public Task MigrateAsync(IServiceProvider scopedServices, CancellationToken cancellationToken) =>
        scopedServices.GetRequiredService<TContext>().Database.MigrateAsync(cancellationToken);
}

public static class ModuleDatabaseMigration
{
    /// <summary>
    /// Applies pending migrations for every module. Intended for local development and tests only;
    /// other environments apply migrations as an explicit deployment step.
    /// </summary>
    public static async Task MigrateModuleDatabasesAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);
        await using var scope = services.CreateAsyncScope();
        foreach (var migrator in scope.ServiceProvider.GetServices<IModuleDatabaseMigrator>())
        {
            await migrator.MigrateAsync(scope.ServiceProvider, cancellationToken);
        }
    }
}
