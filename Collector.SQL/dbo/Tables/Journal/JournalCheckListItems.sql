CREATE TABLE [dbo].[JournalCheckListItems]
(
	[Id] INT NOT NULL PRIMARY KEY DEFAULT (NEXT VALUE FOR [dbo].[SequenceJournalCheckListItems]),
    [CheckListId] INT NOT NULL, 
    [Checked] BIT NOT NULL DEFAULT 0, 
    [Title] NVARCHAR(255) NOT NULL,
    [Icon] INT NOT NULL DEFAULT 0, 
    [Created] DATETIME2 NOT NULL DEFAULT GETUTCDATE(), 
    [Status] INT NOT NULL DEFAULT 1, 
    [Sort] INT NOT NULL DEFAULT 1
)
