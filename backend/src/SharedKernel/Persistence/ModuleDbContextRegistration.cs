using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InvestmentCoPilot.SharedKernel.Persistence;

public static class ModuleDbContextRegistration
{
    /// <summary>Name of the connection string (<c>ConnectionStrings:Database</c>) all modules share.</summary>
    public const string ConnectionStringName = "Database";

    /// <summary>
    /// Registers a module DbContext on the shared Postgres database from <c>ConnectionStrings:Database</c>,
    /// read when a context is created. Its migrations history table is kept in the module's own schema.
    /// </summary>
    public static IServiceCollection AddModuleDbContext<TContext>(this IServiceCollection services)
        where TContext : ModuleDbContext<TContext>, IModuleDbContext =>
        services.AddModuleDbContext<TContext>(sp => GetConnectionString(sp.GetRequiredService<IConfiguration>()));

    /// <summary>Registers a module DbContext with an explicit connection string (for tests and tools).</summary>
    public static IServiceCollection AddModuleDbContext<TContext>(this IServiceCollection services, string connectionString)
        where TContext : ModuleDbContext<TContext>, IModuleDbContext
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        return services.AddModuleDbContext<TContext>(_ => connectionString);
    }

    /// <summary>Reads <c>ConnectionStrings:Database</c>, failing clearly when it is missing.</summary>
    public static string GetConnectionString(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        return string.IsNullOrWhiteSpace(connectionString)
            ? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.")
            : connectionString;
    }

    /// <summary>The Npgsql settings every module context uses.</summary>
    public static DbContextOptionsBuilder ConfigureNpgsql<TContext>(DbContextOptionsBuilder options, string connectionString)
        where TContext : ModuleDbContext<TContext>, IModuleDbContext
    {
        ArgumentNullException.ThrowIfNull(options);
        return options.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable(HistoryRepository.DefaultTableName, TContext.Schema));
    }

    private static IServiceCollection AddModuleDbContext<TContext>(
        this IServiceCollection services, Func<IServiceProvider, string> connectionString)
        where TContext : ModuleDbContext<TContext>, IModuleDbContext
    {
        services.AddDbContext<TContext>((sp, options) => ConfigureNpgsql<TContext>(options, connectionString(sp)));
        services.AddSingleton<IModuleDatabaseMigrator, ModuleDatabaseMigrator<TContext>>();
        return services;
    }
}
