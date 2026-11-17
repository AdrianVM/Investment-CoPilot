namespace InvestmentCoPilot.Assessment.Contracts;

/// <summary>The outcome of an assessment request. Exactly one of the nested types.</summary>
public abstract record AssessmentLookupResult
{
    private AssessmentLookupResult()
    {
    }

    public sealed record Found(StockAssessment Assessment) : AssessmentLookupResult;

    /// <summary>No strategy has this identifier.</summary>
    public sealed record UnknownStrategy(string StrategyId) : AssessmentLookupResult;

    /// <summary>The symbol is not a valid ticker symbol.</summary>
    public sealed record InvalidSymbol(string Reason) : AssessmentLookupResult;

    /// <summary>The market data source does not know the symbol.</summary>
    public sealed record StockNotFound : AssessmentLookupResult;

    /// <summary>Market data is unavailable and nothing is cached for the symbol.</summary>
    public sealed record Unavailable : AssessmentLookupResult;
}
