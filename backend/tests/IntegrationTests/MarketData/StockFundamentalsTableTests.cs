using InvestmentCoPilot.IntegrationTests.Infrastructure;
using InvestmentCoPilot.MarketData.Persistence;
using InvestmentCoPilot.MarketData.Stocks;
using InvestmentCoPilot.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InvestmentCoPilot.IntegrationTests.MarketData;

[Collection(IntegrationTestCollection.Name)]
public sealed class StockFundamentalsTableTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Cache_table_round_trips_present_and_missing_metrics()
    {
        var connectionString = await postgres.CreateEmptyDatabaseAsync();
        await using var factory = new ApiFactory(connectionString); // Development: applies migrations at startup
        var asOf = new DateTimeOffset(2026, 11, 17, 9, 30, 0, TimeSpan.Zero);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MarketDataDbContext>();
            db.StockFundamentals.Add(new CachedStockFundamentals
            {
                Symbol = "FULL",
                CompanyName = "Full Inc.",
                Currency = "EUR",
                Price = new Money(56.8m, "EUR"),
                MarketCap = new Money(3_200_000_000m, "EUR"),
                TrailingPe = 24.6m,
                InsiderOwnership = 0.21m,
                Source = "Test",
                AsOf = asOf,
                FetchedAt = asOf,
            });
            db.StockFundamentals.Add(new CachedStockFundamentals
            {
                Symbol = "EMPTY",
                CompanyName = "Empty Inc.",
                Currency = "USD",
                Source = "Test",
                AsOf = asOf,
                FetchedAt = asOf,
            });
            await db.SaveChangesAsync();
        }

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MarketDataDbContext>();
            var full = await db.StockFundamentals.SingleAsync(s => s.Symbol == "FULL");
            var empty = await db.StockFundamentals.SingleAsync(s => s.Symbol == "EMPTY");

            Assert.Equal(new Money(56.8m, "EUR"), full.Price);
            Assert.Equal(new Money(3_200_000_000m, "EUR"), full.MarketCap);
            Assert.Equal(24.6m, full.TrailingPe);
            Assert.Equal(0.21m, full.InsiderOwnership);
            Assert.Null(full.ForwardPe);
            Assert.Equal(asOf, full.AsOf);

            Assert.Null(empty.Price);
            Assert.Null(empty.MarketCap);
            Assert.Null(empty.TrailingPe);
        }
    }
}
