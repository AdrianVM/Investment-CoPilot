using InvestmentCoPilot.IntegrationTests.Infrastructure;
using InvestmentCoPilot.MarketData.Contracts;
using InvestmentCoPilot.MarketData.ExchangeRates;
using InvestmentCoPilot.SharedKernel;
using Microsoft.Extensions.DependencyInjection;

namespace InvestmentCoPilot.IntegrationTests.MarketData;

/// <summary>Spec market-data: "Amounts can be converted between currencies" and "Missing exchange rates are reported, not guessed".</summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class CurrencyConversionTests(PostgresFixture postgres) : IAsyncDisposable
{
    private readonly ApiFactory factory = new(postgres);

    public ValueTask DisposeAsync() => factory.DisposeAsync();

    [Fact]
    public async Task Converts_euros_to_dollars_with_rate_source_and_as_of_time()
    {
        var result = await ConvertAsync(factory, new Money(3_200_000_000m, "EUR"), "USD");

        var converted = Assert.IsType<CurrencyConversionResult.Converted>(result);
        Assert.Equal(new Money(3_472_000_000m, "USD"), converted.Amount);
        Assert.Equal(1.085m, converted.Rate);
        Assert.Equal("Sample rates (fake provider)", converted.Source);
        Assert.NotEqual(default, converted.AsOf);
    }

    [Fact]
    public async Task Same_currency_returns_the_amount_with_rate_one()
    {
        var result = await ConvertAsync(factory, new Money(42.5m, "USD"), "USD");

        var converted = Assert.IsType<CurrencyConversionResult.Converted>(result);
        Assert.Equal(new Money(42.5m, "USD"), converted.Amount);
        Assert.Equal(1m, converted.Rate);
    }

    [Fact]
    public async Task Unsupported_currency_reports_the_rate_as_unavailable()
    {
        var result = await ConvertAsync(factory, new Money(1_000m, "SEK"), "USD");

        Assert.Equal(new CurrencyConversionResult.RateUnavailable("SEK", "USD"), result);
    }

    [Fact]
    public async Task A_failing_rate_provider_reports_the_rate_as_unavailable()
    {
        await using var failing = new ApiFactory(postgres)
            .WithSetting("MarketData:ExchangeRateProvider", FailingRateProvider.ProviderName)
            .WithServices(services => services.AddSingleton<IExchangeRateProvider, FailingRateProvider>());

        var result = await ConvertAsync(failing, new Money(1_000m, "EUR"), "USD");

        Assert.IsType<CurrencyConversionResult.RateUnavailable>(result);
    }

    private static async Task<CurrencyConversionResult> ConvertAsync(ApiFactory api, Money amount, string to)
    {
        using var scope = api.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IMarketDataModule>().ConvertAsync(amount, to);
    }

    private sealed class FailingRateProvider : IExchangeRateProvider
    {
        public const string ProviderName = "Failing";

        public string Name => ProviderName;

        public string DisplayName => "Failing test provider";

        public Task<ExchangeRate?> GetRateAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken) =>
            throw new HttpRequestException("The rate service is down.");
    }
}
