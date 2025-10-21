CREATE TABLE [dbo].[JournalModules]
(
    [JournalId] INT NOT NULL, 
    [JournalEntryId] UNIQUEIDENTIFIER NOT NULL, 
    [ModuleId] NVARCHAR(16) NOT NULL, 
    [Sort] INT NOT NULL DEFAULT 1, 
    [Width] FLOAT NOT NULL DEFAULT 1, 
    [Height] FLOAT NOT NULL DEFAULT 1
)
