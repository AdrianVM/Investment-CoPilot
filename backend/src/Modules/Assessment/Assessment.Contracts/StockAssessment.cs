using System.Text.Json.Serialization;
using InvestmentCoPilot.SharedKernel;

namespace InvestmentCoPilot.Assessment.Contracts;

/// <summary>A stock assessed against a strategy. Source, as-of time and stale marker come from the fundamentals used.</summary>
/// <param name="Criteria">One entry per criterion, in the strategy's fixed order.</param>
public sealed record StockAssessment(
    string StrategyId,
    string StrategyName,
    string Symbol,
    string CompanyName,
    string Source,
    DateTimeOffset AsOf,
    bool IsStale,
    IReadOnlyList<CriterionAssessment> Criteria,
    AssessmentSummary Summary);

/// <summary>The verdict for one criterion and the values behind it.</summary>
/// <param name="Measures">The values the criterion checks, each with its threshold.</param>
/// <param name="Reason">Why the criterion could not be assessed; null otherwise.</param>
/// <param name="Conversion">The currency conversion used, when a value was converted (market capitalisation).</param>
public sealed record CriterionAssessment(
    string Id,
    string Name,
    Verdict Verdict,
    IReadOnlyList<Measure> Measures,
    UnassessableReason? Reason,
    CurrencyConversion? Conversion);

/// <summary>One value compared with a threshold, in the same units as the fundamentals (fractions, not percentages).</summary>
/// <param name="Value">The stock's value, or null if not available. For money, the amount in <paramref name="Currency"/>.</param>
/// <param name="Currency">ISO 4217 currency of <paramref name="Value"/> and <paramref name="Threshold"/> when <paramref name="Unit"/> is money.</param>
public sealed record Measure(
    string Id,
    string Label,
    decimal? Value,
    MeasureUnit Unit,
    string? Currency,
    Comparison Comparison,
    decimal Threshold);

/// <summary>A conversion of the stock's own value into the criterion's currency.</summary>
/// <param name="Rate">Units of the converted currency per unit of the original currency.</param>
public sealed record CurrencyConversion(Money Original, Money Converted, decimal Rate, string Source, DateTimeOffset AsOf);

/// <summary>How many criteria have each verdict. There is deliberately no combined score.</summary>
public sealed record AssessmentSummary(int Met, int NotMet, int CannotAssess, int Total);

[JsonConverter(typeof(JsonStringEnumConverter<Verdict>))]
public enum Verdict
{
    [JsonStringEnumMemberName("meets")]
    Meets,
    [JsonStringEnumMemberName("doesNotMeet")]
    DoesNotMeet,
    [JsonStringEnumMemberName("cannotAssess")]
    CannotAssess,
}

[JsonConverter(typeof(JsonStringEnumConverter<UnassessableReason>))]
public enum UnassessableReason
{
    [JsonStringEnumMemberName("dataNotAvailable")]
    DataNotAvailable,
    [JsonStringEnumMemberName("exchangeRateNotAvailable")]
    ExchangeRateNotAvailable,
}

[JsonConverter(typeof(JsonStringEnumConverter<MeasureUnit>))]
public enum MeasureUnit
{
    /// <summary>A plain number, such as a P/E ratio.</summary>
    [JsonStringEnumMemberName("ratio")]
    Ratio,

    /// <summary>A fraction, where 0.15 means 15 %.</summary>
    [JsonStringEnumMemberName("fraction")]
    Fraction,

    /// <summary>An amount of money in the measure's currency.</summary>
    [JsonStringEnumMemberName("money")]
    Money,
}

[JsonConverter(typeof(JsonStringEnumConverter<Comparison>))]
public enum Comparison
{
    /// <summary>Meets when the value is strictly less than the threshold.</summary>
    [JsonStringEnumMemberName("lessThan")]
    LessThan,

    /// <summary>Meets when the value is strictly greater than the threshold.</summary>
    [JsonStringEnumMemberName("greaterThan")]
    GreaterThan,
}
