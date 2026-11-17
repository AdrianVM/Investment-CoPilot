using InvestmentCoPilot.Assessment.Contracts;
using InvestmentCoPilot.MarketData.Contracts;
using InvestmentCoPilot.SharedKernel;

namespace InvestmentCoPilot.Assessment.Strategies;

/// <summary>
/// Market capitalisation below a threshold in a fixed currency. A market cap in another currency is converted at
/// the current exchange rate, and the conversion is reported; without a rate the criterion cannot be assessed.
/// </summary>
internal sealed class MarketCapCriterion(Money threshold) : ICriterion
{
    public string Id => "marketCap";

    public string Name => "Market capitalisation";

    public async Task<CriterionAssessment> EvaluateAsync(StockFundamentals stock, IMarketDataModule marketData, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stock);
        ArgumentNullException.ThrowIfNull(marketData);

        if (stock.MarketCap is null)
        {
            return Thresholds.Unassessable(this, [MeasureOf(null)], UnassessableReason.DataNotAvailable);
        }

        if (stock.MarketCap.Currency == threshold.Currency)
        {
            return Compare(stock.MarketCap.Amount, conversion: null);
        }

        return await marketData.ConvertAsync(stock.MarketCap, threshold.Currency, cancellationToken) switch
        {
            CurrencyConversionResult.Converted c => Compare(
                c.Amount.Amount, new CurrencyConversion(stock.MarketCap, c.Amount, c.Rate, c.Source, c.AsOf)),
            _ => Thresholds.Unassessable(this, [MeasureOf(null)], UnassessableReason.ExchangeRateNotAvailable),
        };
    }

    private CriterionAssessment Compare(decimal amount, CurrencyConversion? conversion)
    {
        var verdict = Thresholds.Meets(amount, Comparison.LessThan, threshold.Amount) ? Verdict.Meets : Verdict.DoesNotMeet;
        return new(Id, Name, verdict, [MeasureOf(amount)], Reason: null, conversion);
    }

    private Measure MeasureOf(decimal? amount) =>
        new(Id, "Market capitalisation", amount, MeasureUnit.Money, threshold.Currency, Comparison.LessThan, threshold.Amount);
}
