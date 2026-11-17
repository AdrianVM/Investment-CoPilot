namespace InvestmentCoPilot.Assessment.Strategies;

/// <summary>All available strategies, looked up by identifier (case-insensitive).</summary>
internal sealed class StrategyRegistry(IEnumerable<IStrategy> strategies)
{
    private readonly Dictionary<string, IStrategy> byId = strategies.ToDictionary(s => s.Id, StringComparer.OrdinalIgnoreCase);

    public IStrategy? Find(string id) => byId.GetValueOrDefault(id);
}
