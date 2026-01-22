CREATE TABLE [dbo].[Journals]
(
	[Id] INT NOT NULL PRIMARY KEY DEFAULT (NEXT VALUE FOR [dbo].[SequenceJournals]),
    [AppUserId] UNIQUEIDENTIFIER NOT NULL, 
    [CategoryId] INT NOT NULL, 
    [Title] NVARCHAR(64) NOT NULL, 
    [Created] DATETIME2 NOT NULL DEFAULT GETUTCDATE(), 
    [Status] INT NOT NULL DEFAULT 1, --1 = active, 0 = archived, 8 = custom modules
    [Color] NVARCHAR(16) NOT NULL, --hex color (no #)
    [EntryId] UNIQUEIDENTIFIER NULL --default entry for the journal
)
