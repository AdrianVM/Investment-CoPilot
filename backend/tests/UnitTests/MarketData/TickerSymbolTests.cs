using InvestmentCoPilot.MarketData.Stocks;

namespace InvestmentCoPilot.UnitTests.MarketData;

/// <summary>Spec market-data: "Ticker symbols are validated and normalised".</summary>
public class TickerSymbolTests
{
    [Theory]
    [InlineData("lynch", "LYNCH")]
    [InlineData("  Lynch \t", "LYNCH")]
    [InlineData("sap.de", "SAP.DE")]
    [InlineData("BRK-B", "BRK-B")]
    [InlineData("7203.T", "7203.T")]
    [InlineData("A", "A")]
    [InlineData("ABCDEFGHIJKLMNO", "ABCDEFGHIJKLMNO")] // 15 characters
    public void Valid_symbols_are_accepted_in_upper_case(string input, string expected)
    {
        Assert.True(TickerSymbol.TryParse(input, out var symbol));
        Assert.Equal(expected, symbol.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("AB$C")]
    [InlineData("AB C")]
    [InlineData("ÄBC")]
    [InlineData(".ABC")]
    [InlineData("-ABC")]
    [InlineData("AB\nC")]
    [InlineData("ABCDEFGHIJKLMNOP")] // 16 characters
    public void Invalid_symbols_are_rejected(string? input)
    {
        Assert.False(TickerSymbol.TryParse(input, out var symbol));
        Assert.Null(symbol);
    }

    [Fact]
    public void Symbols_differing_only_in_case_are_equal()
    {
        TickerSymbol.TryParse("lynch", out var lower);
        TickerSymbol.TryParse("LYNCH", out var upper);
        Assert.Equal(lower, upper);
    }
}
