/*
Script: fix-sequences.sql
Description: Resets all sequences in the database to the max ID value of their related tables + 1
*/

PRINT 'Fixing sequences based on max ID values of related tables...'
PRINT ''

-- AnalyzerRules sequence
DECLARE @maxAnalyzerRuleId BIGINT = 0
SELECT @maxAnalyzerRuleId = ISNULL(MAX(ruleId), 0) FROM [dbo].[AnalyzerRules]
PRINT 'Max AnalyzerRules ID: ' + CAST(@maxAnalyzerRuleId AS NVARCHAR(20))
IF @maxAnalyzerRuleId > 0
BEGIN
    DECLARE @resetAnalyzerRules NVARCHAR(200) = 'ALTER SEQUENCE [dbo].[SequenceAnalyzerRules] RESTART WITH ' + CAST(@maxAnalyzerRuleId + 1 AS NVARCHAR(20))
    EXEC sp_executesql @resetAnalyzerRules
    PRINT 'Reset SequenceAnalyzerRules to ' + CAST(@maxAnalyzerRuleId + 1 AS NVARCHAR(20))
END
ELSE
BEGIN
    PRINT 'No records in AnalyzerRules table, sequence left at default'
END
PRINT ''

-- ArticleBugs sequence
DECLARE @maxArticleBugId BIGINT = 0
SELECT @maxArticleBugId = ISNULL(MAX(bugId), 0) FROM [dbo].[ArticleBugs]
PRINT 'Max ArticleBugs ID: ' + CAST(@maxArticleBugId AS NVARCHAR(20))
IF @maxArticleBugId > 0
BEGIN
    DECLARE @resetArticleBugs NVARCHAR(200) = 'ALTER SEQUENCE [dbo].[SequenceArticleBugs] RESTART WITH ' + CAST(@maxArticleBugId + 1 AS NVARCHAR(20))
    EXEC sp_executesql @resetArticleBugs
    PRINT 'Reset SequenceArticleBugs to ' + CAST(@maxArticleBugId + 1 AS NVARCHAR(20))
END
ELSE
BEGIN
    PRINT 'No records in ArticleBugs table, sequence left at default'
END
PRINT ''

-- Articles sequence
DECLARE @maxArticleId BIGINT = 0
SELECT @maxArticleId = ISNULL(MAX(articleId), 0) FROM [dbo].[Articles]
PRINT 'Max Articles ID: ' + CAST(@maxArticleId AS NVARCHAR(20))
IF @maxArticleId > 0
BEGIN
    DECLARE @resetArticles NVARCHAR(200) = 'ALTER SEQUENCE [dbo].[SequenceArticles] RESTART WITH ' + CAST(@maxArticleId + 1 AS NVARCHAR(20))
    EXEC sp_executesql @resetArticles
    PRINT 'Reset SequenceArticles to ' + CAST(@maxArticleId + 1 AS NVARCHAR(20))
END
ELSE
BEGIN
    PRINT 'No records in Articles table, sequence left at default'
END
PRINT ''

-- DomainCollectionGroups sequence
DECLARE @maxDomainCollectionGroupId BIGINT = 0
SELECT @maxDomainCollectionGroupId = ISNULL(MAX(colgroupId), 0) FROM [dbo].[DomainCollectionGroups]
PRINT 'Max DomainCollectionGroups ID: ' + CAST(@maxDomainCollectionGroupId AS NVARCHAR(20))
IF @maxDomainCollectionGroupId > 0
BEGIN
    DECLARE @resetDomainCollectionGroups NVARCHAR(200) = 'ALTER SEQUENCE [dbo].[SequenceDomainCollectionGroups] RESTART WITH ' + CAST(@maxDomainCollectionGroupId + 1 AS NVARCHAR(20))
    EXEC sp_executesql @resetDomainCollectionGroups
    PRINT 'Reset SequenceDomainCollectionGroups to ' + CAST(@maxDomainCollectionGroupId + 1 AS NVARCHAR(20))
END
ELSE
BEGIN
    PRINT 'No records in DomainCollectionGroups table, sequence left at default'
END
PRINT ''

