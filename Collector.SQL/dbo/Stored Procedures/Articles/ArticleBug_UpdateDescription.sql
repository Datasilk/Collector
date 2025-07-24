CREATE PROCEDURE [dbo].[ArticleBug_UpdateDescription]
	@bugId int = 0,
	@description nvarchar(MAX) = ''
AS
	UPDATE ArticleBugs SET description=@description WHERE bugId=@bugId
