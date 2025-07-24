CREATE PROCEDURE [dbo].[Download_Update]
	@qid bigint = 0,
	@status int = 0
AS
	UPDATE DownloadQueue SET status=@status WHERE qid=@qid