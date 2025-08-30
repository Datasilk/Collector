/*
Script: copy-database.sql
Description: Copies all data from a source database to a target database with identical schema
           (excluding authentication tables, user-related tables, and journal tables)
Usage: 
  1. Replace 'SourceDB' with your source database name
  2. Replace 'TargetDB' with your target database name
  3. Execute the script in SQL Server Management Studio or similar tool
*/

-- Set the source and target database names
DECLARE @SourceDB NVARCHAR(128) = 'Collector-Old'
DECLARE @TargetDB NVARCHAR(128) = 'Collector'

-- Disable constraints in target database
PRINT 'Disabling constraints...'
-- We'll disable constraints for specific tables instead of using sp_msforeachtable

-- Disable foreign key constraints for Articles tables
ALTER TABLE [Collector].[dbo].[ArticleSubjects] NOCHECK CONSTRAINT ALL
ALTER TABLE [Collector].[dbo].[ArticleDates] NOCHECK CONSTRAINT ALL
ALTER TABLE [Collector].[dbo].[ArticleSentences] NOCHECK CONSTRAINT ALL
ALTER TABLE [Collector].[dbo].[ArticleWords] NOCHECK CONSTRAINT ALL
ALTER TABLE [Collector].[dbo].[ArticleBugs] NOCHECK CONSTRAINT ALL

-- Disable foreign key constraints for Domains tables
ALTER TABLE [Collector].[dbo].[DomainAddresses] NOCHECK CONSTRAINT ALL
ALTER TABLE [Collector].[dbo].[DomainHierarchy] NOCHECK CONSTRAINT ALL
ALTER TABLE [Collector].[dbo].[DomainLinks] NOCHECK CONSTRAINT ALL
ALTER TABLE [Collector].[dbo].[DomainCollectionGroups] NOCHECK CONSTRAINT ALL
ALTER TABLE [Collector].[dbo].[DomainTypeMatches] NOCHECK CONSTRAINT ALL

-- Disable foreign key constraints for Downloads tables
ALTER TABLE [Collector].[dbo].[Downloads] NOCHECK CONSTRAINT ALL
ALTER TABLE [Collector].[dbo].[DownloadQueue] NOCHECK CONSTRAINT ALL

-- Disable foreign key constraints for Feeds tables
ALTER TABLE [Collector].[dbo].[Feeds] NOCHECK CONSTRAINT ALL
ALTER TABLE [Collector].[dbo].[FeedsCheckedLog] NOCHECK CONSTRAINT ALL

-- Disable foreign key constraints for Words tables
ALTER TABLE [Collector].[dbo].[SubjectWords] NOCHECK CONSTRAINT ALL

-- Disable foreign key constraints for Statistics tables
ALTER TABLE [Collector].[dbo].[StatisticsProjects] NOCHECK CONSTRAINT ALL

-- Reset sequences to start with 1
PRINT 'Resetting sequences...'

-- Reset sequence for Articles
ALTER SEQUENCE [dbo].[SequenceArticles] RESTART WITH 1

-- Reset sequence for Domains
ALTER SEQUENCE [dbo].[SequenceDomains] RESTART WITH 1

-- Reset sequence for DomainCollections
ALTER SEQUENCE [dbo].[SequenceDomainCollections] RESTART WITH 1

-- Reset sequence for DomainCollectionGroups
ALTER SEQUENCE [dbo].[SequenceDomainCollectionGroups] RESTART WITH 1

-- Reset sequence for DomainTypeMatches
ALTER SEQUENCE [dbo].[SequenceDomainTypeMatches] RESTART WITH 1

-- Reset sequence for AnalyzerRules
ALTER SEQUENCE [dbo].[SequenceAnalyzerRules] RESTART WITH 1

-- Reset sequence for DownloadRules
ALTER SEQUENCE [dbo].[SequenceDownloadRules] RESTART WITH 1

-- Reset sequence for DownloadQueue
ALTER SEQUENCE [dbo].[SequenceDownloadQueue] RESTART WITH 1

-- Reset sequence for Feeds
ALTER SEQUENCE [dbo].[SequenceFeeds] RESTART WITH 1

-- Reset sequence for FeedCategories
ALTER SEQUENCE [dbo].[SequenceFeedCategories] RESTART WITH 1

-- Reset sequence for Words
ALTER SEQUENCE [dbo].[SequenceWords] RESTART WITH 1

-- Reset sequence for Subjects
ALTER SEQUENCE [dbo].[SequenceSubjects] RESTART WITH 1

