using InvestmentCoPilot.SharedKernel;
using InvestmentCoPilot.SharedKernel.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace InvestmentCoPilot.UnitTests.SharedKernel;

public class ModuleDbContextTests
{
    private sealed class Widget
    {
        public Guid Id { get; set; }
    }

    private sealed class SampleContext(DbContextOptions<SampleContext> options, ICurrentUser currentUser)
        : ModuleDbContext<SampleContext>(options, currentUser), IModuleDbContext
    {
        public static string Schema => "sample_module";

        public DbSet<Widget> Widgets => Set<Widget>();
    }

    private static SampleContext CreateContext()
    {
        var services = new ServiceCollection()
            .AddSingleton<ICurrentUser>(new FakeCurrentUser())
            .AddModuleDbContext<SampleContext>("Host=unused")
            .BuildServiceProvider();
        return services.CreateScope().ServiceProvider.GetRequiredService<SampleContext>();
    }

    [Fact]
    public void Tables_are_placed_in_the_module_schema()
    {
        using var context = CreateContext();
        Assert.Equal("sample_module", context.Model.GetDefaultSchema());
        Assert.Equal("sample_module", context.Model.FindEntityType(typeof(Widget))!.GetSchema());
    }

    [Fact]
    public void Migrations_history_table_is_in_the_module_schema()
    {
        using var context = CreateContext();
        var relational = RelationalOptionsExtension.Extract(context.GetService<IDbContextOptions>());
        Assert.Equal("sample_module", relational.MigrationsHistoryTableSchema);
        Assert.Equal(HistoryRepository.DefaultTableName, relational.MigrationsHistoryTableName);
    }
}
