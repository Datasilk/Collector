CREATE TABLE [dbo].[ConverseResources]
(
	[ConverseId] INT NOT NULL PRIMARY KEY,
	[AppUserId] [UNIQUEIDENTIFIER] NOT NULL,
	[SubjectId] INT NOT NULL
    CONSTRAINT [AK_ConverseResources_User] UNIQUE ([AppUserId], [ConverseId]),
)