-- Reset sequence for StatisticsProjects
ALTER SEQUENCE [dbo].[SequenceStatisticsProjects] RESTART WITH 1

-- Reset sequence for StatisticsResults
ALTER SEQUENCE [dbo].[SequenceStatisticsResults] RESTART WITH 1

-- Reset sequence for ArticleBugs
ALTER SEQUENCE [dbo].[SequenceArticleBugs] RESTART WITH 1

-- Copy data from source to target database in the correct order based on dependencies

-- 1. First copy tables with no foreign key dependencies

-- Domains
PRINT 'Copying Domains...'
DELETE FROM [Collector].[dbo].[Domains]
INSERT INTO [Collector].[dbo].[Domains]
SELECT * FROM [Collector-Old].[dbo].[Domains]
ORDER BY [domainId]

-- Subjects
PRINT 'Copying Subjects...'
DELETE FROM [Collector].[dbo].[Subjects]
INSERT INTO [Collector].[dbo].[Subjects]
SELECT * FROM [Collector-Old].[dbo].[Subjects]

-- 2. Then copy tables with foreign key dependencies

-- Articles (depends on Domains)
PRINT 'Copying Articles...'
DELETE FROM [Collector].[dbo].[Articles]
INSERT INTO [Collector].[dbo].[Articles]
SELECT * FROM [Collector-Old].[dbo].[Articles]
ORDER BY [articleId]

-- ArticleSubjects (depends on Articles and Subjects)
PRINT 'Copying ArticleSubjects...'
DELETE FROM [Collector].[dbo].[ArticleSubjects]
INSERT INTO [Collector].[dbo].[ArticleSubjects]
SELECT * FROM [Collector-Old].[dbo].[ArticleSubjects]
ORDER BY [articleId], [subjectId]

-- ArticleDates (depends on Articles)
PRINT 'Copying ArticleDates...'
DELETE FROM [Collector].[dbo].[ArticleDates]
INSERT INTO [Collector].[dbo].[ArticleDates]
SELECT * FROM [Collector-Old].[dbo].[ArticleDates]
ORDER BY [articleId]

-- ArticleSentences (depends on Articles)
PRINT 'Copying ArticleSentences...'
DELETE FROM [Collector].[dbo].[ArticleSentences]
INSERT INTO [Collector].[dbo].[ArticleSentences]
SELECT * FROM [Collector-Old].[dbo].[ArticleSentences]
ORDER BY [articleId]

-- ArticleWords (depends on Articles)
PRINT 'Copying ArticleWords...'
DELETE FROM [Collector].[dbo].[ArticleWords]
INSERT INTO [Collector].[dbo].[ArticleWords]
SELECT * FROM [Collector-Old].[dbo].[ArticleWords]
ORDER BY [articleId]

-- ArticleBugs (depends on Articles)
PRINT 'Copying ArticleBugs...'
DELETE FROM [Collector].[dbo].[ArticleBugs]
INSERT INTO [Collector].[dbo].[ArticleBugs]
SELECT * FROM [Collector-Old].[dbo].[ArticleBugs]
ORDER BY [articleId]

-- DomainAddresses (depends on Domains)
PRINT 'Copying DomainAddresses...'
DELETE FROM [Collector].[dbo].[DomainAddresses]
INSERT INTO [Collector].[dbo].[DomainAddresses]
SELECT * FROM [Collector-Old].[dbo].[DomainAddresses]

-- DomainHierarchy (depends on Domains)
PRINT 'Copying DomainHierarchy...'
DELETE FROM [Collector].[dbo].[DomainHierarchy]
INSERT INTO [Collector].[dbo].[DomainHierarchy]
SELECT * FROM [Collector-Old].[dbo].[DomainHierarchy]
ORDER BY [domainId]

-- DomainLinks (depends on Domains)
PRINT 'Copying DomainLinks...'
DELETE FROM [Collector].[dbo].[DomainLinks]
INSERT INTO [Collector].[dbo].[DomainLinks]
SELECT * FROM [Collector-Old].[dbo].[DomainLinks]
ORDER BY [domainId]

-- DomainCollections
PRINT 'Copying DomainCollections...'
DELETE FROM [Collector].[dbo].[DomainCollections]
INSERT INTO [Collector].[dbo].[DomainCollections]
SELECT * FROM [Collector-Old].[dbo].[DomainCollections]

