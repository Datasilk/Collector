CREATE TABLE [dbo].[ConverseSubjects]
(
	[ConverseId] INT NOT NULL PRIMARY KEY,
	[AppUserId] [UNIQUEIDENTIFIER] NOT NULL,
	[SubjectId] INT NOT NULL
    CONSTRAINT [AK_ConverseSubjects_User_Subject] UNIQUE ([AppUserId], [SubjectId], [ConverseId]),
)
