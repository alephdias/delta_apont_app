using DeltaApp.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DeltaApp.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Solicitation> Solicitations => Set<Solicitation>();
    public DbSet<WorkInterval> WorkIntervals => Set<WorkInterval>();
    public DbSet<DayEntry> DayEntries => Set<DayEntry>();
    public DbSet<Evidence> Evidences => Set<Evidence>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Client>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.HasIndex(x => new { x.UserId, x.Name }).IsUnique();
        });

        b.Entity<Solicitation>(e =>
        {
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(2);
            e.Property(x => x.Number).IsRequired().HasMaxLength(40);
            e.HasIndex(x => new { x.UserId, x.Type, x.Number }).IsUnique();
            e.HasIndex(x => x.UserId);
            e.Ignore(x => x.Code);
            e.HasOne(x => x.Client)
                .WithMany(c => c.Solicitations)
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<WorkInterval>(e =>
        {
            e.HasIndex(x => new { x.UserId, x.EndedAt });
            e.HasOne(x => x.Solicitation)
                .WithMany(s => s.Intervals)
                .HasForeignKey(x => x.SolicitationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<DayEntry>(e =>
        {
            e.HasIndex(x => new { x.UserId, x.WorkDate });
            e.HasIndex(x => new { x.UserId, x.SolicitationId, x.WorkDate }).IsUnique();
            e.HasOne(x => x.Solicitation)
                .WithMany(s => s.DayEntries)
                .HasForeignKey(x => x.SolicitationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Evidence>(e =>
        {
            e.Property(x => x.Kind).HasConversion<string>().HasMaxLength(10);
            e.Property(x => x.Value).IsRequired();
            e.HasOne(x => x.Solicitation)
                .WithMany(s => s.Evidences)
                .HasForeignKey(x => x.SolicitationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<UserProfile>(e =>
        {
            e.HasKey(x => x.UserId);
            e.Property(x => x.Email).IsRequired();
        });
    }
}
