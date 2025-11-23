CREATE TABLE [dbo].[JournalEntryTags]
(
	[TagId] INT NOT NULL,
	[JournalEntryId] UNIQUEIDENTIFIER NOT NULL,
	CONSTRAINT [PK_JournalEntryTags] PRIMARY KEY ([TagId], [JournalEntryId])
)
GO
CREATE INDEX [IX_JournalEntryTags_JournalEntryId] ON [dbo].[JournalEntryTags] (JournalEntryId)
GO
CREATE INDEX [IX_JournalEntryTags_JournalEntryAndTag] ON [dbo].[JournalEntryTags] (JournalEntryId, TagId)
