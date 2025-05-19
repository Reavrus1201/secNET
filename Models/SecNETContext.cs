using Microsoft.EntityFrameworkCore;

namespace secNET.Models
{
    public class SecNETContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Branch> Branches { get; set; }
        public DbSet<CCTVLog> CCTVLogs { get; set; }
        public DbSet<IncidentReport> IncidentReports { get; set; }
        public DbSet<UserManagementLog> UserManagementLogs { get; set; }
        public DbSet<UserLoginLog> UserLoginLogs { get; set; }
        public DbSet<UserSecurityLog> UserSecurityLogs { get; set; }
        public DbSet<ChecklistViolation> ChecklistViolations { get; set; }

        public SecNETContext(DbContextOptions<SecNETContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasOne(u => u.Branch)
                .WithMany()
                .HasForeignKey(u => u.BranchId);

            modelBuilder.Entity<CCTVLog>()
                .HasOne(c => c.Branch)
                .WithMany()
                .HasForeignKey(c => c.BranchId);

            modelBuilder.Entity<CCTVLog>()
                .HasOne(c => c.SubmittedBy)
                .WithMany()
                .HasForeignKey(c => c.SubmittedById);

            modelBuilder.Entity<CCTVLog>()
                .HasOne(c => c.ReviewedBy)
                .WithMany()
                .HasForeignKey(c => c.ReviewedById);

            modelBuilder.Entity<IncidentReport>()
                .HasOne(i => i.Branch)
                .WithMany()
                .HasForeignKey(i => i.BranchId);

            modelBuilder.Entity<IncidentReport>()
                .HasOne(i => i.CreatedBy)
                .WithMany()
                .HasForeignKey(i => i.CreatedById);

            // Add indexes for CCTVLogs
            modelBuilder.Entity<CCTVLog>()
                .HasIndex(c => c.BranchId);

            modelBuilder.Entity<CCTVLog>()
                .HasIndex(c => c.Date);

            // Add indexes for IncidentReports
            modelBuilder.Entity<IncidentReport>()
                .HasIndex(i => i.BranchId);

            modelBuilder.Entity<IncidentReport>()
                .HasIndex(i => i.IncidentDateTime);

            // Add computed column and index for DateOnly
            modelBuilder.Entity<CCTVLog>()
                .Property(c => c.DateOnly)
                .HasComputedColumnSql("CAST([Date] AS DATE)");

            modelBuilder.Entity<CCTVLog>()
                .HasIndex(c => c.DateOnly);

            // Add index for ChecklistViolations
            modelBuilder.Entity<ChecklistViolation>()
                .HasIndex(cv => new { cv.CCTVLogId, cv.Question });

            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<Branch>().ToTable("Branches");
            modelBuilder.Entity<CCTVLog>().ToTable("CCTVLogs");
            modelBuilder.Entity<IncidentReport>().ToTable("IncidentReports");
            modelBuilder.Entity<UserManagementLog>().ToTable("UserManagementLogs");
            modelBuilder.Entity<UserLoginLog>().ToTable("UserLoginLogs");
            modelBuilder.Entity<UserSecurityLog>().ToTable("UserSecurityLogs");
            modelBuilder.Entity<ChecklistViolation>().ToTable("ChecklistViolations");
        }
    }
}