using InvestmentCoPilot.SharedKernel;
using InvestmentCoPilot.SharedKernel.Persistence;
using InvestmentCoPilot.SharedKernel.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace InvestmentCoPilot.IntegrationTests.Scoping;

/// <summary>A test-only user-owned entity, built exactly as a real module's would be.</summary>
public sealed class Note : IUserOwned
{
    public Guid Id { get; set; }

    public Guid OwnerId { get; set; }

    public string Text { get; set; } = "";
}

public sealed class NotesDbContext(DbContextOptions<NotesDbContext> options, ICurrentUser currentUser)
    : ModuleDbContext<NotesDbContext>(options, currentUser), IModuleDbContext
{
    public static string Schema => "test_notes";

    public DbSet<Note> Notes => Set<Note>();
}

public sealed record NoteDto(Guid Id, Guid OwnerId, string Text);

/// <summary>The client may send an owner; the API must ignore it.</summary>
public sealed record CreateNoteRequest(string Text, Guid? OwnerId);

public sealed record UpdateNoteRequest(string Text);

/// <summary>
/// Plain CRUD endpoints with no ownership checks of their own: scoping must come from the module DbContext alone.
/// </summary>
public sealed class NotesEndpoints : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder api)
    {
        var notes = api.MapGroup("/test/notes");

        notes.MapGet("/", async (NotesDbContext db) =>
            await db.Notes.Select(n => new NoteDto(n.Id, n.OwnerId, n.Text)).ToListAsync());

        notes.MapGet("/{id:guid}", async Task<Results<Ok<NoteDto>, NotFound>> (Guid id, NotesDbContext db) =>
            await db.Notes.FindAsync(id) is { } note
                ? TypedResults.Ok(new NoteDto(note.Id, note.OwnerId, note.Text))
                : TypedResults.NotFound());

        notes.MapPost("/", async (CreateNoteRequest request, NotesDbContext db) =>
        {
            // Deliberately careless: copies the client-supplied owner onto the entity.
            var note = new Note { Id = Guid.NewGuid(), OwnerId = request.OwnerId ?? Guid.Empty, Text = request.Text };
            db.Notes.Add(note);
            await db.SaveChangesAsync();
            return TypedResults.Created($"/api/test/notes/{note.Id}", new NoteDto(note.Id, note.OwnerId, note.Text));
        });

        notes.MapPut("/{id:guid}", async Task<Results<NoContent, NotFound>> (Guid id, UpdateNoteRequest request, NotesDbContext db) =>
        {
            if (await db.Notes.FindAsync(id) is not { } note)
            {
                return TypedResults.NotFound();
            }

            note.Text = request.Text;
            await db.SaveChangesAsync();
            return TypedResults.NoContent();
        });

        notes.MapDelete("/{id:guid}", async Task<Results<NoContent, NotFound>> (Guid id, NotesDbContext db) =>
            await db.Notes.Where(n => n.Id == id).ExecuteDeleteAsync() == 0
                ? TypedResults.NotFound()
                : TypedResults.NoContent());
    }
}
