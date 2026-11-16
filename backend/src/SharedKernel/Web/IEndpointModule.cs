using Microsoft.AspNetCore.Routing;

namespace InvestmentCoPilot.SharedKernel.Web;

/// <summary>
/// Contributes HTTP endpoints. Register implementations in DI; the host maps every one of them under <c>/api</c>,
/// where authentication is required unless an endpoint explicitly allows anonymous access.
/// </summary>
public interface IEndpointModule
{
    void MapEndpoints(IEndpointRouteBuilder api);
}
