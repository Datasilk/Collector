CREATE TABLE [dbo].[JournalImages]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
	[JournalId] INT NOT NULL, 
    [JournalEntryId] UNIQUEIDENTIFIER NOT NULL, 
    [ModuleId] NVARCHAR(16) NOT NULL,
    [Filename] NVARCHAR(64) NOT NULL,  
    [Width] INT NOT NULL DEFAULT 1, 
    [Height] INT NOT NULL DEFAULT 1
)
GO
CREATE INDEX [IX_JournalImages_ModuleId] ON [dbo].[JournalImages] (ModuleId)
GO
CREATE INDEX [IX_JournalImages_EntryAndModuleId] ON [dbo].[JournalImages] (JournalEntryId, ModuleId)
GO
CREATE INDEX [IX_JournalImages_JournalEntryAndModuleId] ON [dbo].[JournalImages] (JournalId, JournalEntryId, ModuleId)
