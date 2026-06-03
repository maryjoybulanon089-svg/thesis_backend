-- ============================================================
-- 7_AddDoiAndPublishedFlag.sql
-- Idempotent script to add Doi column to Theses and ensure status column exists.
-- Safe to re-run.
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

-- Note: isPublished is derived from Status == 'approved' and does not require a DB column.

SELECT 
    COLUMN_NAME, 
    DATA_TYPE, 
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Theses'
ORDER BY ORDINAL_POSITION;
GO

PRINT 'Migration complete.';
