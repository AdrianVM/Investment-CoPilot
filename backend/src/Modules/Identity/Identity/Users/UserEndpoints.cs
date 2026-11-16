using InvestmentCoPilot.Identity.Contracts;
using InvestmentCoPilot.SharedKernel;
using InvestmentCoPilot.SharedKernel.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace InvestmentCoPilot.Identity.Users;

internal sealed class UserEndpoints : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder api)
    {
        api.MapGet("/me", GetCurrentUserProfileAsync)
            .WithName("GetCurrentUser")
            .WithTags("Users")
            .WithSummary("Get the signed-in user's profile");
    }

    private static async Task<Results<Ok<UserProfile>, NotFound>> GetCurrentUserProfileAsync(
        ICurrentUser currentUser, IIdentityModule identity, CancellationToken cancellationToken)
    {
        var profile = await identity.FindProfileAsync(currentUser.Id, cancellationToken);
        return profile is null ? TypedResults.NotFound() : TypedResults.Ok(profile);
    }
}
