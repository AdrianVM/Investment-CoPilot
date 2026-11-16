using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentCoPilot.SharedKernel;

/// <summary>EF Core mapping for <see cref="Money"/> properties: <c>numeric(19,4)</c> amount plus a <c>char(3)</c> currency.</summary>
public static class MoneyMapping
{
    /// <summary>
    /// Use with <c>ComplexProperty</c>, for example
    /// <c>builder.ComplexProperty(x => x.Price, MoneyMapping.Configure);</c>
    /// </summary>
    public static void Configure(ComplexPropertyBuilder<Money> money)
    {
        ArgumentNullException.ThrowIfNull(money);
        money.Property(m => m.Amount).HasPrecision(Money.Precision, Money.Scale);
        money.Property(m => m.Currency).HasMaxLength(3).IsFixedLength();
    }
}
