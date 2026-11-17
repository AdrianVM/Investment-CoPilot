namespace InvestmentCoPilot.MarketData.Providers;

/// <summary>Thrown by a provider when its data source cannot answer (unreachable, error response, quota exceeded).</summary>
internal sealed class MarketDataProviderException : Exception
{
    public MarketDataProviderException(string message)
        : base(message)
    {
    }

    public MarketDataProviderException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
