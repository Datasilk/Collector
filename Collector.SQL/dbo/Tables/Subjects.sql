CREATE TABLE [dbo].[Subjects]
(
	[subjectId] INT NOT NULL PRIMARY KEY, 
    [parentId] INT NULL DEFAULT 0, 
    [grammartype] INT NULL DEFAULT 0, 
    [score] INT NULL DEFAULT 0, 
    [haswords] BIT NULL DEFAULT 0, 
    [title] NVARCHAR(50) NULL DEFAULT '', 
    [hierarchy] NVARCHAR(50) NULL DEFAULT '', 
    [breadcrumb] NVARCHAR(500) NULL DEFAULT ''
)