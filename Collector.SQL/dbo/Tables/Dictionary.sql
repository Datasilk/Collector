CREATE TABLE [dbo].[Dictionary]
(
	[word] NVARCHAR(25) NOT NULL PRIMARY KEY, 
    [vocabtype] TINYINT NULL,  
    [grammertype] TINYINT NULL,
    [socialtype] TINYINT NULL, 
    [objecttype] TINYINT NULL, 
    [score] TINYINT NULL
)