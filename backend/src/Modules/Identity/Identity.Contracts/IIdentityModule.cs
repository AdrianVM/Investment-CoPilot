namespace InvestmentCoPilot.Identity.Contracts;

/// <summary>
/// Public interface of the Identity module: users and their identities at the external sign-in provider.
/// Other modules may depend only on the types in this project, never on the module's implementation.
/// </summary>
public interface IIdentityModule
{
    /// <summary>Returns the user's profile, or <c>null</c> if no such user exists.</summary>
    Task<UserProfile?> FindProfileAsync(Guid userId, CancellationToken cancellationToken = default);
}
