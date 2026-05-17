-- ============================================================
-- 2_AlterDatabase.sql
-- Run this script in SSMS (ThesisRepositoryDB) ONCE before
-- starting the C# backend for the first time.
-- All statements are idempotent (safe to re-run).
-- Server  : CPE\SQLEXPRESS
-- Database: ThesisRepositoryDB
-- ============================================================

USE ThesisRepositoryDB;
GO

-- ──────────────────────────────────────────────────────────────────────────────
-- 1. Add IsApproved column to Users (not in the original schema)
--    All pre-existing users are approved automatically.
-- ──────────────────────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[Users]')
      AND name = N'IsApproved'
)
BEGIN
    ALTER TABLE [dbo].[Users]
        ADD [IsApproved] BIT NOT NULL DEFAULT 0;

    PRINT 'Column [IsApproved] added to [Users] and all existing users approved.';
END
ELSE
BEGIN
    PRINT 'Column [IsApproved] already exists in [Users] — skipped.';
END
GO

-- Approve all existing users (runs after column is created)
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[Users]')
      AND name = N'IsApproved'
)
BEGIN
    UPDATE [dbo].[Users] SET [IsApproved] = 1;
    PRINT 'All existing users have been approved.';
END
GO

-- ──────────────────────────────────────────────────────────────────────────────
-- 2. Create PasswordResetRequests table
--    (does not exist in the original ThesisRepositoryDB schema)
-- ──────────────────────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM sys.tables WHERE name = N'PasswordResetRequests'
)
BEGIN
    CREATE TABLE [dbo].[PasswordResetRequests] (
        [Id]          NVARCHAR(450) NOT NULL
                          CONSTRAINT PK_PasswordResetRequests PRIMARY KEY,
        [Email]       NVARCHAR(255) NOT NULL,
        [Status]      NVARCHAR(50)  NOT NULL
                          CONSTRAINT DF_PRR_Status DEFAULT 'pending',
        [RequestedAt] DATETIME2     NOT NULL
                          CONSTRAINT DF_PRR_RequestedAt DEFAULT GETUTCDATE(),
        [ProcessedAt] DATETIME2         NULL,
        [ProcessedBy] NVARCHAR(255)     NULL
    );

    CREATE NONCLUSTERED INDEX IX_PasswordResetRequests_Email
        ON [dbo].[PasswordResetRequests] ([Email]);

    CREATE NONCLUSTERED INDEX IX_PasswordResetRequests_Status
        ON [dbo].[PasswordResetRequests] ([Status]);

    PRINT 'Table [PasswordResetRequests] created successfully.';
END
ELSE
BEGIN
    PRINT 'Table [PasswordResetRequests] already exists — skipped.';
END
GO

-- ──────────────────────────────────────────────────────────────────────────────
-- 3. Verify schema
-- ──────────────────────────────────────────────────────────────────────────────
SELECT
    TABLE_NAME,
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME IN ('Users', 'Theses', 'PasswordResetRequests')
ORDER BY TABLE_NAME, ORDINAL_POSITION;
GO

PRINT '========================================================';
PRINT '2_AlterDatabase.sql completed.';
PRINT 'You can now start the C# ASP.NET Core backend.';
PRINT '========================================================';
GO
-- Add StudentId column to Users if missing (required by current backend model)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[Users]')
      AND name = N'StudentId'
)
BEGIN
    ALTER TABLE [dbo].[Users]
        ADD [StudentId] NVARCHAR(50) NULL;

    PRINT 'Column [StudentId] added to [Users].';
END
ELSE
BEGIN
    PRINT 'Column [StudentId] already exists in [Users] - skipped.';
END
GO
