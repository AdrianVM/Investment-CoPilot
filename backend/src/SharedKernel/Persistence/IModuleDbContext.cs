namespace InvestmentCoPilot.SharedKernel.Persistence;

/// <summary>Declares the Postgres schema a module's DbContext owns.</summary>
public interface IModuleDbContext
{
    /// <summary>The module's Postgres schema. Each module uses its own; no two modules may share one.</summary>
    static abstract string Schema { get; }
}
