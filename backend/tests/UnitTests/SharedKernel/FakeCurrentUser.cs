using InvestmentCoPilot.SharedKernel;

namespace InvestmentCoPilot.UnitTests.SharedKernel;

internal sealed class FakeCurrentUser(Guid? id = null) : ICurrentUser
{
    public Guid? UserId { get; set; } = id;

    public bool IsAuthenticated => UserId.HasValue;

    public Guid Id => UserId ?? throw new InvalidOperationException("No authenticated user.");
}
