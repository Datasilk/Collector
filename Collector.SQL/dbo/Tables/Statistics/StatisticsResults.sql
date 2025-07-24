CREATE TABLE [dbo].[StatisticsResults]
(
	[statId] INT NOT NULL PRIMARY KEY, 
    [projectId] INT NULL, 
    [year] INT NULL, 
    [month] INT NULL, 
    [day] INT NULL,
    [test] FLOAT NULL, 
    [result] FLOAT NULL, 
    [country] NVARCHAR(3) NULL, 
    [city] NVARCHAR(45) NULL, 
    [state] NVARCHAR(5) NULL, 
    [topic] NVARCHAR(50) NULL, 
    [target] NVARCHAR(50) NULL, 
    [sentence] NVARCHAR(250) NULL
)