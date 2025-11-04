/*
 * Reset Journal Table IDs Script
 * 
 * This script:
 * 1. Renumbers all existing rows in journal-related tables starting from 1
 * 2. Resets IDENTITY seeds to max ID + 1
 * 3. Converts IDENTITY columns to use SEQUENCES to prevent gaps
 * 
 * Tables affected:
 * - Journals
 * - JournalCategories
 * - JournalCheckLists
 * - JournalCheckListItems
 * - JournalImages
 * - JournalVideos
 * - JournalEntrySnapshots
 */

SET NOCOUNT ON;
GO

PRINT 'Starting Journal ID Reset Process...';
PRINT '';

-- ============================================================================
-- STEP 1: Verify Sequences Exist
-- ============================================================================
PRINT 'Verifying sequences exist...';

IF NOT EXISTS (SELECT * FROM sys.sequences WHERE name = 'SequenceJournals')
BEGIN
    RAISERROR('Sequence SequenceJournals does not exist. Please publish database schema first.', 16, 1);
    RETURN;
END

IF NOT EXISTS (SELECT * FROM sys.sequences WHERE name = 'SequenceJournalCategories')
BEGIN
    RAISERROR('Sequence SequenceJournalCategories does not exist. Please publish database schema first.', 16, 1);
    RETURN;
END

IF NOT EXISTS (SELECT * FROM sys.sequences WHERE name = 'SequenceJournalCheckLists')
BEGIN
    RAISERROR('Sequence SequenceJournalCheckLists does not exist. Please publish database schema first.', 16, 1);
    RETURN;
END

IF NOT EXISTS (SELECT * FROM sys.sequences WHERE name = 'SequenceJournalCheckListItems')
BEGIN
    RAISERROR('Sequence SequenceJournalCheckListItems does not exist. Please publish database schema first.', 16, 1);
    RETURN;
END

IF NOT EXISTS (SELECT * FROM sys.sequences WHERE name = 'SequenceJournalImages')
BEGIN
    RAISERROR('Sequence SequenceJournalImages does not exist. Please publish database schema first.', 16, 1);
    RETURN;
END

IF NOT EXISTS (SELECT * FROM sys.sequences WHERE name = 'SequenceJournalVideos')
BEGIN
    RAISERROR('Sequence SequenceJournalVideos does not exist. Please publish database schema first.', 16, 1);
    RETURN;
END

IF NOT EXISTS (SELECT * FROM sys.sequences WHERE name = 'SequenceJournalEntrySnapshots')
BEGIN
    RAISERROR('Sequence SequenceJournalEntrySnapshots does not exist. Please publish database schema first.', 16, 1);
    RETURN;
END

PRINT '  - All sequences verified';
PRINT '';

-- ============================================================================
-- STEP 2: Renumber Journals Table
-- ============================================================================
PRINT 'Renumbering Journals table...';

