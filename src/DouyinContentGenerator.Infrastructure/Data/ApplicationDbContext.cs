using Microsoft.EntityFrameworkCore;
using DouyinContentGenerator.Core.Models;

namespace DouyinContentGenerator.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ImageTemplate> ImageTemplates => Set<ImageTemplate>();
    public DbSet<CopywritingTemplate> CopywritingTemplates => Set<CopywritingTemplate>();
    public DbSet<GenerationTask> GenerationTasks => Set<GenerationTask>();
    public DbSet<TaskImageTemplate> TaskImageTemplates => Set<TaskImageTemplate>();
    public DbSet<GeneratedImage> GeneratedImages => Set<GeneratedImage>();
    public DbSet<GeneratedText> GeneratedTexts => Set<GeneratedText>();
    public DbSet<AiProviderConfig> AiProviderConfigs => Set<AiProviderConfig>();
    public DbSet<UserAISettings> UserAISettings => Set<UserAISettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // UserRole composite key
        modelBuilder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserId, ur.RoleId });

        // TaskImageTemplate composite key
        modelBuilder.Entity<TaskImageTemplate>()
            .HasKey(tit => new { tit.TaskId, tit.ImageTemplateId });

        // User -> UserRoles
        modelBuilder.Entity<User>()
            .HasMany(u => u.UserRoles)
            .WithOne(ur => ur.User)
            .HasForeignKey(ur => ur.UserId);

        modelBuilder.Entity<Role>()
            .HasMany(r => r.UserRoles)
            .WithOne(ur => ur.Role)
            .HasForeignKey(ur => ur.RoleId);

        // Product -> ProductImages
        modelBuilder.Entity<Product>()
            .HasMany(p => p.ProductImages)
            .WithOne(pi => pi.Product)
            .HasForeignKey(pi => pi.ProductId);

        // Product -> User
        modelBuilder.Entity<Product>()
            .HasOne(p => p.User)
            .WithMany(u => u.Products)
            .HasForeignKey(p => p.UserId);

        // GenerationTask -> User
        modelBuilder.Entity<GenerationTask>()
            .HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // GenerationTask -> Product
        modelBuilder.Entity<GenerationTask>()
            .HasOne(t => t.Product)
            .WithMany()
            .HasForeignKey(t => t.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // GenerationTask -> TaskImageTemplates
        modelBuilder.Entity<GenerationTask>()
            .HasMany(t => t.TaskImageTemplates)
            .WithOne(tit => tit.Task)
            .HasForeignKey(tit => tit.TaskId);

        // GenerationTask -> GeneratedImages
        modelBuilder.Entity<GenerationTask>()
            .HasMany(t => t.GeneratedImages)
            .WithOne(gi => gi.Task)
            .HasForeignKey(gi => gi.TaskId);

        // GenerationTask -> GeneratedTexts
        modelBuilder.Entity<GenerationTask>()
            .HasMany(t => t.GeneratedTexts)
            .WithOne(gt => gt.Task)
            .HasForeignKey(gt => gt.TaskId);

        // GeneratedImage -> ImageTemplate
        modelBuilder.Entity<GeneratedImage>()
            .HasOne(gi => gi.ImageTemplate)
            .WithMany()
            .HasForeignKey(gi => gi.ImageTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        // GeneratedText -> CopywritingTemplate
        modelBuilder.Entity<GeneratedText>()
            .HasOne(gt => gt.CopywritingTemplate)
            .WithMany()
            .HasForeignKey(gt => gt.CopywritingTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for performance
        modelBuilder.Entity<GenerationTask>()
            .HasIndex(t => new { t.UserId, t.Status });

        modelBuilder.Entity<GeneratedImage>()
            .HasIndex(gi => new { gi.TaskId, gi.ImageTemplateId });

        modelBuilder.Entity<GeneratedText>()
            .HasIndex(gt => new { gt.TaskId, gt.CopywritingTemplateId });

        modelBuilder.Entity<GeneratedImage>()
            .HasIndex(gi => gi.IdempotencyKey)
            .IsUnique()
            .HasFilter("\"IdempotencyKey\" IS NOT NULL");

        modelBuilder.Entity<GeneratedText>()
            .HasIndex(gt => gt.IdempotencyKey)
            .IsUnique()
            .HasFilter("\"IdempotencyKey\" IS NOT NULL");

        // Seed default roles
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Admin" },
            new Role { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Operator" }
        );

        // UserAISettings: one record per user
        modelBuilder.Entity<UserAISettings>()
            .HasIndex(s => s.UserId)
            .IsUnique();

        modelBuilder.Entity<UserAISettings>()
            .HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
