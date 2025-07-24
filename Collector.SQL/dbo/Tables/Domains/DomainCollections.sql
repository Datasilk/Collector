CREATE TABLE [dbo].[DomainCollections]
(
	[colId] INT NOT NULL PRIMARY KEY, 
    [colgroupId] INT NULL, -- collection group ID
    [name] NVARCHAR(32) NOT NULL,
    [search] NVARCHAR(128) NOT NULL DEFAULT '',
    [subjectId] INT NOT NULL DEFAULT 0,
    [filtertype] INT NOT NULL DEFAULT 0,
    [type] INT NOT NULL DEFAULT -1,
    [sort] INT NOT NULL DEFAULT 0,
    [lang] VARCHAR(6) NOT NULL DEFAULT '',
    [datecreated] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
)
GO

CREATE INDEX [IX_Domains_DomainCollectionDates] ON [dbo].[DomainCollections] ([datecreated] DESC)
GO

CREATE INDEX [IX_Domains_DomainCollectionNames] ON [dbo].[DomainCollections] ([name] DESC)
GO