using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using InvestmentCoPilot.IntegrationTests.Infrastructure;
using Microsoft.IdentityModel.Tokens;

namespace InvestmentCoPilot.IntegrationTests.Authentication;

/// <summary>
/// Spec user-identity: "API requires authentication" and "Access tokens are validated against the configured provider".
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class AuthenticationTests(PostgresFixture postgres) : IAsyncDisposable
{
    private const string ProtectedPath = "/api/test/whoami";

    private readonly ApiFactory factory = new ApiFactory(postgres).WithEndpoints<WhoAmIEndpoints>();
    private readonly TestUser user = TestUser.Create("Alice");

    public ValueTask DisposeAsync() => factory.DisposeAsync();

    [Fact]
    public async Task Request_without_a_token_is_rejected_with_no_data()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(ProtectedPath);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task Health_check_does_not_require_a_token(string path)
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Valid_token_is_processed_as_that_user()
    {
        using var client = factory.CreateClientAs(user);

        var response = await client.GetAsync(ProtectedPath);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var whoAmI = await response.Content.ReadFromJsonAsync<WhoAmIEndpoints.WhoAmI>();
        Assert.Equal(user.Subject, whoAmI!.Subject);
    }

    [Fact]
    public async Task Expired_token_is_rejected()
    {
        var token = TestIssuer.CreateToken(user, expires: DateTime.UtcNow.AddSeconds(-1));

        await AssertRejectedAsync(token);
    }

    [Fact]
    public async Task Token_from_another_issuer_is_rejected()
    {
        var token = TestIssuer.CreateToken(user, issuer: "https://another-issuer.test");

        await AssertRejectedAsync(token);
    }

    [Fact]
    public async Task Token_for_another_audience_is_rejected()
    {
        var token = TestIssuer.CreateToken(user, audience: "some-other-api");

        await AssertRejectedAsync(token);
    }

    [Fact]
    public async Task Token_signed_with_an_untrusted_key_is_rejected()
    {
        var untrustedKey = new RsaSecurityKey(RSA.Create(2048)) { KeyId = TestIssuer.SigningKey.KeyId };
        var token = TestIssuer.CreateToken(user, signingKey: untrustedKey);

        await AssertRejectedAsync(token);
    }

    [Fact]
    public async Task Token_with_a_modified_payload_is_rejected()
    {
        var parts = TestIssuer.CreateToken(user).Split('.');
        var payload = Encoding.UTF8.GetString(Base64UrlEncoder.DecodeBytes(parts[1]));
        var forged = payload.Replace(user.Subject, "sub-someone-else", StringComparison.Ordinal);
        var token = $"{parts[0]}.{Base64UrlEncoder.Encode(forged)}.{parts[2]}";

        await AssertRejectedAsync(token);
    }

    [Fact]
    public async Task Malformed_token_is_rejected()
    {
        await AssertRejectedAsync("not-a-jwt");
    }

    private async Task AssertRejectedAsync(string token)
    {
        using var client = factory.CreateClientWithToken(token);

        var response = await client.GetAsync(ProtectedPath);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsStringAsync());
    }
}
