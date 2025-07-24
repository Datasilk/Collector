CREATE TABLE [dbo].[DownloadRules]
(
	[ruleId] INT NOT NULL PRIMARY KEY, 
	[domainId] INT NOT NULL, 
    [rule] bit NOT NULL DEFAULT 0, -- 0 = don't bother downloading at all, 1 = scrape links only and is not an article
    [url] VARCHAR(64) NOT NULL DEFAULT '',
    [title] VARCHAR(64) NOT NULL DEFAULT '',
    [summary] VARCHAR(64) NOT NULL DEFAULT '',
    [datecreated] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
)