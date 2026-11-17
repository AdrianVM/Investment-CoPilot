using System.Net;
using System.Net.Http.Json;
using InvestmentCoPilot.IntegrationTests.Infrastructure;
using InvestmentCoPilot.MarketData.Contracts;
using InvestmentCoPilot.MarketData.Providers;
using InvestmentCoPilot.SharedKernel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace InvestmentCoPilot.IntegrationTests.MarketData;

/// <summary>Spec market-data: lookup over HTTP, with the fake provider from the default settings.</summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class StockFundamentalsEndpointTests(PostgresFixture postgres) : IAsyncLifetime
{
    private ApiFactory factory = null!;

    public async Task InitializeAsync() => factory = new ApiFactory(await postgres.CreateEmptyDatabaseAsync());

    public async Task DisposeAsync() => await factory.DisposeAsync();

    [Fact]
    public async Task Known_symbol_returns_its_fundamentals_with_source_and_as_of_time()
    {
        using var client = factory.CreateClientAs(TestUser.Create("Alice"));

        var stock = await client.GetFromJsonAsync<StockFundamentals>("/api/market-data/stocks/LYNCH");

        Assert.NotNull(stock);
        Assert.Equal("LYNCH", stock.Symbol);
        Assert.Equal(new Money(42.50m, "USD"), stock.Price);
        Assert.Equal(0.26m, stock.InsiderOwnership); // a fraction, not 26
        Assert.Equal("Sample data (fake provider)", stock.Source);
        Assert.NotEqual(default, stock.AsOf);
        Assert.False(stock.IsStale);
    }

    [Fact]
    public async Task Missing_metrics_are_null_in_the_response()
    {
        using var client = factory.CreateClientAs(TestUser.Create("Alice"));

        var json = await client.GetStringAsync("/api/market-data/stocks/SPARSE");

        Assert.Contains("\"forwardPe\":null", json, StringComparison.Ordinal);
        Assert.Contains("\"debtToEquity\":null", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Non_usd_stock_keeps_its_listing_currency()
    {
        using var client = factory.CreateClientAs(TestUser.Create("Alice"));

        var stock = await client.GetFromJsonAsync<StockFundamentals>("/api/market-data/stocks/EUROCO");

        Assert.Equal("EUR", stock!.Currency);
        Assert.Equal("EUR", stock.Price!.Currency);
        Assert.Equal("EUR", stock.MarketCap!.Currency);
    }

    [Fact]
    public async Task Lower_case_symbol_returns_the_upper_case_symbol()
    {
        using var client = factory.CreateClientAs(TestUser.Create("Alice"));

        var stock = await client.GetFromJsonAsync<StockFundamentals>("/api/market-data/stocks/lynch");

        Assert.Equal("LYNCH", stock!.Symbol);
    }

    [Theory]
    [InlineData("AB$C")]
    [InlineData("ABCDEFGHIJKLMNOP")]
    [InlineData("%20")]
    public async Task Invalid_symbol_returns_400_problem_details(string symbol)
    {
        using var client = factory.CreateClientAs(TestUser.Create("Alice"));

        var response = await client.GetAsync($"/api/market-data/stocks/{symbol}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Invalid ticker symbol", problem!.Title);
    }

    [Fact]
    public async Task Unknown_symbol_returns_404()
    {
        using var client = factory.CreateClientAs(TestUser.Create("Alice"));

        var response = await client.GetAsync("/api/market-data/stocks/NOSUCH");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Provider_failure_without_cached_data_returns_503()
    {
        var failing = new TestMarketDataProvider { Fails = true };
        await using var failingFactory = new ApiFactory(await postgres.CreateEmptyDatabaseAsync())
            .WithSetting("MarketData:Provider", TestMarketDataProvider.ProviderName)
            .WithServices(services => services.AddSingleton<IMarketDataProvider>(failing));
        using var client = failingFactory.CreateClientAs(TestUser.Create("Alice"));

        var response = await client.GetAsync("/api/market-data/stocks/ACME");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Market data unavailable", problem!.Title);
    }

    [Fact]
    public async Task Request_without_a_token_returns_401()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/market-data/stocks/LYNCH");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Different_users_get_the_same_data()
    {
        using var alice = factory.CreateClientAs(TestUser.Create("Alice"));
        using var bob = factory.CreateClientAs(TestUser.Create("Bob"));

        var forAlice = await alice.GetFromJsonAsync<StockFundamentals>("/api/market-data/stocks/LYNCH");
        var forBob = await bob.GetFromJsonAsync<StockFundamentals>("/api/market-data/stocks/LYNCH");

        Assert.Equal(forAlice, forBob); // Bob is served Alice's cached row, including its as-of time
    }
}
