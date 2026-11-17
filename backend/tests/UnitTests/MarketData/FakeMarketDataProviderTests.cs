using InvestmentCoPilot.MarketData.Providers;
using InvestmentCoPilot.MarketData.Providers.Fake;
using InvestmentCoPilot.MarketData.Stocks;
using InvestmentCoPilot.SharedKernel;

namespace InvestmentCoPilot.UnitTests.MarketData;

/// <summary>Spec market-data: "Fake provider supplies deterministic sample data".</summary>
public class FakeMarketDataProviderTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 17, 12, 0, 0, TimeSpan.Zero);
    private readonly FakeMarketDataProvider provider = new(new FixedTimeProvider(Now));

    public static TheoryData<string> AllSymbols => [.. FakeMarketDataProvider.Symbols];

    [Theory]
    [MemberData(nameof(AllSymbols))]
    public async Task Same_symbol_returns_the_same_values_every_time(string symbol)
    {
        var first = await FetchAsync(symbol);
        var second = await FetchAsync(symbol);

        Assert.NotNull(first);
        Assert.Equal(first, second);
        Assert.Equal(Now, first.AsOf);
    }

    [Theory]
    [MemberData(nameof(AllSymbols))]
    public async Task Money_is_in_the_listing_currency_and_percentages_are_fractions(string symbol)
    {
        var data = (await FetchAsync(symbol))!;

        Assert.All(new[] { data.Price, data.MarketCap }.OfType<Money>(), m => Assert.Equal(data.Currency, m.Currency));
        Assert.All(
            new[] { data.InstitutionalOwnership, data.InsiderOwnership }.OfType<decimal>(),
            fraction => Assert.InRange(fraction, 0m, 1m));
    }

    [Fact]
    public async Task Unknown_symbol_returns_null()
    {
        Assert.Null(await FetchAsync("NOSUCH"));
    }

    [Fact]
    public async Task Lynch_meets_every_criterion()
    {
        var data = (await FetchAsync("LYNCH"))!;

        Assert.Equal(6, CriteriaMet(data));
    }

    [Fact]
    public async Task Bigco_fails_several_criteria()
    {
        var data = (await FetchAsync("BIGCO"))!;

        Assert.True(CriteriaMet(data) <= 2, $"BIGCO meets {CriteriaMet(data)} criteria");
    }

    [Fact]
    public async Task Sample_set_covers_missing_metrics_losses_and_non_usd()
    {
        var sparse = (await FetchAsync("SPARSE"))!;
        var lossco = (await FetchAsync("LOSSCO"))!;
        var euroco = (await FetchAsync("EUROCO"))!;

        Assert.Null(sparse.ForwardPe);
        Assert.Null(sparse.InstitutionalOwnership);
        Assert.Null(sparse.DebtToEquity);
        Assert.Null(lossco.TrailingPe);
        Assert.Null(lossco.ForwardPe);
        Assert.Equal("EUR", euroco.Currency);
    }

    /// <summary>The thresholds from docs/peter-lynch-stock-criteria.jpeg (market cap compared in USD only).</summary>
    private static int CriteriaMet(ProviderFundamentals d) =>
        new[]
        {
            d.TrailingPe < 25m && d.ForwardPe < 15m,
            d.InstitutionalOwnership < 0.10m,
            d.InsiderOwnership > 0.20m,
            d.EpsGrowth > 0.15m,
            d.DebtToEquity < 0.35m,
            d.MarketCap is { Currency: "USD" } cap && cap.Amount < 5_000_000_000m,
        }.Count(met => met);

    private Task<ProviderFundamentals?> FetchAsync(string symbol)
    {
        Assert.True(TickerSymbol.TryParse(symbol, out var parsed));
        return provider.FetchAsync(parsed, CancellationToken.None);
    }
}
