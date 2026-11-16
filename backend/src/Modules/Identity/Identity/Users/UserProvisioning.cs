using InvestmentCoPilot.Identity.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InvestmentCoPilot.Identity.Users;

/// <summary>The identity claims taken from a validated access token.</summary>
internal sealed record ProviderIdentity(string Issuer, string Subject, string? Email, string? DisplayName);

/// <summary>Finds the local user for a provider identity, creating it on first sight and refreshing its profile.</summary>
internal sealed class UserProvisioning(IdentityDbContext db, TimeProvider clock)
{
    public async Task<Guid> EnsureUserAsync(ProviderIdentity identity, CancellationToken cancellationToken)
    {
        var user = await FindAsync(identity, cancellationToken);
        if (user is null)
        {
            // Concurrent first requests for the same identity race here. The unique (issuer, subject) index plus
            // ON CONFLICT DO NOTHING lets exactly one insert win; every request then reads that single row.
            await db.Database.ExecuteSqlAsync(
                $"""
                INSERT INTO "identity"."users" (id, issuer, subject, email, display_name, created_at)
                VALUES ({Guid.CreateVersion7()}, {identity.Issuer}, {identity.Subject}, {identity.Email}, {identity.DisplayName}, {clock.GetUtcNow()})
                ON CONFLICT (issuer, subject) DO NOTHING
                """,
                cancellationToken);

            user = await FindAsync(identity, cancellationToken)
                ?? throw new InvalidOperationException("The user could not be created.");
        }

        // Missing optional claims leave the stored values unchanged.
        var changed = false;
        if (identity.Email is not null && identity.Email != user.Email)
        {
            user.Email = identity.Email;
            changed = true;
        }

        if (identity.DisplayName is not null && identity.DisplayName != user.DisplayName)
        {
            user.DisplayName = identity.DisplayName;
            changed = true;
        }

        if (changed)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return user.Id;
    }

    private Task<User?> FindAsync(ProviderIdentity identity, CancellationToken cancellationToken) =>
        db.Users.SingleOrDefaultAsync(u => u.Issuer == identity.Issuer && u.Subject == identity.Subject, cancellationToken);
}
