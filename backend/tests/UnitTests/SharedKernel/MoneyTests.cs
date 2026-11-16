using InvestmentCoPilot.SharedKernel;

namespace InvestmentCoPilot.UnitTests.SharedKernel;

public class MoneyTests
{
    [Fact]
    public void Amounts_with_same_value_and_currency_are_equal()
    {
        Assert.Equal(new Money(10.5m, "USD"), new Money(10.50m, "USD"));
    }

    [Fact]
    public void Amounts_in_different_currencies_are_not_equal()
    {
        Assert.NotEqual(new Money(10m, "USD"), new Money(10m, "EUR"));
    }

    [Theory]
    [InlineData("1.23455", "1.2346")] // half rounds to the even digit (6)
    [InlineData("1.23445", "1.2344")] // half rounds to the even digit (4)
    [InlineData("1.23456", "1.2346")]
    [InlineData("-1.23455", "-1.2346")]
    public void Amount_is_rounded_to_four_places_with_bankers_rounding(string input, string expected)
    {
        var money = new Money(decimal.Parse(input, System.Globalization.CultureInfo.InvariantCulture), "USD");
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), money.Amount);
    }

    [Fact]
    public void Multiplication_result_is_rounded()
    {
        var result = new Money(10m, "USD") * 0.123456m;
        Assert.Equal(1.2346m, result.Amount);
    }

    [Fact]
    public void Same_currency_amounts_can_be_added_and_subtracted()
    {
        var a = new Money(10.25m, "EUR");
        var b = new Money(2.5m, "EUR");
        Assert.Equal(new Money(12.75m, "EUR"), a + b);
        Assert.Equal(new Money(7.75m, "EUR"), a - b);
    }

    [Fact]
    public void Adding_different_currencies_throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => new Money(1m, "USD") + new Money(1m, "EUR"));
        Assert.Contains("USD", ex.Message);
        Assert.Contains("EUR", ex.Message);
    }

    [Fact]
    public void Subtracting_different_currencies_throws()
    {
        Assert.Throws<InvalidOperationException>(() => new Money(1m, "USD") - new Money(1m, "GBP"));
    }

    [Theory]
    [InlineData("usd")]
    [InlineData("US")]
    [InlineData("USDT")]
    [InlineData("")]
    public void Invalid_currency_codes_are_rejected(string currency)
    {
        Assert.Throws<ArgumentException>(() => new Money(1m, currency));
    }
}
