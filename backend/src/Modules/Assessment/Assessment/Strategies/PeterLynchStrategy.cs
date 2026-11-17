using InvestmentCoPilot.Assessment.Contracts;
using InvestmentCoPilot.SharedKernel;

namespace InvestmentCoPilot.Assessment.Strategies;

/// <summary>
/// Peter Lynch's six rules of thumb (docs/peter-lynch-stock-criteria.jpeg). Thresholds use the fundamentals' units:
/// fractions for percentages (0.10 = 10 %), plain ratios for P/E and debt to equity, and US dollars for market capitalisation.
/// </summary>
internal sealed class PeterLynchStrategy : IStrategy
{
    public const string StrategyId = "peter-lynch";

    public const decimal MaxTrailingPe = 25m;
    public const decimal MaxForwardPe = 15m;
    public const decimal MaxInstitutionalOwnership = 0.10m;
    public const decimal MinInsiderOwnership = 0.20m;
    public const decimal MinEpsGrowth = 0.15m;
    public const decimal MaxDebtToEquity = 0.35m;
    public static readonly Money MaxMarketCap = new(5_000_000_000m, "USD");

    public string Id => StrategyId;

    public string Name => "Peter Lynch";

    public IReadOnlyList<ICriterion> Criteria { get; } =
    [
        new PeCriterion(MaxTrailingPe, MaxForwardPe),
        new ThresholdCriterion(
            "institutionalOwnership", "Institutional ownership", "Institutional ownership",
            s => s.InstitutionalOwnership, MeasureUnit.Fraction, Comparison.LessThan, MaxInstitutionalOwnership),
        new ThresholdCriterion(
            "insiderOwnership", "Insider ownership", "Insider ownership",
            s => s.InsiderOwnership, MeasureUnit.Fraction, Comparison.GreaterThan, MinInsiderOwnership),
        new ThresholdCriterion(
            "epsGrowth", "EPS growth", "EPS growth (year over year)",
            s => s.EpsGrowth, MeasureUnit.Fraction, Comparison.GreaterThan, MinEpsGrowth),
        new ThresholdCriterion(
            "debtToEquity", "Debt to equity", "Debt to equity",
            s => s.DebtToEquity, MeasureUnit.Ratio, Comparison.LessThan, MaxDebtToEquity),
        new MarketCapCriterion(MaxMarketCap),
    ];
}
