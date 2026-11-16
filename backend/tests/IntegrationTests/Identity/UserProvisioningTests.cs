using System.Net.Http.Json;
using InvestmentCoPilot.IntegrationTests.Infrastructure;

namespace InvestmentCoPilot.IntegrationTests.Identity;

/// <summary>Spec user-identity: "Local user is created on first sign-in".</summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class UserProvisioningTests(PostgresFixture postgres) : IAsyncDisposable
{
    private readonly ApiFactory factory = new ApiFactory(postgres).WithEndpoints<WhoAmIEndpoints>();

    public ValueTask DisposeAsync() => factory.DisposeAsync();

    [Fact]
    public async Task First_request_creates_exactly_one_local_user()
    {
        var user = TestUser.Create("Alice");

        var whoAmI = await WhoAmIAsync(user);

        var row = Assert.Single(await UserTable.FindBySubjectAsync(postgres.ConnectionString, user.Subject));
        Assert.Equal(whoAmI.UserId, row.Id);
        Assert.Equal(TestIssuer.Issuer, row.Issuer);
        Assert.Equal(user.Email, row.Email);
        Assert.Equal(user.Name, row.DisplayName);
    }

    [Fact]
    public async Task Local_user_id_is_not_the_provider_subject()
    {
        var user = TestUser.Create("Alice");

        var whoAmI = await WhoAmIAsync(user);

        Assert.NotEqual(Guid.Empty, whoAmI.UserId);
        Assert.DoesNotContain(whoAmI.UserId.ToString("N"), user.Subject, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Returning_user_reuses_the_existing_local_user()
    {
        var user = TestUser.Create("Alice");

        var first = await WhoAmIAsync(user);
        var second = await WhoAmIAsync(user);

        Assert.Equal(first.UserId, second.UserId);
        Assert.Single(await UserTable.FindBySubjectAsync(postgres.ConnectionString, user.Subject));
    }

    [Fact]
    public async Task Different_identities_get_different_local_users()
    {
        var alice = TestUser.Create("Alice");
        var bob = TestUser.Create("Bob");

        Assert.NotEqual((await WhoAmIAsync(alice)).UserId, (await WhoAmIAsync(bob)).UserId);
    }

    [Fact]
    public async Task Simultaneous_first_requests_create_exactly_one_user_and_all_succeed()
    {
        const int concurrentRequests = 25;
        var user = TestUser.Create("Alice");
        var token = TestIssuer.CreateToken(user);
        using var gate = new SemaphoreSlim(0);

        var requests = Enumerable.Range(0, concurrentRequests).Select(async _ =>
        {
            using var client = factory.CreateClientWithToken(token);
            await gate.WaitAsync();
            return await client.GetAsync("/api/test/whoami");
        }).ToList();
        gate.Release(concurrentRequests); // release them all at once
        var responses = await Task.WhenAll(requests);

        Assert.All(responses, r => Assert.True(r.IsSuccessStatusCode, $"Request failed with {(int)r.StatusCode}"));
        var userIds = await Task.WhenAll(responses.Select(async r => (await r.Content.ReadFromJsonAsync<WhoAmIEndpoints.WhoAmI>())!.UserId));
        var row = Assert.Single(await UserTable.FindBySubjectAsync(postgres.ConnectionString, user.Subject));
        Assert.All(userIds, id => Assert.Equal(row.Id, id));
    }

    [Fact]
    public async Task Changed_claims_update_the_stored_profile()
    {
        var user = TestUser.Create("Alice");
        await WhoAmIAsync(user);

        await WhoAmIAsync(user with { Email = "alice.new@example.test", Name = "Alice Renamed" });

        var row = Assert.Single(await UserTable.FindBySubjectAsync(postgres.ConnectionString, user.Subject));
        Assert.Equal("alice.new@example.test", row.Email);
        Assert.Equal("Alice Renamed", row.DisplayName);
    }

    [Fact]
    public async Task Missing_optional_claims_leave_the_stored_profile_unchanged()
    {
        var user = TestUser.Create("Alice");
        await WhoAmIAsync(user);

        await WhoAmIAsync(user with { Email = null, Name = null });

        var row = Assert.Single(await UserTable.FindBySubjectAsync(postgres.ConnectionString, user.Subject));
        Assert.Equal(user.Email, row.Email);
        Assert.Equal(user.Name, row.DisplayName);
    }

    [Fact]
    public async Task Claim_names_are_configurable()
    {
        await using var configured = new ApiFactory(postgres)
            .WithEndpoints<WhoAmIEndpoints>()
            .WithSetting("Auth:Claims:Email", "contact_email")
            .WithSetting("Auth:Claims:DisplayName", "nickname");
        var user = TestUser.Create("Alice") with
        {
            ExtraClaims = new Dictionary<string, string> { ["contact_email"] = "alice@contact.test", ["nickname"] = "Ali" },
        };
        using var client = configured.CreateClientAs(user);

        (await client.GetAsync("/api/test/whoami")).EnsureSuccessStatusCode();

        var row = Assert.Single(await UserTable.FindBySubjectAsync(postgres.ConnectionString, user.Subject));
        Assert.Equal("alice@contact.test", row.Email);
        Assert.Equal("Ali", row.DisplayName);
    }

    private async Task<WhoAmIEndpoints.WhoAmI> WhoAmIAsync(TestUser user)
    {
        using var client = factory.CreateClientAs(user);
        return (await client.GetFromJsonAsync<WhoAmIEndpoints.WhoAmI>("/api/test/whoami"))!;
    }
}
