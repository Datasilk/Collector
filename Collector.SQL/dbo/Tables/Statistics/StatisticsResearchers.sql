CREATE TABLE [dbo].[StatisticsResearchers]
(
	[researcherId] INT NOT NULL PRIMARY KEY, 
    [name] NVARCHAR(100) NULL, 
    [credentials] NVARCHAR(MAX) NULL,
    [bday] DATE NULL
)