CREATE TABLE [dbo].[JournalVideos]
(
	[Id] INT NOT NULL PRIMARY KEY DEFAULT (NEXT VALUE FOR [dbo].[SequenceJournalVideos]),
	[JournalId] INT NOT NULL, 
    [JournalEntryId] UNIQUEIDENTIFIER NOT NULL, 
    [ModuleId] NVARCHAR(16) NOT NULL,
    [Filename] NVARCHAR(64) NOT NULL,
    [OriginalFilename] NVARCHAR(64) NULL DEFAULT '',
    [Url] NVARCHAR(128) NULL DEFAULT '',
    [Downloaded] BIT NOT NULL DEFAULT 0,
    [Duration] INT NOT NULL DEFAULT 0,
    [Width] INT NOT NULL DEFAULT 1, 
    [Height] INT NOT NULL DEFAULT 1, 
    [Created] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [Metadata] NVARCHAR(128) NULL, 
    [Title] NVARCHAR(128) NULL DEFAULT '', 
    [Description] NVARCHAR(50) NULL
)
GO
CREATE INDEX [IX_JournalVideos_ModuleId] ON [dbo].[JournalVideos] (ModuleId)
GO
CREATE INDEX [IX_JournalVideos_EntryAndModuleId] ON [dbo].[JournalVideos] (JournalEntryId, ModuleId)
GO
CREATE INDEX [IX_JournalVideos_JournalEntryAndModuleId] ON [dbo].[JournalVideos] (JournalId, JournalEntryId, ModuleId)
