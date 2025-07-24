CREATE PROCEDURE [dbo].[ArticleWords_Remove]
	@articleId int = 0,
	@word nvarchar(50) = ''
AS
	IF @word = '' BEGIN
		DELETE FROM ArticleWords WHERE articleId=@articleId
	END ELSE BEGIN
		DECLARE @wordId int = 0
		SELECT @wordId=wordId FROM words WHERE word=@word
		DELETE FROM ArticleWords WHERE articleId=@articleId AND wordId=@wordId
	END
RETURN 0
