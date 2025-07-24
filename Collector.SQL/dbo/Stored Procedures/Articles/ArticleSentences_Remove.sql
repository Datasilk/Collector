CREATE PROCEDURE [dbo].[ArticleSentences_Remove]
	@articleId int = 0
AS
	DELETE FROM ArticleSentences WHERE articleId=@articleId
RETURN 0
