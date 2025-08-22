CREATE TABLE [dbo].[Journals]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    [AppUserId] UNIQUEIDENTIFIER NOT NULL, 
    [CategoryId] INT NOT NULL, 
    [Title] NVARCHAR(64) NOT NULL, 
    [Created] DATETIME2 NOT NULL DEFAULT GETUTCDATE(), 
    [Status] INT NOT NULL DEFAULT 1, 
    [ThemeId] INT NULL, 
    [Color] NVARCHAR(16) NOT NULL --hex color (no #)
)