-- DomainCollections sequence
DECLARE @maxDomainCollectionId BIGINT = 0
SELECT @maxDomainCollectionId = ISNULL(MAX(colId), 0) FROM [dbo].[DomainCollections]
PRINT 'Max DomainCollections ID: ' + CAST(@maxDomainCollectionId AS NVARCHAR(20))
IF @maxDomainCollectionId > 0
BEGIN
    DECLARE @resetDomainCollections NVARCHAR(200) = 'ALTER SEQUENCE [dbo].[SequenceDomainCollections] RESTART WITH ' + CAST(@maxDomainCollectionId + 1 AS NVARCHAR(20))
    EXEC sp_executesql @resetDomainCollections
    PRINT 'Reset SequenceDomainCollections to ' + CAST(@maxDomainCollectionId + 1 AS NVARCHAR(20))
END
ELSE
BEGIN
    PRINT 'No records in DomainCollections table, sequence left at default'
END
PRINT ''

-- DomainTypeMatches sequence
DECLARE @maxDomainTypeMatchId BIGINT = 0
SELECT @maxDomainTypeMatchId = ISNULL(MAX(matchId), 0) FROM [dbo].[DomainTypeMatches]
PRINT 'Max DomainTypeMatches ID: ' + CAST(@maxDomainTypeMatchId AS NVARCHAR(20))
IF @maxDomainTypeMatchId > 0
BEGIN
    DECLARE @resetDomainTypeMatches NVARCHAR(200) = 'ALTER SEQUENCE [dbo].[SequenceDomainTypeMatches] RESTART WITH ' + CAST(@maxDomainTypeMatchId + 1 AS NVARCHAR(20))
    EXEC sp_executesql @resetDomainTypeMatches
    PRINT 'Reset SequenceDomainTypeMatches to ' + CAST(@maxDomainTypeMatchId + 1 AS NVARCHAR(20))
END
ELSE
BEGIN
    PRINT 'No records in DomainTypeMatches table, sequence left at default'
END
PRINT ''

-- Domains sequence
DECLARE @maxDomainId BIGINT = 0
SELECT @maxDomainId = ISNULL(MAX(domainId), 0) FROM [dbo].[Domains]
PRINT 'Max Domains ID: ' + CAST(@maxDomainId AS NVARCHAR(20))
IF @maxDomainId > 0
BEGIN
    DECLARE @resetDomains NVARCHAR(200) = 'ALTER SEQUENCE [dbo].[SequenceDomains] RESTART WITH ' + CAST(@maxDomainId + 1 AS NVARCHAR(20))
    EXEC sp_executesql @resetDomains
    PRINT 'Reset SequenceDomains to ' + CAST(@maxDomainId + 1 AS NVARCHAR(20))
END
ELSE
BEGIN
    PRINT 'No records in Domains table, sequence left at default'
END
PRINT ''

-- DownloadQueue sequence
DECLARE @maxDownloadQueueId BIGINT = 0
SELECT @maxDownloadQueueId = ISNULL(MAX(qid), 0) FROM [dbo].[DownloadQueue]
PRINT 'Max DownloadQueue ID: ' + CAST(@maxDownloadQueueId AS NVARCHAR(20))
IF @maxDownloadQueueId > 0
BEGIN
    DECLARE @resetDownloadQueue NVARCHAR(200) = 'ALTER SEQUENCE [dbo].[SequenceDownloadQueue] RESTART WITH ' + CAST(@maxDownloadQueueId + 1 AS NVARCHAR(20))
    EXEC sp_executesql @resetDownloadQueue
    PRINT 'Reset SequenceDownloadQueue to ' + CAST(@maxDownloadQueueId + 1 AS NVARCHAR(20))
END
ELSE
BEGIN
    PRINT 'No records in DownloadQueue table, sequence left at default'
END
PRINT ''

