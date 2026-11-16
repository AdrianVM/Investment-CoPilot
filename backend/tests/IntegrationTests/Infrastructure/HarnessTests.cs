using System.Net.Http.Json;

namespace InvestmentCoPilot.IntegrationTests.Infrastructure;

[Collection(IntegrationTestCollection.Name)]
public class HarnessTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Test_can_call_the_api_as_a_chosen_user()
    {
        await using var factory = new ApiFactory(postgres).WithEndpoints<WhoAmIEndpoints>();
        var alice = TestUser.Create("Alice");
        using var client = factory.CreateClientAs(alice);

        var whoAmI = await client.GetFromJsonAsync<WhoAmIEndpoints.WhoAmI>("/api/test/whoami");

        Assert.Equal(alice.Subject, whoAmI!.Subject);
    }
}
