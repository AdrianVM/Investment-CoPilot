using System.Net;
using System.Net.Http.Json;
using InvestmentCoPilot.Identity.Contracts;
using InvestmentCoPilot.IntegrationTests.Infrastructure;

namespace InvestmentCoPilot.IntegrationTests.Identity;

/// <summary>Spec user-identity: "Current user can view their profile".</summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class CurrentUserProfileTests(PostgresFixture postgres) : IAsyncDisposable
{
    private readonly ApiFactory factory = new(postgres);

    public ValueTask DisposeAsync() => factory.DisposeAsync();

    [Fact]
    public async Task Fetching_the_profile_returns_id_email_and_display_name()
    {
        var user = TestUser.Create("Alice");
        using var client = factory.CreateClientAs(user);

        var profile = await client.GetFromJsonAsync<UserProfile>("/api/me");

        var row = Assert.Single(await UserTable.FindBySubjectAsync(postgres.ConnectionString, user.Subject));
        Assert.Equal(new UserProfile(row.Id, user.Email, user.Name), profile);
    }

    [Fact]
    public async Task Profile_reflects_changes_at_the_provider()
    {
        var user = TestUser.Create("Alice");
        using (var before = factory.CreateClientAs(user))
        {
            await before.GetFromJsonAsync<UserProfile>("/api/me");
        }

        var renamed = user with { Email = "alice.new@example.test", Name = "Alice Renamed" };
        using var after = factory.CreateClientAs(renamed);
        var profile = await after.GetFromJsonAsync<UserProfile>("/api/me");

        Assert.Equal("alice.new@example.test", profile!.Email);
        Assert.Equal("Alice Renamed", profile.DisplayName);
    }

    [Fact]
    public async Task Each_user_sees_only_their_own_profile()
    {
        var alice = TestUser.Create("Alice");
        var bob = TestUser.Create("Bob");
        using var aliceClient = factory.CreateClientAs(alice);
        using var bobClient = factory.CreateClientAs(bob);

        var aliceProfile = await aliceClient.GetFromJsonAsync<UserProfile>("/api/me");
        var bobProfile = await bobClient.GetFromJsonAsync<UserProfile>("/api/me");

        Assert.Equal(alice.Email, aliceProfile!.Email);
        Assert.Equal(bob.Email, bobProfile!.Email);
        Assert.NotEqual(aliceProfile.Id, bobProfile.Id);
    }

    [Fact]
    public async Task Profile_requires_authentication()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
