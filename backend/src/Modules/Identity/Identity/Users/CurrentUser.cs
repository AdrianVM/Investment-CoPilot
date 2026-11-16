using InvestmentCoPilot.SharedKernel;

namespace InvestmentCoPilot.Identity.Users;

/// <summary>The current request's local user, set by <see cref="UserProvisioningMiddleware"/>.</summary>
internal sealed class CurrentUser : ICurrentUser
{
    private Guid? id;

    public bool IsAuthenticated => id.HasValue;

    public Guid Id => id ?? throw new InvalidOperationException("There is no authenticated user for this request.");

    public void Set(Guid userId)
    {
        if (id.HasValue && id != userId)
        {
            throw new InvalidOperationException("The current user is already set for this request.");
        }

        id = userId;
    }
}
