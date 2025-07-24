CREATE PROCEDURE [dbo].[Domain_DownloadRule_GetArticles]
	@ruleId int
AS
	DECLARE @domainId int, @url nvarchar(64), @title nvarchar(64), @summary nvarchar(64)
	SELECT @domainId = domainId, @url = [url], @title = [title], @summary = [summary] FROM DownloadRules WHERE ruleId = @ruleId

	SELECT articleId
	FROM Articles 
	WHERE domainId=@domainId 
	AND (
		[url] LIKE @url
		OR (LEN(@title) > 0 AND title LIKE @title)
		OR (LEN(@summary) > 0 AND summary LIKE @summary)
	)
