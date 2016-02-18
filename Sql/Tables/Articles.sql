﻿CREATE TABLE [dbo].[Articles]
(
	[articleId] INT NOT NULL PRIMARY KEY, 
    [feedId] INT NULL, 
	[subjects] TINYINT NULL,
    [images] TINYINT NULL, 
	[filesize] FLOAT DEFAULT 0,
    [wordcount] INT DEFAULT 0, 
    [sentencecount] SMALLINT DEFAULT 0, 
    [paragraphcount] SMALLINT DEFAULT 0,
    [importantcount] SMALLINT DEFAULT 0, 
	[analyzecount] SMALLINT DEFAULT 0,
    [yearstart] SMALLINT NULL, 
    [yearend] SMALLINT NULL, 
	[years] NVARCHAR(50),
    [datecreated] DATETIME NULL, 
    [datepublished] DATETIME NULL, 
    [relavance] SMALLINT NULL, 
    [importance] SMALLINT NULL, 
    [fiction] SMALLINT NULL, 
    [domain] NVARCHAR(50) NULL, 
    [url] NVARCHAR(250) NULL, 
    [title] NVARCHAR(250) NULL, 
    [summary] NVARCHAR(250) NULL,
	[analyzed] FLOAT DEFAULT 0,
	[cached] BIT NULL DEFAULT 0, 
    [active] BIT NULL DEFAULT 0, 
    [deleted] BIT NULL DEFAULT 0
)
