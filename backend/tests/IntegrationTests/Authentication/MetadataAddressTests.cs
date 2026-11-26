using System.Net;
using InvestmentCoPilot.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace InvestmentCoPilot.IntegrationTests.Authentication;

/// <summary>
/// Auth:MetadataAddress lets the API fetch discovery and keys from another address than the issuer (Docker Compose),
/// without weakening issuer validation (spec user-identity: "Access tokens are validated against the configured provider").
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class MetadataAddressTests(PostgresFixture postgres)
{
    private const string InternalAddress = "http://provider.internal/.well-known/openid-configuration";

    [Fact]
    public async Task Unset_metadata_address_is_derived_from_the_authority()
    {
        await using var factory = new ApiFactory(postgres);

        Assert.Equal($"{TestIssuer.Issuer}/.well-known/openid-configuration", JwtOptions(factory).MetadataAddress);
    }

    [Fact]
    public async Task Metadata_address_is_used_for_discovery_and_tokens_are_still_accepted()
    {
        await using var factory = new ApiFactory(postgres).WithSetting("Auth:MetadataAddress", InternalAddress);
        using var client = factory.CreateClientAs(TestUser.Create("Alice"));

        var response = await client.GetAsync("/api/me");

        Assert.Equal(InternalAddress, JwtOptions(factory).MetadataAddress);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Issuer_is_still_validated_when_a_metadata_address_is_set()
    {
        await using var factory = new ApiFactory(postgres).WithSetting("Auth:MetadataAddress", InternalAddress);
        using var client = factory.CreateClientWithToken(
            TestIssuer.CreateToken(TestUser.Create("Alice"), issuer: "https://another-issuer.test"));

        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static JwtBearerOptions JwtOptions(ApiFactory factory) =>
        factory.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme);
}
