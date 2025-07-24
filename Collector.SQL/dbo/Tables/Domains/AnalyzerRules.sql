CREATE TABLE [dbo].[AnalyzerRules]
(
	[ruleId] INT NOT NULL PRIMARY KEY, 
	[domainId] INT NOT NULL, 
    [selector] VARCHAR(64) NOT NULL DEFAULT '',
    [rule] bit NOT NULL DEFAULT 0, -- 0 = exclude, 1 = include
    [datecreated] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
)