using ThesisRepository.Models;
using Microsoft.EntityFrameworkCore;

namespace ThesisRepository.Data
{
    public static class DbInitializer
    {
        /// <summary>
        /// Called at startup. Applies schema patches to the existing SSMS database
        /// and seeds initial data if the Users table is empty.
        /// </summary>
        public static void Initialize(ApplicationDbContext context)
        {
            // Step 1 – patch the existing SSMS schema with any missing pieces
            EnsureSchemaUpdates(context);

            // Step 2 – seed only if the table is empty
            if (context.Users.Any())
            {
                return; // already seeded
            }

            SeedUsers(context);
            SeedTheses(context);
        }

        // ────────────────────────────────────────────────────────────────────────
        // Schema patches  (safe to run repeatedly – uses IF NOT EXISTS guards)
        // ────────────────────────────────────────────────────────────────────────
        private static void EnsureSchemaUpdates(ApplicationDbContext context)
        {
            // Add IsApproved column to Users if it was not in the original SQL script
            context.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = N'IsApproved'
                )
                BEGIN
                    ALTER TABLE [dbo].[Users] ADD [IsApproved] BIT NOT NULL DEFAULT 0;
                    UPDATE [dbo].[Users] SET [IsApproved] = 1;
                END
            ");

            // Add FirstName column to Users if missing
            context.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = N'FirstName'
                )
                BEGIN
                    ALTER TABLE [dbo].[Users] ADD [FirstName] NVARCHAR(100) NOT NULL DEFAULT '';
                END
            ");

