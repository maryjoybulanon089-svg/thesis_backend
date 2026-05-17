-- ============================================================
-- 3_SeedData.sql  (OPTIONAL)
-- Use this only if you want to pre-populate the database
-- WITHOUT running the C# backend first.
--
-- ⚠ IMPORTANT: BCrypt password hashes below were generated
--   by the C# DbInitializer. If you have already run the
--   backend once, the seed data was inserted automatically.
--   Running this script a second time will cause duplicate-
--   email errors.
--
-- Credentials:
--   admin@thesis.com    → AdminPass123!
--   faculty@thesis.com  → password123
--   student@thesis.com  → password123
--   uploader@thesis.com → password123
--   approver@thesis.com → password123
-- ============================================================

USE ThesisRepositoryDB;
GO

-- ──────────────────────────────────────────────────────────────────────────────
-- Seed Users (only if table is empty)
-- ──────────────────────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT TOP 1 1 FROM [dbo].[Users])
BEGIN
    -- NOTE: Replace the PasswordHash values below with BCrypt hashes generated
    --       by running the C# application once (it will seed these automatically).
    --       The hashes shown are placeholders only.
    --
    --       Alternatively, just run the backend: it will call DbInitializer
    --       and insert these rows with real BCrypt hashes.

    PRINT 'Users table is empty. Please start the C# backend to seed users';
    PRINT 'with properly BCrypt-hashed passwords, or generate hashes manually.';
END
ELSE
BEGIN
    PRINT 'Users table already has data — seed skipped.';
END
GO

-- ──────────────────────────────────────────────────────────────────────────────
-- Quick health-check queries
-- ──────────────────────────────────────────────────────────────────────────────
SELECT COUNT(*) AS TotalUsers        FROM [dbo].[Users];
SELECT COUNT(*) AS TotalTheses       FROM [dbo].[Theses];
SELECT COUNT(*) AS TotalResetRequests FROM [dbo].[PasswordResetRequests];
GO
