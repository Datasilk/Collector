DELETE FROM ArticleSubjects
DELETE FROM ArticleSentences
DELETE FROM ArticleWords
DELETE FROM ArticleBugs
DELETE FROM ArticleDates
DELETE FROM Articles
--DELETE FROM Domains
DELETE FROM DownloadQueue
DELETE FROM FeedsCheckedLog
--DELETE FROM Feeds
UPDATE Feeds SET lastChecked = DATEADD(HOUR, -24, GETUTCDATE())

ALTER SEQUENCE SequenceArticleBugs RESTART WITH 1  
ALTER SEQUENCE SequenceArticles RESTART WITH 1  
ALTER SEQUENCE SequenceDownloadQueue RESTART WITH 1 

SELECT * FROM DownloadQueue
SELECT * FROM Domains
SELECT * FROM Feeds
SELECT * FROM FeedsCheckedLog
SELECT * FROM Blacklist_Domains
SELECT * FROM Whitelist_Domains