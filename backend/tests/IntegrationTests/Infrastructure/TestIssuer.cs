using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace InvestmentCoPilot.IntegrationTests.Infrastructure;

/// <summary>A user as the identity provider knows them.</summary>
public sealed record TestUser(string Subject, string? Email, string? Name)
{
    /// <summary>Additional claims to put in the token, for providers that use non-standard claim names.</summary>
    public IReadOnlyDictionary<string, string> ExtraClaims { get; init; } = new Dictionary<string, string>();

    public static TestUser Create(string name) =>
        new($"sub-{Guid.NewGuid():N}", $"{name.ToLowerInvariant()}@example.test", name);
}

/// <summary>
/// Stands in for the OpenID Connect provider: mints signed access tokens with a key the API is told to trust,
/// so integration tests need no running provider.
/// </summary>
public static class TestIssuer
{
    public const string Issuer = "https://issuer.test";
    public const string Audience = "investment-copilot-api";

    public static readonly SecurityKey SigningKey = new RsaSecurityKey(RSA.Create(2048)) { KeyId = "test-key" };

    /// <summary>Mints an access token. Override any parameter to produce an invalid token for a negative test.</summary>
    public static string CreateToken(
        TestUser user,
        string issuer = Issuer,
        string audience = Audience,
        DateTime? expires = null,
        SecurityKey? signingKey = null)
    {
        var now = DateTime.UtcNow;
        var claims = new List<Claim> { new("sub", user.Subject) };
        if (user.Email is not null)
        {
            claims.Add(new Claim("email", user.Email));
        }

        if (user.Name is not null)
        {
            claims.Add(new Claim("name", user.Name));
        }

        claims.AddRange(user.ExtraClaims.Select(c => new Claim(c.Key, c.Value)));

        var expiry = expires ?? now.AddMinutes(5);
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = expiry < now ? expiry.AddMinutes(-5) : now,
            NotBefore = expiry < now ? expiry.AddMinutes(-5) : now,
            Expires = expiry,
            SigningCredentials = new SigningCredentials(signingKey ?? SigningKey, SecurityAlgorithms.RsaSha256),
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
