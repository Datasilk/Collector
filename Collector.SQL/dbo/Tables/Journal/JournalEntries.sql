CREATE TABLE [dbo].[JournalEntries]
(
	[Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    [JournalId] INT NOT NULL, 
    [ChapterId] INT NULL, 
    [Title] NVARCHAR(128) NOT NULL, 
    [Description] NVARCHAR(512) NOT NULL, 
    [Created] DATETIME2 NOT NULL DEFAULT GETUTCDATE(), 
    [Modified] DATETIME2 NOT NULL DEFAULT GETUTCDATE(), 
    [Status] INT NOT NULL DEFAULT 1, --0=deleted, 1=active, 2=published, 3=archived
    [Encrypted] BIT NOT NULL DEFAULT 0, --if encrypted, cannot be published
    [Thumbnail] NVARCHAR(128) NULL --image to use as thumbnail
)
