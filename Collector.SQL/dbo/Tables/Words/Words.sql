CREATE TABLE [dbo].[Words]
(
	[wordId] INT NOT NULL PRIMARY KEY,
	[word] NVARCHAR(64) NOT NULL, 
	[grammartype] INT NULL, 
	[score] INT NULL
)