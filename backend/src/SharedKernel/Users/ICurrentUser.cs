namespace InvestmentCoPilot.SharedKernel;

/// <summary>
/// The local user making the current request. Resolved by the Identity module after the access token is validated.
/// </summary>
public interface ICurrentUser
{
    /// <summary>True once a valid token has been resolved to a local user.</summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// The local user ID. This is an application-generated ID, never the identity provider's subject.
    /// Throws <see cref="InvalidOperationException"/> when there is no authenticated user.
    /// </summary>
    Guid Id { get; }
}
