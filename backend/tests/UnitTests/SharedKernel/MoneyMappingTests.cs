using InvestmentCoPilot.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace InvestmentCoPilot.UnitTests.SharedKernel;

public class MoneyMappingTests
{
    private sealed class Priced
    {
        public Guid Id { get; set; }
        public Money Price { get; set; } = Money.Zero("USD");
    }

    private sealed class TestContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder options) =>
            options.UseNpgsql("Host=unused"); // the model is inspected only; no connection is opened

        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            modelBuilder.Entity<Priced>().ComplexProperty(p => p.Price, MoneyMapping.Configure);
    }

    [Fact]
    public void Money_maps_to_numeric_19_4_amount_and_char_3_currency()
    {
        using var context = new TestContext();
        var price = context.Model.FindEntityType(typeof(Priced))!.FindComplexProperty(nameof(Priced.Price))!.ComplexType;

        Assert.Equal("numeric(19,4)", price.FindProperty(nameof(Money.Amount))!.GetColumnType());
        Assert.Equal("character(3)", price.FindProperty(nameof(Money.Currency))!.GetColumnType());
    }
}
