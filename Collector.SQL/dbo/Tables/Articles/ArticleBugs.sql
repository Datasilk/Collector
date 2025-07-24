CREATE TABLE [dbo].[ArticleBugs]
(
	[bugId] INT NOT NULL PRIMARY KEY, 
    [articleId] INT NULL, 
    [title] NVARCHAR(100) NULL, 
    [description] NVARCHAR(MAX) NULL, 
    [datecreated] DATETIME NULL, 
    [status] TINYINT NULL
)
