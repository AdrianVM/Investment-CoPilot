using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace InvestmentCoPilot.SharedKernel.Persistence;

/// <summary>
/// Base class for a module's DbContext.
/// <list type="bullet">
/// <item>All of the module's tables live in its own schema, so one module cannot map or query another's tables.</item>
/// <item>Entities implementing <see cref="IUserOwned"/> are visible only to their owner: every query gets a
/// <see cref="OwnerFilterName"/> filter on the current user, and <see cref="IUserOwned.OwnerId"/> is set on insert
/// and cannot be changed afterwards.</item>
/// </list>
/// Derived contexts configure their model in <see cref="ConfigureModel"/>.
/// </summary>
public abstract class ModuleDbContext<TSelf> : DbContext
    where TSelf : ModuleDbContext<TSelf>, IModuleDbContext
{
    /// <summary>Name of the query filter that restricts <see cref="IUserOwned"/> entities to the current user.</summary>
    public const string OwnerFilterName = "UserOwned";

    private static readonly PropertyInfo CurrentOwnerIdProperty =
        typeof(ModuleDbContext<TSelf>).GetProperty(nameof(CurrentOwnerId), BindingFlags.Instance | BindingFlags.NonPublic)!;

    private readonly ICurrentUser currentUser;

    protected ModuleDbContext(DbContextOptions<TSelf> options, ICurrentUser currentUser)
        : base(options)
    {
        ArgumentNullException.ThrowIfNull(currentUser);
        this.currentUser = currentUser;
    }

    /// <summary>
    /// The owner that user-owned queries are restricted to. With no authenticated user it is <see cref="Guid.Empty"/>,
    /// which matches no rows, so an unauthenticated query sees nothing rather than everything.
    /// EF Core evaluates this per query, for the context instance running the query.
    /// </summary>
    protected Guid CurrentOwnerId => currentUser.IsAuthenticated ? currentUser.Id : Guid.Empty;

    protected sealed override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema(TSelf.Schema);
        ConfigureModel(modelBuilder);
        ApplyOwnership(modelBuilder);
    }

    /// <summary>Configure the module's entities here.</summary>
    protected virtual void ConfigureModel(ModelBuilder modelBuilder)
    {
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyOwnershipRules();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyOwnershipRules();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyOwnership(ModelBuilder modelBuilder)
    {
        var ownedTypes = modelBuilder.Model.GetEntityTypes()
            .Where(t => t.BaseType is null && !t.IsOwned() && typeof(IUserOwned).IsAssignableFrom(t.ClrType))
            .ToList();

        foreach (var entityType in ownedTypes)
        {
            // e => e.OwnerId == this.CurrentOwnerId
            var entity = Expression.Parameter(entityType.ClrType, "e");
            var filter = Expression.Lambda(
                Expression.Equal(
                    Expression.Property(entity, nameof(IUserOwned.OwnerId)),
                    Expression.Property(Expression.Constant(this), CurrentOwnerIdProperty)),
                entity);

            entityType.SetQueryFilter(OwnerFilterName, filter);
            modelBuilder.Entity(entityType.ClrType).HasIndex(nameof(IUserOwned.OwnerId));
        }
    }

    private void ApplyOwnershipRules()
    {
        foreach (var entry in ChangeTracker.Entries<IUserOwned>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (!currentUser.IsAuthenticated)
                    {
                        throw new InvalidOperationException(
                            $"Cannot create {entry.Metadata.DisplayName()} without an authenticated user.");
                    }

                    // The owner always comes from the current user, never from request data.
                    entry.Entity.OwnerId = currentUser.Id;
                    break;

                case EntityState.Modified:
                    var owner = entry.Property(e => e.OwnerId);
                    if (owner.IsModified && owner.OriginalValue != owner.CurrentValue)
                    {
                        throw new InvalidOperationException(
                            $"The owner of {entry.Metadata.DisplayName()} cannot be changed.");
                    }

                    break;
            }
        }
    }
}
