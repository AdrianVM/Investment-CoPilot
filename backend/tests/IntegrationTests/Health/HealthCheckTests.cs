using System.Net;
using InvestmentCoPilot.IntegrationTests.Infrastructure;

namespace InvestmentCoPilot.IntegrationTests.Health;

[Collection(IntegrationTestCollection.Name)]
public class HealthCheckTests(PostgresFixture postgres)
{
    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task Health_checks_respond_without_a_token(string path)
    {
        await using var factory = new ApiFactory(postgres);
        using var client = factory.CreateClient();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Ready_reports_unhealthy_when_postgres_is_unreachable()
    {
        // Development startup migrates the database, so use Production to start without one.
        await using var factory = new ApiFactory("Host=127.0.0.1;Port=1;Timeout=1", environment: "Production");
        using var client = factory.CreateClient();

        var live = await client.GetAsync("/health/live");
        var ready = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, ready.StatusCode);
    }
}
