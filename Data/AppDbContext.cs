using KinectCare.API.Models;
using Microsoft.EntityFrameworkCore;

namespace KinectCare.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Child> Children => Set<Child>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<AIAnalysisResult> AIAnalysisResults => Set<AIAnalysisResult>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<RehabilitationPlan> RehabilitationPlans => Set<RehabilitationPlan>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ParentPermission> ParentPermissions => Set<ParentPermission>();
    public DbSet<OtpToken> OtpTokens => Set<OtpToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // User — Role constraint
        modelBuilder.Entity<User>()
            .Property(u => u.Role)
            .HasConversion<string>();

        // Child → Specialist (restrict delete)
        modelBuilder.Entity<Child>()
            .HasOne(c => c.Specialist)
            .WithMany(u => u.ManagedChildren)
            .HasForeignKey(c => c.SpecialistId)
            .OnDelete(DeleteBehavior.Restrict);

        // Child → Parent (restrict delete)
        modelBuilder.Entity<Child>()
            .HasOne(c => c.Parent)
            .WithMany(u => u.ParentChildren)
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Child → Admin (restrict delete)
        modelBuilder.Entity<Child>()
            .HasOne(c => c.CreatedByAdmin)
            .WithMany()
            .HasForeignKey(c => c.CreatedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);

        // Session → Child
        modelBuilder.Entity<Session>()
            .HasOne(s => s.Child)
            .WithMany(c => c.Sessions)
            .HasForeignKey(s => s.ChildId)
            .OnDelete(DeleteBehavior.Cascade);

        // Session → Specialist
        modelBuilder.Entity<Session>()
            .HasOne(s => s.Specialist)
            .WithMany(u => u.Sessions)
            .HasForeignKey(s => s.SpecialistId)
            .OnDelete(DeleteBehavior.Restrict);

        // Session ↔ AIAnalysisResult (One-to-One)
        modelBuilder.Entity<AIAnalysisResult>()
            .HasOne(a => a.Session)
            .WithOne(s => s.AIAnalysisResult)
            .HasForeignKey<AIAnalysisResult>(a => a.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Report → Session
        modelBuilder.Entity<Report>()
            .HasOne(r => r.Session)
            .WithMany(s => s.Reports)
            .HasForeignKey(r => r.SessionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Report → Child
        modelBuilder.Entity<Report>()
            .HasOne(r => r.Child)
            .WithMany(c => c.Reports)
            .HasForeignKey(r => r.ChildId)
            .OnDelete(DeleteBehavior.Restrict);

        // Report → Specialist
        modelBuilder.Entity<Report>()
            .HasOne(r => r.Specialist)
            .WithMany(u => u.Reports)
            .HasForeignKey(r => r.SpecialistId)
            .OnDelete(DeleteBehavior.Restrict);

        // RehabPlan → Child
        modelBuilder.Entity<RehabilitationPlan>()
            .HasOne(p => p.Child)
            .WithMany(c => c.RehabPlans)
            .HasForeignKey(p => p.ChildId)
            .OnDelete(DeleteBehavior.Cascade);

        // RehabPlan → Specialist
        modelBuilder.Entity<RehabilitationPlan>()
            .HasOne(p => p.Specialist)
            .WithMany(u => u.RehabPlans)
            .HasForeignKey(p => p.SpecialistId)
            .OnDelete(DeleteBehavior.Restrict);

        // ParentPermission → Parent (One-to-One)
        modelBuilder.Entity<ParentPermission>()
            .HasOne(p => p.Parent)
            .WithOne(u => u.ParentPermission)
            .HasForeignKey<ParentPermission>(p => p.ParentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Notification → User
        modelBuilder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Seed — Admin user افتراضي
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = 1,
            FullName = "System Admin",
            Email = "admin@kinectcare.com",
            PasswordHash = "$2a$11$IYAzb.LONI/06QoK3BMVxexoGkMgVLCeRJKChFQhCZb2A00YSlsyO",
            PhoneNumber = "01000000000",
            Role = "Admin",
            IsActive = true,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}