CREATE PROCEDURE [dbo].[Article_Clean]
	@articleId int = 0
AS
	EXEC ArticleSubjects_Remove @articleId=@articleId
	EXEC ArticleWords_Remove @articleId=@articleId
RETURN 0
