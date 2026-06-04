-- ============================================================
-- 8_AddPdfDataColumn.sql
-- Idempotent script to add PdfData (bytea) column to Theses table.
-- Safe to re-run.
-- ============================================================

USE ThesisRepositoryDB;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[Theses]')
      AND name = N'PdfData'
)
BEGIN
    ALTER TABLE [dbo].[Theses]
        ADD [PdfData] bytea NULL;

    PRINT 'Column [PdfData] added to [Theses].';
END
ELSE
BEGIN
    PRINT 'Column [PdfData] already exists in [Theses] - skipped.';
END
GO

SELECT 
    COLUMN_NAME, 
    DATA_TYPE, 
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Theses'
ORDER BY ORDINAL_POSITION;
GO

PRINT 'Migration complete.';