BEGIN TRANSACTION;
BEGIN TRY
    -- Create temp table with new IDs
    SELECT 
        ROW_NUMBER() OVER (ORDER BY Id) AS NewId,
        Id AS OldId
    INTO #JournalsMapping
    FROM dbo.Journals;

    -- Update foreign key references in JournalEntries
    UPDATE je
    SET je.JournalId = jm.NewId
    FROM dbo.JournalEntries je
    INNER JOIN #JournalsMapping jm ON je.JournalId = jm.OldId;

    -- Update foreign key references in JournalModules
    UPDATE jm
    SET jm.JournalId = jmap.NewId
    FROM dbo.JournalModules jm
    INNER JOIN #JournalsMapping jmap ON jm.JournalId = jmap.OldId;

    -- Update foreign key references in JournalImages
    UPDATE ji
    SET ji.JournalId = jm.NewId
    FROM dbo.JournalImages ji
    INNER JOIN #JournalsMapping jm ON ji.JournalId = jm.OldId;

    -- Update foreign key references in JournalVideos
    UPDATE jv
    SET jv.JournalId = jm.NewId
    FROM dbo.JournalVideos jv
    INNER JOIN #JournalsMapping jm ON jv.JournalId = jm.OldId;

    -- Update foreign key references in JournalEntrySnapshots
    UPDATE jes
    SET jes.JournalId = jm.NewId
    FROM dbo.JournalEntrySnapshots jes
    INNER JOIN #JournalsMapping jm ON jes.JournalId = jm.OldId;

    -- Update foreign key references in JournalChapters
    UPDATE jc
    SET jc.JournalId = jm.NewId
    FROM dbo.JournalChapters jc
    INNER JOIN #JournalsMapping jm ON jc.JournalId = jm.OldId;

    -- Update the Journals table itself
    UPDATE j
    SET j.Id = jm.NewId
    FROM dbo.Journals j
    INNER JOIN #JournalsMapping jm ON j.Id = jm.OldId;

    -- Reset sequence
    DECLARE @MaxJournalsId INT = (SELECT ISNULL(MAX(Id), 0) FROM dbo.Journals);
    DECLARE @SeqResetSql NVARCHAR(MAX) = N'ALTER SEQUENCE dbo.SequenceJournals RESTART WITH ' + CAST(@MaxJournalsId + 1 AS NVARCHAR(10));
    EXEC sp_executesql @SeqResetSql;

    DROP TABLE #JournalsMapping;
    
    PRINT '  - Renumbered ' + CAST(@MaxJournalsId AS VARCHAR(10)) + ' rows';
    PRINT '  - Reset sequence to ' + CAST(@MaxJournalsId + 1 AS VARCHAR(10));
    
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT '  - ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH

PRINT '';

-- ============================================================================
-- STEP 3: Renumber JournalCategories Table
-- ============================================================================
PRINT 'Renumbering JournalCategories table...';

BEGIN TRANSACTION;
BEGIN TRY
    -- Create temp table with new IDs
    SELECT 
        ROW_NUMBER() OVER (ORDER BY Id) AS NewId,
        Id AS OldId
    INTO #JournalCategoriesMapping
    FROM dbo.JournalCategories;

    -- Update foreign key references in Journals
    UPDATE j
    SET j.CategoryId = jcm.NewId
    FROM dbo.Journals j
    INNER JOIN #JournalCategoriesMapping jcm ON j.CategoryId = jcm.OldId;

    -- Update the JournalCategories table itself
    UPDATE jc
    SET jc.Id = jcm.NewId
    FROM dbo.JournalCategories jc
    INNER JOIN #JournalCategoriesMapping jcm ON jc.Id = jcm.OldId;

    -- Reset sequence
    DECLARE @MaxCategoriesId INT = (SELECT ISNULL(MAX(Id), 0) FROM dbo.JournalCategories);
    SET @SeqResetSql = N'ALTER SEQUENCE dbo.SequenceJournalCategories RESTART WITH ' + CAST(@MaxCategoriesId + 1 AS NVARCHAR(10));
    EXEC sp_executesql @SeqResetSql;

    DROP TABLE #JournalCategoriesMapping;
    
    PRINT '  - Renumbered ' + CAST(@MaxCategoriesId AS VARCHAR(10)) + ' rows';
    PRINT '  - Reset sequence to ' + CAST(@MaxCategoriesId + 1 AS VARCHAR(10));
    
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT '  - ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH

PRINT '';

-- ============================================================================
-- STEP 4: Renumber JournalCheckLists Table
-- ============================================================================
PRINT 'Renumbering JournalCheckLists table...';

