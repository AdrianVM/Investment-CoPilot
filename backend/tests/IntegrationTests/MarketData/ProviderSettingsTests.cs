using InvestmentCoPilot.IntegrationTests.Infrastructure;
using Microsoft.Extensions.Options;

namespace InvestmentCoPilot.IntegrationTests.MarketData;

/// <summary>Spec market-data: "The provider is chosen through settings".</summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class ProviderSettingsTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Startup_fails_when_the_provider_is_unknown()
    {
        await using var factory = new ApiFactory(postgres).WithSetting("MarketData:Provider", "NoSuchVendor");

        var error = Assert.Throws<OptionsValidationException>(() => factory.Services);

        Assert.Contains("MarketData:Provider", error.Message, StringComparison.Ordinal);
        Assert.Contains("'NoSuchVendor'", error.Message, StringComparison.Ordinal);
        Assert.Contains("Valid providers: Fake", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Startup_fails_when_the_cache_age_is_not_positive()
    {
        await using var factory = new ApiFactory(postgres).WithSetting("MarketData:CacheMaxAge", "00:00:00");

        var error = Assert.Throws<OptionsValidationException>(() => factory.Services);

        Assert.Contains("MarketData:CacheMaxAge", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Default_settings_select_the_fake_provider()
    {
        await using var factory = new ApiFactory(postgres);

        _ = factory.Services; // starts without throwing
    }
}
