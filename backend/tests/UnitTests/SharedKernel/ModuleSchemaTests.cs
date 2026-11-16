using InvestmentCoPilot.Assessment;
using InvestmentCoPilot.Assessment.Persistence;
using InvestmentCoPilot.Identity;
using InvestmentCoPilot.Identity.Persistence;
using InvestmentCoPilot.MarketData;
using InvestmentCoPilot.MarketData.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InvestmentCoPilot.UnitTests.SharedKernel;

public class ModuleSchemaTests
{
    private static readonly IServiceProvider Services = new ServiceCollection()
        .AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Database"] = "Host=unused" })
            .Build())
        .AddIdentityModule()
        .AddMarketDataModule()
        .AddAssessmentModule()
        .BuildServiceProvider();

    public static TheoryData<Type, string> Contexts => new()
    {
        { typeof(IdentityDbContext), "identity" },
        { typeof(MarketDataDbContext), "market_data" },
        { typeof(AssessmentDbContext), "assessment" },
    };

    [Theory]
    [MemberData(nameof(Contexts))]
    public void Each_module_context_uses_its_own_schema(Type contextType, string schema)
    {
        using var scope = Services.CreateScope();
        var context = (DbContext)scope.ServiceProvider.GetRequiredService(contextType);

        Assert.Equal(schema, context.Model.GetDefaultSchema());
        Assert.Equal(schema, RelationalOptionsExtension.Extract(context.GetService<IDbContextOptions>()).MigrationsHistoryTableSchema);
    }
}
