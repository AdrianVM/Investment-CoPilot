using InvestmentCoPilot.Assessment.Contracts;
using InvestmentCoPilot.Assessment.Persistence;
using InvestmentCoPilot.SharedKernel.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace InvestmentCoPilot.Assessment;

public static class AssessmentModule
{
    /// <summary>Registers the Assessment module's services. Called once by the host.</summary>
    public static IServiceCollection AddAssessmentModule(this IServiceCollection services)
    {
        services.AddModuleDbContext<AssessmentDbContext>();
        services.AddScoped<IAssessmentModule, AssessmentModuleFacade>();
        return services;
    }
}

internal sealed class AssessmentModuleFacade : IAssessmentModule
{
}
