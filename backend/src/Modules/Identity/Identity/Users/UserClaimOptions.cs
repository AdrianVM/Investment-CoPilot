namespace InvestmentCoPilot.Identity.Users;

/// <summary>
/// Which access token claims carry the user's email address and display name (configuration section
/// <c>Auth:Claims</c>). Defaults are the OpenID Connect standard names; some providers use others.
/// </summary>
public sealed class UserClaimOptions
{
    public const string SectionName = "Auth:Claims";

    public string Email { get; set; } = "email";

    public string DisplayName { get; set; } = "name";
}
