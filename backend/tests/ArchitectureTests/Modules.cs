using System.Reflection;

namespace InvestmentCoPilot.ArchitectureTests;

/// <summary>The modules and shared assemblies the architecture rules apply to.</summary>
internal static class Modules
{
    public sealed record Module(string Name, Assembly Contracts, Assembly Implementation)
    {
        public string ImplementationNamespace => $"InvestmentCoPilot.{Name}";

        public string PersistenceNamespace => $"InvestmentCoPilot.{Name}.Persistence";

        public override string ToString() => Name;
    }

    public static readonly Module Identity = new(
        "Identity", typeof(Identity.Contracts.IIdentityModule).Assembly, typeof(Identity.IdentityModule).Assembly);

    public static readonly Module MarketData = new(
        "MarketData", typeof(MarketData.Contracts.IMarketDataModule).Assembly, typeof(MarketData.MarketDataModule).Assembly);

    public static readonly Module Assessment = new(
        "Assessment", typeof(Assessment.Contracts.IAssessmentModule).Assembly, typeof(Assessment.AssessmentModule).Assembly);

    public static readonly IReadOnlyList<Module> All = [Identity, MarketData, Assessment];

    public static readonly Assembly SharedKernel = typeof(SharedKernel.ICurrentUser).Assembly;

    public static readonly Assembly Host = typeof(Program).Assembly;

    /// <summary>Every pair of distinct modules, as (module, other module).</summary>
    public static TheoryData<string, string> Pairs()
    {
        var data = new TheoryData<string, string>();
        foreach (var module in All)
        {
            foreach (var other in All.Where(o => o != module))
            {
                data.Add(module.Name, other.Name);
            }
        }

        return data;
    }

    public static TheoryData<string> Names()
    {
        var data = new TheoryData<string>();
        foreach (var module in All)
        {
            data.Add(module.Name);
        }

        return data;
    }

    public static Module Named(string name) => All.Single(m => m.Name == name);
}
