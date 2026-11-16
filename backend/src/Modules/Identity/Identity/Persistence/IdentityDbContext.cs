using InvestmentCoPilot.Identity.Users;
using InvestmentCoPilot.SharedKernel;
using InvestmentCoPilot.SharedKernel.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InvestmentCoPilot.Identity.Persistence;

internal sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options, ICurrentUser currentUser)
    : ModuleDbContext<IdentityDbContext>(options, currentUser), IModuleDbContext
{
    public static string Schema => "identity";

    public DbSet<User> Users => Set<User>();

    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(user =>
        {
            user.ToTable("users");
            user.HasKey(u => u.Id);
            user.Property(u => u.Id).HasColumnName("id").ValueGeneratedNever();
            user.Property(u => u.Issuer).HasColumnName("issuer").HasMaxLength(User.MaxIssuerLength);
            user.Property(u => u.Subject).HasColumnName("subject").HasMaxLength(User.MaxSubjectLength);
            user.Property(u => u.Email).HasColumnName("email").HasMaxLength(User.MaxEmailLength);
            user.Property(u => u.DisplayName).HasColumnName("display_name").HasMaxLength(User.MaxDisplayNameLength);
            user.Property(u => u.CreatedAt).HasColumnName("created_at");
            user.HasIndex(u => new { u.Issuer, u.Subject }).IsUnique().HasDatabaseName("ux_users_issuer_subject");
        });
    }
}
