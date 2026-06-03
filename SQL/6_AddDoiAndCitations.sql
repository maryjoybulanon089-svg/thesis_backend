-- ============================================================
-- 6_AddDoiAndCitations.sql
-- Run this script in SSMS (ThesisRepositoryDB) to add DOI and
-- citation storage columns to the Theses table.
-- All statements are idempotent (safe to re-run).
-- ============================================================

USE ThesisRepositoryDB;
GO

-- Add Doi column
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[Theses]')
      AND name = N'Doi'
)
BEGIN
    ALTER TABLE [dbo].[Theses]
        ADD [Doi] NVARCHAR(255) NULL;

    PRINT 'Column [Doi] added to [Theses].';
END
ELSE
BEGIN
    PRINT 'Column [Doi] already exists in [Theses] - skipped.';
END
GO

-- Add IeeeCitation column
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[Theses]')
      AND name = N'IeeeCitation'
)
BEGIN
    ALTER TABLE [dbo].[Theses]
        ADD [IeeeCitation] NVARCHAR(MAX) NULL;

    PRINT 'Column [IeeeCitation] added to [Theses].';
END
ELSE
BEGIN
    PRINT 'Column [IeeeCitation] already exists in [Theses] - skipped.';
END
GO

-- Add AcsCitation column
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[Theses]')
      AND name = N'AcsCitation'
)
BEGIN
    ALTER TABLE [dbo].[Theses]
        ADD [AcsCitation] NVARCHAR(MAX) NULL;

    PRINT 'Column [AcsCitation] added to [Theses].';
END
ELSE
BEGIN
    PRINT 'Column [AcsCitation] already exists in [Theses] - skipped.';
END
GO

-- Verification: Show the updated Theses table schema
SELECT 
    COLUMN_NAME, 
    DATA_TYPE, 
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Theses'
ORDER BY ORDINAL_POSITION;
GO

PRINT 'Migration complete. All new columns have been added to the Theses table.';
