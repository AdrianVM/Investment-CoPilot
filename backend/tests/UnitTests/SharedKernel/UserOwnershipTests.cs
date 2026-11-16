using InvestmentCoPilot.SharedKernel;
using InvestmentCoPilot.SharedKernel.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InvestmentCoPilot.UnitTests.SharedKernel;

public class UserOwnershipTests
{
    private static readonly Guid Alice = Guid.NewGuid();
    private static readonly Guid Bob = Guid.NewGuid();

    private sealed class Note : IUserOwned
    {
        public Guid Id { get; set; }

        public Guid OwnerId { get; set; }

        public string Text { get; set; } = "";
    }

    private sealed class NotesContext(DbContextOptions<NotesContext> options, ICurrentUser currentUser)
        : ModuleDbContext<NotesContext>(options, currentUser), IModuleDbContext
    {
        public static string Schema => "notes";

        public DbSet<Note> Notes => Set<Note>();
    }

    private readonly string database = Guid.NewGuid().ToString();

    private NotesContext ContextFor(Guid? user) => new(
        new DbContextOptionsBuilder<NotesContext>().UseInMemoryDatabase(database).Options,
        new FakeCurrentUser(user));

    private async Task<Guid> CreateNoteAsync(Guid owner, string text)
    {
        await using var context = ContextFor(owner);
        var note = new Note { Id = Guid.NewGuid(), Text = text };
        context.Notes.Add(note);
        await context.SaveChangesAsync();
        return note.Id;
    }

    [Fact]
    public async Task Queries_return_only_the_current_users_records()
    {
        await CreateNoteAsync(Alice, "alice's");
        await CreateNoteAsync(Bob, "bob's");

        await using var context = ContextFor(Alice);
        var notes = await context.Notes.ToListAsync();

        Assert.Equal("alice's", Assert.Single(notes).Text);
    }

    [Fact]
    public async Task Another_users_record_is_not_found()
    {
        var bobsNote = await CreateNoteAsync(Bob, "bob's");

        await using var context = ContextFor(Alice);

        Assert.Null(await context.Notes.FindAsync(bobsNote));
        Assert.Null(await context.Notes.SingleOrDefaultAsync(n => n.Id == bobsNote));
    }

    [Fact]
    public async Task Unauthenticated_queries_see_nothing()
    {
        await CreateNoteAsync(Alice, "alice's");

        await using var context = ContextFor(null);

        Assert.Empty(await context.Notes.ToListAsync());
    }

    [Fact]
    public async Task Insert_sets_the_owner_to_the_current_user_ignoring_any_supplied_owner()
    {
        await using (var context = ContextFor(Alice))
        {
            context.Notes.Add(new Note { Id = Guid.NewGuid(), OwnerId = Bob, Text = "claims to be bob's" });
            await context.SaveChangesAsync();
        }

        await using var asAlice = ContextFor(Alice);
        Assert.Equal(Alice, Assert.Single(await asAlice.Notes.ToListAsync()).OwnerId);
        await using var asBob = ContextFor(Bob);
        Assert.Empty(await asBob.Notes.ToListAsync());
    }

    [Fact]
    public async Task Insert_without_an_authenticated_user_is_rejected()
    {
        await using var context = ContextFor(null);
        context.Notes.Add(new Note { Id = Guid.NewGuid(), Text = "orphan" });

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Changing_the_owner_is_rejected()
    {
        var id = await CreateNoteAsync(Alice, "alice's");

        await using var context = ContextFor(Alice);
        var note = await context.Notes.SingleAsync(n => n.Id == id);
        note.OwnerId = Bob;

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        Assert.Contains("owner", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Owner_can_update_their_own_record()
    {
        var id = await CreateNoteAsync(Alice, "draft");

        await using (var context = ContextFor(Alice))
        {
            (await context.Notes.SingleAsync(n => n.Id == id)).Text = "final";
            await context.SaveChangesAsync();
        }

        await using var check = ContextFor(Alice);
        Assert.Equal("final", (await check.Notes.SingleAsync(n => n.Id == id)).Text);
    }

    [Fact]
    public void Ownership_filter_is_named_and_owner_is_indexed()
    {
        using var context = ContextFor(Alice);
        var entity = context.Model.FindEntityType(typeof(Note))!;

        Assert.NotNull(entity.FindDeclaredQueryFilter(NotesContext.OwnerFilterName));
        Assert.Contains(entity.GetIndexes(), i => i.Properties.Single().Name == nameof(IUserOwned.OwnerId));
    }
}
