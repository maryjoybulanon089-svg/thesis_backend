using Microsoft.EntityFrameworkCore;
using ThesisRepository.Models;

namespace ThesisRepository.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Thesis> Theses { get; set; }
        public DbSet<PasswordResetRequest> PasswordResetRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── Users ────────────────────────────────────────────────────────────
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(u => u.UserId);
                entity.Property(u => u.UserId).UseIdentityByDefaultColumn();

                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasIndex(u => u.Role);

                entity.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(u => u.LastName).IsRequired().HasMaxLength(100);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(255);
                entity.Property(u => u.PasswordHash).IsRequired();
                entity.Property(u => u.Role).IsRequired().HasMaxLength(50);
                entity.Property(u => u.Department).HasMaxLength(100);
                entity.Property(u => u.IsActive).IsRequired().HasDefaultValue(true);
                entity.Property(u => u.IsApproved).IsRequired().HasDefaultValue(false);
                entity.Property(u => u.CreatedAt).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // ── Theses ───────────────────────────────────────────────────────────
            modelBuilder.Entity<Thesis>(entity =>
            {
                entity.ToTable("Theses");
                entity.HasKey(t => t.ThesisId);
                entity.Property(t => t.ThesisId).UseIdentityByDefaultColumn();

                entity.HasIndex(t => t.Status);
                entity.HasIndex(t => t.Year);
                entity.HasIndex(t => t.Department);
                entity.HasIndex(t => t.FieldOfResearch);

                entity.Property(t => t.Title).IsRequired().HasMaxLength(500);
                entity.Property(t => t.Abstract).IsRequired();
                entity.Property(t => t.Authors).IsRequired().HasMaxLength(200);
                entity.Property(t => t.Advisors).HasMaxLength(200);
                entity.Property(t => t.Department).IsRequired().HasMaxLength(100);
                entity.Property(t => t.FieldOfResearch).IsRequired().HasMaxLength(150);
                entity.Property(t => t.Keywords).HasMaxLength(500).HasDefaultValue("[]");
                entity.Property(t => t.FilePath).HasMaxLength(1000);
                entity.Property(t => t.Status).IsRequired().HasMaxLength(50).HasDefaultValue("pending");
                // Use database-agnostic default timestamp; Postgres will map this appropriately
                entity.Property(t => t.UploadedAt).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(t => t.ViewCount).HasDefaultValue(0);
                entity.Property(t => t.DownloadCount).HasDefaultValue(0);

                // UploadedBy and ApprovedBy are plain int FK values managed at DB level
                // No EF navigation property — SSMS FK constraint enforces integrity
                entity.Property(t => t.UploadedBy).IsRequired(false);
                entity.Property(t => t.ApprovedBy).IsRequired(false);
            });

            // ── PasswordResetRequests ────────────────────────────────────────────
            modelBuilder.Entity<PasswordResetRequest>(entity =>
            {
                entity.ToTable("PasswordResetRequests");
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Id).HasMaxLength(450);
                entity.Property(r => r.Email).IsRequired().HasMaxLength(255);
                entity.Property(r => r.Status).IsRequired().HasMaxLength(50).HasDefaultValue("pending");
                entity.Property(r => r.RequestedAt).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(r => r.ProcessedBy).HasMaxLength(255);
            });
        }
    }
}
