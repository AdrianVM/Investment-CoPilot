namespace InvestmentCoPilot.MarketData.Contracts;

/// <summary>The outcome of a stock fundamentals lookup. Exactly one of the nested types.</summary>
public abstract record StockLookupResult
{
    private StockLookupResult()
    {
    }

    /// <summary>The fundamentals were found (possibly stale; see <see cref="StockFundamentals.IsStale"/>).</summary>
    public sealed record Found(StockFundamentals Fundamentals) : StockLookupResult;

    /// <summary>The symbol is not a valid ticker symbol. The provider was not contacted.</summary>
    public sealed record InvalidSymbol(string Reason) : StockLookupResult;

    /// <summary>The provider does not know the symbol.</summary>
    public sealed record NotFound : StockLookupResult;

    /// <summary>The provider failed and nothing is cached for the symbol.</summary>
    public sealed record Unavailable : StockLookupResult;
}