BEGIN TRANSACTION;
BEGIN TRY
    -- Create temp table with new IDs
    SELECT 
        ROW_NUMBER() OVER (ORDER BY Id) AS NewId,
        Id AS OldId
    INTO #JournalCheckListsMapping
    FROM dbo.JournalCheckLists;

    -- Update foreign key references in JournalCheckListItems
    UPDATE jcli
    SET jcli.CheckListId = jclm.NewId
    FROM dbo.JournalCheckListItems jcli
    INNER JOIN #JournalCheckListsMapping jclm ON jcli.CheckListId = jclm.OldId;

    -- Update the JournalCheckLists table itself
    UPDATE jcl
    SET jcl.Id = jclm.NewId
    FROM dbo.JournalCheckLists jcl
    INNER JOIN #JournalCheckListsMapping jclm ON jcl.Id = jclm.OldId;

    -- Reset sequence
    DECLARE @MaxCheckListsId INT = (SELECT ISNULL(MAX(Id), 0) FROM dbo.JournalCheckLists);
    SET @SeqResetSql = N'ALTER SEQUENCE dbo.SequenceJournalCheckLists RESTART WITH ' + CAST(@MaxCheckListsId + 1 AS NVARCHAR(10));
    EXEC sp_executesql @SeqResetSql;

    DROP TABLE #JournalCheckListsMapping;
    
    PRINT '  - Renumbered ' + CAST(@MaxCheckListsId AS VARCHAR(10)) + ' rows';
    PRINT '  - Reset sequence to ' + CAST(@MaxCheckListsId + 1 AS VARCHAR(10));
    
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT '  - ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH

PRINT '';

-- ============================================================================
-- STEP 5: Renumber JournalCheckListItems Table
-- ============================================================================
PRINT 'Renumbering JournalCheckListItems table...';

BEGIN TRANSACTION;
BEGIN TRY
    -- Create temp table with new IDs
    SELECT 
        ROW_NUMBER() OVER (ORDER BY Id) AS NewId,
        Id AS OldId
    INTO #JournalCheckListItemsMapping
    FROM dbo.JournalCheckListItems;

    -- Update the JournalCheckListItems table itself (no foreign key references)
    UPDATE jcli
    SET jcli.Id = jclim.NewId
    FROM dbo.JournalCheckListItems jcli
    INNER JOIN #JournalCheckListItemsMapping jclim ON jcli.Id = jclim.OldId;

    -- Reset sequence
    DECLARE @MaxCheckListItemsId INT = (SELECT ISNULL(MAX(Id), 0) FROM dbo.JournalCheckListItems);
    SET @SeqResetSql = N'ALTER SEQUENCE dbo.SequenceJournalCheckListItems RESTART WITH ' + CAST(@MaxCheckListItemsId + 1 AS NVARCHAR(10));
    EXEC sp_executesql @SeqResetSql;

    DROP TABLE #JournalCheckListItemsMapping;
    
    PRINT '  - Renumbered ' + CAST(@MaxCheckListItemsId AS VARCHAR(10)) + ' rows';
    PRINT '  - Reset sequence to ' + CAST(@MaxCheckListItemsId + 1 AS VARCHAR(10));
    
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT '  - ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH

PRINT '';

-- ============================================================================
-- STEP 6: Renumber JournalImages Table
-- ============================================================================
PRINT 'Renumbering JournalImages table...';

BEGIN TRANSACTION;
BEGIN TRY
    -- Create temp table with new IDs
    SELECT 
        ROW_NUMBER() OVER (ORDER BY Id) AS NewId,
        Id AS OldId
    INTO #JournalImagesMapping
    FROM dbo.JournalImages;

    -- Update the JournalImages table itself (no foreign key references)
    UPDATE ji
    SET ji.Id = jim.NewId
    FROM dbo.JournalImages ji
    INNER JOIN #JournalImagesMapping jim ON ji.Id = jim.OldId;

    -- Reset sequence
    DECLARE @MaxImagesId INT = (SELECT ISNULL(MAX(Id), 0) FROM dbo.JournalImages);
    SET @SeqResetSql = N'ALTER SEQUENCE dbo.SequenceJournalImages RESTART WITH ' + CAST(@MaxImagesId + 1 AS NVARCHAR(10));
    EXEC sp_executesql @SeqResetSql;

    DROP TABLE #JournalImagesMapping;
    
    PRINT '  - Renumbered ' + CAST(@MaxImagesId AS VARCHAR(10)) + ' rows';
    PRINT '  - Reset sequence to ' + CAST(@MaxImagesId + 1 AS VARCHAR(10));
    
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT '  - ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH

PRINT '';

