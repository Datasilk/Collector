CREATE TABLE [dbo].[JournalFiles]
(
	[Id] INT NOT NULL PRIMARY KEY DEFAULT (NEXT VALUE FOR [dbo].[SequenceJournalFiles]),
	[JournalId] INT NOT NULL, 
    [JournalEntryId] UNIQUEIDENTIFIER NOT NULL, 
    [ModuleId] NVARCHAR(16) NOT NULL,
    [Filename] NVARCHAR(256) NOT NULL,  
    [FileSize] BIGINT NOT NULL DEFAULT 0,
    [DateUploaded] DATETIME NOT NULL DEFAULT GETDATE()
)
GO
CREATE INDEX [IX_JournalFiles_ModuleId] ON [dbo].[JournalFiles] (ModuleId)
GO
CREATE INDEX [IX_JournalFiles_EntryAndModuleId] ON [dbo].[JournalFiles] (JournalEntryId, ModuleId)
GO
CREATE INDEX [IX_JournalFiles_JournalEntryAndModuleId] ON [dbo].[JournalFiles] (JournalId, JournalEntryId, ModuleId)
