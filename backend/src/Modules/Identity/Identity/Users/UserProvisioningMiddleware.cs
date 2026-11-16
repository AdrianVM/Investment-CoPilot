using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace InvestmentCoPilot.Identity.Users;

/// <summary>
/// Runs after authentication: resolves the token's (issuer, subject) to a local user, creating it on the user's
/// first request, and exposes it to all modules through <see cref="SharedKernel.ICurrentUser"/>.
/// </summary>
internal sealed class UserProvisioningMiddleware(RequestDelegate next, IOptions<UserClaimOptions> claimOptions)
{
    public async Task InvokeAsync(HttpContext context, UserProvisioning provisioning, CurrentUser currentUser)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var identity = ReadIdentity(context.User);
            if (identity is null)
            {
                // A validated token that does not identify a user (no subject) cannot be served.
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            currentUser.Set(await provisioning.EnsureUserAsync(identity, context.RequestAborted));
        }

        await next(context);
    }

    private ProviderIdentity? ReadIdentity(ClaimsPrincipal principal)
    {
        var subject = principal.FindFirst("sub");
        if (string.IsNullOrEmpty(subject?.Value) || subject.Value.Length > User.MaxSubjectLength)
        {
            return null;
        }

        var issuer = principal.FindFirst("iss")?.Value ?? subject.Issuer;
        if (string.IsNullOrEmpty(issuer) || issuer.Length > User.MaxIssuerLength)
        {
            return null;
        }

        var claims = claimOptions.Value;
        var email = principal.FindFirst(claims.Email)?.Value;
        if (email is { Length: > User.MaxEmailLength })
        {
            email = null; // not a valid address; keep the stored one
        }

        var displayName = principal.FindFirst(claims.DisplayName)?.Value;
        if (displayName is { Length: > User.MaxDisplayNameLength })
        {
            displayName = displayName[..User.MaxDisplayNameLength];
        }

        return new ProviderIdentity(issuer, subject.Value, NullIfEmpty(email), NullIfEmpty(displayName));
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
