using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TeamTaskManager.Entities;

namespace TeamTaskManager.DataAccess;

// Identity tablolarını ve uygulama entity'lerini EF Core üzerinden SQL Server'a bağlar.
public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<TaskItem> TaskItems => Set<TaskItem>();
    public DbSet<TaskComment> TaskComments => Set<TaskComment>();
    public DbSet<TaskAttachment> TaskAttachments => Set<TaskAttachment>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    // Tablo/indeks kısıtlarını, foreign key ilişkilerini ve silme davranışlarını tanımlar.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Identity kullanıcısının proje/görev ilişkilerinde kullanılan profil sütunları.
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("AspNetUsers");
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.FirstName).HasMaxLength(100);
            entity.Property(u => u.LastName).HasMaxLength(100);
            entity.Property(u => u.UserName).HasMaxLength(100);
            entity.Property(u => u.Email).HasMaxLength(255);
        });

        // Proje adı, durum ve tarih aramaları için sınırlar/indeksler; yönetici silinirse FK null olur.
        modelBuilder.Entity<Project>(entity =>
        {
            entity.ToTable("Projects");
            entity.Property(p => p.Name).IsRequired().HasMaxLength(150);
            entity.Property(p => p.Description).HasMaxLength(2000);
            entity.HasIndex(p => new { p.ManagerUserId, p.Name }).IsUnique();
            entity.HasIndex(p => p.ProjectStatus);
            entity.HasIndex(p => p.StartDate);
            entity.HasIndex(p => p.TargetEndDate);

            entity.HasOne(p => p.ManagerUser)
                .WithMany(u => u.ManagedProjects)
                .HasForeignKey(p => p.ManagerUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Aynı kullanıcı-proje ikilisinin tekrarlanmasını DB'de engeller; proje/kullanıcı silinince üyelik kalkar.
        modelBuilder.Entity<ProjectMember>(entity =>
        {
            entity.ToTable("ProjectMembers");
            entity.HasIndex(pm => new { pm.ProjectId, pm.UserId }).IsUnique();
            entity.HasIndex(pm => pm.UserId);
            entity.HasIndex(pm => pm.MemberRole);

            entity.HasOne(pm => pm.Project)
                .WithMany(p => p.Members)
                .HasForeignKey(pm => pm.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pm => pm.User)
                .WithMany(u => u.ProjectMemberships)
                .HasForeignKey(pm => pm.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Proje silinince görevler silinir; atanan/oluşturan kullanıcı silme davranışı Restrict'tir.
        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.ToTable("TaskItems");
            entity.Property(t => t.Title).IsRequired().HasMaxLength(200);
            entity.Property(t => t.Description).HasMaxLength(4000);
            entity.HasIndex(t => t.ProjectId);
            entity.HasIndex(t => t.AssignedToUserId);
            entity.HasIndex(t => t.Status);
            entity.HasIndex(t => t.Priority);

            entity.HasOne(t => t.Project)
                .WithMany(p => p.Tasks)
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(t => t.AssignedToUser)
                .WithMany(u => u.AssignedTasks)
                .HasForeignKey(t => t.AssignedToUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.CreatedByUser)
                .WithMany(u => u.CreatedTasks)
                .HasForeignKey(t => t.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Görev veya yorum sahibi kullanıcı silinince yorumlar cascade ile kaldırılır.
        modelBuilder.Entity<TaskComment>(entity =>
        {
            entity.ToTable("TaskComments");
            entity.Property(tc => tc.Content).IsRequired().HasMaxLength(1500);
            entity.HasIndex(tc => tc.TaskItemId);
            entity.HasIndex(tc => tc.UserId);

            entity.HasOne(tc => tc.TaskItem)
                .WithMany(t => t.Comments)
                .HasForeignKey(tc => tc.TaskItemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(tc => tc.User)
                .WithMany(u => u.TaskComments)
                .HasForeignKey(tc => tc.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Görev silinince dosya metadata'sı kalkar; yükleyen kullanıcı silinirse FK boşaltılır.
        modelBuilder.Entity<TaskAttachment>(entity =>
        {
            entity.ToTable("TaskAttachments");
            entity.Property(ta => ta.OriginalFileName).IsRequired().HasMaxLength(255);
            entity.Property(ta => ta.StoredFileName).IsRequired().HasMaxLength(255);
            entity.Property(ta => ta.FilePath).IsRequired().HasMaxLength(500);
            entity.Property(ta => ta.ContentType).HasMaxLength(200);
            entity.HasIndex(ta => ta.TaskItemId);
            entity.HasIndex(ta => ta.UploadedByUserId);

            entity.HasOne(ta => ta.TaskItem)
                .WithMany(t => t.Attachments)
                .HasForeignKey(ta => ta.TaskItemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ta => ta.UploadedByUser)
                .WithMany(u => u.UploadedAttachments)
                .HasForeignKey(ta => ta.UploadedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Bildirim kullanıcıya bağlıdır ve kullanıcı silinince bildirimleri cascade edilir.
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");
            entity.Property(n => n.Title).IsRequired().HasMaxLength(200);
            entity.Property(n => n.Message).IsRequired().HasMaxLength(2000);
            entity.HasIndex(n => n.UserId);
            entity.HasIndex(n => n.IsRead);
            entity.HasIndex(n => n.Type);

            entity.HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Audit geçmişini korumak için kullanıcı silinince aktivite kaydının UserId alanı null olur.
        modelBuilder.Entity<ActivityLog>(entity =>
        {
            entity.ToTable("ActivityLogs");
            entity.Property(al => al.Action).IsRequired().HasMaxLength(200);
            entity.Property(al => al.EntityType).IsRequired().HasMaxLength(200);
            entity.Property(al => al.Description).HasMaxLength(2000);
            entity.HasIndex(al => al.UserId);
            entity.HasIndex(al => al.EntityType);
            entity.HasIndex(al => al.EntityId);

            entity.HasOne(al => al.User)
                .WithMany(u => u.ActivityLogs)
                .HasForeignKey(al => al.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
