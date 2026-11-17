using System.Text.Json.Nodes;
using InvestmentCoPilot.IntegrationTests.Infrastructure;

namespace InvestmentCoPilot.IntegrationTests.OpenApi;

[Collection(IntegrationTestCollection.Name)]
public sealed class OpenApiDocumentTests(PostgresFixture postgres) : IAsyncDisposable
{
    private readonly ApiFactory factory = new(postgres);

    public ValueTask DisposeAsync() => factory.DisposeAsync();

    [Fact]
    public async Task Numbers_are_documented_as_numbers_only()
    {
        using var client = factory.CreateClient();
        var document = JsonNode.Parse(await client.GetStringAsync("/openapi/v1.json"))!;
        var schemas = document["components"]!["schemas"]!;

        Assert.Equal("number", schemas["Money"]!["properties"]!["amount"]!["type"]!.GetValue<string>());
        var trailingPe = schemas["StockFundamentals"]!["properties"]!["trailingPe"]!;
        Assert.Equal(["null", "number"], trailingPe["type"]!.AsArray().Select(t => t!.GetValue<string>()));
        Assert.Null(trailingPe["pattern"]);
        var met = schemas["AssessmentSummary"]!["properties"]!["met"]!;
        Assert.Equal("integer", met["type"]!.GetValue<string>());
        Assert.Null(met["pattern"]);
    }
}
