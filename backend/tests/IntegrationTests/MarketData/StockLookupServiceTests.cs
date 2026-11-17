using InvestmentCoPilot.IntegrationTests.Infrastructure;
using InvestmentCoPilot.MarketData.Contracts;
using InvestmentCoPilot.MarketData.Persistence;
using InvestmentCoPilot.MarketData.Providers;
using InvestmentCoPilot.MarketData.Stocks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InvestmentCoPilot.IntegrationTests.MarketData;

/// <summary>
/// Spec market-data: "Fundamentals are cached for a configurable time" and "Stale data is served when the provider fails".
/// Runs the real lookup against Postgres, with a test provider and a hand-moved clock.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class StockLookupServiceTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static readonly DateTimeOffset Start = new(2026, 11, 17, 9, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan MaxAge = TimeSpan.FromHours(6);

    private readonly TestMarketDataProvider provider = new();
    private readonly ManualTimeProvider clock = new(Start);
    private ApiFactory factory = null!;

    public async Task InitializeAsync()
    {
        // Each test gets its own database, so cached rows do not leak between tests.
        var connectionString = await postgres.CreateEmptyDatabaseAsync();
        factory = new ApiFactory(connectionString)
            .WithSetting("MarketData:Provider", TestMarketDataProvider.ProviderName)
            .WithSetting("MarketData:CacheMaxAge", MaxAge.ToString())
            .WithServices(services => services.AddSingleton<IMarketDataProvider>(provider));
        provider.Set("ACME", TestMarketDataProvider.Sample("Acme Corp.", 10m, Start));
    }

    public async Task DisposeAsync() => await factory.DisposeAsync();

    [Fact]
    public async Task First_lookup_fetches_from_the_provider_and_reports_its_source()
    {
        var found = Assert.IsType<StockLookupResult.Found>(await LookupAsync("acme"));

        Assert.Equal("ACME", found.Fundamentals.Symbol);
        Assert.Equal("Acme Corp.", found.Fundamentals.CompanyName);
        Assert.Equal("Test provider", found.Fundamentals.Source);
        Assert.Equal(Start, found.Fundamentals.AsOf);
        Assert.Null(found.Fundamentals.ForwardPe);
        Assert.False(found.Fundamentals.IsStale);
        Assert.Equal(1, provider.Calls);
    }

    [Fact]
    public async Task Fresh_cached_data_is_not_fetched_again()
    {
        await LookupAsync("ACME");
        provider.Set("ACME", TestMarketDataProvider.Sample("Acme Renamed", 20m, Start));
        clock.Advance(MaxAge - TimeSpan.FromMinutes(1));

        var found = Assert.IsType<StockLookupResult.Found>(await LookupAsync("ACME"));

        Assert.Equal("Acme Corp.", found.Fundamentals.CompanyName);
        Assert.Equal(1, provider.Calls);
    }

    [Fact]
    public async Task Expired_cached_data_is_fetched_again()
    {
        await LookupAsync("ACME");
        var later = Start + MaxAge + TimeSpan.FromMinutes(1);
        provider.Set("ACME", TestMarketDataProvider.Sample("Acme Renamed", 20m, later));
        clock.Now = later;

        var found = Assert.IsType<StockLookupResult.Found>(await LookupAsync("ACME"));

        Assert.Equal("Acme Renamed", found.Fundamentals.CompanyName);
        Assert.Equal(20m, found.Fundamentals.Price!.Amount);
        Assert.Equal(later, found.Fundamentals.AsOf);
        Assert.False(found.Fundamentals.IsStale);
        Assert.Equal(2, provider.Calls);
    }

    [Fact]
    public async Task Fresh_data_from_a_previously_configured_provider_is_fetched_again()
    {
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MarketDataDbContext>();
            db.StockFundamentals.Add(new CachedStockFundamentals
            {
                Symbol = "ACME",
                CompanyName = "Acme (old provider)",
                Currency = "USD",
                Source = "Old provider",
                AsOf = Start,
                FetchedAt = Start,
            });
            await db.SaveChangesAsync();
        }

        var found = Assert.IsType<StockLookupResult.Found>(await LookupAsync("ACME"));

        Assert.Equal("Test provider", found.Fundamentals.Source);
        Assert.Equal(1, provider.Calls);
    }

    [Fact]
    public async Task Provider_failure_serves_cached_data_marked_stale_with_its_original_as_of_time()
    {
        await LookupAsync("ACME");
        clock.Advance(MaxAge + TimeSpan.FromMinutes(1));
        provider.Fails = true;

        var found = Assert.IsType<StockLookupResult.Found>(await LookupAsync("ACME"));

        Assert.True(found.Fundamentals.IsStale);
        Assert.Equal(Start, found.Fundamentals.AsOf);
        Assert.Equal("Acme Corp.", found.Fundamentals.CompanyName);
    }

    [Fact]
    public async Task Provider_failure_without_cached_data_is_unavailable()
    {
        provider.Fails = true;

        Assert.IsType<StockLookupResult.Unavailable>(await LookupAsync("ACME"));
    }

    [Fact]
    public async Task Unknown_symbol_is_not_found_and_not_cached()
    {
        Assert.IsType<StockLookupResult.NotFound>(await LookupAsync("NOSUCH"));
        Assert.IsType<StockLookupResult.NotFound>(await LookupAsync("NOSUCH"));

        Assert.Equal(2, provider.Calls);
        Assert.Equal(0, await CachedRowCountAsync());
    }

    [Theory]
    [InlineData("AB$C")]
    [InlineData("ABCDEFGHIJKLMNOP")]
    [InlineData("")]
    public async Task Invalid_symbol_never_reaches_the_provider(string symbol)
    {
        var invalid = Assert.IsType<StockLookupResult.InvalidSymbol>(await LookupAsync(symbol));

        Assert.False(string.IsNullOrWhiteSpace(invalid.Reason));
        Assert.Equal(0, provider.Calls);
    }

    [Fact]
    public async Task Concurrent_first_lookups_of_one_symbol_both_succeed_and_leave_one_row()
    {
        var bothFetching = new TaskCompletionSource();
        provider.Gate = bothFetching.Task;

        var first = LookupAsync("ACME");
        var second = LookupAsync("ACME");
        await WaitUntilAsync(() => provider.Calls == 2);
        bothFetching.SetResult();
        var results = await Task.WhenAll(first, second);

        Assert.All(results, r => Assert.Equal("Acme Corp.", Assert.IsType<StockLookupResult.Found>(r).Fundamentals.CompanyName));
        Assert.Equal(1, await CachedRowCountAsync());
    }

    /// <summary>Runs one lookup in its own DI scope (as one HTTP request would), using the test clock.</summary>
    private async Task<StockLookupResult> LookupAsync(string symbol)
    {
        using var scope = factory.Services.CreateScope();
        var lookup = ActivatorUtilities.CreateInstance<StockLookupService>(scope.ServiceProvider, (TimeProvider)clock);
        return await lookup.LookupAsync(symbol, CancellationToken.None);
    }

    private async Task<int> CachedRowCountAsync()
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<MarketDataDbContext>().StockFundamentals.CountAsync();
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (!condition())
        {
            await Task.Delay(10, timeout.Token);
        }
    }
}
