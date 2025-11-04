CREATE TABLE [dbo].[JournalCategories]
(
	[Id] INT NOT NULL PRIMARY KEY DEFAULT (NEXT VALUE FOR [dbo].[SequenceJournalCategories]),
    [AppUserId] UNIQUEIDENTIFIER NOT NULL, 
    [Title] NVARCHAR(64) NOT NULL, 
    [Created] DATETIME2 NOT NULL DEFAULT GETUTCDATE(), 
    [Status] INT NOT NULL DEFAULT 1, 
    [Color] NVARCHAR(16) NOT NULL --hex color (no #)
)