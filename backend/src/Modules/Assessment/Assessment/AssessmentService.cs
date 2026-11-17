using InvestmentCoPilot.Assessment.Contracts;
using InvestmentCoPilot.Assessment.Strategies;
using InvestmentCoPilot.MarketData.Contracts;

namespace InvestmentCoPilot.Assessment;

/// <summary>Assesses a stock against a strategy, on demand, from its current fundamentals. Nothing is stored.</summary>
internal sealed class AssessmentService(StrategyRegistry strategies, IMarketDataModule marketData)
{
    public async Task<AssessmentLookupResult> AssessAsync(string strategyId, string symbol, CancellationToken cancellationToken)
    {
        if (strategies.Find(strategyId) is not { } strategy)
        {
            return new AssessmentLookupResult.UnknownStrategy(strategyId);
        }

        switch (await marketData.GetStockFundamentalsAsync(symbol, cancellationToken))
        {
            case StockLookupResult.Found found:
                return new AssessmentLookupResult.Found(await AssessAsync(strategy, found.Fundamentals, cancellationToken));
            case StockLookupResult.InvalidSymbol invalid:
                return new AssessmentLookupResult.InvalidSymbol(invalid.Reason);
            case StockLookupResult.NotFound:
                return new AssessmentLookupResult.StockNotFound();
            case StockLookupResult.Unavailable:
                return new AssessmentLookupResult.Unavailable();
            case var other:
                throw new InvalidOperationException($"Unhandled lookup result {other.GetType().Name}.");
        }
    }

    private async Task<StockAssessment> AssessAsync(IStrategy strategy, StockFundamentals stock, CancellationToken cancellationToken)
    {
        var criteria = new List<CriterionAssessment>(strategy.Criteria.Count);
        foreach (var criterion in strategy.Criteria)
        {
            criteria.Add(await criterion.EvaluateAsync(stock, marketData, cancellationToken));
        }

        var summary = new AssessmentSummary(
            Met: criteria.Count(c => c.Verdict == Verdict.Meets),
            NotMet: criteria.Count(c => c.Verdict == Verdict.DoesNotMeet),
            CannotAssess: criteria.Count(c => c.Verdict == Verdict.CannotAssess),
            Total: criteria.Count);

        return new StockAssessment(
            strategy.Id, strategy.Name, stock.Symbol, stock.CompanyName, stock.Source, stock.AsOf, stock.IsStale, criteria, summary);
    }
}
