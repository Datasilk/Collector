CREATE TABLE [dbo].[Domains]
(
	[domainId] INT NOT NULL PRIMARY KEY, 
    [domain] NVARCHAR(64) NOT NULL,
    [lang] VARCHAR(6) NOT NULL DEFAULT 'en',
    [parentId] INT NOT NULL DEFAULT 0,
    [hastitle] BIT NOT NULL DEFAULT 0,
    [paywall] BIT NOT NULL DEFAULT 0,
    [free] BIT NOT NULL DEFAULT 0,
    [https] BIT NOT NULL DEFAULT 0,
    [www] BIT NOT NULL DEFAULT 0,
    [empty] BIT NOT NULL DEFAULT 0,
    [deleted] BIT NOT NULL DEFAULT 0,
    [type] INT NOT NULL DEFAULT -1,
    [type2] INT NOT NULL DEFAULT -1, 
    [articles] INT NOT NULL DEFAULT 0,
    [inqueue] INT NOT NULL DEFAULT 0,
    [title] NVARCHAR(128) NOT NULL DEFAULT '',
    [description] NVARCHAR(255) NOT NULL DEFAULT '',
    [datecreated] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [dateupdated] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [lastchecked] DATETIME2 NOT NULL DEFAULT GETUTCDATE() -- last scraped a URL from the domain
)