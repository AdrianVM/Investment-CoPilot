using InvestmentCoPilot.Assessment;
using InvestmentCoPilot.Assessment.Contracts;
using InvestmentCoPilot.Assessment.Strategies;
using InvestmentCoPilot.MarketData.Contracts;
using InvestmentCoPilot.MarketData.Providers.Fake;
using InvestmentCoPilot.MarketData.Stocks;
using InvestmentCoPilot.UnitTests.MarketData;

namespace InvestmentCoPilot.UnitTests.Assessment;

/// <summary>
/// Spec strategy-assessment: assessing the fake provider's sample companies, the summary count, lookup outcomes and
/// stale data. Uses the real sample data, so these results are the ones the app and the README show.
/// </summary>
public class AssessmentServiceTests
{
    private readonly FakeMarketData marketData = new();
    private readonly AssessmentService service;

    public AssessmentServiceTests()
    {
        service = new AssessmentService(new StrategyRegistry([new PeterLynchStrategy()]), marketData);
        var samples = new FakeMarketDataProvider(new FixedTimeProvider(Stock.AsOf));
        foreach (var symbol in FakeMarketDataProvider.Symbols)
        {
            TickerSymbol.TryParse(symbol, out var parsed);
            var p = samples.FetchAsync(parsed!, CancellationToken.None).Result!;
            marketData.Stocks[symbol] = new StockLookupResult.Found(new StockFundamentals(
                symbol, p.CompanyName, p.Currency, p.Price, p.MarketCap, p.TrailingPe, p.ForwardPe, p.EpsGrowth,
                p.InstitutionalOwnership, p.InsiderOwnership, p.DebtToEquity, "Sample data", p.AsOf, IsStale: false));
        }
    }

    [Theory]
    [InlineData("LYNCH", 6, 0, 0)]
    [InlineData("EUROCO", 6, 0, 0)] // market cap converted from EUR
    [InlineData("BIGCO", 0, 6, 0)]
    [InlineData("SPARSE", 2, 1, 3)]
    [InlineData("LOSSCO", 4, 0, 2)]
    public async Task Sample_companies_give_the_documented_results(string symbol, int met, int notMet, int cannotAssess)
    {
        var assessment = await AssessAsync(symbol);

        Assert.Equal(new AssessmentSummary(met, notMet, cannotAssess, 6), assessment.Summary);
        Assert.Equal(6, assessment.Criteria.Count);
    }

    [Fact]
    public async Task Euroco_reports_the_conversion_it_used()
    {
        var marketCap = (await AssessAsync("EUROCO")).Criteria.Single(c => c.Id == "marketCap");

        Assert.Equal("EUR", marketCap.Conversion!.Original.Currency);
        Assert.Equal("USD", marketCap.Conversion.Converted.Currency);
    }

    [Fact]
    public async Task Assessment_carries_the_strategy_and_the_fundamentals_provenance()
    {
        var assessment = await AssessAsync("LYNCH");

        Assert.Equal(("peter-lynch", "Peter Lynch", "LYNCH", "Sample data", Stock.AsOf, false),
            (assessment.StrategyId, assessment.StrategyName, assessment.Symbol, assessment.Source, assessment.AsOf, assessment.IsStale));
    }

    [Fact]
    public async Task Stale_fundamentals_give_a_stale_assessment()
    {
        marketData.Stocks["OLD"] = new StockLookupResult.Found(Stock.Passing("OLD") with { IsStale = true });

        var assessment = await AssessAsync("OLD");

        Assert.True(assessment.IsStale);
        Assert.Equal(Stock.AsOf, assessment.AsOf);
    }

    [Fact]
    public async Task Strategy_id_is_case_insensitive_and_unknown_ids_are_reported()
    {
        Assert.IsType<AssessmentLookupResult.Found>(await service.AssessAsync("Peter-Lynch", "LYNCH", CancellationToken.None));
        Assert.Equal(new AssessmentLookupResult.UnknownStrategy("graham"), await service.AssessAsync("graham", "LYNCH", CancellationToken.None));
    }

    [Fact]
    public async Task Lookup_outcomes_are_passed_through()
    {
        marketData.Stocks["BAD"] = new StockLookupResult.InvalidSymbol("Not a symbol.");
        marketData.Stocks["DOWN"] = new StockLookupResult.Unavailable();

        Assert.IsType<AssessmentLookupResult.StockNotFound>(await service.AssessAsync("peter-lynch", "NOSUCH", CancellationToken.None));
        Assert.Equal(new AssessmentLookupResult.InvalidSymbol("Not a symbol."), await service.AssessAsync("peter-lynch", "BAD", CancellationToken.None));
        Assert.IsType<AssessmentLookupResult.Unavailable>(await service.AssessAsync("peter-lynch", "DOWN", CancellationToken.None));
    }

    private async Task<StockAssessment> AssessAsync(string symbol) =>
        Assert.IsType<AssessmentLookupResult.Found>(await service.AssessAsync("peter-lynch", symbol, CancellationToken.None)).Assessment;
}