            // Add LastName column to Users if missing
            context.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = N'LastName'
                )
                BEGIN
                    ALTER TABLE [dbo].[Users] ADD [LastName] NVARCHAR(100) NOT NULL DEFAULT '';
                END
            ");

            // Add IsDeleted column to Theses if missing
            context.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'[dbo].[Theses]') AND name = N'IsDeleted'
                )
                BEGIN
                    ALTER TABLE [dbo].[Theses] ADD [IsDeleted] BIT NOT NULL DEFAULT 0;
                END
            ");

            // Add RejectedAt column to Theses if missing
            context.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'[dbo].[Theses]') AND name = N'RejectedAt'
                )
                BEGIN
                    ALTER TABLE [dbo].[Theses] ADD [RejectedAt] DATETIME2 NULL;
                END
            ");

            // Create PasswordResetRequests table if it does not exist
            context.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.tables WHERE name = N'PasswordResetRequests'
                )
                BEGIN
                    CREATE TABLE [dbo].[PasswordResetRequests] (
                        [Id]          NVARCHAR(450) NOT NULL PRIMARY KEY,
                        [Email]       NVARCHAR(255) NOT NULL,
                        [Status]      NVARCHAR(50)  NOT NULL DEFAULT 'pending',
                        [RequestedAt] DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
                        [ProcessedAt] DATETIME2         NULL,
                        [ProcessedBy] NVARCHAR(255)     NULL
                    );
                    PRINT 'Table PasswordResetRequests created by DbInitializer.';
                END
            ");
        }

        // ────────────────────────────────────────────────────────────────────────
        // Seed Users
        // ────────────────────────────────────────────────────────────────────────
        private static void SeedUsers(ApplicationDbContext context)
        {
            var users = new[]
            {
                new User
                {
                    FirstName   = "System",
                    LastName    = "Administrator",
                    Email       = "admin@thesis.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("AdminPass123!"),
                    Role        = "admin",
                    IsApproved  = true,
                    IsActive    = true,
                    CreatedAt   = DateTime.UtcNow
                },
                new User
                {
                    FirstName   = "John",
                    LastName    = "Smith",
                    Email       = "faculty@thesis.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                    Role        = "faculty",
                    Department  = "Computer Engineering",
                    IsApproved  = true,
                    IsActive    = true,
                    CreatedAt   = DateTime.UtcNow
                },
                new User
                {
                    FirstName   = "Jane",
                    LastName    = "Doe",
                    Email       = "student@thesis.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                    Role        = "student",
                    Department  = "Computer Engineering",
                    IsApproved  = true,
                    IsActive    = true,
                    CreatedAt   = DateTime.UtcNow
                },
                new User
                {
                    FirstName   = "Mary",
                    LastName    = "Johnson",
                    Email       = "uploader@thesis.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                    Role        = "uploader",
                    Department  = "Computer Engineering",
                    IsApproved  = true,
                    IsActive    = true,
                    CreatedAt   = DateTime.UtcNow
                },
                new User
                {
                    FirstName   = "Robert",
                    LastName    = "Brown",
                    Email       = "approver@thesis.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                    Role        = "approver",
                    Department  = "Computer Engineering",
                    IsApproved  = true,
                    IsActive    = true,
                    CreatedAt   = DateTime.UtcNow
                }
            };

            context.Users.AddRange(users);
            context.SaveChanges();
        }

        // ────────────────────────────────────────────────────────────────────────
        // Seed Theses  (using the real int UserId values assigned by SQL Server)
        // ────────────────────────────────────────────────────────────────────────
        private static void SeedTheses(ApplicationDbContext context)
        {
            // Re-query seeded users to obtain their auto-generated int UserId values
            var uploader = context.Users.FirstOrDefault(u => u.Email == "uploader@thesis.com");
            var approver = context.Users.FirstOrDefault(u => u.Email == "approver@thesis.com");

            if (uploader == null) return;

            var theses = new[]
            {
                new Thesis
                {
                    Title           = "Machine Learning Applications in Sustainable Energy Systems",
                    Abstract        = "This research explores the application of machine learning algorithms in optimizing sustainable energy systems. The study focuses on predictive maintenance, energy forecasting, and grid optimization using deep learning techniques.",
                    Keywords        = "[\"machine learning\",\"sustainable energy\",\"renewable energy\",\"deep learning\",\"optimization\"]",
                    Authors         = "Jane Doe, John Smith",
                    Advisors        = "Dr. Robert Brown",
                    Department      = "Computer Engineering",
                    FieldOfResearch = "Artificial Intelligence",
                    Year            = 2024,
                    Status          = "approved",
                    UploadedBy      = uploader.UserId,
                    ApprovedBy      = approver?.UserId,
                    UploadedAt      = new DateTime(2024, 1, 15),
                    ApprovedAt      = new DateTime(2024, 1, 20)
                },
                new Thesis
                {
                    Title           = "IoT-Based Smart Agriculture Monitoring System",
                    Abstract        = "This thesis presents a comprehensive IoT-based monitoring system for precision agriculture. The system integrates soil sensors, weather stations, and automated irrigation controls to optimize crop yield and resource utilization.",
                    Keywords        = "[\"IoT\",\"smart agriculture\",\"precision farming\",\"sensors\",\"automation\"]",
                    Authors         = "Michael Chen",
                    Advisors        = "Dr. John Smith",
                    Department      = "Electrical Engineering",
                    FieldOfResearch = "Internet of Things",
                    Year            = 2024,
                    Status          = "approved",
                    UploadedBy      = uploader.UserId,
                    ApprovedBy      = approver?.UserId,
                    UploadedAt      = new DateTime(2024, 2, 1),
                    ApprovedAt      = new DateTime(2024, 2, 10)
                },
                new Thesis
                {
                    Title           = "Blockchain Technology for Supply Chain Transparency",
                    Abstract        = "An investigation into the use of blockchain technology to enhance transparency and traceability in supply chain management. The study implements a proof-of-concept system for tracking pharmaceutical products.",
                    Keywords        = "[\"blockchain\",\"supply chain\",\"transparency\",\"distributed ledger\",\"smart contracts\"]",
                    Authors         = "Sarah Williams",
                    Department      = "Computer Engineering",
                    FieldOfResearch = "Distributed Systems",
                    Year            = 2023,
                    Status          = "pending",
                    UploadedBy      = uploader.UserId,
                    UploadedAt      = new DateTime(2024, 3, 1)
                },
                new Thesis
                {
                    Title           = "Autonomous Vehicle Navigation Using Computer Vision",
                    Abstract        = "This research develops a real-time navigation system for autonomous vehicles using advanced computer vision techniques. The system employs convolutional neural networks for object detection and path planning.",
                    Keywords        = "[\"autonomous vehicles\",\"computer vision\",\"deep learning\",\"object detection\",\"navigation\"]",
                    Authors         = "David Lee, Emily Rodriguez",
                    Advisors        = "Dr. Robert Brown",
                    Department      = "Mechatronics Engineering",
                    FieldOfResearch = "Robotics and Automation",
                    Year            = 2023,
                    Status          = "approved",
                    UploadedBy      = uploader.UserId,
                    ApprovedBy      = approver?.UserId,
                    UploadedAt      = new DateTime(2023, 12, 1),
                    ApprovedAt      = new DateTime(2023, 12, 15)
                },
                new Thesis
                {
                    Title           = "Cybersecurity Framework for Industrial Control Systems",
                    Abstract        = "A comprehensive cybersecurity framework designed specifically for protecting industrial control systems from cyber threats. The framework includes intrusion detection, anomaly detection, and incident response protocols.",
                    Keywords        = "[\"cybersecurity\",\"industrial control systems\",\"SCADA\",\"intrusion detection\",\"incident response\"]",
                    Authors         = "Alex Martinez",
                    Department      = "Computer Engineering",
                    FieldOfResearch = "Cybersecurity",
                    Year            = 2024,
                    Status          = "pending",
                    UploadedBy      = uploader.UserId,
                    UploadedAt      = new DateTime(2024, 2, 20)
                }
            };

            context.Theses.AddRange(theses);
            context.SaveChanges();
        }
    }
}
