using InvestmentCoPilot.Identity.Contracts;
using InvestmentCoPilot.Identity.Persistence;
using InvestmentCoPilot.Identity.Users;
using InvestmentCoPilot.SharedKernel;
using InvestmentCoPilot.SharedKernel.Persistence;
using InvestmentCoPilot.SharedKernel.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InvestmentCoPilot.Identity;

public static class IdentityModule
{
    /// <summary>Registers the Identity module's services. Called once by the host.</summary>
    public static IServiceCollection AddIdentityModule(this IServiceCollection services)
    {
        services.AddModuleDbContext<IdentityDbContext>();
        services.AddOptions<UserClaimOptions>().BindConfiguration(UserClaimOptions.SectionName);
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<CurrentUser>();
        services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<CurrentUser>());
        services.AddScoped<UserProvisioning>();
        services.AddScoped<IIdentityModule, IdentityModuleFacade>();
        services.AddSingleton<IEndpointModule, UserEndpoints>();
        return services;
    }

    /// <summary>
    /// Resolves the authenticated caller to a local user (creating it on first sign-in).
    /// Must run after <c>UseAuthentication</c> and before <c>UseAuthorization</c>.
    /// </summary>
    public static IApplicationBuilder UseCurrentUser(this IApplicationBuilder app) =>
        app.UseMiddleware<UserProvisioningMiddleware>();
}
