using InvestmentCoPilot.Assessment.Contracts;
using InvestmentCoPilot.MarketData.Contracts;

namespace InvestmentCoPilot.Assessment.Strategies;

/// <summary>A criterion that compares one value of the fundamentals with one threshold.</summary>
internal sealed class ThresholdCriterion(
    string id,
    string name,
    string measureLabel,
    Func<StockFundamentals, decimal?> value,
    MeasureUnit unit,
    Comparison comparison,
    decimal threshold) : ICriterion
{
    public string Id => id;

    public string Name => name;

    public Task<CriterionAssessment> EvaluateAsync(StockFundamentals stock, IMarketDataModule marketData, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stock);
        var actual = value(stock);
        Measure[] measures = [new(id, measureLabel, actual, unit, Currency: null, comparison, threshold)];

        var result = actual is { } v
            ? new CriterionAssessment(id, name, Thresholds.Meets(v, comparison, threshold) ? Verdict.Meets : Verdict.DoesNotMeet, measures, Reason: null, Conversion: null)
            : Thresholds.Unassessable(this, measures, UnassessableReason.DataNotAvailable);
        return Task.FromResult(result);
    }
}
