using InvestmentCoPilot.Assessment.Contracts;
using InvestmentCoPilot.Assessment.Strategies;
using InvestmentCoPilot.MarketData.Contracts;
using InvestmentCoPilot.SharedKernel;

namespace InvestmentCoPilot.UnitTests.Assessment;

/// <summary>
/// Spec strategy-assessment: "Peter Lynch thresholds", "Missing data makes a criterion unassessable, never failed",
/// "The P/E criterion combines two measures" and "Market capitalisation is compared in US dollars".
/// </summary>
public class PeterLynchCriteriaTests
{
    private readonly PeterLynchStrategy strategy = new();
    private readonly FakeMarketData marketData = new();

    [Fact]
    public void Criteria_are_in_the_documented_order()
    {
        Assert.Equal(
            ["pe", "institutionalOwnership", "insiderOwnership", "epsGrowth", "debtToEquity", "marketCap"],
            strategy.Criteria.Select(c => c.Id));
    }

    public static TheoryData<string, decimal, Verdict> SingleThresholds => new()
    {
        { "institutionalOwnership", 0.09m, Verdict.Meets },
        { "institutionalOwnership", 0.10m, Verdict.DoesNotMeet }, // at the threshold: strict
        { "insiderOwnership", 0.21m, Verdict.Meets },
        { "insiderOwnership", 0.20m, Verdict.DoesNotMeet },
        { "epsGrowth", 0.16m, Verdict.Meets },
        { "epsGrowth", 0.15m, Verdict.DoesNotMeet },
        { "debtToEquity", 0.34m, Verdict.Meets },
        { "debtToEquity", 0.35m, Verdict.DoesNotMeet },
        { "debtToEquity", 1.45m, Verdict.DoesNotMeet },
    };

    [Theory]
    [MemberData(nameof(SingleThresholds))]
    public async Task Single_value_criteria_use_strict_thresholds(string criterionId, decimal value, Verdict expected)
    {
        var result = await EvaluateAsync(criterionId, With(Stock.Passing(), criterionId, value));

        Assert.Equal(expected, result.Verdict);
        Assert.Equal(value, Assert.Single(result.Measures).Value);
        Assert.Null(result.Reason);
    }

    [Theory]
    [InlineData("institutionalOwnership")]
    [InlineData("insiderOwnership")]
    [InlineData("epsGrowth")]
    [InlineData("debtToEquity")]
    public async Task Missing_value_cannot_be_assessed(string criterionId)
    {
        var result = await EvaluateAsync(criterionId, With(Stock.Passing(), criterionId, null));

        Assert.Equal(Verdict.CannotAssess, result.Verdict);
        Assert.Equal(UnassessableReason.DataNotAvailable, result.Reason);
        Assert.Null(Assert.Single(result.Measures).Value);
    }

    [Fact]
    public async Task Insider_ownership_reports_value_comparison_and_threshold()
    {
        var result = await EvaluateAsync("insiderOwnership", Stock.Passing() with { InsiderOwnership = 0.26m });

        var measure = Assert.Single(result.Measures);
        Assert.Equal((Verdict.Meets, 0.26m, Comparison.GreaterThan, 0.20m, MeasureUnit.Fraction),
            (result.Verdict, measure.Value, measure.Comparison, measure.Threshold, measure.Unit));
    }

    [Theory]
    [InlineData("14.2", "11.8", Verdict.Meets)]
    [InlineData("38.5", null, Verdict.DoesNotMeet)]   // one present and failing
    [InlineData(null, "29.1", Verdict.DoesNotMeet)]
    [InlineData("14.2", "15", Verdict.DoesNotMeet)]   // forward exactly at its threshold
    [InlineData("25", "11.8", Verdict.DoesNotMeet)]   // trailing exactly at its threshold
    [InlineData("19.7", null, Verdict.CannotAssess)]  // one present and passing
    [InlineData(null, null, Verdict.CannotAssess)]    // loss-making
    public async Task Pe_combines_trailing_and_forward(string? trailing, string? forward, Verdict expected)
    {
        var stock = Stock.Passing() with { TrailingPe = Parse(trailing), ForwardPe = Parse(forward) };

        var result = await EvaluateAsync("pe", stock);

        Assert.Equal(expected, result.Verdict);
        Assert.Equal(expected == Verdict.CannotAssess ? UnassessableReason.DataNotAvailable : null, result.Reason);
        Assert.Equal([25m, 15m], result.Measures.Select(m => m.Threshold));
    }

    [Theory]
    [InlineData("4999999999", Verdict.Meets)]
    [InlineData("5000000000", Verdict.DoesNotMeet)]
    [InlineData("410000000000", Verdict.DoesNotMeet)]
    public async Task Usd_market_cap_is_compared_directly(string amount, Verdict expected)
    {
        var stock = Stock.Passing() with { MarketCap = new Money(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), "USD") };

        var result = await EvaluateAsync("marketCap", stock);

        Assert.Equal(expected, result.Verdict);
        Assert.Null(result.Conversion);
        Assert.Equal("USD", Assert.Single(result.Measures).Currency);
    }

    [Fact]
    public async Task Euro_market_cap_is_converted_and_the_conversion_is_reported()
    {
        var stock = Stock.Passing() with { Currency = "EUR", MarketCap = new Money(3_200_000_000m, "EUR") };

        var result = await EvaluateAsync("marketCap", stock);

        Assert.Equal(Verdict.Meets, result.Verdict);
        Assert.Equal(3_472_000_000m, Assert.Single(result.Measures).Value);
        Assert.Equal(
            new CurrencyConversion(new Money(3_200_000_000m, "EUR"), new Money(3_472_000_000m, "USD"), FakeMarketData.EurUsd, "Test rates", Stock.AsOf),
            result.Conversion);
    }

    [Fact]
    public async Task Market_cap_without_a_rate_cannot_be_assessed()
    {
        var stock = Stock.Passing() with { Currency = "SEK", MarketCap = new Money(10_000_000m, "SEK") };

        var result = await EvaluateAsync("marketCap", stock);

        Assert.Equal(Verdict.CannotAssess, result.Verdict);
        Assert.Equal(UnassessableReason.ExchangeRateNotAvailable, result.Reason);
    }

    [Fact]
    public async Task Missing_market_cap_cannot_be_assessed()
    {
        var result = await EvaluateAsync("marketCap", Stock.Passing() with { MarketCap = null });

        Assert.Equal(UnassessableReason.DataNotAvailable, result.Reason);
    }

    private Task<CriterionAssessment> EvaluateAsync(string criterionId, StockFundamentals stock) =>
        strategy.Criteria.Single(c => c.Id == criterionId).EvaluateAsync(stock, marketData, CancellationToken.None);

    private static StockFundamentals With(StockFundamentals stock, string criterionId, decimal? value) => criterionId switch
    {
        "institutionalOwnership" => stock with { InstitutionalOwnership = value },
        "insiderOwnership" => stock with { InsiderOwnership = value },
        "epsGrowth" => stock with { EpsGrowth = value },
        "debtToEquity" => stock with { DebtToEquity = value },
        _ => throw new ArgumentOutOfRangeException(nameof(criterionId)),
    };

    private static decimal? Parse(string? value) =>
        value is null ? null : decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
}
