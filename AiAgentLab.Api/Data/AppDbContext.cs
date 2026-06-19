using AiAgentLab.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiAgentLab.Api.Data;

/// <summary>
/// Entity Framework Core database context for the AiAgentLab application.
/// </summary>
public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ConversationEntity> Conversations => Set<ConversationEntity>();
    public DbSet<MessageEntity> Messages => Set<MessageEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Conversations
        modelBuilder.Entity<ConversationEntity>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).ValueGeneratedNever();
            b.Property(e => e.UserId).IsRequired();
            b.Property(e => e.CreatedAt).IsRequired();
            b.Property(e => e.UpdatedAt).IsRequired();
            b.HasMany(e => e.Messages)
             .WithOne(m => m.Conversation)
             .HasForeignKey(m => m.ConversationId)
             .OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(e => e.UserId);
        });
        
        // Messages
        modelBuilder.Entity<MessageEntity>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).ValueGeneratedNever();
            // Match Conversations.Id column length (nvarchar(450)) to satisfy FK requirements
            b.Property(e => e.ConversationId).IsRequired().HasMaxLength(450);
            b.Property(e => e.Role).IsRequired().HasMaxLength(50);
            b.Property(e => e.Content).IsRequired();
            b.Property(e => e.CreatedAt).IsRequired();
            b.Property(e => e.IntentDomain).HasMaxLength(100);
            b.Property(e => e.IntentAction).HasMaxLength(100);
            b.Property(e => e.Metadata).HasColumnType("nvarchar(max)");
            b.HasIndex(e => e.ConversationId);
        });
    }
}