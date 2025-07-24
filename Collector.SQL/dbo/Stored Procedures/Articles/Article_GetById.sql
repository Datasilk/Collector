CREATE PROCEDURE [dbo].[Article_GetById]
	@articleId int
AS
	SELECT a.*, d.domain FROM Articles a
	LEFT JOIN Domains d ON d.domainId = a.domainId
	WHERE a.articleId=@articleId
RETURN 0
