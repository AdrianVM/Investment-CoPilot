using InvestmentCoPilot.MarketData.ExchangeRates;
using InvestmentCoPilot.MarketData.Providers;
using Microsoft.Extensions.Options;

namespace InvestmentCoPilot.MarketData;

/// <summary>Market data settings (configuration section <c>MarketData</c>).</summary>
internal sealed class MarketDataOptions
{
    public const string SectionName = "MarketData";

    /// <summary>Name of the provider to use, matching <see cref="IMarketDataProvider.Name"/> (case-insensitive).</summary>
    public string? Provider { get; set; }

    /// <summary>Name of the exchange rate provider, matching <see cref="IExchangeRateProvider.Name"/> (case-insensitive).</summary>
    public string? ExchangeRateProvider { get; set; }

    /// <summary>How long fetched fundamentals are reused before they are fetched again.</summary>
    public TimeSpan CacheMaxAge { get; set; } = TimeSpan.FromHours(24);
}

/// <summary>Checks the settings against the registered providers, so a typo fails at startup rather than on first use.</summary>
internal sealed class MarketDataOptionsValidator(
    IEnumerable<IMarketDataProvider> providers,
    IEnumerable<IExchangeRateProvider> rateProviders) : IValidateOptions<MarketDataOptions>
{
    public ValidateOptionsResult Validate(string? name, MarketDataOptions options)
    {
        var failures = new List<string>();
        var valid = string.Join(", ", providers.Select(p => p.Name).Order(StringComparer.Ordinal));

        if (string.IsNullOrWhiteSpace(options.Provider))
        {
            failures.Add($"{MarketDataOptions.SectionName}:{nameof(MarketDataOptions.Provider)} is not set. Valid providers: {valid}.");
        }
        else if (!providers.Any(p => string.Equals(p.Name, options.Provider, StringComparison.OrdinalIgnoreCase)))
        {
            failures.Add(
                $"{MarketDataOptions.SectionName}:{nameof(MarketDataOptions.Provider)} is '{options.Provider}', which is not a known provider. Valid providers: {valid}.");
        }

        var validRateProviders = string.Join(", ", rateProviders.Select(p => p.Name).Order(StringComparer.Ordinal));
        if (string.IsNullOrWhiteSpace(options.ExchangeRateProvider))
        {
            failures.Add(
                $"{MarketDataOptions.SectionName}:{nameof(MarketDataOptions.ExchangeRateProvider)} is not set. Valid providers: {validRateProviders}.");
        }
        else if (!rateProviders.Any(p => string.Equals(p.Name, options.ExchangeRateProvider, StringComparison.OrdinalIgnoreCase)))
        {
            failures.Add(
                $"{MarketDataOptions.SectionName}:{nameof(MarketDataOptions.ExchangeRateProvider)} is '{options.ExchangeRateProvider}', which is not a known provider. Valid providers: {validRateProviders}.");
        }

        if (options.CacheMaxAge <= TimeSpan.Zero)
        {
            failures.Add($"{MarketDataOptions.SectionName}:{nameof(MarketDataOptions.CacheMaxAge)} must be greater than zero.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
