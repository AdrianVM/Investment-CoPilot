using InvestmentCoPilot.Assessment.Contracts;
using InvestmentCoPilot.SharedKernel.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace InvestmentCoPilot.Assessment;

internal sealed class AssessmentEndpoints : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder api)
    {
        api.MapGet("/assessments/{strategy}/stocks/{symbol}", GetStockAssessmentAsync)
            .WithName("GetStockAssessment")
            .WithTags("Assessment")
            .WithSummary("Assess a stock against an investment strategy")
            .WithDescription(
                "Strategy 'peter-lynch' checks Peter Lynch's six criteria. Each criterion is met, not met, or cannot be " +
                "assessed (missing data or no exchange rate). Values use the fundamentals' units: fractions for " +
                "percentages, plain ratios, and money with its currency. Computed on demand; for information only.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }

    private static async Task<Results<Ok<StockAssessment>, ProblemHttpResult>> GetStockAssessmentAsync(
        string strategy, string symbol, IAssessmentModule assessments, CancellationToken cancellationToken) =>
        await assessments.AssessAsync(strategy, symbol, cancellationToken) switch
        {
            AssessmentLookupResult.Found found => TypedResults.Ok(found.Assessment),
            AssessmentLookupResult.UnknownStrategy unknown => TypedResults.Problem(
                title: "Unknown strategy", detail: $"There is no strategy '{unknown.StrategyId}'.", statusCode: StatusCodes.Status404NotFound),
            AssessmentLookupResult.StockNotFound => TypedResults.Problem(
                title: "Stock not found", detail: "The market data source does not know this symbol.", statusCode: StatusCodes.Status404NotFound),
            AssessmentLookupResult.InvalidSymbol invalid => TypedResults.Problem(
                title: "Invalid ticker symbol", detail: invalid.Reason, statusCode: StatusCodes.Status400BadRequest),
            AssessmentLookupResult.Unavailable => TypedResults.Problem(
                title: "Market data unavailable",
                detail: "The market data source could not be reached and no cached data exists for this symbol. Try again later.",
                statusCode: StatusCodes.Status503ServiceUnavailable),
            var other => throw new InvalidOperationException($"Unhandled assessment result {other.GetType().Name}."),
        };
}
