using InvestmentCoPilot.SharedKernel;
using InvestmentCoPilot.SharedKernel.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InvestmentCoPilot.Assessment.Persistence;

internal sealed class AssessmentDbContext(DbContextOptions<AssessmentDbContext> options, ICurrentUser currentUser)
    : ModuleDbContext<AssessmentDbContext>(options, currentUser), IModuleDbContext
{
    public static string Schema => "assessment";
}
