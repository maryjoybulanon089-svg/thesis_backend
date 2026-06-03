using ThesisRepository.Models;
using Microsoft.EntityFrameworkCore;

namespace ThesisRepository.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            EnsureSchemaUpdates(context);

            if (context.Users.Any())
                return;

            SeedUsers(context);
            SeedTheses(context);
        }

        // ─────────────────────────────────────────────
        // PostgreSQL-safe schema patches
        // ─────────────────────────────────────────────
        private static void EnsureSchemaUpdates(ApplicationDbContext context)
        {
            // IsApproved column
            context.Database.ExecuteSqlRaw(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name='Users' AND column_name='IsApproved'
                    ) THEN
                        ALTER TABLE ""Users"" ADD COLUMN ""IsApproved"" boolean DEFAULT false;
                        UPDATE ""Users"" SET ""IsApproved"" = true;
                    END IF;
                END $$;
            ");

            // FirstName column
            context.Database.ExecuteSqlRaw(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name='Users' AND column_name='FirstName'
                    ) THEN
                        ALTER TABLE ""Users"" ADD COLUMN ""FirstName"" varchar(100) DEFAULT '';
                    END IF;
                END $$;
            ");

            // LastName column
            context.Database.ExecuteSqlRaw(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name='Users' AND column_name='LastName'
                    ) THEN
                        ALTER TABLE ""Users"" ADD COLUMN ""LastName"" varchar(100) DEFAULT '';
                    END IF;
                END $$;
            ");

            // IsDeleted column (Theses)
            context.Database.ExecuteSqlRaw(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name='Theses' AND column_name='IsDeleted'
                    ) THEN
                        ALTER TABLE ""Theses"" ADD COLUMN ""IsDeleted"" boolean DEFAULT false;
                    END IF;
                END $$;
            ");

            // RejectedAt column (Theses)
            context.Database.ExecuteSqlRaw(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name='Theses' AND column_name='RejectedAt'
                    ) THEN
                        ALTER TABLE ""Theses"" ADD COLUMN ""RejectedAt"" timestamp with time zone;
                    END IF;
                END $$;
            ");

            // Doi column (Theses)
            context.Database.ExecuteSqlRaw(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name='Theses' AND column_name='Doi'
                    ) THEN
                        ALTER TABLE ""Theses"" ADD COLUMN ""Doi"" varchar(255);
                    END IF;
                END $$;
            ");

            // IeeeCitation column (Theses)
            context.Database.ExecuteSqlRaw(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name='Theses' AND column_name='IeeeCitation'
                    ) THEN
                        ALTER TABLE ""Theses"" ADD COLUMN ""IeeeCitation"" text;
                    END IF;
                END $$;
            ");

            // AcsCitation column (Theses)
            context.Database.ExecuteSqlRaw(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name='Theses' AND column_name='AcsCitation'
                    ) THEN
                        ALTER TABLE ""Theses"" ADD COLUMN ""AcsCitation"" text;
                    END IF;
                END $$;
            ");

            // PasswordResetRequests table
            context.Database.ExecuteSqlRaw(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.tables
                        WHERE table_name='PasswordResetRequests'
                    ) THEN
                        CREATE TABLE ""PasswordResetRequests"" (
                            ""Id"" varchar(450) PRIMARY KEY,
                            ""Email"" varchar(255) NOT NULL,
                            ""Status"" varchar(50) NOT NULL DEFAULT 'pending',
                            ""RequestedAt"" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                            ""ProcessedAt"" timestamp with time zone,
                            ""ProcessedBy"" varchar(255)
                        );
                    END IF;
                END $$;
            ");
        }

        // ─────────────────────────────────────────────
        // Seed Users
        // ─────────────────────────────────────────────
        private static void SeedUsers(ApplicationDbContext context)
        {
            var users = new[]
            {
                new User
                {
                    FirstName = "System",
                    LastName = "Administrator",
                    Email = "admin@thesis.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("AdminPass123!"),
                    Role = "admin",
                    IsApproved = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    FirstName = "John",
                    LastName = "Smith",
                    Email = "faculty@thesis.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                    Role = "faculty",
                    Department = "Computer Engineering",
                    IsApproved = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    FirstName = "Jane",
                    LastName = "Doe",
                    Email = "student@thesis.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                    Role = "student",
                    Department = "Computer Engineering",
                    IsApproved = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    FirstName = "Mary",
                    LastName = "Johnson",
                    Email = "uploader@thesis.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                    Role = "uploader",
                    Department = "Computer Engineering",
                    IsApproved = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    FirstName = "Robert",
                    LastName = "Brown",
                    Email = "approver@thesis.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                    Role = "approver",
                    Department = "Computer Engineering",
                    IsApproved = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            context.Users.AddRange(users);
            context.SaveChanges();
        }

        // ─────────────────────────────────────────────
        // Seed Theses
        // ─────────────────────────────────────────────
        private static void SeedTheses(ApplicationDbContext context)
        {
            var uploader = context.Users.FirstOrDefault(u => u.Email == "uploader@thesis.com");
            var approver = context.Users.FirstOrDefault(u => u.Email == "approver@thesis.com");

            if (uploader == null) return;

            var theses = new[]
            {
                new Thesis
                {
                    Title = "Machine Learning Applications in Sustainable Energy Systems",
                    Abstract = "AI in energy optimization.",
                    Keywords = "[\"AI\",\"energy\"]",
                    Authors = "Jane Doe",
                    Department = "Computer Engineering",
                    FieldOfResearch = "AI",
                    Year = 2024,
                    Status = "approved",
                    UploadedBy = uploader.UserId,
                    ApprovedBy = approver?.UserId,
                    UploadedAt = new DateTime(2024, 1, 15),
                    ApprovedAt = new DateTime(2024, 1, 20)
                }
            };

            context.Theses.AddRange(theses);
            context.SaveChanges();
        }
    }
}
