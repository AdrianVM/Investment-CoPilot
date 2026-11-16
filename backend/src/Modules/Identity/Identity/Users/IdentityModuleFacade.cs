using InvestmentCoPilot.Identity.Contracts;
using InvestmentCoPilot.Identity.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InvestmentCoPilot.Identity.Users;

internal sealed class IdentityModuleFacade(IdentityDbContext db) : IIdentityModule
{
    public Task<UserProfile?> FindProfileAsync(Guid userId, CancellationToken cancellationToken = default) =>
        db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new UserProfile(u.Id, u.Email, u.DisplayName))
            .SingleOrDefaultAsync(cancellationToken);
}
