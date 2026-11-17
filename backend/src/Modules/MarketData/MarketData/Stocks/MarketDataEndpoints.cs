using InvestmentCoPilot.MarketData.Contracts;
using InvestmentCoPilot.SharedKernel.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace InvestmentCoPilot.MarketData.Stocks;

internal sealed class MarketDataEndpoints : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder api)
    {
        api.MapGet("/market-data/stocks/{symbol}", GetStockFundamentalsAsync)
            .WithName("GetStockFundamentals")
            .WithTags("MarketData")
            .WithSummary("Get a stock's current fundamentals by ticker symbol")
            .WithDescription(
                "Money is in the stock's listing currency; percentages are fractions (0.15 = 15 %). " +
                "Metrics the data source does not supply are null. When the source is unavailable, " +
                "the last cached data is returned with isStale set.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }

    private static async Task<Results<Ok<StockFundamentals>, ProblemHttpResult, NotFound>> GetStockFundamentalsAsync(
        string symbol, IMarketDataModule marketData, CancellationToken cancellationToken) =>
        await marketData.GetStockFundamentalsAsync(symbol, cancellationToken) switch
        {
            StockLookupResult.Found found => TypedResults.Ok(found.Fundamentals),
            StockLookupResult.InvalidSymbol invalid => TypedResults.Problem(
                title: "Invalid ticker symbol", detail: invalid.Reason, statusCode: StatusCodes.Status400BadRequest),
            StockLookupResult.NotFound => TypedResults.NotFound(),
            StockLookupResult.Unavailable => TypedResults.Problem(
                title: "Market data unavailable",
                detail: "The market data source could not be reached and no cached data exists for this symbol. Try again later.",
                statusCode: StatusCodes.Status503ServiceUnavailable),
            var other => throw new InvalidOperationException($"Unhandled lookup result {other.GetType().Name}."),
        };
}
