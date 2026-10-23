using Microsoft.EntityFrameworkCore;
using SyncWaves.Documents.Infrastructure.Data.Models;

namespace SyncWaves.Documents.Infrastructure.Data;

public class DocumentsDbContext : DbContext
{
    public DocumentsDbContext(DbContextOptions<DocumentsDbContext> options) : base(options) { }

    public DbSet<DocumentModel> Documents { get; set; }

    public DbSet<DocumentCollaboratorModel> Collaborators { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("documents");

        modelBuilder.Entity<DocumentModel>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Title)
                  .HasMaxLength(200)
                  .IsRequired();
            entity.Property(d => d.Content)
                  .IsRequired();
            entity.Property(d => d.Version)
                  .IsConcurrencyToken();
        });

        modelBuilder.Entity<DocumentCollaboratorModel>(entity =>
        {
            entity.HasKey(x => new { x.DocumentId, x.UserId });

            entity.Property(x => x.UserId)
                .IsRequired();

            entity.HasOne(x => x.Document).WithMany(x => x.Collaborators);
        });

        base.OnModelCreating(modelBuilder);
    }
}
