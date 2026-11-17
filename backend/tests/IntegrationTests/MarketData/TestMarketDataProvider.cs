using System.Collections.Concurrent;
using InvestmentCoPilot.MarketData.Providers;
using InvestmentCoPilot.MarketData.Stocks;
using InvestmentCoPilot.SharedKernel;

namespace InvestmentCoPilot.IntegrationTests.MarketData;

/// <summary>A provider the test controls: what it knows, whether it fails, and how often it was called.</summary>
internal sealed class TestMarketDataProvider : IMarketDataProvider
{
    public const string ProviderName = "Test";

    private readonly ConcurrentDictionary<string, ProviderFundamentals> stocks = new(StringComparer.Ordinal);
    private int calls;

    public string Name => ProviderName;

    public string DisplayName => "Test provider";

    public int Calls => calls;

    /// <summary>When set, every fetch throws.</summary>
    public bool Fails { get; set; }

    /// <summary>When set, each fetch waits for it before answering.</summary>
    public Task? Gate { get; set; }

    public void Set(string symbol, ProviderFundamentals data) => stocks[symbol] = data;

    public async Task<ProviderFundamentals?> FetchAsync(TickerSymbol symbol, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref calls);
        if (Gate is not null)
        {
            await Gate.WaitAsync(cancellationToken);
        }

        if (Fails)
        {
            throw new MarketDataProviderException("The test provider is down.");
        }

        return stocks.TryGetValue(symbol.Value, out var data) ? data : null;
    }

    public static ProviderFundamentals Sample(string companyName, decimal price, DateTimeOffset asOf) =>
        new(companyName, "USD", new Money(price, "USD"), new Money(price * 1_000_000m, "USD"),
            TrailingPe: 12m, ForwardPe: null, EpsGrowth: 0.2m,
            InstitutionalOwnership: 0.05m, InsiderOwnership: 0.3m, DebtToEquity: 0.1m, asOf);
}

/// <summary>A clock the test moves by hand.</summary>
internal sealed class ManualTimeProvider(DateTimeOffset start) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = start;

    public override DateTimeOffset GetUtcNow() => Now;

    public void Advance(TimeSpan by) => Now += by;
}
