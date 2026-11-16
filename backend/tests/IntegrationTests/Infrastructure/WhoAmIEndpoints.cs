using InvestmentCoPilot.SharedKernel;
using InvestmentCoPilot.SharedKernel.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InvestmentCoPilot.IntegrationTests.Infrastructure;

/// <summary>Test-only endpoint at <c>/api/test/whoami</c> that echoes the caller's token subject and local user ID.</summary>
public sealed class WhoAmIEndpoints : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder api) =>
        api.MapGet(
            "/test/whoami",
            (HttpContext context, ICurrentUser currentUser) => new WhoAmI(context.User.FindFirst("sub")?.Value, currentUser.Id));

    public sealed record WhoAmI(string? Subject, Guid UserId);
}
