CREATE TABLE [dbo].[JournalChapters]
(
    [ChapterId] INT NOT NULL,
    [JournalId] INT NOT NULL,
    [Title] NVARCHAR(128) NOT NULL,
    [Sort] INT NOT NULL DEFAULT 1,
    [Icon] INT NOT NULL DEFAULT 0,
    [Color] INT NOT NULL DEFAULT 0,
    [Description] NVARCHAR(256) NULL
)
