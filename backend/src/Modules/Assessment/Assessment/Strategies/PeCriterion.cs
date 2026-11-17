using InvestmentCoPilot.Assessment.Contracts;
using InvestmentCoPilot.MarketData.Contracts;

namespace InvestmentCoPilot.Assessment.Strategies;

/// <summary>
/// Trailing and forward P/E, both below their thresholds. Any present value at or above its threshold fails the
/// criterion; it is met only when both are present and below; otherwise it cannot be assessed.
/// </summary>
internal sealed class PeCriterion(decimal trailingThreshold, decimal forwardThreshold) : ICriterion
{
    public string Id => "pe";

    public string Name => "P/E";

    public Task<CriterionAssessment> EvaluateAsync(StockFundamentals stock, IMarketDataModule marketData, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stock);
        Measure[] measures =
        [
            new("trailingPe", "Trailing P/E", stock.TrailingPe, MeasureUnit.Ratio, Currency: null, Comparison.LessThan, trailingThreshold),
            new("forwardPe", "Forward P/E", stock.ForwardPe, MeasureUnit.Ratio, Currency: null, Comparison.LessThan, forwardThreshold),
        ];

        var present = measures.Where(m => m.Value is not null).ToList();
        CriterionAssessment result;
        if (present.Any(m => !Thresholds.Meets(m.Value!.Value, m.Comparison, m.Threshold)))
        {
            result = new(Id, Name, Verdict.DoesNotMeet, measures, Reason: null, Conversion: null);
        }
        else if (present.Count == measures.Length)
        {
            result = new(Id, Name, Verdict.Meets, measures, Reason: null, Conversion: null);
        }
        else
        {
            result = Thresholds.Unassessable(this, measures, UnassessableReason.DataNotAvailable);
        }

        return Task.FromResult(result);
    }
}
