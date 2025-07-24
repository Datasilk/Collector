CREATE PROCEDURE [dbo].[ArticleBug_UpdateStatus]
	@bugId int = 0,
	@status int = 0
AS
	UPDATE ArticleBugs SET [status]=@status WHERE bugId=@bugId
