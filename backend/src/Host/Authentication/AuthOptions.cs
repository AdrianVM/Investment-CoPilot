using System.ComponentModel.DataAnnotations;

namespace InvestmentCoPilot.Host.Authentication;

/// <summary>
/// Identity provider settings (configuration section <c>Auth</c>). Any standards-compliant OpenID Connect
/// provider works; switching providers means changing these values only.
/// </summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>The provider's issuer URL. Its OIDC discovery document supplies the token signing keys.</summary>
    [Required]
    public string Authority { get; set; } = "";

    /// <summary>The audience access tokens must be issued for (this API's identifier at the provider).</summary>
    [Required]
    public string Audience { get; set; } = "";

    /// <summary>Require HTTPS for the discovery document. Only turn this off for a local development provider.</summary>
    public bool RequireHttpsMetadata { get; set; } = true;
}
