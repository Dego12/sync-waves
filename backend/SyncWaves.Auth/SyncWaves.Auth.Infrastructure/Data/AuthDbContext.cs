using Microsoft.EntityFrameworkCore;
using SyncWaves.Auth.Infrastructure.Data.Models;

namespace SyncWaves.Auth.Infrastructure.Data;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

    public DbSet<UserModel> Users { get; set; }
    public DbSet<RefreshTokenModel> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("users");

        modelBuilder.Entity<UserModel>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Name)
                  .HasMaxLength(200)
                  .IsRequired();
            entity.Property(d => d.PasswordHash)
                  .HasMaxLength(200)
                  .IsRequired();
            entity.HasIndex(d => new { d.ExternalProvider, d.ExternalId });

            entity.Property(d => d.ExternalProvider)
                  .HasMaxLength(50);

            entity.Property(d => d.ExternalId)
                  .HasMaxLength(256);  
        });

        modelBuilder.Entity<RefreshTokenModel>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.Property(x => x.TokenHash).IsRequired();

            entity.HasOne(x => x.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(x => x.UserId);
        });

        base.OnModelCreating(modelBuilder);
    }
}
