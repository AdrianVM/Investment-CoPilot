namespace InvestmentCoPilot.Identity.Contracts;

/// <summary>A user's profile, with the email address and display name last provided by the identity provider.</summary>
/// <param name="Id">The local user ID.</param>
/// <param name="Email">Email address, if the provider supplied one.</param>
/// <param name="DisplayName">Display name, if the provider supplied one.</param>
public sealed record UserProfile(Guid Id, string? Email, string? DisplayName);