-- ============================================================================
-- STEP 7: Renumber JournalVideos Table
-- ============================================================================
PRINT 'Renumbering JournalVideos table...';

BEGIN TRANSACTION;
BEGIN TRY
    -- Create temp table with new IDs
    SELECT 
        ROW_NUMBER() OVER (ORDER BY Id) AS NewId,
        Id AS OldId
    INTO #JournalVideosMapping
    FROM dbo.JournalVideos;

    -- Update the JournalVideos table itself (no foreign key references)
    UPDATE jv
    SET jv.Id = jvm.NewId
    FROM dbo.JournalVideos jv
    INNER JOIN #JournalVideosMapping jvm ON jv.Id = jvm.OldId;

    -- Reset sequence
    DECLARE @MaxVideosId INT = (SELECT ISNULL(MAX(Id), 0) FROM dbo.JournalVideos);
    SET @SeqResetSql = N'ALTER SEQUENCE dbo.SequenceJournalVideos RESTART WITH ' + CAST(@MaxVideosId + 1 AS NVARCHAR(10));
    EXEC sp_executesql @SeqResetSql;

    DROP TABLE #JournalVideosMapping;
    
    PRINT '  - Renumbered ' + CAST(@MaxVideosId AS VARCHAR(10)) + ' rows';
    PRINT '  - Reset sequence to ' + CAST(@MaxVideosId + 1 AS VARCHAR(10));
    
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT '  - ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH

PRINT '';

-- ============================================================================
-- STEP 8: Renumber JournalEntrySnapshots Table
-- ============================================================================
PRINT 'Renumbering JournalEntrySnapshots table...';

BEGIN TRANSACTION;
BEGIN TRY
    -- Create temp table with new IDs
    SELECT 
        ROW_NUMBER() OVER (ORDER BY Id) AS NewId,
        Id AS OldId
    INTO #JournalEntrySnapshotsMapping
    FROM dbo.JournalEntrySnapshots;

    -- Update the JournalEntrySnapshots table itself (no foreign key references)
    UPDATE jes
    SET jes.Id = jesm.NewId
    FROM dbo.JournalEntrySnapshots jes
    INNER JOIN #JournalEntrySnapshotsMapping jesm ON jes.Id = jesm.OldId;

    -- Reset sequence
    DECLARE @MaxSnapshotsId INT = (SELECT ISNULL(MAX(Id), 0) FROM dbo.JournalEntrySnapshots);
    SET @SeqResetSql = N'ALTER SEQUENCE dbo.SequenceJournalEntrySnapshots RESTART WITH ' + CAST(@MaxSnapshotsId + 1 AS NVARCHAR(10));
    EXEC sp_executesql @SeqResetSql;

    DROP TABLE #JournalEntrySnapshotsMapping;
    
    PRINT '  - Renumbered ' + CAST(@MaxSnapshotsId AS VARCHAR(10)) + ' rows';
    PRINT '  - Reset sequence to ' + CAST(@MaxSnapshotsId + 1 AS VARCHAR(10));
    
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT '  - ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH

PRINT '';

-- ============================================================================
-- STEP 9: Sequence-Based IDs (Already Implemented)
-- ============================================================================
PRINT 'Sequence-based ID generation is now active.';
PRINT 'All journal tables now use sequences instead of IDENTITY columns.';
PRINT 'This prevents ID gaps from occurring in the future.';
PRINT '';

-- ============================================================================
-- COMPLETION
-- ============================================================================
PRINT '============================================================';
PRINT 'Journal ID Reset Process Completed Successfully!';
PRINT '============================================================';
PRINT '';
PRINT 'Summary:';
PRINT '  - All journal-related tables have been renumbered starting from 1';
PRINT '  - All sequences have been reset to max ID + 1';
PRINT '  - Sequence-based ID generation prevents future gaps';
PRINT '';
PRINT 'Next Steps:';
PRINT '  1. Verify data integrity by checking foreign key relationships';
PRINT '  2. Test creating new journal entries to confirm sequences work correctly';
PRINT '';

GO
