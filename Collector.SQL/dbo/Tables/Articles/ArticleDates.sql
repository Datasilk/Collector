CREATE TABLE [dbo].[ArticleDates]
(
	[articleId] INT NOT NULL PRIMARY KEY, 
    [date] DATE NULL, 
    [hasyear] BIT NULL, 
    [hasmonth] BIT NULL, 
    [hasday] BIT NULL
)