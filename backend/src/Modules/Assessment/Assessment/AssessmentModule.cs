using InvestmentCoPilot.Assessment.Contracts;
using InvestmentCoPilot.Assessment.Persistence;
using InvestmentCoPilot.Assessment.Strategies;
using InvestmentCoPilot.SharedKernel.Persistence;
using InvestmentCoPilot.SharedKernel.Web;
using Microsoft.Extensions.DependencyInjection;

namespace InvestmentCoPilot.Assessment;

public static class AssessmentModule
{
    /// <summary>Registers the Assessment module's services. Called once by the host.</summary>
    public static IServiceCollection AddAssessmentModule(this IServiceCollection services)
    {
        services.AddModuleDbContext<AssessmentDbContext>();

        // Every available strategy, looked up by identifier.
        services.AddSingleton<IStrategy, PeterLynchStrategy>();
        services.AddSingleton<StrategyRegistry>();

        services.AddScoped<AssessmentService>();
        services.AddScoped<IAssessmentModule, AssessmentModuleFacade>();
        services.AddSingleton<IEndpointModule, AssessmentEndpoints>();
        return services;
    }
}

internal sealed class AssessmentModuleFacade(AssessmentService assessments) : IAssessmentModule
{
    public Task<AssessmentLookupResult> AssessAsync(string strategyId, string symbol, CancellationToken cancellationToken = default) =>
        assessments.AssessAsync(strategyId, symbol, cancellationToken);
}
