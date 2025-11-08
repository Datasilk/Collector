-- =============================================
-- Reset All Sequences to MAX(Id) + 1
-- =============================================
-- This script resets all sequences in the database to the maximum ID value + 1
-- from their corresponding tables to prevent ID conflicts.
-- =============================================

DECLARE @SequenceName NVARCHAR(128)
DECLARE @TableName NVARCHAR(128)
DECLARE @MaxId BIGINT
DECLARE @SQL NVARCHAR(MAX)

-- Create a temporary table to hold sequence-to-table mappings
CREATE TABLE #SequenceMappings (
    SequenceName NVARCHAR(128),
    TableName NVARCHAR(128),
    IdColumnName NVARCHAR(128)
)

-- Insert all sequence-to-table mappings
INSERT INTO #SequenceMappings (SequenceName, TableName, IdColumnName) VALUES
    ('SequenceAnalyzerRules', 'AnalyzerRules', 'ruleId'),
    ('SequenceArticleBugs', 'ArticleBugs', 'bugId'),
    ('SequenceArticles', 'Articles', 'articleId'),
    ('SequenceDomainCollectionGroups', 'DomainCollectionGroups', 'colgroupId'),
    ('SequenceDomainCollections', 'DomainCollections', 'colId'),
    ('SequenceDomainTypeMatches', 'DomainTypeMatches', 'matchId'),
    ('SequenceDomains', 'Domains', 'domainId'),
    ('SequenceDownloadQueue', 'DownloadQueue', 'qid'),
    ('SequenceDownloadRules', 'DownloadRules', 'ruleId'),
    ('SequenceFeedCategories', 'FeedCategories', 'categoryId'),
    ('SequenceFeeds', 'Feeds', 'feedId'),
    ('SequenceJournalCategories', 'JournalCategories', 'Id'),
    ('SequenceJournalCheckListItems', 'JournalCheckListItems', 'Id'),
    ('SequenceJournalCheckLists', 'JournalCheckLists', 'Id'),
    ('SequenceJournalEntrySnapshots', 'JournalEntrySnapshots', 'Id'),
    ('SequenceJournalFiles', 'JournalFiles', 'Id'),
    ('SequenceJournalImages', 'JournalImages', 'Id'),
    ('SequenceJournalVideos', 'JournalVideos', 'Id'),
    ('SequenceJournals', 'Journals', 'Id'),
    ('SequenceStatisticsProjects', 'StatisticsProjects', 'projectId'),
    ('SequenceStatisticsResults', 'StatisticsResults', 'statId'),
    ('SequenceSubjects', 'Subjects', 'subjectId'),
    ('SequenceWords', 'Words', 'wordId')

-- Cursor to iterate through all sequences
DECLARE sequence_cursor CURSOR FOR
SELECT SequenceName, TableName, IdColumnName FROM #SequenceMappings

OPEN sequence_cursor

FETCH NEXT FROM sequence_cursor INTO @SequenceName, @TableName, @SQL

WHILE @@FETCH_STATUS = 0
BEGIN
    -- Build dynamic SQL to get MAX(Id) from the table
    SET @SQL = N'SELECT @MaxIdOut = ISNULL(MAX(' + @SQL + '), 0) FROM [dbo].[' + @TableName + ']'
    
    -- Execute dynamic SQL to get the max ID
    EXEC sp_executesql @SQL, N'@MaxIdOut BIGINT OUTPUT', @MaxIdOut = @MaxId OUTPUT
    
    -- Reset the sequence to MAX(Id) + 1
    IF @MaxId > 0
    BEGIN
        SET @SQL = N'ALTER SEQUENCE [dbo].[' + @SequenceName + '] RESTART WITH ' + CAST(@MaxId + 1 AS NVARCHAR(20))
        EXEC sp_executesql @SQL
        PRINT 'Reset ' + @SequenceName + ' to ' + CAST(@MaxId + 1 AS NVARCHAR(20))
    END
    ELSE
    BEGIN
        -- If table is empty, reset to 1
        SET @SQL = N'ALTER SEQUENCE [dbo].[' + @SequenceName + '] RESTART WITH 1'
        EXEC sp_executesql @SQL
        PRINT 'Reset ' + @SequenceName + ' to 1 (table is empty)'
    END
    
    FETCH NEXT FROM sequence_cursor INTO @SequenceName, @TableName, @SQL
END

CLOSE sequence_cursor
DEALLOCATE sequence_cursor

-- Clean up
DROP TABLE #SequenceMappings

PRINT 'All sequences have been reset successfully!'
