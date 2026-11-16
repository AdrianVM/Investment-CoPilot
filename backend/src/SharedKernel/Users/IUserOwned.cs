namespace InvestmentCoPilot.SharedKernel;

/// <summary>
/// Marks an entity as owned by a single user. Module DbContexts automatically restrict queries on these entities
/// to the current user and set <see cref="OwnerId"/> on insert; see <c>backend/README.md</c>.
/// </summary>
public interface IUserOwned
{
    Guid OwnerId { get; set; }
}
