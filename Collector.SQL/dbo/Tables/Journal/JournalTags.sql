CREATE TABLE [dbo].[JournalTags]
(
	[Id] INT NOT NULL PRIMARY KEY DEFAULT (NEXT VALUE FOR [dbo].[SequenceJournalTags]),
	[JournalId] INT NOT NULL, 
    [Tag] NVARCHAR(32) NOT NULL
)
GO
CREATE INDEX [IX_JournalTags_JournalId] ON [dbo].[JournalTags] (JournalId)
GO
CREATE INDEX [IX_JournalTags_JournalAndTag] ON [dbo].[JournalTags] (JournalId, Tag)
