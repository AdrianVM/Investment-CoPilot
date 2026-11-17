namespace InvestmentCoPilot.Assessment.Contracts;

/// <summary>
/// Public interface of the Assessment module: assessing stocks against investment strategies.
/// Other modules may depend only on the types in this project, never on the module's implementation.
/// </summary>
public interface IAssessmentModule
{
    /// <summary>
    /// Assesses a stock against a strategy (for example <c>peter-lynch</c>) using its current fundamentals.
    /// Computed on demand; nothing is stored.
    /// </summary>
    Task<AssessmentLookupResult> AssessAsync(string strategyId, string symbol, CancellationToken cancellationToken = default);
}
