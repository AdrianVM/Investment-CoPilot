using InvestmentCoPilot.SharedKernel;
using InvestmentCoPilot.SharedKernel.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InvestmentCoPilot.MarketData.Persistence;

internal sealed class MarketDataDbContext(DbContextOptions<MarketDataDbContext> options, ICurrentUser currentUser)
    : ModuleDbContext<MarketDataDbContext>(options, currentUser), IModuleDbContext
{
    public static string Schema => "market_data";
}
