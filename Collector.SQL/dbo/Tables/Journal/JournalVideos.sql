CREATE TABLE [dbo].[JournalVideos]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
	[JournalId] INT NOT NULL, 
    [JournalEntryId] UNIQUEIDENTIFIER NOT NULL, 
    [ModuleId] NVARCHAR(16) NOT NULL,
    [Filename] NVARCHAR(64) NOT NULL,
    [OriginalFilename] NVARCHAR(64) NOT NULL DEFAULT '',
    [Duration] INT NOT NULL DEFAULT 0,
    [Width] INT NOT NULL DEFAULT 1, 
    [Height] INT NOT NULL DEFAULT 1, 
    [Created] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [Metadata] NVARCHAR(128) NOT NULL, 
    [Title] NVARCHAR(128) NOT NULL DEFAULT '', 
    [Description] NVARCHAR(50) NULL
)
GO
CREATE INDEX [IX_JournalVideos_ModuleId] ON [dbo].[JournalVideos] (ModuleId)
GO
CREATE INDEX [IX_JournalVideos_EntryAndModuleId] ON [dbo].[JournalVideos] (JournalEntryId, ModuleId)
GO
CREATE INDEX [IX_JournalVideos_JournalEntryAndModuleId] ON [dbo].[JournalVideos] (JournalId, JournalEntryId, ModuleId)
