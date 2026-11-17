using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace InvestmentCoPilot.MarketData.Stocks;

/// <summary>
/// A validated, upper-case ticker symbol: 1 to 15 letters, digits, <c>.</c> or <c>-</c>, starting with a letter or digit.
/// Exchange suffixes such as <c>SAP.DE</c> are allowed.
/// </summary>
internal sealed partial record TickerSymbol
{
    public const int MaxLength = 15;

    private TickerSymbol(string value) => Value = value;

    public string Value { get; }

    /// <summary>Parses user input, ignoring surrounding whitespace and letter case.</summary>
    public static bool TryParse(string? input, [NotNullWhen(true)] out TickerSymbol? symbol)
    {
        var candidate = input?.Trim().ToUpperInvariant();
        if (candidate is null || !Pattern().IsMatch(candidate))
        {
            symbol = null;
            return false;
        }

        symbol = new TickerSymbol(candidate);
        return true;
    }

    public override string ToString() => Value;

    // \z rather than $, so a trailing newline is not accepted.
    [GeneratedRegex(@"^[A-Z0-9][A-Z0-9.\-]{0,14}\z")]
    private static partial Regex Pattern();
}
