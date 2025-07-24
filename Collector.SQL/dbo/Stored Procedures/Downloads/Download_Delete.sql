CREATE PROCEDURE [dbo].[Download_Delete]
	@qid bigint = 0
AS
	DECLARE @url nvarchar(250), @domainId int
	SELECT @url = [url], @domainId=domainId FROM DownloadQueue WHERE qid=@qid
	
	--delete the article associated with download
	DELETE FROM Articles WHERE [url] = (SELECT [url] FROM DownloadQueue WHERE qid=@qid)

	DELETE FROM DownloadQueue WHERE qid=@qid
	DELETE FROM Downloads WHERE id=@qid

	UPDATE Domains SET inqueue-=1 WHERE domainId=@domainId
	