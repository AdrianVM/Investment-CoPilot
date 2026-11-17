using InvestmentCoPilot.Assessment.Contracts;
using InvestmentCoPilot.MarketData.Contracts;

namespace InvestmentCoPilot.Assessment.Strategies;

/// <summary>An investment strategy: a fixed, ordered list of criteria. Add a strategy by implementing this and registering it.</summary>
internal interface IStrategy
{
    /// <summary>Identifier used in URLs, for example <c>peter-lynch</c>.</summary>
    string Id { get; }

    string Name { get; }

    IReadOnlyList<ICriterion> Criteria { get; }
}

/// <summary>One rule of a strategy, evaluated against a stock's fundamentals.</summary>
internal interface ICriterion
{
    string Id { get; }

    string Name { get; }

    /// <summary>Evaluates the rule. Missing data gives <see cref="Verdict.CannotAssess"/>, never a failure.</summary>
    Task<CriterionAssessment> EvaluateAsync(StockFundamentals stock, IMarketDataModule marketData, CancellationToken cancellationToken);
}

/// <summary>Compares values with thresholds. Comparisons are strict: a value equal to the threshold does not meet it.</summary>
internal static class Thresholds
{
    public static bool Meets(decimal value, Comparison comparison, decimal threshold) => comparison switch
    {
        Comparison.LessThan => value < threshold,
        Comparison.GreaterThan => value > threshold,
        _ => throw new ArgumentOutOfRangeException(nameof(comparison), comparison, null),
    };

    public static CriterionAssessment Unassessable(ICriterion criterion, IReadOnlyList<Measure> measures, UnassessableReason reason) =>
        new(criterion.Id, criterion.Name, Verdict.CannotAssess, measures, reason, Conversion: null);
}