-- DownloadRules sequence
DECLARE @maxDownloadRuleId BIGINT = 0
SELECT @maxDownloadRuleId = ISNULL(MAX(ruleId), 0) FROM [dbo].[DownloadRules]
PRINT 'Max DownloadRules ID: ' + CAST(@maxDownloadRuleId AS NVARCHAR(20))
IF @maxDownloadRuleId > 0
BEGIN
    DECLARE @resetDownloadRules NVARCHAR(200) = 'ALTER SEQUENCE [dbo].[SequenceDownloadRules] RESTART WITH ' + CAST(@maxDownloadRuleId + 1 AS NVARCHAR(20))
    EXEC sp_executesql @resetDownloadRules
    PRINT 'Reset SequenceDownloadRules to ' + CAST(@maxDownloadRuleId + 1 AS NVARCHAR(20))
END
ELSE
BEGIN
    PRINT 'No records in DownloadRules table, sequence left at default'
END
PRINT ''

-- FeedCategories sequence
DECLARE @maxFeedCategoryId BIGINT = 0
SELECT @maxFeedCategoryId = ISNULL(MAX(categoryId), 0) FROM [dbo].[FeedCategories]
PRINT 'Max FeedCategories ID: ' + CAST(@maxFeedCategoryId AS NVARCHAR(20))
IF @maxFeedCategoryId > 0
BEGIN
    DECLARE @resetFeedCategories NVARCHAR(200) = 'ALTER SEQUENCE [dbo].[SequenceFeedCategories] RESTART WITH ' + CAST(@maxFeedCategoryId + 1 AS NVARCHAR(20))
    EXEC sp_executesql @resetFeedCategories
    PRINT 'Reset SequenceFeedCategories to ' + CAST(@maxFeedCategoryId + 1 AS NVARCHAR(20))
END
ELSE
BEGIN
    PRINT 'No records in FeedCategories table, sequence left at default'
END
PRINT ''

-- Feeds sequence
DECLARE @maxFeedId BIGINT = 0
SELECT @maxFeedId = ISNULL(MAX(feedId), 0) FROM [dbo].[Feeds]
PRINT 'Max Feeds ID: ' + CAST(@maxFeedId AS NVARCHAR(20))
IF @maxFeedId > 0
BEGIN
    DECLARE @resetFeeds NVARCHAR(200) = 'ALTER SEQUENCE [dbo].[SequenceFeeds] RESTART WITH ' + CAST(@maxFeedId + 1 AS NVARCHAR(20))
    EXEC sp_executesql @resetFeeds
    PRINT 'Reset SequenceFeeds to ' + CAST(@maxFeedId + 1 AS NVARCHAR(20))
END
ELSE
BEGIN
    PRINT 'No records in Feeds table, sequence left at default'
END
PRINT ''

-- JournalCheckLists sequence
DECLARE @maxJournalCheckListId BIGINT = 0
SELECT @maxJournalCheckListId = ISNULL(MAX(Id), 0) FROM [dbo].[JournalCheckLists]
PRINT 'Max JournalCheckLists ID: ' + CAST(@maxJournalCheckListId AS NVARCHAR(20))
IF @maxJournalCheckListId > 0
BEGIN
    DECLARE @resetJournalCheckLists NVARCHAR(200) = 'ALTER SEQUENCE [dbo].[SequenceJournalCheckLists] RESTART WITH ' + CAST(@maxJournalCheckListId + 1 AS NVARCHAR(20))
    EXEC sp_executesql @resetJournalCheckLists
    PRINT 'Reset SequenceJournalCheckLists to ' + CAST(@maxJournalCheckListId + 1 AS NVARCHAR(20))
END
ELSE
BEGIN
    PRINT 'No records in JournalCheckLists table, sequence left at default'
END
PRINT ''

-- JournalCheckListItems sequence
DECLARE @maxJournalCheckListItemId BIGINT = 0
SELECT @maxJournalCheckListItemId = ISNULL(MAX(Id), 0) FROM [dbo].[JournalCheckListItems]
PRINT 'Max JournalCheckListItems ID: ' + CAST(@maxJournalCheckListItemId AS NVARCHAR(20))
IF @maxJournalCheckListItemId > 0
BEGIN
    DECLARE @resetJournalCheckListItems NVARCHAR(200) = 'ALTER SEQUENCE [dbo].[SequenceJournalCheckListItems] RESTART WITH ' + CAST(@maxJournalCheckListItemId + 1 AS NVARCHAR(20))
    EXEC sp_executesql @resetJournalCheckListItems
    PRINT 'Reset SequenceJournalCheckListItems to ' + CAST(@maxJournalCheckListItemId + 1 AS NVARCHAR(20))
