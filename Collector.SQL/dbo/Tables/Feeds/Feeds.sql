CREATE TABLE [dbo].[Feeds]
(
	[feedId] INT NOT NULL PRIMARY KEY, 
    [domainId] INT NOT NULL DEFAULT 0,
    [doctype] INT NULL, -- 1 = RSS, 2 = HTML
    [categoryId] INT NULL, 
    [title] NVARCHAR(100) NULL, 
	[url] NVARCHAR(100) NULL, 
    [checkIntervals] INT NULL, 
    [lastChecked] DATETIME NULL, 
    [filter] NVARCHAR(MAX) NULL
)