-- DomainCollectionGroups (depends on DomainCollections)
PRINT 'Copying DomainCollectionGroups...'
DELETE FROM [Collector].[dbo].[DomainCollectionGroups]
INSERT INTO [Collector].[dbo].[DomainCollectionGroups]
SELECT * FROM [Collector-Old].[dbo].[DomainCollectionGroups]

-- DomainTypeMatches (depends on Domains)
PRINT 'Copying DomainTypeMatches...'
DELETE FROM [Collector].[dbo].[DomainTypeMatches]
INSERT INTO [Collector].[dbo].[DomainTypeMatches]
SELECT * FROM [Collector-Old].[dbo].[DomainTypeMatches]

-- AnalyzerRules
PRINT 'Copying AnalyzerRules...'
DELETE FROM [Collector].[dbo].[AnalyzerRules]
INSERT INTO [Collector].[dbo].[AnalyzerRules]
SELECT * FROM [Collector-Old].[dbo].[AnalyzerRules]

-- DownloadRules
PRINT 'Copying DownloadRules...'
DELETE FROM [Collector].[dbo].[DownloadRules]
INSERT INTO [Collector].[dbo].[DownloadRules]
SELECT * FROM [Collector-Old].[dbo].[DownloadRules]

-- Blacklist_Domains
PRINT 'Copying Blacklist_Domains...'
DELETE FROM [Collector].[dbo].[Blacklist_Domains]
INSERT INTO [Collector].[dbo].[Blacklist_Domains]
SELECT * FROM [Collector-Old].[dbo].[Blacklist_Domains]
ORDER BY [domain]

-- Blacklist_Wildcards
PRINT 'Copying Blacklist_Wildcards...'
DELETE FROM [Collector].[dbo].[Blacklist_Wildcards]
INSERT INTO [Collector].[dbo].[Blacklist_Wildcards]
SELECT * FROM [Collector-Old].[dbo].[Blacklist_Wildcards]
ORDER BY [domain]

-- Whitelist_Domains
PRINT 'Copying Whitelist_Domains...'
DELETE FROM [Collector].[dbo].[Whitelist_Domains]
INSERT INTO [Collector].[dbo].[Whitelist_Domains]
SELECT * FROM [Collector-Old].[dbo].[Whitelist_Domains]
ORDER BY [domain]

-- Dictionary
PRINT 'Copying Dictionary...'
DELETE FROM [Collector].[dbo].[Dictionary]
INSERT INTO [Collector].[dbo].[Dictionary]
SELECT * FROM [Collector-Old].[dbo].[Dictionary]

-- Downloads
PRINT 'Copying Downloads...'
DELETE FROM [Collector].[dbo].[Downloads]
INSERT INTO [Collector].[dbo].[Downloads]
SELECT * FROM [Collector-Old].[dbo].[Downloads]
ORDER BY [id]

-- DownloadQueue
PRINT 'Copying DownloadQueue...'
DELETE FROM [Collector].[dbo].[DownloadQueue]
INSERT INTO [Collector].[dbo].[DownloadQueue]
SELECT * FROM [Collector-Old].[dbo].[DownloadQueue]
ORDER BY [qid]

-- FeedCategories
PRINT 'Copying FeedCategories...'
DELETE FROM [Collector].[dbo].[FeedCategories]
INSERT INTO [Collector].[dbo].[FeedCategories]
SELECT * FROM [Collector-Old].[dbo].[FeedCategories]
ORDER BY [categoryId]

-- Feeds
PRINT 'Copying Feeds...'
DELETE FROM [Collector].[dbo].[Feeds]
INSERT INTO [Collector].[dbo].[Feeds]
SELECT * FROM [Collector-Old].[dbo].[Feeds]
ORDER BY [feedId]

-- FeedsCheckedLog
PRINT 'Copying FeedsCheckedLog...'
DELETE FROM [Collector].[dbo].[FeedsCheckedLog]
INSERT INTO [Collector].[dbo].[FeedsCheckedLog]
SELECT * FROM [Collector-Old].[dbo].[FeedsCheckedLog]
ORDER BY [feedId]

-- Words
PRINT 'Copying Words...'
DELETE FROM [Collector].[dbo].[Words]
INSERT INTO [Collector].[dbo].[Words]
SELECT * FROM [Collector-Old].[dbo].[Words]
ORDER BY [wordId]