END
ELSE
BEGIN
    PRINT 'No records in JournalCheckListItems table, sequence left at default'
END
PRINT ''

-- StatisticsProjects sequence
DECLARE @maxStatisticsProjectId BIGINT = 0
SELECT @maxStatisticsProjectId = ISNULL(MAX(projectId), 0) FROM [dbo].[StatisticsProjects]
PRINT 'Max StatisticsProjects ID: ' + CAST(@maxStatisticsProjectId AS NVARCHAR(20))
IF @maxStatisticsProjectId > 0
BEGIN
    DECLARE @resetStatisticsProjects NVARCHAR(200) = 'ALTER SEQUENCE [dbo].[SequenceStatisticsProjects] RESTART WITH ' + CAST(@maxStatisticsProjectId + 1 AS NVARCHAR(20))
    EXEC sp_executesql @resetStatisticsProjects
    PRINT 'Reset SequenceStatisticsProjects to ' + CAST(@maxStatisticsProjectId + 1 AS NVARCHAR(20))
END
ELSE
BEGIN
    PRINT 'No records in StatisticsProjects table, sequence left at default'
END
PRINT ''

-- StatisticsResults sequence
DECLARE @maxStatisticsResultId BIGINT = 0
SELECT @maxStatisticsResultId = ISNULL(MAX(statId), 0) FROM [dbo].[StatisticsResults]
PRINT 'Max StatisticsResults ID: ' + CAST(@maxStatisticsResultId AS NVARCHAR(20))
IF @maxStatisticsResultId > 0
BEGIN
    DECLARE @resetStatisticsResults NVARCHAR(200) = 'ALTER SEQUENCE [dbo].[SequenceStatisticsResults] RESTART WITH ' + CAST(@maxStatisticsResultId + 1 AS NVARCHAR(20))
    EXEC sp_executesql @resetStatisticsResults
    PRINT 'Reset SequenceStatisticsResults to ' + CAST(@maxStatisticsResultId + 1 AS NVARCHAR(20))
END
ELSE
BEGIN
    PRINT 'No records in StatisticsResults table, sequence left at default'
END
PRINT ''

-- Subjects sequence
DECLARE @maxSubjectId BIGINT = 0
SELECT @maxSubjectId = ISNULL(MAX(subjectId), 0) FROM [dbo].[Subjects]
PRINT 'Max Subjects ID: ' + CAST(@maxSubjectId AS NVARCHAR(20))
IF @maxSubjectId > 0
BEGIN
    DECLARE @resetSubjects NVARCHAR(200) = 'ALTER SEQUENCE [dbo].[SequenceSubjects] RESTART WITH ' + CAST(@maxSubjectId + 1 AS NVARCHAR(20))
    EXEC sp_executesql @resetSubjects
    PRINT 'Reset SequenceSubjects to ' + CAST(@maxSubjectId + 1 AS NVARCHAR(20))
END
ELSE
BEGIN
    PRINT 'No records in Subjects table, sequence left at default'
END
PRINT ''

-- Words sequence
DECLARE @maxWordId BIGINT = 0
SELECT @maxWordId = ISNULL(MAX(wordId), 0) FROM [dbo].[Words]
PRINT 'Max Words ID: ' + CAST(@maxWordId AS NVARCHAR(20))
IF @maxWordId > 0
BEGIN
    DECLARE @resetWords NVARCHAR(200) = 'ALTER SEQUENCE [dbo].[SequenceWords] RESTART WITH ' + CAST(@maxWordId + 1 AS NVARCHAR(20))
    EXEC sp_executesql @resetWords
    PRINT 'Reset SequenceWords to ' + CAST(@maxWordId + 1 AS NVARCHAR(20))
END
ELSE
BEGIN
    PRINT 'No records in Words table, sequence left at default'
END
PRINT ''

PRINT 'All sequences have been fixed!'
