using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using InvestmentCoPilot.IntegrationTests.Infrastructure;
using InvestmentCoPilot.IntegrationTests.MarketData;
using InvestmentCoPilot.MarketData.Providers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace InvestmentCoPilot.IntegrationTests.Assessment;

/// <summary>Spec strategy-assessment: "A stock can be assessed against the Peter Lynch strategy", over HTTP.</summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class StockAssessmentEndpointTests(PostgresFixture postgres) : IAsyncLifetime
{
    private ApiFactory factory = null!;

    public async Task InitializeAsync() => factory = new ApiFactory(await postgres.CreateEmptyDatabaseAsync());

    public async Task DisposeAsync() => await factory.DisposeAsync();

    [Fact]
    public async Task Assessing_lynch_returns_six_criteria_in_order_with_string_verdicts()
    {
        using var client = factory.CreateClientAs(TestUser.Create("Alice"));

        var json = JsonNode.Parse(await client.GetStringAsync("/api/assessments/peter-lynch/stocks/lynch"))!;

        Assert.Equal("peter-lynch", json["strategyId"]!.GetValue<string>());
        Assert.Equal("LYNCH", json["symbol"]!.GetValue<string>());
        var criteria = json["criteria"]!.AsArray();
        Assert.Equal(
            ["pe", "institutionalOwnership", "insiderOwnership", "epsGrowth", "debtToEquity", "marketCap"],
            criteria.Select(c => c!["id"]!.GetValue<string>()));
        Assert.All(criteria, c => Assert.Equal("meets", c!["verdict"]!.GetValue<string>()));
        Assert.Equal("lessThan", criteria[0]!["measures"]![0]!["comparison"]!.GetValue<string>());
        Assert.Equal("fraction", criteria[2]!["measures"]![0]!["unit"]!.GetValue<string>());
        Assert.Equal(6, json["summary"]!["met"]!.GetValue<int>());
    }

    [Fact]
    public async Task Euro_listing_reports_the_market_cap_conversion()
    {
        using var client = factory.CreateClientAs(TestUser.Create("Alice"));

        var json = JsonNode.Parse(await client.GetStringAsync("/api/assessments/peter-lynch/stocks/EUROCO"))!;

        var conversion = json["criteria"]![5]!["conversion"]!;
        Assert.Equal("EUR", conversion["original"]!["currency"]!.GetValue<string>());
        Assert.Equal("USD", conversion["converted"]!["currency"]!.GetValue<string>());
        Assert.Equal(1.085m, conversion["rate"]!.GetValue<decimal>());
    }

    [Fact]
    public async Task Missing_data_is_reported_as_cannot_assess_with_a_reason()
    {
        using var client = factory.CreateClientAs(TestUser.Create("Alice"));

        var json = JsonNode.Parse(await client.GetStringAsync("/api/assessments/peter-lynch/stocks/SPARSE"))!;

        var institutional = json["criteria"]![1]!;
        Assert.Equal("cannotAssess", institutional["verdict"]!.GetValue<string>());
        Assert.Equal("dataNotAvailable", institutional["reason"]!.GetValue<string>());
        Assert.Equal(3, json["summary"]!["cannotAssess"]!.GetValue<int>());
    }

    [Theory]
    [InlineData("/api/assessments/graham/stocks/LYNCH", HttpStatusCode.NotFound, "Unknown strategy")]
    [InlineData("/api/assessments/peter-lynch/stocks/NOSUCH", HttpStatusCode.NotFound, "Stock not found")]
    [InlineData("/api/assessments/peter-lynch/stocks/AB$C", HttpStatusCode.BadRequest, "Invalid ticker symbol")]
    public async Task Errors_return_problem_details(string url, HttpStatusCode status, string title)
    {
        using var client = factory.CreateClientAs(TestUser.Create("Alice"));

        var response = await client.GetAsync(url);

        Assert.Equal(status, response.StatusCode);
        Assert.Equal(title, (await response.Content.ReadFromJsonAsync<ProblemDetails>())!.Title);
    }

    [Fact]
    public async Task Unavailable_market_data_returns_503()
    {
        var failing = new TestMarketDataProvider { Fails = true };
        await using var failingFactory = new ApiFactory(await postgres.CreateEmptyDatabaseAsync())
            .WithSetting("MarketData:Provider", TestMarketDataProvider.ProviderName)
            .WithServices(services => services.AddSingleton<IMarketDataProvider>(failing));
        using var client = failingFactory.CreateClientAs(TestUser.Create("Alice"));

        var response = await client.GetAsync("/api/assessments/peter-lynch/stocks/ACME");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task Request_without_a_token_returns_401()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/assessments/peter-lynch/stocks/LYNCH");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
