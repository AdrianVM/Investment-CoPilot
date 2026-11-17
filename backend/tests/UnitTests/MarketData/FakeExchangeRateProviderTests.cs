using InvestmentCoPilot.MarketData.ExchangeRates;
using InvestmentCoPilot.MarketData.ExchangeRates.Fake;

namespace InvestmentCoPilot.UnitTests.MarketData;

/// <summary>Spec market-data: "Fake exchange rate provider supplies fixed sample rates".</summary>
public class FakeExchangeRateProviderTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 17, 12, 0, 0, TimeSpan.Zero);
    private readonly FakeExchangeRateProvider provider = new(new FixedTimeProvider(Now));

    [Fact]
    public async Task Rates_are_repeatable_and_dated_now()
    {
        var first = await RateAsync("EUR", "USD");
        var second = await RateAsync("EUR", "USD");

        Assert.Equal(first, second);
        Assert.Equal(1.085m, first!.Rate);
        Assert.Equal(Now, first.AsOf);
    }

    [Theory]
    [InlineData("EUR")]
    [InlineData("GBP")]
    [InlineData("CHF")]
    [InlineData("JPY")]
    [InlineData("CAD")]
    public async Task Rate_from_usd_is_the_inverse_of_the_rate_to_usd_rounded_to_six_places(string currency)
    {
        var toUsd = (await RateAsync(currency, "USD"))!.Rate;
        var fromUsd = (await RateAsync("USD", currency))!.Rate;

        Assert.Equal(Math.Round(1m / toUsd, FakeExchangeRateProvider.RateDecimals), fromUsd);
    }

    [Theory]
    [InlineData("GBP", "JPY")]
    [InlineData("CHF", "CAD")]
    [InlineData("JPY", "EUR")]
    public async Task Cross_rates_in_both_directions_agree_within_a_hundredth_of_a_percent(string from, string to)
    {
        var forward = (await RateAsync(from, to))!.Rate;
        var reverse = (await RateAsync(to, from))!.Rate;

        Assert.InRange(forward * reverse, 0.9999m, 1.0001m);
    }

    [Fact]
    public async Task Cross_rates_go_through_us_dollars()
    {
        var eurGbp = (await RateAsync("EUR", "GBP"))!.Rate;

        Assert.Equal(Math.Round(1.0850m / 1.2700m, 6), eurGbp);
    }

    [Fact]
    public async Task Every_supported_currency_has_rates_to_every_other()
    {
        foreach (var from in FakeExchangeRateProvider.Currencies)
        {
            foreach (var to in FakeExchangeRateProvider.Currencies)
            {
                Assert.NotNull(await RateAsync(from, to));
            }
        }
    }

    [Theory]
    [InlineData("SEK", "USD")]
    [InlineData("USD", "SEK")]
    [InlineData("XXX", "EUR")]
    public async Task Other_currencies_have_no_rate(string from, string to)
    {
        Assert.Null(await RateAsync(from, to));
    }

    private Task<ExchangeRate?> RateAsync(string from, string to) => provider.GetRateAsync(from, to, CancellationToken.None);
}
