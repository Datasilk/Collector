CREATE TABLE [dbo].[StatisticsProjects]
(
	[projectId] INT NOT NULL PRIMARY KEY, 
    [title] NVARCHAR(100) NULL, 
    [url] NVARCHAR(100) NULL, 
    [summary] NVARCHAR(250) NULL, 
    [publishdate] DATETIME NULL 
)