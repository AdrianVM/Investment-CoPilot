using System.Text.RegularExpressions;

namespace InvestmentCoPilot.SharedKernel;

/// <summary>
/// An amount of money in a single currency. Amounts are always <see cref="decimal"/>, never floating point,
/// and are rounded to <see cref="Scale"/> decimal places using banker's rounding (round half to even).
/// Arithmetic between different currencies is not allowed.
/// </summary>
public sealed partial record Money
{
    /// <summary>Number of decimal places stored, matching the <c>numeric(19,4)</c> database column.</summary>
    public const int Scale = 4;

    /// <summary>Total number of significant digits stored, matching the <c>numeric(19,4)</c> database column.</summary>
    public const int Precision = 19;

    public Money(decimal amount, string currency)
    {
        ArgumentNullException.ThrowIfNull(currency);
        if (!CurrencyCodePattern().IsMatch(currency))
        {
            throw new ArgumentException(
                $"'{currency}' is not an ISO 4217 currency code (three upper-case letters).", nameof(currency));
        }

        Amount = Math.Round(amount, Scale, MidpointRounding.ToEven);
        Currency = currency;
    }

    public decimal Amount { get; }

    /// <summary>ISO 4217 alphabetic currency code, for example <c>USD</c> or <c>EUR</c>.</summary>
    public string Currency { get; }

    public static Money Zero(string currency) => new(0m, currency);

    public static Money operator +(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public static Money operator -(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount - right.Amount, left.Currency);
    }

    public static Money operator -(Money value) => new(-value.Amount, value.Currency);

    public static Money operator *(Money value, decimal factor) => new(value.Amount * factor, value.Currency);

    public static Money operator *(decimal factor, Money value) => value * factor;

    public override string ToString() => $"{Amount.ToString(System.Globalization.CultureInfo.InvariantCulture)} {Currency}";

    private static void EnsureSameCurrency(Money left, Money right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        if (left.Currency != right.Currency)
        {
            throw new InvalidOperationException(
                $"Cannot combine amounts in different currencies ({left.Currency} and {right.Currency}).");
        }
    }

    [GeneratedRegex("^[A-Z]{3}$")]
    private static partial Regex CurrencyCodePattern();
}