-- CommonWords
PRINT 'Copying CommonWords...'
DELETE FROM [Collector].[dbo].[CommonWords]
INSERT INTO [Collector].[dbo].[CommonWords]
SELECT * FROM [Collector-Old].[dbo].[CommonWords]
ORDER BY [word]

-- SubjectWords
PRINT 'Copying SubjectWords...'
DELETE FROM [Collector].[dbo].[SubjectWords]
INSERT INTO [Collector].[dbo].[SubjectWords]
SELECT * FROM [Collector-Old].[dbo].[SubjectWords]
ORDER BY [wordId]

-- StatisticsProjects
PRINT 'Copying StatisticsProjects...'
DELETE FROM [Collector].[dbo].[StatisticsProjects]
INSERT INTO [Collector].[dbo].[StatisticsProjects]
SELECT * FROM [Collector-Old].[dbo].[StatisticsProjects]
ORDER BY [projectId]

-- StatisticsResearchers
PRINT 'Copying StatisticsResearchers...'
DELETE FROM [Collector].[dbo].[StatisticsResearchers]
INSERT INTO [Collector].[dbo].[StatisticsResearchers]
SELECT * FROM [Collector-Old].[dbo].[StatisticsResearchers]
ORDER BY [researcherId]

-- StatisticsProjectResearchers
PRINT 'Copying StatisticsProjectResearchers...'
DELETE FROM [Collector].[dbo].[StatisticsProjectResearchers]
INSERT INTO [Collector].[dbo].[StatisticsProjectResearchers]
SELECT * FROM [Collector-Old].[dbo].[StatisticsProjectResearchers]
ORDER BY [projectId]

-- StatisticsResults
PRINT 'Copying StatisticsResults...'
DELETE FROM [Collector].[dbo].[StatisticsResults]
INSERT INTO [Collector].[dbo].[StatisticsResults]
SELECT * FROM [Collector-Old].[dbo].[StatisticsResults]
ORDER BY [statId]

-- None of these tables have identity columns, so no need for IDENTITY_INSERT

-- Enable constraints in target database
PRINT 'Enabling constraints...'

-- Enable foreign key constraints for Articles tables
ALTER TABLE [Collector].[dbo].[ArticleSubjects] WITH CHECK CHECK CONSTRAINT ALL
ALTER TABLE [Collector].[dbo].[ArticleDates] WITH CHECK CHECK CONSTRAINT ALL
ALTER TABLE [Collector].[dbo].[ArticleSentences] WITH CHECK CHECK CONSTRAINT ALL
ALTER TABLE [Collector].[dbo].[ArticleWords] WITH CHECK CHECK CONSTRAINT ALL
ALTER TABLE [Collector].[dbo].[ArticleBugs] WITH CHECK CHECK CONSTRAINT ALL

-- Enable foreign key constraints for Domains tables
ALTER TABLE [Collector].[dbo].[DomainAddresses] WITH CHECK CHECK CONSTRAINT ALL
ALTER TABLE [Collector].[dbo].[DomainHierarchy] WITH CHECK CHECK CONSTRAINT ALL
ALTER TABLE [Collector].[dbo].[DomainLinks] WITH CHECK CHECK CONSTRAINT ALL
ALTER TABLE [Collector].[dbo].[DomainCollectionGroups] WITH CHECK CHECK CONSTRAINT ALL
ALTER TABLE [Collector].[dbo].[DomainTypeMatches] WITH CHECK CHECK CONSTRAINT ALL

-- Enable foreign key constraints for Downloads tables
ALTER TABLE [Collector].[dbo].[Downloads] WITH CHECK CHECK CONSTRAINT ALL
ALTER TABLE [Collector].[dbo].[DownloadQueue] WITH CHECK CHECK CONSTRAINT ALL

-- Enable foreign key constraints for Feeds tables
ALTER TABLE [Collector].[dbo].[Feeds] WITH CHECK CHECK CONSTRAINT ALL
ALTER TABLE [Collector].[dbo].[FeedsCheckedLog] WITH CHECK CHECK CONSTRAINT ALL

-- Enable foreign key constraints for Words tables
ALTER TABLE [Collector].[dbo].[SubjectWords] WITH CHECK CHECK CONSTRAINT ALL

-- Enable foreign key constraints for Statistics tables
ALTER TABLE [Collector].[dbo].[StatisticsProjectResearchers] WITH CHECK CHECK CONSTRAINT ALL
ALTER TABLE [Collector].[dbo].[StatisticsResults] WITH CHECK CHECK CONSTRAINT ALL

PRINT 'Database copy completed successfully!'
