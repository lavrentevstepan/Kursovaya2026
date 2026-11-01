using Microsoft.EntityFrameworkCore;
using SecureKnowledgeBase.Domain.Entities;

namespace SecureKnowledgeBase.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<ProcessingJob> ProcessingJobs => Set<ProcessingJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.Username).IsRequired().HasMaxLength(64);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(128);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Role).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.CreatedAtUtc).IsRequired();
        });

        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Title);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.AccessLevel);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.OwnerId);

            entity.Property(e => e.Title).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Description).HasMaxLength(1024);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(64);
            entity.Property(e => e.OriginalFileName).IsRequired().HasMaxLength(256);
            entity.Property(e => e.StoredFileName).IsRequired().HasMaxLength(256);
            entity.Property(e => e.ContentType).IsRequired().HasMaxLength(128);
            entity.Property(e => e.FileSha256Hash).IsRequired().HasMaxLength(64);

            entity.HasOne(e => e.Owner)
                .WithMany(u => u.OwnedDocuments)
                .HasForeignKey(e => e.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Chunks)
                .WithOne(c => c.Document)
                .HasForeignKey(c => c.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.ProcessingJobs)
                .WithOne(j => j.Document)
                .HasForeignKey(j => j.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentChunk>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.DocumentId, e.ChunkNumber });
            entity.Property(e => e.TextContent).IsRequired();
            entity.Property(e => e.SectionHeader).HasMaxLength(256);
            entity.Property(e => e.IsIndexed).IsRequired();
            entity.Property(e => e.CreatedAtUtc).IsRequired();
        });

        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(256);
            entity.Property(e => e.StartedAtUtc).IsRequired();
            entity.Property(e => e.ResponseMode).IsRequired();

            entity.HasOne(e => e.User)
                .WithMany(u => u.ChatSessions)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Messages)
                .WithOne(m => m.ChatSession)
                .HasForeignKey(m => m.ChatSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ChatSessionId);
            entity.HasIndex(e => e.CreatedAtUtc);
            entity.Property(e => e.QueryText).IsRequired();
            entity.Property(e => e.ResponseText).IsRequired();
            entity.Property(e => e.ResponseMode).IsRequired();
            entity.Property(e => e.SourcesJson).IsRequired();
            entity.Property(e => e.CreatedAtUtc).IsRequired();
        });

        modelBuilder.Entity<AuditEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TimestampUtc);
            entity.HasIndex(e => e.Username);
            entity.HasIndex(e => e.Action);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(64);
            entity.Property(e => e.Action).IsRequired();
            entity.Property(e => e.TargetType).HasMaxLength(64);
            entity.Property(e => e.TargetId).HasMaxLength(64);
            entity.Property(e => e.Result).IsRequired().HasMaxLength(32);
            entity.Property(e => e.ClientIpAddress).HasMaxLength(64);
            entity.Property(e => e.TimestampUtc).IsRequired();
        });

        modelBuilder.Entity<ProcessingJob>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DocumentId);
            entity.HasIndex(e => e.Status);
            entity.Property(e => e.JobType).IsRequired().HasMaxLength(64);
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.ErrorMessage).HasMaxLength(1024);
            entity.Property(e => e.CreatedAtUtc).IsRequired();
        });
    }
}
