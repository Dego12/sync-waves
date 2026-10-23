using Microsoft.EntityFrameworkCore;
using SyncWaves.RTC.Infrastructure.Data.Models;

namespace SyncWaves.RTC.Infrastructure.Data;

public class RtcDbContext : DbContext
{
    public RtcDbContext(DbContextOptions<RtcDbContext> options)
            : base(options) { }

    public DbSet<DocumentStateModel> DocumentStates { get; set; }
    public DbSet<DocumentVersionModel> DocumentVersions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("rtc");

        modelBuilder.Entity<DocumentStateModel>(entity =>
        {
            entity.HasKey(x => x.DocumentId);
            entity.Property(x => x.DocumentId).HasMaxLength(64);
            entity.Property(x => x.Content).IsRequired();
            entity.Property(x => x.CurrentVersion).IsRequired();
            entity.Property(x => x.LastUpdated).IsRequired();

            entity.HasMany(x => x.Versions)
                  .WithOne()
                  .HasForeignKey(v => v.DocumentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentVersionModel>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.DocumentId).HasMaxLength(64);
            entity.Property(x => x.VersionNumber).IsRequired();
            entity.Property(x => x.Content).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();

            entity.HasIndex(x => new { x.DocumentId, x.VersionNumber })
                  .IsUnique();
        });

        base.OnModelCreating(modelBuilder);
    }
}
