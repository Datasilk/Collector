CREATE PROCEDURE [dbo].[Article_Remove]
	@articleId int = 0
AS
	DECLARE @domainId int
	SELECT @domainId = domainId FROM Articles WHERE articleId=@articleId
	IF @domainId IS NOT NULL BEGIN
		DELETE FROM ArticleSentences WHERE articleId=@articleId
		DELETE FROM ArticleWords WHERE articleId=@articleId
		DELETE FROM ArticleSubjects WHERE articleId=@articleId
		/* DELETE FROM ArticleStatistics WHERE articleId=@articleId */
		DELETE FROM Articles WHERE articleId=@articleId
		UPDATE Domains SET articles -= 1 WHERE domainId=@domainId
	END
RETURN 0
