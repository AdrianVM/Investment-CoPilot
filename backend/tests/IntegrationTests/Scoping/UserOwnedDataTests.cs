using System.Net;
using System.Net.Http.Json;
using InvestmentCoPilot.IntegrationTests.Infrastructure;
using InvestmentCoPilot.SharedKernel.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace InvestmentCoPilot.IntegrationTests.Scoping;

/// <summary>Spec user-identity: "User-owned data is accessible only to its owner".</summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class UserOwnedDataTests(PostgresFixture postgres) : IAsyncLifetime
{
    private readonly TestUser alice = TestUser.Create("Alice");
    private readonly TestUser bob = TestUser.Create("Bob");
    private ApiFactory factory = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await postgres.CreateEmptyDatabaseAsync();
        factory = new ApiFactory(connectionString)
            .WithEndpoints<NotesEndpoints>()
            .WithServices(services => services.AddModuleDbContext<NotesDbContext>(connectionString));

        // The test module has no migrations; create its table from the model.
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NotesDbContext>();
        await db.GetService<IRelationalDatabaseCreator>().CreateTablesAsync();
    }

    public async Task DisposeAsync() => await factory.DisposeAsync();

    [Fact]
    public async Task Listing_returns_only_the_users_own_records()
    {
        await CreateAsync(alice, "alice 1");
        await CreateAsync(alice, "alice 2");
        await CreateAsync(bob, "bob 1");

        var aliceNotes = await ListAsync(alice);
        var bobNotes = await ListAsync(bob);

        Assert.Equal(["alice 1", "alice 2"], aliceNotes.Select(n => n.Text).Order());
        Assert.Equal(["bob 1"], bobNotes.Select(n => n.Text));
    }

    [Fact]
    public async Task Owner_can_read_their_record()
    {
        var note = await CreateAsync(alice, "mine");
        using var client = factory.CreateClientAs(alice);

        var read = await client.GetFromJsonAsync<NoteDto>($"/api/test/notes/{note.Id}");

        Assert.Equal("mine", read!.Text);
    }

    [Fact]
    public async Task Reading_another_users_record_returns_404()
    {
        var bobsNote = await CreateAsync(bob, "secret");
        using var client = factory.CreateClientAs(alice);

        var response = await client.GetAsync($"/api/test/notes/{bobsNote.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain("secret", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Another_users_record_is_indistinguishable_from_a_missing_one()
    {
        var bobsNote = await CreateAsync(bob, "secret");
        using var client = factory.CreateClientAs(alice);

        var existing = await client.GetAsync($"/api/test/notes/{bobsNote.Id}");
        var missing = await client.GetAsync($"/api/test/notes/{Guid.NewGuid()}");

        Assert.Equal(missing.StatusCode, existing.StatusCode);
        Assert.Equal(await missing.Content.ReadAsStringAsync(), await existing.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Updating_another_users_record_returns_404_and_leaves_it_unchanged()
    {
        var bobsNote = await CreateAsync(bob, "original");
        using var client = factory.CreateClientAs(alice);

        var response = await client.PutAsJsonAsync($"/api/test/notes/{bobsNote.Id}", new UpdateNoteRequest("hacked"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("original", Assert.Single(await ListAsync(bob)).Text);
    }

    [Fact]
    public async Task Deleting_another_users_record_returns_404_and_leaves_it_in_place()
    {
        var bobsNote = await CreateAsync(bob, "keep me");
        using var client = factory.CreateClientAs(alice);

        var response = await client.DeleteAsync($"/api/test/notes/{bobsNote.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(bobsNote.Id, Assert.Single(await ListAsync(bob)).Id);
    }

    [Fact]
    public async Task Owner_can_update_and_delete_their_record()
    {
        var note = await CreateAsync(alice, "draft");
        using var client = factory.CreateClientAs(alice);

        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync($"/api/test/notes/{note.Id}", new UpdateNoteRequest("final"))).StatusCode);
        Assert.Equal("final", Assert.Single(await ListAsync(alice)).Text);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/test/notes/{note.Id}")).StatusCode);
        Assert.Empty(await ListAsync(alice));
    }

    [Fact]
    public async Task Creating_a_record_sets_the_caller_as_owner_ignoring_a_supplied_owner()
    {
        var bobsId = await LearnUserIdAsync(bob);
        using var client = factory.CreateClientAs(alice);

        var response = await client.PostAsJsonAsync("/api/test/notes", new CreateNoteRequest("planted", bobsId));
        response.EnsureSuccessStatusCode();

        Assert.DoesNotContain(await ListAsync(bob), n => n.Text == "planted");
        var created = Assert.Single(await ListAsync(alice));
        Assert.Equal("planted", created.Text);
        Assert.NotEqual(bobsId, created.OwnerId);
    }

    private async Task<NoteDto> CreateAsync(TestUser user, string text)
    {
        using var client = factory.CreateClientAs(user);
        var response = await client.PostAsJsonAsync("/api/test/notes", new CreateNoteRequest(text, null));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<NoteDto>())!;
    }

    private async Task<List<NoteDto>> ListAsync(TestUser user)
    {
        using var client = factory.CreateClientAs(user);
        return (await client.GetFromJsonAsync<List<NoteDto>>("/api/test/notes"))!;
    }

    /// <summary>Creates and deletes a note to learn the user's local ID, so a test can try to plant data on them.</summary>
    private async Task<Guid> LearnUserIdAsync(TestUser user)
    {
        var note = await CreateAsync(user, "probe");
        using var client = factory.CreateClientAs(user);
        (await client.DeleteAsync($"/api/test/notes/{note.Id}")).EnsureSuccessStatusCode();
        return note.OwnerId;
    }
}
