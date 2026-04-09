using CodeGraphWeb.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CodeGraphWeb.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Risk> Risks => Set<Risk>();
    public DbSet<AnalysisResult> AnalysisResults => Set<AnalysisResult>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Project>(entity =>
        {
            entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Description).HasMaxLength(1000);

            entity
                .HasOne(x => x.Company)
                .WithMany(x => x.Projects)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ProjectMember>(entity =>
        {
            entity.HasIndex(x => new { x.UserId, x.ProjectId }).IsUnique();
            entity.Property(x => x.Role).IsRequired().HasMaxLength(32);

            entity
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(x => x.Project)
                .WithMany(x => x.Members)
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Company>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(200);
        });

        builder.Entity<ApplicationUser>(entity =>
        {
            entity
                .HasOne(x => x.Company)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Risk>(entity =>
        {
            entity.Property(x => x.Severity).HasMaxLength(32);
            entity.Property(x => x.Description).HasMaxLength(2000);
        });

        builder.Entity<AnalysisResult>(entity =>
        {
            entity.Property(x => x.Summary).HasMaxLength(2000);
        });
    }
}
