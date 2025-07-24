CREATE PROCEDURE [dbo].[Domain_DownloadRule_GetDownloads]
	@ruleId int
AS
	DECLARE @domainId int, @url nvarchar(64)
	SELECT @domainId = domainId, @url = [url] FROM DownloadRules WHERE ruleId = @ruleId

	SELECT qid
	FROM DownloadQueue 
	WHERE domainId=@domainId 
	AND (
		[url] LIKE @url
	)
